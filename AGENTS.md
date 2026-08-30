## Approach
- Read existing files before writing. Don't re-read unless changed.
- Thorough in reasoning, concise in output.
- Skip files over 100KB unless required.
- No sycophantic openers or closing fluff.
- No emojis or em-dashes.
- Do not guess APIs, versions, flags, commit SHAs, or package names. Verify by reading code or docs before asserting.

## Project
Desktop app that tracks Cursor quota allowances across a billing cycle. Users sign in with a Cursor account; an embedded native WebView session fetches `https://cursor.com/api/usage-summary` (dashboard endpoint, not a documented public personal-plan API). The billing cycle and usage samples come from Cursor. The calendar and chart show two independent percentages: Cursor models vs other models.

Stack: C# / .NET 10 / Avalonia 12. Namespace `CursorPace`. Target `net10.0`.

Treat source and `.csproj` as truth. When you change user-visible behavior, update `README.md` and `QUICKSTART.md` in the same session unless the user says docs are out of scope. Do not invent features in docs.

## Layout
Flat repo. App project: `CursorPace.csproj`. Tests: `Tests/CursorPace.Tests.csproj` (xUnit). Solution file: `CursorPace.slnx`.

| Path | Role |
|---|---|
| `Models/` | Data types only. No I/O, no UI. |
| `Services/` | Interfaces plus implementations. Business logic and OS integration. |
| `ViewModels/` | MVVM binding, commands, `INotifyPropertyChanged`. |
| `Views/` | Avalonia AXAML/code-behind. Lifecycle, dialogs, window chrome. |
| `Converters/` | AXAML value converters. |
| `Assets/` | Icon and tray image. |
| `Tests/` | Unit tests. App csproj excludes this folder. |
| `dev/` | Maintainer files: `CHANGELOG.md`, `DEVELOPMENT.md`, release-notes prompt. |
| `packaging/` | Linux packaging inputs: `.desktop` file, AppStream `.appdata.xml`. Consumed by `scripts/build-appimage.sh`. |
| `scripts/` | Maintainer scripts: PowerShell (`.ps1`) and bash (`.sh`) for `dev`, `build`, `clean`, `release`. |
| `Program.cs` | Avalonia entry: `BuildAvaloniaApp()`. |
| `App.axaml.cs` | Process entry: single-instance, DI wiring, tray, `--background`. |

Root `UnitTest1.cs` is excluded from the app project. Do not revive it. Put new tests under `Tests/`.

## Architecture
Construct services in `App.OnFrameworkInitializationCompleted` and pass them in. Do not add a DI container unless asked.

- Time: `IClock` / `SystemClock`. Never call `DateTime.Now`/`Today` from calculator or view models.
- Cycle math: `ICycleCalculator` / `CycleCalculator`. Keep it pure (no file I/O, no UI).
- Persistence: `IPlanStore` / `JsonPlanStore`. Path `%LocalAppData%\CursorPace\settings.json` (or the OS LocalApplicationData equivalent). `Load()` must never let a transient I/O read failure (locked file, permission error) overwrite the on-disk file. Only a JSON parse failure counts as corruption; back up and reset in that case only.
- Usage samples: `IUsageSampleStore` / `JsonUsageSampleStore`. Path `%LocalAppData%\CursorPace\usage-samples.json`. Same load exception contract as `IPlanStore`.
- Backup: `IDataBackupService` / `DataBackupService`. One zip of `settings.json` and `usage-samples.json`. Does not include the WebView profile.
- Cursor usage: `ICursorUsageClient` / `NativeWebViewCursorUsageClient`. Profile folder is `WebViewProfilePaths.ProfileDirectory`: Windows uses `%LocalAppData%\CursorPace\WebView2`; Linux/macOS use `WebView` under LocalApplicationData, except a Linux AppImage run (detected via the `APPIMAGE` env var), which uses `WebView-AppImage` so its bundled WebKitGTK never shares a cookie database with a system-WebKitGTK dev build. Keep the `Avalonia.Controls.WebView` package version aligned with `Avalonia` / `Avalonia.Desktop` when a matching WebView package exists.
- Sync: `IUsageSyncService` / `UsageSyncService`. Clock-aligned auto refresh; `SyncSchedule` decides launch skip. Takes `IUiDispatcher`, not a platform dispatcher. Services that raise events consumed by view models (`StateChanged` / `SnapshotReceived`, WebView navigation callbacks) must marshal through `IUiDispatcher` before invoking; never assume a continuation after `await` resumes on the UI thread.
- Startup: `IStartupRegistration` via `StartupRegistration.Create()` (Windows Run key, macOS Launch Agent, Linux XDG autostart).
- Tray: `ITrayService` / `TrayService` (Avalonia `TrayIcon`). Lives for the whole process.
- UI state: `MainViewModel` plus calendar/day row VMs and a read-only `UsageChartViewModel`. Views subscribe to VM events; they do not own cycle math. The main window can show the calendar, the usage chart, or Settings. Settings is a page in the main window (`Back` and a `Settings` heading below the title bar), not a second window.

