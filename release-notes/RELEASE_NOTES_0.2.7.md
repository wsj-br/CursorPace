# Cursor Pace 0.2.7 Release Notes

## Highlights

- Settings is grouped into **Account & Usage**, **Sync Server**, **App**, **Data**, and **About** tabs.
- The usage chart labels the last measured Cursor and Other Models percents (`xx.x%`) in each series color, and a dotted vertical guide shows the linear expected percent at that same time.
- Those endpoint labels use a plot-background outline so they stay readable over the grid and series.
- On Linux, **Refresh** no longer flashes a vertical line during a silent usage fetch.

## Why this release matters

Settings is easier to scan, and the chart shows the latest measured usage next to the straight expected pace without covering the plot.

## Detailed Changes

See [`dev/CHANGELOG.md`](https://github.com/wsj-br/CursorPace/blob/master/dev/CHANGELOG.md#027---2026-09-22) for the full list of changes in this release.

---

## Install

Download the package for your platform from this release:

- Windows: `CursorPace-0.2.7-win-x64-setup.exe`. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**. **Sign in** needs the Microsoft Edge WebView2 Runtime; the installer offers the download page if it is missing.
- Linux: `CursorPace-0.2.7-linux-x64.AppImage` or `CursorPace-0.2.7-linux-arm64.AppImage`. Make the file executable (`chmod +x`) before running it.
- macOS: `CursorPace-0.2.7-osx-arm64.zip` or `CursorPace-0.2.7-osx-x64.zip`. Unzip, then right-click `CursorPace.app` and choose **Open** the first time to bypass Gatekeeper (the build is unsigned).

---

## Documentation

- [Quick start](https://github.com/wsj-br/CursorPace/blob/master/QUICKSTART.md) — install, sign-in, daily use, tray, troubleshooting.
- [Development](https://github.com/wsj-br/CursorPace/blob/master/dev/DEVELOPMENT.md) — build, test, package, contribute.
- [README](https://github.com/wsj-br/CursorPace/blob/master/README.md) — product overview and source build.

---

## License

MIT © [Waldemar Scudeller Jr.](https://github.com/wsj-br/CursorPace)
