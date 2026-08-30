# Changelog

All notable changes to this project will be documented in this file.

Use conventional types (**Added**, **Changed**, **Fixed**, **Removed**), a short **scope** (UI area or subsystem), and a clear description.

Add new entries in the `## [Unreleased]` section. When releasing, move those entries to `## [x.y.z] - YYYY-MM-DD` using `dev/release-new-version-prompt.md`.

## [Unreleased]

- **Fixed**: install - `packaging/cursor-usage-progress.appdata.xml` is renamed to `packaging/io.github.wsj_br.CursorUsageProgress.appdata.xml` (matching the component `<id>`), and `build-appimage.sh` copies it into `usr/share/metainfo/` under that same name; `appstreamcli`'s tree validation flagged the previous mismatched filename as a `metainfo-filename-cid-mismatch` warning, which made `appimagetool` fail AppStream validation and abort the `linux-arm64` AppImage build in CI.

## [0.2.0] - 2026-08-30

- **Fixed**: window - on first launch, `MainWindow` restores the saved position from the `Opened` event (in addition to `Activated`), since Linux window managers do not reliably raise `Activated` when the window is first shown; previously the window sat at the WM's default position (left edge of the monitor) until the user's first click gave it focus.
- **Fixed**: build - `global.json` uses `rollForward: latestFeature` so local Windows/macOS installs with a newer 10.0 SDK (for example `10.0.400`) work while CI still pins `10.0.111`.
- **Changed**: ui - sync alert banner now uses a light-red border with a brighter, semi-transparent red fill instead of a flat, dull rose; the alert text color is themed (`SyncAlertForegroundBrush`) instead of hardcoded white so it stays readable against the lighter fill.
- **Added**: install - Linux ARM64 AppImage release build; `build-linux` in `.github/workflows/dotnet-desktop.yml` is now a matrix over `linux-x64` (`ubuntu-24.04`) and `linux-arm64` (`ubuntu-24.04-arm`), and `scripts/build.sh` / `scripts/build-appimage.sh` accept `--rid linux-arm64` and select the matching `linuxdeploy-aarch64` tool.
- **Added**: install - `packaging/cursor-usage-progress.appdata.xml` AppStream metadata, bundled into `usr/share/metainfo/` by `build-appimage.sh`, so `appimagetool` no longer warns about missing upstream metadata.

