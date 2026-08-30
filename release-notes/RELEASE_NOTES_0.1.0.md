# Cursor Usage Progress 0.1.0 Release Notes

## Highlights

- Calendar for the current cycle, with today, renewal, and projected run-out days highlighted
- Separate Cursor Models and Other Models percentages
- Manual day edits that act as interpolation anchors, plus Theil-Sen daily usage and run-out estimates
- System tray: closing the window hides it; Quit exits
- Optional start at Windows sign-in (user-level, no elevation)
- Single-instance: a second launch brings the existing window forward

## Why this release matters

First public build of a local Windows planner for Cursor model quota. It does not read Cursor usage or call any Cursor API.

## Detailed Changes

- **Added**: app — Windows desktop planner for Cursor model quota across a monthly renewal cycle (no Cursor API).
- **Added**: calendar — current-cycle month view with today, renewal, and projected run-out days highlighted.
- **Added**: quotas — independent Cursor Models and Other Models percentages, with manual day edits as interpolation anchors.
- **Added**: estimates — Theil-Sen daily usage and run-out day projection.
- **Added**: tray — close hides the window; Quit exits; optional Run at Windows sign-in (per-user, no elevation).
- **Added**: process — single-instance mutex; a second launch shows the existing window.
- **Added**: install — per-user Inno Setup build (`CursorUsageProgress-<version>-win-x64-setup.exe`).

---

## Install

Download `CursorUsageProgress-1.0.0-win-x64-setup.exe` from this release. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**.

---

## Documentation

- [Quick start](https://github.com/wsj-br/CursorUsageProgress/blob/master/QUICKSTART.md) — install, daily use, tray, troubleshooting.
- [Development](https://github.com/wsj-br/CursorUsageProgress/blob/master/dev/DEVELOPMENT.md) — build, test, package, contribute.
- [README](https://github.com/wsj-br/CursorUsageProgress/blob/master/README.md) — product overview and source build.

---

## License

MIT © [Waldemar Scudeller Jr.](https://github.com/wsj-br/CursorUsageProgress)
