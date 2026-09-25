# Cursor Pace 0.3.0 Release Notes

## Highlights

- The main window is the usage chart. The calendar view and calendar/chart toggle are gone. Cycle day rows remain in CSV export and the tray tooltip.
- Range buttons cover **1D**, **2D**, **7D**, **1W**, **2W**, and **1M** (the displayed cycle). Drag across the plot to zoom; right-click returns to **1M**. Hover shows Cursor, Other Models, and Expected usage at that time, and a zoom also shows the start values and the change from that start.
- Short ranges and short zooms can plot every sample, with a small circle on each measured point. **Settings** → **Startup & Display** → **Every sample up to** chooses **2D**, **4D**, or **7D** (default **4D**). Longer ranges keep the last sample of each local day.
- Cycle start and next renewal share one card. The two run-out times share another. A last-measure card shows the newest in-cycle sample time, Cursor %, Other %, and linear Expected usage %.
- Settings tabs are **Startup & Display**, **Cursor account**, **Sync Server**, **Export & Backup**, and **About**. A new install opens on **Startup & Display**. The last tab is restored the next time Settings opens, including after a restart.
- When a sync server is configured, the status line under the cycle heading shows **Sync server** and the last successful sync time. Click the Cursor time or the sync-server status to open that Settings tab. A red dot marks a failed Cursor refresh, a signed-out account, or a failed server sync.

## Why this release matters

The quota calendar is gone. The main window is a chart you can range and zoom, with cycle cards and sync status that match how people actually check usage.

## Detailed Changes

See [`dev/CHANGELOG.md`](https://github.com/wsj-br/CursorPace/blob/master/dev/CHANGELOG.md#030---2026-09-25) for the full list of changes in this release.

---

## Install

Download the package for your platform from this release:

- Windows: `CursorPace-0.3.0-win-x64-setup.exe`. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**. **Sign in** needs the Microsoft Edge WebView2 Runtime; the installer offers the download page if it is missing.
- Linux: `CursorPace-0.3.0-linux-x64.AppImage` or `CursorPace-0.3.0-linux-arm64.AppImage`. Make the file executable (`chmod +x`) before running it.
- macOS: `CursorPace-0.3.0-osx-arm64.zip` or `CursorPace-0.3.0-osx-x64.zip`. Unzip, then right-click `CursorPace.app` and choose **Open** the first time to bypass Gatekeeper (the build is unsigned).

---

## Documentation

- [Quick start](https://github.com/wsj-br/CursorPace/blob/master/QUICKSTART.md) — install, sign-in, daily use, tray, troubleshooting.
- [Development](https://github.com/wsj-br/CursorPace/blob/master/dev/DEVELOPMENT.md) — build, test, package, contribute.
- [README](https://github.com/wsj-br/CursorPace/blob/master/README.md) — product overview and source build.

---

## License

MIT © [Waldemar Scudeller Jr.](https://github.com/wsj-br/CursorPace)