- **Fixed**: sync - a Linux AppImage run now uses a separate `WebView-AppImage` profile folder instead of sharing `WebView` with a `dotnet run`/`dev.sh` build; the AppImage bundles its own WebKitGTK, and the two WebKit builds reading/writing the same cookie database was causing recurring, false `AuthRequired` sign-outs.
- **Fixed**: sync - the app no longer treats "the WebView profile folder is non-empty" as evidence of a signed-in Cursor session; on Linux (WebKitGTK) that folder gets cache/HSTS/storage files as soon as the embedded browser is first used, even if sign-in is cancelled or `Continue` is clicked before Cursor accepts the session, which was showing `(connected)` for an account that was never actually authenticated. `ICursorUsageClient.HasPersistedProfile` is removed; "signed in" now comes only from `cursorAccountConnected`/prior sync evidence and actual fetch results.
- **Changed**: sync - `Sign out` deletes only `cursor.com` cookies via the WebView's cookie manager when the current platform backend supports one, keeping any Google or GitHub session stored in the same browser profile; it falls back to deleting the whole profile folder (Google/GitHub session included) when no cookie manager is available. Confirmed on Linux: the WebKitGTK backend in `Avalonia.Controls.WebView` 12.1.0 has no cookie manager, so Sign out still clears the whole profile there; Windows (WebView2) and macOS (WKWebView) back a cookie manager natively and are expected to keep the Google/GitHub session, but that is unverified on this change.
- **Fixed**: install - `build-appimage.sh` pins `linuxdeploy-plugin-gtk` to a commit SHA instead of tracking `master`, and caches `linuxdeploy`/the GTK plugin under version-qualified filenames so bumping either pinned ref actually re-downloads instead of silently reusing a stale `.appimage-build/` copy.
- **Changed**: install - tag-triggered release builds now validate version and notes once, build unsigned Windows/Linux plus both macOS architectures with read-only jobs, verify every checksum, and publish only after all packages succeed.
- **Changed**: install - use maintained GitHub Actions major versions and locked NuGet restores with Dependabot updates.
- **Fixed**: window - custom title bar drag on Linux/macOS tracks pointer movement and updates `Window.Position` (`BeginMoveDrag` is unreliable under WSLg and many Linux compositors); Windows still uses `BeginMoveDrag`.
- **Fixed**: ui - calendar, chart, and dialog text use `SelectableTextBlock`; Ctrl+C copies the current selection from the main window and dialogs.
- **Added**: window - sync alert for sign-in required shows **Sign in** and **Settings** actions; **Sign in** stays available in Settings when a session exists but sync needs re-authentication.
- **Added**: install - Linux AppImage packaging via `./scripts/build.sh` and `scripts/build-appimage.sh` (linuxdeploy + GTK plugin).
- **Added**: install - macOS `.app` bundle packaging via `./scripts/build.sh` and `scripts/build-appbundle.sh` (zipped for release).
- **Changed**: scripts - `build.sh` detects Linux/macOS and packages accordingly; Windows/Inno remains in `build.ps1` only.
- **Added**: scripts - bash equivalents of `dev`, `build`, `clean`, and `release` (`scripts/*.sh`) for Linux/macOS without PowerShell.
- **Fixed**: sync - Linux/macOS WebKit sign-in no longer fails with `Unsupported result type` after Google login; usage fetch returns a JSON string from `InvokeScript` and still accepts the `invokeCSharpAction` message path when the script result cannot be marshaled.
- **Changed**: window - move `Updated dd/MM HH:mm` under the calendar month heading / chart title (hidden while a sync alert banner is shown).
- **Fixed**: sync - launch refresh no longer runs mid-slot once the last update is 20+ minutes old; the 20-minute window is a hard floor, and after that only a missed aligned slot or a full interval triggers a start refresh.
- **Changed**: settings - completion dialogs place `Open Folder` on the left and `OK` on the right.
- **Changed**: settings - export and backup completion dialogs offer `Open Folder` for local destinations.
- **Changed**: settings - CSV and backup suggested filenames now include `yyyy-MM-dd-HH_mm_ss` timestamps to avoid filename clashes.
- **Changed**: install - version is `0.2.0`.
- **Fixed**: persistence - `Load()` treats only JSON parse failures as corruption; a locked or unreadable file no longer overwrites `settings.json` or `usage-samples.json`.
- **Fixed**: sync - auto-refresh timer ticks and Sign in / Refresh / Sign out commands catch exceptions instead of crashing the process.
- **Fixed**: settings - Backup/Restore file and store failures surface an error dialog instead of an unhandled exception; a failed sample write rolls back settings.
- **Fixed**: settings - a locked-down startup registry or Launch Agent failure no longer prevents the app from starting.
- **Changed**: calendar - theme-aware colors; projected percents show the number only (green for ≤100%, red for >100%); remove the cryptic legend under the month heading.
- **Changed**: window - first-run hides empty metric cards; syncing shows a progress bar; failed or auth-required updates use a banner.
- **Changed**: tray - tooltip spells out projected percent at renewal instead of `EOP`.
- **Changed**: chart - empty plot shows `Not enough data yet` and series colors follow the system theme.
- **Fixed**: window - title-bar Close tooltip is `Hide to tray`.
- **Changed**: settings - `Sign in` is hidden while connected; Export Cycle CSV explains why it is disabled.
- **Fixed**: dialogs - Escape cancels, focus starts on the safe button, and Windows uses primary-then-cancel order.
- **Changed**: install - keep `Avalonia.Controls.WebView` on the latest published version that matches Avalonia 12 (currently 12.1.0).
- **Fixed**: window - restore the `Cursor Usage Progress` label to the left side of the custom title bar.
- **Changed**: window - add a small gap between `Quit` and the window controls.
- **Fixed**: window - use a fully custom fixed-size title bar so Minimize and Close are right-aligned without competing with Avalonia's overlay caption controls.
- **Changed**: settings - opens in the main window with a `Back` control and clickable `Settings` heading below the title bar.
- **Added**: settings - `Backup` and `Restore` write or read a zip of `settings.json` and `usage-samples.json` (the Cursor session is not included).