Percentages use `decimal` in models and calculator. UI may round to integers for display. Do not switch storage or interpolation to `double`.

`QuotaKind` is a two-value enum. When switching on it, handle both cases and keep a `never` default so a new kind fails at compile time.

## Cycle contract
A cycle exists only from a signed-in snapshot via `GenerateCycleFromBounds` with Cursor `billingCycleStart` / `billingCycleEnd` (timed instants, not necessarily midnight). There is no manual renewal-day path and no day pinning.

Calendar rows are every local date that intersects `[CycleStart, NextRenewal)`. `D` is that count. Day 1 midnight is clamped to `CycleStart` when the cycle starts later that day. When `NextRenewal` is after midnight, the renewal calendar date is a normal data row; `ExpectedPercent` still evaluates at that day's midnight, not at the renewal instant. Renewal remains the 100% anchor at the `NextRenewal` instant.

Chart and interpolation use elapsed seconds from `CycleStart` (`CycleCalculator.AxisSeconds` / `CycleSeconds`, ticks over `TicksPerSecond`). The plot domain is exactly `[CycleStart, NextRenewal]` (`X = 0` .. `CycleSeconds`). Midnight is a grid marker, not a unit.

`ExpectedPercentAt` is linear interpolation along `(0, 0%)`, every in-cycle sample at its timestamp, and `(CycleSeconds, 100%)`. Days before the first sample interpolate toward that sample. After the last sample the line paces remaining quota to 100% at `NextRenewal`. `ExpectedPercent(dayNumber)` evaluates that curve at the day's midnight (clamped to `CycleStart`). Editing one kind never existed independently of the other; kinds stay independent because samples carry both percents.

Daily burn, projected percents, and run-out are a separate Theil-Sen series on last-of-day sample second-offsets (plus a 0% origin when the cycle-start date has no in-cycle sample) and must not replace `ExpectedPercent`. `EstimateDailyUsage` is `ratePerSecond * 86400`. Solid estimated lines are two points: last sample to `NextRenewal` via `ProjectedPercentAt`. `EstimateRunOutInstant` is the source; `EstimateRunOutDayNumber` maps it onto a calendar row or returns null if the instant is outside 1..D. The calendar left-hand percent is the last in-cycle sample of that local date when one exists; otherwise it is `ExpectedPercent` at that day's midnight. The calendar right-hand estimated percent is shown only after the last-update date.

Chart slots: local midnights strictly inside `(CycleStart, NextRenewal)` split the domain. On a timed cycle slot 0 is the truncated `[CycleStart, first midnight)` and carries `IsLeadingPartial`; it gets no label because it is too narrow and its date repeats on the next cycle. Every other slot, including the trailing `[last midnight, NextRenewal]`, is labelled with `Date.Day`. The axis shows the day of the month, not a cycle day number, so it matches the calendar view and the date row above the plot. Thin day labels by slot index, never by the day value, or spacing breaks at a month boundary. Gridlines sit at slot starts except the plot's left edge.

`QuotaCycle.Days` is a derived in-memory calendar. `JsonPlanStore` persists cycle bounds only (`version: 2`; ignore leftover `renewalDay` / `edits` / `days[]` on load). Atomic save: write `.tmp`, then move.

A signed-in snapshot with a new cycle start replaces the cycle and `UsageSampleAppender` clears previous samples.

If you change `CycleCalculator`, `QuotaCycle`, or `JsonPlanStore` serialization, update and run `Tests/CycleCalculatorTests.cs`. Sample-driven expected/estimate cases live in `Tests/SampleEstimationTests.cs`. If you change chart X/Y mapping, update `Tests/UsageChartSeriesBuilderTests.cs`. If you change launch/interval skip rules, update `Tests/SyncScheduleTests.cs`.

## Sync contract
Allowed intervals: 1, 2, 4, 6, 12 hours (`SyncInterval.Clamp`). Auto refresh fires at `SyncSchedule.NextAlignedLocal`. On launch, never refresh when `lastUsageSyncUtc` is under 20 minutes old. After that window, refresh only when a clock-aligned slot was missed or the last update is already older than the interval; otherwise wait for the next aligned timer. Duplicate snapshots within 30 seconds are not appended (`UsageSampleAppender`).

`cursorAccountConnected` in `settings.json` records whether the Cursor account is signed in; it is the only durable "signed in" signal (plus `ActiveCycle`/`LastUsageSyncUtc` as a fallback for a prior successful sync). Do not derive "signed in" from the WebView profile folder existing or being non-empty: the browser engine writes cache/HSTS/storage housekeeping files to that folder as soon as it is first used, regardless of whether login ever succeeded, so folder presence is not evidence of a valid Cursor session. Sign out deletes only `cursor.com` cookies via the WebView's cookie manager when the current backend supports one (falls back to deleting the whole profile directory, including any Google/GitHub session, when it does not — confirmed on Linux WebKitGTK, which has no cookie manager in `Avalonia.Controls.WebView` 12.1.0); it does not delete `usage-samples.json`.

