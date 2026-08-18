# Changelog

All notable changes to this project will be documented in this file.

Use conventional types (**Added**, **Changed**, **Fixed**, **Removed**), a short **scope** (UI area or subsystem), and a clear description.

Add new entries in the `## [Unreleased]` section. When releasing, move those entries to `## [x.y.z] - YYYY-MM-DD` using `dev/release-new-version-prompt.md`.

## [Unreleased]

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