- **Fixed**: window - custom title text no longer overlaps the Avalonia 12 overlay caption on the main window (including the in-window Settings view).
- **Fixed**: chart - plot rebuilds from the host size so the canvas is not left empty after switching from the calendar.

- **Fixed**: build - `MainWindow` has a public parameterless constructor so Avalonia no longer warns `AVLN3001`.
- **Changed**: ui - replaced WinUI 3 with Avalonia 12 so the same desktop app runs on Windows, Linux, and macOS.
- **Changed**: install - Windows publish output is now `bin\Release\net10.0\win-x64\publish`; `scripts/build.ps1` no longer sets `WindowsAppSDKSelfContained`.
- **Removed**: settings - Windows-only Mica title bar; Fluent theme follows the system light/dark variant instead.
- **Added**: settings - launch-at-login on macOS (Launch Agent) and Linux (XDG autostart); the setting is labeled `Launch at login`.
- **Changed**: tray - notification icon uses Avalonia `TrayIcon` instead of `H.NotifyIcon.WinUI`.
- **Changed**: sync - usage fetch runs in Avalonia `NativeWebView` with a persistent per-OS profile; Windows still uses `%LocalAppData%\CursorUsageProgress\WebView2`.

- **Fixed**: build - (WinUI era) `dotnet test` and `scripts/build.ps1` succeed with `EnableMsixTooling` on RID-less AnyCPU builds (`AllowNeutralPackageWithAppHost`).
- **Fixed**: install - (WinUI era) unpackaged publish now includes `resources.pri`, so the Inno-installed exe no longer starts and exits immediately (`Microsoft.UI.Xaml.dll` `0xc000027b`).
- **Added**: settings - `Start in notification tray` beside `Run at Windows sign-in`; when on, launch hides the window and the Run key uses `--background`.
- **Changed**: settings - Startup toggles use a 48px gap and line up with automatic updates and the refresh interval.
- **Changed**: calendar - the renewal date shows expected and estimated percents when `NextRenewal` is after midnight.
- **Changed**: calendar - weekday labels are larger and closer to the grid, with the cycle-start month centered above them.
- **Changed**: window - cycle start, next renewal, and run-out cards use `dd-MMM HH:mm`.
- **Changed**: chart - the X axis is elapsed seconds from `CycleStart` to `NextRenewal`; midnight is a grid marker.
- **Changed**: chart - X axis slots are labeled with the day of the month instead of a cycle day number, so the axis matches the calendar view and the date row; the truncated slot before the first midnight is left unlabeled and the renewal-date slot now gets a label.
- **Changed**: cycle - `ExpectedPercentAt` interpolates `(0, 0%)`, every in-cycle sample timestamp, and `100%` at `NextRenewal`; days before the first sample rise toward that sample.
- **Changed**: cycle - Theil-Sen burn uses second offsets; estimated chart lines are two points from the last sample to `NextRenewal`.
- **Removed**: calendar - manual day edits, the edit panel, `Reset`, `Reset cycle`, `Change renewal day`, and the first-run renewal-day dialog.
- **Changed**: settings - `settings.json` is `version` 2; `renewalDay` and cycle `edits` are no longer written.
- **Added**: window - empty state with `Sign in` until a Cursor snapshot creates the cycle.
- **Fixed**: window - persist the last position on close-to-tray or quit and restore it on the next show and launch.

