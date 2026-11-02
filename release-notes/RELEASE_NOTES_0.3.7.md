# CursorPace 0.3.7 Release Notes

## Highlights

- **Settings** → **Startup & Display** has a **Language** menu. The choice is stored as `uiLocale` and comes from `locales/ui-languages.json`. If that file is missing, the menu offers English only.
- Choosing a language no longer crashes. A combo box selection was bubbling to the tab control and replacing the theme list while that list was still updating.
- The English README and Quick start link to machine translations under `translated-docs/`.
- The source build steps start with `dotnet restore` again.

## Why this release matters

You can switch the app language from Settings, and the README and Quick start are available in the same translated set. Picking a language no longer takes the Settings page down with it.

## Detailed Changes

See [`dev/CHANGELOG.md`](https://github.com/wsj-br/CursorPace/blob/master/dev/CHANGELOG.md#037---2026-11-02) for the full list of changes in this release.

---

## Install

Download the package for your platform from this release:

- Windows: `CursorPace-0.3.7-win-x64-setup.exe`. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**. **Sign in** needs the Microsoft Edge WebView2 Runtime; the installer offers the download page if it is missing.
- Linux: `CursorPace-0.3.7-linux-x64.AppImage` or `CursorPace-0.3.7-linux-arm64.AppImage`. Make the file executable (`chmod +x`) before running it. On Debian or Ubuntu, `CursorPace-0.3.7-linux-x64.deb` or `CursorPace-0.3.7-linux-arm64.deb` installs with `sudo apt install ./CursorPace-*.deb` and pulls GTK/WebKit dependencies.
- macOS: `CursorPace-0.3.7-osx-arm64.dmg` or `CursorPace-0.3.7-osx-x64.dmg`. Open the DMG and drag `CursorPace.app` to Applications, then right-click **Open** the first time to bypass Gatekeeper (the build is unsigned).

---

## Documentation

- [Quick start](https://github.com/wsj-br/CursorPace/blob/master/QUICKSTART.md) — install, sign-in, daily use, tray, troubleshooting.
- [Development](https://github.com/wsj-br/CursorPace/blob/master/dev/DEVELOPMENT.md) — build, test, package, contribute.
- [README](https://github.com/wsj-br/CursorPace/blob/master/README.md) — product overview and source build.

---

## License

MIT © [Waldemar Scudeller Jr.](https://github.com/wsj-br/CursorPace)
