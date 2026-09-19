# Cursor Pace 0.2.5 Release Notes

## Highlights

- Optional sync server in Settings (URL, API token, machine name, **Re-sync now**) shares usage samples and cycle bounds across machines on launch, after each new Cursor sample, every 10 minutes, and on demand.
- Billing-cycle history: a new Cursor cycle archives the previous bounds instead of clearing samples, and Previous/Next chevrons on the month heading page stored cycles in both the calendar and the chart.
- Resizable main window (minimum 760x787) with Maximize/Restore, title-bar double-click, and edge resize grips; size and maximized state restore with position, and calendar cells plus summary cards stretch to fill extra space. The title-bar **Quit** button is removed; exit from the tray menu instead.
- Linux reliability: the Cursor session persists across reboots, **Launch at login** uses the AppImage path so autostart actually relaunches, the taskbar shows the app icon instead of the generic gear, AppImageLauncher no longer starts a second copy at login, and a duplicate login process no longer crashes with `Dispatcher shut down`.
- macOS reliability: sign-in no longer crashes during `WKWebView` creation, the Dock icon hides while the window is hidden or minimized and returns before the window shows, the Dock shows the app icon instead of the generic Unix executable, and the tray menu no longer crashes on update.
- Polish: the chart heading and last-update line sit above the plot, the last top-axis date no longer clips at the right edge, tray **Open** raises a window already behind others, and About **Build date** includes the UTC compile time.

## Why this release matters

This release keeps quota history across machines and billing cycles, makes the main window resizable, and fixes the Linux sign-out-after-reboot and the macOS sign-in crash so staying signed in from the tray just works.

## Detailed Changes

See [`dev/CHANGELOG.md`](https://github.com/wsj-br/CursorPace/blob/master/dev/CHANGELOG.md#025---2026-09-19) for the full list of changes in this release.

---

## Install

Download the package for your platform from this release:

- Windows: `CursorPace-0.2.5-win-x64-setup.exe`. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**. **Sign in** needs the Microsoft Edge WebView2 Runtime; the installer offers the download page if it is missing.
- Linux: `CursorPace-0.2.5-linux-x64.AppImage` or `CursorPace-0.2.5-linux-arm64.AppImage`. Make the file executable (`chmod +x`) before running it.
- macOS: `CursorPace-0.2.5-osx-arm64.zip` or `CursorPace-0.2.5-osx-x64.zip`. Unzip, then right-click the app and choose **Open** the first time to bypass Gatekeeper (the build is unsigned).

---

## Documentation

- [Quick start](https://github.com/wsj-br/CursorPace/blob/master/QUICKSTART.md) — install, sign-in, daily use, tray, troubleshooting.
- [Development](https://github.com/wsj-br/CursorPace/blob/master/dev/DEVELOPMENT.md) — build, test, package, contribute.
- [README](https://github.com/wsj-br/CursorPace/blob/master/README.md) — product overview and source build.

---

## License

MIT © [Waldemar Scudeller Jr.](https://github.com/wsj-br/CursorPace)
