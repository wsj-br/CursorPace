# Changelog

All notable changes to this project will be documented in this file.

Use conventional types (**Added**, **Changed**, **Fixed**, **Removed**), a short **scope** (UI area or subsystem), and a clear description.

Add new entries in the `## [Unreleased]` section. When releasing, move those entries to `## [x.y.z] - YYYY-MM-DD` using `dev/release-new-version-prompt.md`.

## [Unreleased]

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

- **Changed**: sync - skip the launch usage refresh when `cursorAccountConnected` is set and `lastUsageSyncUtc` is under 20 minutes old, unless a clock-aligned interval slot was missed or the last update is already older than the interval (for example last update 19:55, start 20:05, interval 1h).
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
- **Changed**: tray - hover tooltip shows today's percent and end-of-period (`EOP`) projection for Cursor and Other models; `EOP` is the estimate on the last cycle day (the day before next renewal).
- **Changed**: app — renamed product to Cursor Usage Progress (`CursorUsageProgress`); settings, mutex, startup registry, installer, and GitHub repo use the new name.
- **Fixed**: install - if the app is running, Retry waits until it is closed and Cancel aborts; previously both buttons aborted.
- **Changed**: scripts - moved `dev.ps1`, `build.ps1`, `clean.ps1`, and `release.ps1` to `scripts/`.
- **Fixed**: calendar - restore `CursorProjectedAtOrAbove100` and `OtherProjectedAtOrAbove100` on `CalendarCellViewModel` so `x:Bind` can compile projected quota colors.

## [1.0.0] - 2026-08-17

- **Added**: app — Windows desktop planner for Cursor model quota across a monthly renewal cycle (no Cursor API).
- **Added**: calendar — current-cycle month view with today, renewal, and projected run-out days highlighted.
- **Added**: quotas — independent Cursor Models and Other Models percentages, with manual day edits as interpolation anchors.
- **Added**: estimates — Theil-Sen daily usage and run-out day projection.
- **Added**: tray — close hides the window; Quit exits; optional Run at Windows sign-in (per-user, no elevation).
- **Added**: process — single-instance mutex; a second launch shows the existing window.
- **Added**: install — per-user Inno Setup build (`CursorUsageProgress-<version>-win-x64-setup.exe`).
