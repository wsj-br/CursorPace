# Changelog

All notable changes to this project will be documented in this file.

Use conventional types (**Added**, **Changed**, **Fixed**, **Removed**), a short **scope** (UI area or subsystem), and a clear description.

Add new entries in the `## [Unreleased]` section. When releasing, move those entries to `## [x.y.z] - YYYY-MM-DD` using `dev/release-new-version-prompt.md`.

## [Unreleased]

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
