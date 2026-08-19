## Approach
- Read existing files before writing. Don't re-read unless changed.
- Thorough in reasoning, concise in output.
- Skip files over 100KB unless required.
- No sycophantic openers or closing fluff.
- No emojis or em-dashes.
- Do not guess APIs, versions, flags, commit SHAs, or package names. Verify by reading code or docs before asserting.

## Project
Windows 11 desktop app that tracks Cursor quota allowances across a billing cycle. Users sign in with a Cursor account; an embedded WebView2 session fetches `https://cursor.com/api/usage-summary` (dashboard endpoint, not a documented public personal-plan API). The billing cycle and usage samples come from Cursor. The calendar and chart show two independent percentages: Cursor models vs other models.

Stack: C# / .NET 10 / WinUI 3 (Windows App SDK). Unpackaged (`WindowsPackageType` None). Namespace `CursorUsageProgress`. Target `net10.0-windows10.0.19041.0`.

Treat source and `.csproj` as truth. When you change user-visible behavior, update `README.md` and `QUICKSTART.md` in the same session unless the user says docs are out of scope. Do not invent features in docs.

## Layout
Flat repo. App project: `CursorUsageProgress.csproj`. Tests: `Tests/CursorUsageProgress.Tests.csproj` (xUnit). Solution file: `CursorUsageProgress.slnx`.

| Path | Role |
|---|---|
| `Models/` | Data types only. No I/O, no UI. |
| `Services/` | Interfaces plus implementations. Business logic and OS integration. |
| `ViewModels/` | MVVM binding, commands, `INotifyPropertyChanged`. |
| `Views/` | WinUI XAML/code-behind. Lifecycle, dialogs, window chrome. |
| `Converters/` | XAML value converters registered in `App.xaml`. |
| `Assets/` | Icon and tray image. |
| `Tests/` | Unit tests. App csproj excludes this folder. |
| `dev/` | Maintainer files: `CHANGELOG.md`, `DEVELOPMENT.md`, release-notes prompt. |
| `scripts/` | Maintainer PowerShell: `dev.ps1`, `build.ps1`, `clean.ps1`, `release.ps1`. |
| `App.xaml.cs` | Process entry: mutex, DI wiring, tray, `--background`. |

Root `UnitTest1.cs` is excluded from the app project. Do not revive it. Put new tests under `Tests/`.

## Architecture
Construct services in `App.OnLaunched` and pass them in. Do not add a DI container unless asked.

- Time: `IClock` / `SystemClock`. Never call `DateTime.Now`/`Today` from calculator or view models.
- Cycle math: `ICycleCalculator` / `CycleCalculator`. Keep it pure (no file I/O, no UI).
- Persistence: `IPlanStore` / `JsonPlanStore`. Path `%LocalAppData%\CursorUsageProgress\settings.json`.
- Usage samples: `IUsageSampleStore` / `JsonUsageSampleStore`. Path `%LocalAppData%\CursorUsageProgress\usage-samples.json`.
- Cursor usage: `ICursorUsageClient` / `WebView2CursorUsageClient`. Profile folder `%LocalAppData%\CursorUsageProgress\WebView2`.
- Sync: `IUsageSyncService` / `UsageSyncService`. Clock-aligned auto refresh; `SyncSchedule` decides launch skip.
- Startup: `IStartupRegistration` / `WindowsStartupRegistration` (current-user Run key).
- Tray: `ITrayService` / `TrayService` (`H.NotifyIcon.WinUI`). Lives for the whole process.
- UI state: `MainViewModel` plus calendar/day row VMs and a read-only `UsageChartViewModel`. Views subscribe to VM events; they do not own cycle math. The main window can show the calendar or the usage chart.

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
Allowed intervals: 1, 2, 4, 6, 12 hours (`SyncInterval.Clamp`). Auto refresh fires at `SyncSchedule.NextAlignedLocal`. On launch, skip the usage refresh when `cursorAccountConnected` is set and `lastUsageSyncUtc` is under 20 minutes old, unless a clock-aligned slot was missed or the last update is already older than the interval. Duplicate snapshots within 30 seconds are not appended (`UsageSampleAppender`).

`cursorAccountConnected` in `settings.json` records whether the Cursor account is signed in. `HasPersistedProfile` on the WebView2 folder can also mark the session connected. Sign out deletes the WebView2 profile; it does not delete `usage-samples.json`.

`Export Usage` is visible when connected. There is no calendar editing, **Reset**, or **Change renewal day**.

Do not add a documented-public-API client. Keep the usage fetch inside WebView2 (`fetch` with credentials) so session cookies never copy into `HttpClient`.

## Process and window
Single instance via named mutex `CursorUsageProgress_SingleInstance`. A second launch signals an EventWaitHandle and exits; the first instance shows its window.

Close hides the window. Process stays in the tray. Only Quit (button or tray menu) calls `App.Quit()`. `--background` or **Start in notification tray** skips showing the main window. The Run key includes `--background` when **Start in notification tray** is on.

First run (`ActiveCycle` unset): the main window shows an empty state with **Sign in**. After a successful snapshot, `GenerateCycleFromBounds` creates the cycle.

Main window is fixed size, custom title bar, Mica/theme from system. Do not make it resizable unless asked. Persist `WindowX` / `WindowY` on close-to-tray or quit; restore with `WindowPlacement.ClampToWorkArea`. Midnight/timezone: `MainWindow` polls `CheckForNewDay` on a 5-minute timer and on activate.

## Commands
```
dotnet test .\Tests\CursorUsageProgress.Tests.csproj
dotnet run --project .\CursorUsageProgress.csproj
.\scripts\dev.ps1                  # Debug run
.\scripts\dev.ps1 -Test
.\scripts\dev.ps1 -Background
.\scripts\build.ps1 -SkipInstaller # self-contained win-x64 publish
```

Do not add trim, ReadyToRun, or PublishSingleFile. `scripts/build.ps1` already sets `PublishSingleFile=false` and `WindowsAppSDKSelfContained=true`.

## Changelog
After any behavioral change, bug fix, settings/schema change, or dependency update, add a bullet under `## [Unreleased]` in `dev/CHANGELOG.md` in the same edit session as the code.

Format: `- **{Type}**: {scope} - description.` Types: `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`. Use backticks for identifiers. One bullet per logical change. Scopes are short (`calendar`, `tray`, `cycle`, `persistence`, `settings`, `install`).

Skip only for documentation-only or comment-only edits with no user-visible effect.

Do not move `[Unreleased]` into a versioned section, and do not write `release-notes/RELEASE_NOTES_*.md`, unless you are following `dev/release-new-version-prompt.md`.

## When changing code
- Match existing naming, file placement, and WinUI namespaces (`Microsoft.UI.Xaml`, not WPF `System.Windows` except `ICommand`).
- Prefer editing an existing service/VM over new layers.
- Keep view code-behind thin: window lifetime, dialogs, scrolling, theme. Put state and commands on the view model.
- After calculator or persistence changes: `dotnet test .\Tests\CursorUsageProgress.Tests.csproj`.
- After UI changes: run the app (`.\scripts\dev.ps1`) and check first-run, close-to-tray, quit, and second-instance activation if those paths were touched.
- Log user-visible work in `dev/CHANGELOG.md` (see Changelog above).
- Do not commit installer output, `bin/`, `obj/`, or files under `installer/` (gitignored).
- Do not commit unless asked.
