# CursorPace 0.3.8 Release Notes

## Highlights

- **Settings** → **Startup & Display** → **Language** on an installed build lists every locale. The menu reads the language list already packed into the app.

## Why this release matters

An installed copy now offers the full language list in Settings, the same set a source build already showed.

## Detailed Changes

See [`dev/CHANGELOG.md`](https://github.com/wsj-br/CursorPace/blob/master/dev/CHANGELOG.md#038---2026-11-02) for the full list of changes in this release.

---

## Install

Download the package for your platform from this release:

- Windows: `CursorPace-0.3.8-win-x64-setup.exe`. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**. **Sign in** needs the Microsoft Edge WebView2 Runtime; the installer offers the download page if it is missing.
- Linux: `CursorPace-0.3.8-linux-x64.AppImage` or `CursorPace-0.3.8-linux-arm64.AppImage`. Make the file executable (`chmod +x`) before running it. On Debian or Ubuntu, `CursorPace-0.3.8-linux-x64.deb` or `CursorPace-0.3.8-linux-arm64.deb` installs with `sudo apt install ./CursorPace-*.deb` and pulls GTK/WebKit dependencies.
- macOS: `CursorPace-0.3.8-osx-arm64.dmg` or `CursorPace-0.3.8-osx-x64.dmg`. Open the DMG and drag `CursorPace.app` to Applications, then right-click **Open** the first time to bypass Gatekeeper (the build is unsigned).

---

## Documentation

- [Quick start](https://github.com/wsj-br/CursorPace/blob/master/QUICKSTART.md) — install, sign-in, daily use, tray, troubleshooting.
- [Development](https://github.com/wsj-br/CursorPace/blob/master/dev/DEVELOPMENT.md) — build, test, package, contribute.
- [README](https://github.com/wsj-br/CursorPace/blob/master/README.md) — product overview and source build.

---

## License

MIT © [Waldemar Scudeller Jr.](https://github.com/wsj-br/CursorPace)
