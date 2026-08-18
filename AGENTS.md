## Approach
- Read existing files before writing. Don't re-read unless changed.
- Thorough in reasoning, concise in output.
- Skip files over 100KB unless required.
- No sycophantic openers or closing fluff.
- No emojis or em-dashes.
- Do not guess APIs, versions, flags, commit SHAs, or package names. Verify by reading code or docs before asserting.

## Project
Windows 11 desktop app that tracks Cursor quota allowances across a billing cycle. Users pick a renewal day (1-31), see two independent percentages (Cursor models vs other models), and can pin manual values on specific days. They can also sign in with a Cursor account; an embedded WebView2 session fetches `https://cursor.com/api/usage-summary` (dashboard endpoint, not a documented public personal-plan API). While signed in, the billing cycle comes from Cursor and calendar edits are disabled.

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
- UI state: `MainViewModel` plus calendar/day row VMs and a read-only `UsageChartViewModel`. Views subscribe to VM events; they do not own cycle math. The main window can show the calendar or the usage chart; editing stays on the calendar.

Percentages use `decimal` in models and calculator. UI may round to integers for NumberBox/display. Do not switch storage or interpolation to `double`.

`QuotaKind` is a two-value enum. When switching on it, handle both cases and keep a `never` default so a new kind fails at compile time.

## Cycle contract
Renewal day is 1-31. Months that lack that day are skipped (Jan 31 -> Mar 31). Signed-in snapshots call `GenerateCycleFromBounds` with Cursor `billingCycleStart` / `billingCycleEnd` (timed instants, not necessarily midnight).

`D = (NextRenewal - CycleStart).Days`. Day numbers are 1..D. Day 1 is 0% unless pinned by a last-of-day sample or a manual edit. Renewal itself is not a row; it is the 100% anchor at the `NextRenewal` instant.

Observed anchors (per `QuotaKind`, independently) are the last sync sample on that local date, else a manual edit on that day; a sample wins on the same day. `ExpectedPercent` is that pin on an observed day. After a pin it interpolates remaining quota to 100% at `NextRenewal` (later pins do not pull the gap). Days before the first pin stay on `LinearPercent`. Editing one kind never changes the other. Daily burn, projected percents, and run-out dates are a separate derived Theil-Sen series and must not replace `ExpectedPercent`. The chart dashed lines are `ExpectedPercent` from `CycleStart` to `NextRenewal`; solid estimated lines start at the last sample or edit of that kind and continue to `NextRenewal` along elapsed time (`ProjectedPercentAt`), not day-index Y values. Day slots are 24h (midnight `n` to midnight `n+1`); the last slot ends at `D+1` with no extra axis tick. The plot can continue to `NextRenewal` after that midnight. The calendar right-hand estimated percent is shown only after that last-update date. Sample markers use the stored timestamp (not last-of-day).

`QuotaCycle.Edits` is the source of truth for overrides. `QuotaCycle.Days` is a derived in-memory calendar. `RebuildDays` after any edit/clear. `JsonPlanStore` persists edits only (legacy `days[]` is migrated on load, then dropped on save). Atomic save: write `.tmp`, then move.

Changing renewal day starts a new cycle and drops previous edits. That is intentional. A signed-in snapshot with a new cycle start replaces the cycle and `UsageSampleAppender` clears previous samples.

If you change `CycleCalculator`, `QuotaCycle`, `QuotaDayEdit`, or `JsonPlanStore` serialization, update and run `Tests/CycleCalculatorTests.cs`. Sample-driven expected/estimate cases live in `Tests/SampleEstimationTests.cs`. If you change chart X/Y mapping, update `Tests/UsageChartSeriesBuilderTests.cs`. If you change launch/interval skip rules, update `Tests/SyncScheduleTests.cs`.

## Sync contract
Allowed intervals: 1, 2, 4, 6, 12 hours (`SyncInterval.Clamp`). Auto refresh fires at `SyncSchedule.NextAlignedLocal`. On launch, skip the usage refresh when `cursorAccountConnected` is set and `lastUsageSyncUtc` is under 20 minutes old, unless a clock-aligned slot was missed or the last update is already older than the interval. Duplicate snapshots within 30 seconds are not appended (`UsageSampleAppender`).

`cursorAccountConnected` in `settings.json` records whether the Cursor account is signed in. `HasPersistedProfile` on the WebView2 folder can also mark the session connected. Sign out deletes the WebView2 profile; it does not delete `usage-samples.json`.

While `IsCursorConnected`: hide title-bar **Reset** and Settings **Reset cycle** / **Change renewal day**; `CanEditDays` is false. `Export Usage` is visible when connected.

Do not add a documented-public-API client. Keep the usage fetch inside WebView2 (`fetch` with credentials) so session cookies never copy into `HttpClient`.

## Process and window
Single instance via named mutex `CursorUsageProgress_SingleInstance`. A second launch signals an EventWaitHandle and exits; the first instance shows its window.

Close hides the window. Process stays in the tray. Only Quit (button or tray menu) calls `App.Quit()`. `--background` skips showing the main window when already initialized.

First run (`RenewalDay` unset): show `RenewalDayDialog` before the main UI is usable. After a successful signed-in snapshot, `GenerateCycleFromBounds` can set `RenewalDay` from Cursor.

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
