## Approach
- Read existing files before writing. Don't re-read unless changed.
- Thorough in reasoning, concise in output.
- Skip files over 100KB unless required.
- No sycophantic openers or closing fluff.
- No emojis or em-dashes.
- Do not guess APIs, versions, flags, commit SHAs, or package names. Verify by reading code or docs before asserting.

## Project
Windows 11 desktop app that tracks Cursor quota allowances across a billing cycle. Users pick a renewal day (1-31), see two independent percentages (Cursor models vs other models), and can pin manual values on specific days. There is no Cursor API; tracking is manual.

Stack: C# / .NET 10 / WinUI 3 (Windows App SDK). Unpackaged (`WindowsPackageType` None). Namespace `CursorUsageProgress`. Target `net10.0-windows10.0.19041.0`.

Treat source and `.csproj` as truth. Other markdown in this repo may lag the code and is being reviewed separately. Do not "fix" README or sibling docs unless asked.

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
- Startup: `IStartupRegistration` / `WindowsStartupRegistration` (current-user Run key).
- Tray: `ITrayService` / `TrayService` (`H.NotifyIcon.WinUI`). Lives for the whole process.
- UI state: `MainViewModel` plus calendar/day row VMs. Views subscribe to VM events; they do not own cycle math.

Percentages use `decimal` in models and calculator. UI may round to integers for NumberBox/display. Do not switch storage or interpolation to `double`.

`QuotaKind` is a two-value enum. When switching on it, handle both cases and keep a `never` default so a new kind fails at compile time.

## Cycle contract
Renewal day is 1-31. Months that lack that day are skipped (Jan 31 -> Mar 31).

`D = (NextRenewal - CycleStart).Days`. Day numbers are 1..D. Day 1 is always 0% unless edited. Renewal itself is not a row; it is the 100% anchor after the last day.

Unedited days are interpolated between anchors: cycle start (0%), each manual edit of that `QuotaKind`, then renewal (100%). Editing one kind never changes the other. A later edit is an interpolation endpoint, not something to wipe. `ExpectedPercent` is the renewal-paced series. Daily burn, projected percents, and run-out dates are a separate derived Theil-Sen series and must not replace `ExpectedPercent`.

`QuotaCycle.Edits` is the source of truth for overrides. `QuotaCycle.Days` is a derived in-memory calendar. `RebuildDays` after any edit/clear. `JsonPlanStore` persists edits only (legacy `days[]` is migrated on load, then dropped on save). Atomic save: write `.tmp`, then move.

Changing renewal day starts a new cycle and drops previous edits. That is intentional.

If you change `CycleCalculator`, `QuotaCycle`, `QuotaDayEdit`, or `JsonPlanStore` serialization, update and run `Tests/CycleCalculatorTests.cs`.

## Process and window
Single instance via named mutex `CursorUsageProgress_SingleInstance`. A second launch signals an EventWaitHandle and exits; the first instance shows its window.

Close hides the window. Process stays in the tray. Only Quit (button or tray menu) calls `App.Quit()`. `--background` skips showing the main window when already initialized.

First run (`RenewalDay` unset): show `RenewalDayDialog` before the main UI is usable.

Main window is fixed size, custom title bar, Mica/theme from system. Do not make it resizable unless asked. Midnight/timezone: `MainWindow` polls `CheckForNewDay` on a 5-minute timer and on activate.

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
