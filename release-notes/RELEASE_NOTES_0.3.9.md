# CursorPace 0.3.9 Release Notes

## Highlights

- On macOS, the app explains why it uses the local network: it connects to the configured sync server to synchronize usage samples and billing-cycle information. macOS shows that reason in the local-network permission dialog.

## Why this release matters

A sync server on the same network can be allowed on macOS, because the app now states why it needs local network access.

## Detailed Changes

See [`dev/CHANGELOG.md`](https://github.com/wsj-br/CursorPace/blob/master/dev/CHANGELOG.md#039---2026-11-22) for the full list of changes in this release.

---

## Install

Download the package for your platform from this release:

- Windows: `CursorPace-0.3.9-win-x64-setup.exe`. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**. **Sign in** needs the Microsoft Edge WebView2 Runtime; the installer offers the download page if it is missing.
- Linux: `CursorPace-0.3.9-linux-x64.AppImage` or `CursorPace-0.3.9-linux-arm64.AppImage`. Make the file executable (`chmod +x`) before running it. On Debian or Ubuntu, `CursorPace-0.3.9-linux-x64.deb` or `CursorPace-0.3.9-linux-arm64.deb` installs with `sudo apt install ./CursorPace-*.deb` and pulls GTK/WebKit dependencies.
- macOS: `CursorPace-0.3.9-osx-arm64.dmg` or `CursorPace-0.3.9-osx-x64.dmg`. Open the DMG and drag `CursorPace.app` to Applications, then right-click **Open** the first time to bypass Gatekeeper (the build is unsigned).

---

## Documentation

- [Quick start](https://github.com/wsj-br/CursorPace/blob/master/QUICKSTART.md) — install, sign-in, daily use, tray, troubleshooting.
- [Development](https://github.com/wsj-br/CursorPace/blob/master/dev/DEVELOPMENT.md) — build, test, package, contribute.
- [README](https://github.com/wsj-br/CursorPace/blob/master/README.md) — product overview and source build.

---

## License

MIT © [Waldemar Scudeller Jr.](https://github.com/wsj-br/CursorPace)