- **Changed**: sync - on launch, never refresh when `lastUsageSyncUtc` is under 20 minutes old; after that, refresh only when a clock-aligned slot was missed or the last update is older than the interval.
- **Changed**: sync - automatic updates run on the clock hour aligned to the configured interval (`1h` at 00:00/01:00/02:00, `2h` at 00:00/02:00/04:00, `4h` at 00:00/04:00/08:00, and the same pattern for `6h`/`12h`).

- **Added**: persistence - `cursorAccountConnected` in `settings.json` records whether the Cursor account is signed in so launch and other features can detect it.
- **Changed**: settings - cycle export is labeled `Export Cycle CSV`; `Export Usage` appears on the same row when the Cursor account is connected and writes collected usage samples as CSV.
- **Added**: chart - Calendar/Chart switch on the main window plots expected (dashed) and estimated (solid) Cursor and Other Models series, a 100% limit line, and sample markers plus the cycle-start origin.
- **Changed**: chart - calendar and chart icons sit on the info-card row; the selected icon uses the accent color.
- **Fixed**: chart - day labels sit in the day slot between midnight ticks, the last day has a full 24h close tick, the plot is boxed, vertical gridlines are subtle, and the legend no longer overlaps.
- **Changed**: settings - CSV headers use `expected` and `estimated`; expected columns write `ExpectedPercent` instead of the calendar-left sample overlay.
- **Changed**: settings - refresh interval sits on the same row as automatic updates; the account heading shows `(connected)` or `(disconnected)`; `Sign in` is disabled while signed in; Renewal Day is hidden while the Cursor account is connected.
- **Added**: ui - informational labels can be selected and copied.
- **Fixed**: estimates - projection uses the last sample per local date and a 0% cycle-start anchor so same-day clusters do not skew daily burn.
- **Changed**: settings - CSV export writes the calendar left (quota) and right (projection) percents into the existing linear and recalculated columns.
- **Changed**: settings - the Cursor account action is labeled `Sign out` instead of Disconnect.
- **Fixed**: sync - opening the app no longer shows a JSON string conversion error under the cards; WebView2 usage results are read as an object or a JSON string.
- **Fixed**: sync - sign-in completes automatically when Cursor accepts the session; the window explains this and has a Continue button if it does not.
- **Fixed**: window - extra height for the last-updated caption so the calendar is not cropped at the bottom.
- **Fixed**: sync - WebView2 native binaries now match the x64 process, so Sign in no longer fails with "The specified module could not be found".
- **Added**: settings - Cursor account sign-in, disconnect, refresh, and 1/2/4/6/12 hour sync interval.
- **Changed**: calendar - days with API samples show the last reading of that day (teal day number); days without samples still show the plan.
- **Changed**: tray - hover tooltip shows today's percent and the projected percent at the next renewal instant for Cursor and Other models.
- **Changed**: app — renamed product to Cursor Usage Progress (`CursorUsageProgress`); settings, mutex, startup registry, installer, and GitHub repo use the new name.
- **Fixed**: install - if the app is running, Retry waits until it is closed and Cancel aborts; previously both buttons aborted.
- **Changed**: scripts - moved `dev.ps1`, `build.ps1`, `clean.ps1`, and `release.ps1` to `scripts/`.
- **Fixed**: calendar - restore `CursorProjectedAtOrAbove100` and `OtherProjectedAtOrAbove100` on `CalendarCellViewModel` so compiled bindings can apply projected quota colors.

## [0.1.0] - 2026-08-17

- **Added**: app — Windows desktop planner for Cursor model quota across a monthly renewal cycle (no Cursor API).
- **Added**: calendar — current-cycle month view with today, renewal, and projected run-out days highlighted.
- **Added**: quotas — independent Cursor Models and Other Models percentages, with manual day edits as interpolation anchors.
- **Added**: estimates — Theil-Sen daily usage and run-out day projection.
- **Added**: tray — close hides the window; Quit exits; optional Run at Windows sign-in (per-user, no elevation).
- **Added**: process — single-instance mutex; a second launch shows the existing window.
- **Added**: install — per-user Inno Setup build (`CursorUsageProgress-<version>-win-x64-setup.exe`).
