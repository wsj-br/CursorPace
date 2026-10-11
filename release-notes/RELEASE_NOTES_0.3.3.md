# Cursor Pace 0.3.3 Release Notes

## Highlights

- The last-sample percent labels sit just above or below their point, with a short tick tying each label to the point, so they stay off the curve when values are close or the point is at the right edge of a zoom.
- Estimated Cursor and Other Models lines, and their legend swatches, are round dots instead of solid thin, distinct from the dashed Expected usage line.
- Linux releases ship Debian packages (`CursorPace-0.3.3-linux-x64.deb` and `*-linux-arm64.deb`) alongside the AppImage, so Debian and Ubuntu users can install with `apt` and pull GTK/WebKit dependencies automatically.
- macOS releases ship a drag-to-Applications DMG (`CursorPace-0.3.3-osx-arm64.dmg` / `*-osx-x64.dmg`) instead of a zip of `CursorPace.app`.

## Why this release matters

Small chart readability fixes: sample labels no longer sit on the measured lines, and estimated lines are visually distinct from the Expected usage line. Installation is also simpler: Debian-based Linux gets native packages, and macOS gets a standard DMG.

## Detailed Changes

See [`dev/CHANGELOG.md`](https://github.com/wsj-br/CursorPace/blob/master/dev/CHANGELOG.md#033---2026-10-11) for the full list of changes in this release.

---

## Install

Download the package for your platform from this release:

- Windows: `CursorPace-0.3.3-win-x64-setup.exe`. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**. **Sign in** needs the Microsoft Edge WebView2 Runtime; the installer offers the download page if it is missing.
- Linux: `CursorPace-0.3.3-linux-x64.AppImage` or `CursorPace-0.3.3-linux-arm64.AppImage`. Make the file executable (`chmod +x`) before running it. On Debian or Ubuntu, `CursorPace-0.3.3-linux-x64.deb` or `CursorPace-0.3.3-linux-arm64.deb` installs with `sudo apt install ./CursorPace-*.deb` and pulls GTK/WebKit dependencies.
- macOS: `CursorPace-0.3.3-osx-arm64.dmg` or `CursorPace-0.3.3-osx-x64.dmg`. Open the DMG and drag `CursorPace.app` to Applications, then right-click **Open** the first time to bypass Gatekeeper (the build is unsigned).

---

## Documentation

- [Quick start](https://github.com/wsj-br/CursorPace/blob/master/QUICKSTART.md) — install, sign-in, daily use, tray, troubleshooting.
- [Development](https://github.com/wsj-br/CursorPace/blob/master/dev/DEVELOPMENT.md) — build, test, package, contribute.
- [README](https://github.com/wsj-br/CursorPace/blob/master/README.md) — product overview and source build.

---

## License

MIT © [Waldemar Scudeller Jr.](https://github.com/wsj-br/CursorPace)
