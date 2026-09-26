# Cursor Pace 0.3.1 Release Notes

## Highlights

- On Linux, startup no longer hangs at 100% CPU or crashes with a missing glyph typeface when the system default font is a WOFF file such as OpenDyslexic. The app ignores those fonts and uses bundled Inter.
- Launching a newer AppImage retargets **Launch at login** to that file, including when the previous version is still running and this process exits as the second instance.

## Why this release matters

Linux installs start reliably on desktops that ship WOFF system fonts, and an AppImage upgrade keeps login pointed at the new file instead of the old one.

## Detailed Changes

See [`dev/CHANGELOG.md`](https://github.com/wsj-br/CursorPace/blob/master/dev/CHANGELOG.md#031---2026-09-26) for the full list of changes in this release.

---

## Install

Download the package for your platform from this release:

- Windows: `CursorPace-0.3.1-win-x64-setup.exe`. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**. **Sign in** needs the Microsoft Edge WebView2 Runtime; the installer offers the download page if it is missing.
- Linux: `CursorPace-0.3.1-linux-x64.AppImage` or `CursorPace-0.3.1-linux-arm64.AppImage`. Make the file executable (`chmod +x`) before running it.
- macOS: `CursorPace-0.3.1-osx-arm64.zip` or `CursorPace-0.3.1-osx-x64.zip`. Unzip, then right-click `CursorPace.app` and choose **Open** the first time to bypass Gatekeeper (the build is unsigned).

---

## Documentation

- [Quick start](https://github.com/wsj-br/CursorPace/blob/master/QUICKSTART.md) — install, sign-in, daily use, tray, troubleshooting.
- [Development](https://github.com/wsj-br/CursorPace/blob/master/dev/DEVELOPMENT.md) — build, test, package, contribute.
- [README](https://github.com/wsj-br/CursorPace/blob/master/README.md) — product overview and source build.

---

## License

MIT © [Waldemar Scudeller Jr.](https://github.com/wsj-br/CursorPace)