`Export Usage` is visible when connected. There is no calendar editing, **Reset**, or **Change renewal day**.

Do not add a documented-public-API client. Keep the usage fetch inside `NativeWebView` (`fetch` with credentials) so session cookies never copy into `HttpClient`. Do not use `WebAuthenticationBroker`, and do not use `TryGetCookieManager` / `GetCookiesAsync` to read or export cookies for use outside the WebView. The one exception: `NativeWebViewCursorUsageClient.DisconnectAsync` uses `TryGetCookieManager` to delete only `cursor.com` cookies on Sign out, so a Google/GitHub session kept in the same profile survives; it falls back to deleting the whole profile folder if no cookie manager is available on that backend.

## Process and window
Single instance via named mutex `CursorPace_SingleInstance` on Windows (Inno `CheckForMutexes` still uses this name). A second launch signals an EventWaitHandle and exits; the first instance shows its window. Unix uses a lock file plus a Unix domain socket under LocalApplicationData.

Close hides the window. Process stays in the tray. Only Quit (button or tray menu) calls `App.Quit()`. `--background` or **Start in notification tray** skips showing the main window. Startup registration includes `--background` when **Start in notification tray** is on.

First run (`ActiveCycle` unset): the main window shows an empty state with **Sign in**. After a successful snapshot, `GenerateCycleFromBounds` creates the cycle.

Main window is fixed size, fully custom title bar (`WindowDecorations="None"`) with app-owned Settings, Quit, Minimize, and Close controls; Fluent theme follows `themeMode` in settings (`System` / `Light` / `Dark`, default System). Do not make it resizable unless asked. Persist `WindowX` / `WindowY` on close-to-tray or quit; restore with `WindowPlacement.ClampToWorkArea`. Midnight/timezone: `MainWindow` polls `CheckForNewDay` on a 5-minute timer and on activate. Title bar drag: Windows uses `BeginMoveDrag`; Linux/macOS track pointer movement and set `Window.Position` directly, because `BeginMoveDrag` is unreliable under WSLg and many Linux compositors. Keep both paths if you touch title-bar drag.

## Commands
```
dotnet test .\Tests\CursorPace.Tests.csproj
dotnet run --project .\CursorPace.csproj
.\scripts\dev.ps1                  # Debug run (Windows / PowerShell)
./scripts/dev.sh                   # Debug run (Linux / macOS)
.\scripts\dev.ps1 -Test
./scripts/dev.sh --test
.\scripts\dev.ps1 -Background
./scripts/dev.sh --background
.\scripts\build.ps1                # Windows: publish + Inno Setup installer
.\scripts\build.ps1 -SkipInstaller # Windows: publish only
./scripts/build.sh                 # Linux/macOS: publish + AppImage or app bundle
./scripts/build.sh --skip-installer
```

Do not add trim, ReadyToRun, or PublishSingleFile. `scripts/build.ps1` / `scripts/build.sh` already set `PublishSingleFile=false`.

## Changelog
After any behavioral change, bug fix, settings/schema change, or dependency update, add a bullet under `## [Unreleased]` in `dev/CHANGELOG.md` in the same edit session as the code.

Format: `- **{Type}**: {scope} - description.` Types: `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`. Use backticks for identifiers. One bullet per logical change. Scopes are short (`calendar`, `tray`, `cycle`, `persistence`, `settings`, `install`).

Skip only for documentation-only or comment-only edits with no user-visible effect.

Do not move `[Unreleased]` into a versioned section, and do not write `release-notes/RELEASE_NOTES_*.md`, unless you are following `dev/release-new-version-prompt.md`.

## When changing code
- Match existing naming, file placement, and Avalonia namespaces (`Avalonia.Controls`, not WinUI `Microsoft.UI.Xaml`). `System.Windows.Input.ICommand` is still used. Command bodies bound through `RelayCommand` must not be bare `async () => await ...` lambdas assigned to the `Action` overload; that compiles to async-void and can crash the process. Use `AsyncRelayCommand`.
- Prefer editing an existing service/VM over new layers.
- Keep view code-behind thin: window lifetime, dialogs, scrolling, theme. Put state and commands on the view model.
- After calculator or persistence changes: `dotnet test .\Tests\CursorPace.Tests.csproj`.
- After UI changes: run the app (`.\scripts\dev.ps1` or `./scripts/dev.sh`) and check first-run, close-to-tray, quit, and second-instance activation if those paths were touched.
- Log user-visible work in `dev/CHANGELOG.md` (see Changelog above).
- Do not commit installer output, `bin/`, `obj/`, or files under `installer/` (gitignored).
- Do not commit unless asked.
