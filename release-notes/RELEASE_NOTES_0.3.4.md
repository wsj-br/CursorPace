# Cursor Pace 0.3.4 Release Notes

## Highlights

- When sample circles are visible, the hover guide sticks to a nearby sample time and follows the pointer again once you move clear of it.
- Past the last measured sample, the hover readout shows the estimated Cursor and Other percents, labelled `estimated`, instead of a dash.
- When a model will not run out this cycle, its card shows when the dashed expected line catches a reading that is above that line, with the wait beside that date, or the projected percent at renewal when the reading is on or below the line. A run-out date inside the cycle stays the headline.
- The last-measure card shows how far Cursor and Other are from linear Expected usage, with an arrow and a signed percent.

## Why this release matters

The chart hover now lands on real samples and keeps reading into the estimate after the last measurement. The summary cards also show whether each quota is ahead of or behind a linear pace, and what happens when it will not run out this cycle.

## Detailed Changes

See [`dev/CHANGELOG.md`](https://github.com/wsj-br/CursorPace/blob/master/dev/CHANGELOG.md#034---2026-10-15) for the full list of changes in this release.

---

## Install

Download the package for your platform from this release:

- Windows: `CursorPace-0.3.4-win-x64-setup.exe`. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**. **Sign in** needs the Microsoft Edge WebView2 Runtime; the installer offers the download page if it is missing.
- Linux: `CursorPace-0.3.4-linux-x64.AppImage` or `CursorPace-0.3.4-linux-arm64.AppImage`. Make the file executable (`chmod +x`) before running it. On Debian or Ubuntu, `CursorPace-0.3.4-linux-x64.deb` or `CursorPace-0.3.4-linux-arm64.deb` installs with `sudo apt install ./CursorPace-*.deb` and pulls GTK/WebKit dependencies.
- macOS: `CursorPace-0.3.4-osx-arm64.dmg` or `CursorPace-0.3.4-osx-x64.dmg`. Open the DMG and drag `CursorPace.app` to Applications, then right-click **Open** the first time to bypass Gatekeeper (the build is unsigned).

---

## Documentation

- [Quick start](https://github.com/wsj-br/CursorPace/blob/master/QUICKSTART.md) — install, sign-in, daily use, tray, troubleshooting.
- [Development](https://github.com/wsj-br/CursorPace/blob/master/dev/DEVELOPMENT.md) — build, test, package, contribute.
- [README](https://github.com/wsj-br/CursorPace/blob/master/README.md) — product overview and source build.

---

## License

MIT © [Waldemar Scudeller Jr.](https://github.com/wsj-br/CursorPace)
