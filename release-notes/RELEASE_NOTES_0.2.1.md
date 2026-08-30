# Cursor Pace 0.2.1 Release Notes

## Highlights

- The app is now **Cursor Pace** (`CursorPace`). Installer names, the Windows AppId and mutex, the local data folder, the Linux desktop id, and the macOS bundle id all use the new identity.
- Settings and the WebView sign-in profile are not migrated from Cursor Usage Progress. A new `%LocalAppData%\CursorPace` folder starts empty; sign in again after installing.
- Linux ARM64 AppImage packaging succeeds again: AppStream metadata is installed under the filename that matches its component id, so `appimagetool` validation no longer aborts that CI build.

## Why this release matters

This is the first release under the Cursor Pace name, with matching package identities, and it restores the Linux ARM64 AppImage that 0.2.0 CI could not publish.

## Detailed Changes

See [`dev/CHANGELOG.md`](https://github.com/wsj-br/CursorPace/blob/master/dev/CHANGELOG.md#021---2026-08-30) for the full list of changes in this release.

---

## Install

Download the package for your platform from this release:

- Windows: `CursorPace-0.2.1-win-x64-setup.exe`. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**. **Sign in** needs the Microsoft Edge WebView2 Runtime; the installer offers the download page if it is missing.
- Linux: `CursorPace-0.2.1-linux-x64.AppImage` or `CursorPace-0.2.1-linux-arm64.AppImage`. Make the file executable (`chmod +x`) before running it.
- macOS: `CursorPace-0.2.1-osx-arm64.zip` or `CursorPace-0.2.1-osx-x64.zip`. Unzip, then right-click the app and choose **Open** the first time to bypass Gatekeeper (the build is unsigned).

---

## Documentation

- [Quick start](https://github.com/wsj-br/CursorPace/blob/master/QUICKSTART.md) — install, sign-in, daily use, tray, troubleshooting.
- [Development](https://github.com/wsj-br/CursorPace/blob/master/dev/DEVELOPMENT.md) — build, test, package, contribute.
- [README](https://github.com/wsj-br/CursorPace/blob/master/README.md) — product overview and source build.

---

## License

MIT © [Waldemar Scudeller Jr.](https://github.com/wsj-br/CursorPace)
