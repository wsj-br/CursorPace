# Cursor Pace 0.2.3 Release Notes

## Highlights

- The usage chart now draws thicker solid Cursor and Other Models lines through the last sample of each day, with no sample markers. A single dashed **Expected usage** line runs from 0% at cycle start to 100% at next renewal. Calendar and CSV expected values are unchanged.
- **Start in notification tray** and `--background` keep the main window hidden on launch. `--show` forces it open and overrides both.
- Settings includes an About card with version, UTC build date, copyright, MIT license, and a link to the GitHub repository.
- On Linux, the window applies its saved position before it is shown, so it no longer flashes at the top-left corner and then jumps.

## Why this release matters

The chart is easier to read at a glance, tray launch actually stays in the tray, and Linux no longer flashes the window in the wrong place before restoring the last position.

## Detailed Changes

See [`dev/CHANGELOG.md`](https://github.com/wsj-br/CursorPace/blob/master/dev/CHANGELOG.md#023---2026-09-07) for the full list of changes in this release.

---

## Install

Download the package for your platform from this release:

- Windows: `CursorPace-0.2.3-win-x64-setup.exe`. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**. **Sign in** needs the Microsoft Edge WebView2 Runtime; the installer offers the download page if it is missing.
- Linux: `CursorPace-0.2.3-linux-x64.AppImage` or `CursorPace-0.2.3-linux-arm64.AppImage`. Make the file executable (`chmod +x`) before running it.
- macOS: `CursorPace-0.2.3-osx-arm64.zip` or `CursorPace-0.2.3-osx-x64.zip`. Unzip, then right-click the app and choose **Open** the first time to bypass Gatekeeper (the build is unsigned).

---

## Documentation

- [Quick start](https://github.com/wsj-br/CursorPace/blob/master/QUICKSTART.md) — install, sign-in, daily use, tray, troubleshooting.
- [Development](https://github.com/wsj-br/CursorPace/blob/master/dev/DEVELOPMENT.md) — build, test, package, contribute.
- [README](https://github.com/wsj-br/CursorPace/blob/master/README.md) — product overview and source build.

---

## License

MIT © [Waldemar Scudeller Jr.](https://github.com/wsj-br/CursorPace)
