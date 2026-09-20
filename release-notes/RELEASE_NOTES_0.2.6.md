# Cursor Pace 0.2.6 Release Notes

## Highlights

- The title bar now has **Refresh** next to Settings, so you can fetch Cursor usage without opening Settings.
- macOS **Launch at login** opens `CursorPace.app` as a GUI app, so login gets a tray icon and can hide the Dock. Previously it launched the inner binary as a background item.
- Signed macOS builds register under **Open at Login**, so System Settings shows `CursorPace` with its app icon. Unsigned and older-system fallbacks still identify as CursorPace instead of `/usr/bin/open`.
- The macOS bundle is now `CursorPace.app`. Finder and the release zip no longer use `Cursor Pace.app`.

## Why this release matters

macOS launch-at-login now starts a real GUI app with a tray icon after reboot, and Refresh in the title bar makes a manual Cursor fetch one click.

## Detailed Changes

See [`dev/CHANGELOG.md`](https://github.com/wsj-br/CursorPace/blob/master/dev/CHANGELOG.md#026---2026-09-20) for the full list of changes in this release.

---

## Install

Download the package for your platform from this release:

- Windows: `CursorPace-0.2.6-win-x64-setup.exe`. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**. **Sign in** needs the Microsoft Edge WebView2 Runtime; the installer offers the download page if it is missing.
- Linux: `CursorPace-0.2.6-linux-x64.AppImage` or `CursorPace-0.2.6-linux-arm64.AppImage`. Make the file executable (`chmod +x`) before running it.
- macOS: `CursorPace-0.2.6-osx-arm64.zip` or `CursorPace-0.2.6-osx-x64.zip`. Unzip, then right-click `CursorPace.app` and choose **Open** the first time to bypass Gatekeeper (the build is unsigned).

---

## Documentation

- [Quick start](https://github.com/wsj-br/CursorPace/blob/master/QUICKSTART.md) — install, sign-in, daily use, tray, troubleshooting.
- [Development](https://github.com/wsj-br/CursorPace/blob/master/dev/DEVELOPMENT.md) — build, test, package, contribute.
- [README](https://github.com/wsj-br/CursorPace/blob/master/README.md) — product overview and source build.

---

## License

MIT © [Waldemar Scudeller Jr.](https://github.com/wsj-br/CursorPace)
