# Cursor Usage Progress 0.2.0 Release Notes

## Highlights

- Cross-platform: the WinUI 3 UI is replaced with Avalonia 12, so the same app now builds and runs on Windows, Linux, and macOS.
- New packaging: Linux AppImage (x64 and ARM64) and a macOS `.app` bundle, alongside the existing Windows installer.
- New `Backup` / `Restore` in Settings, writing a zip of `settings.json` and `usage-samples.json` (the Cursor session itself is never included).
- New `Launch at login` support on macOS (Launch Agent) and Linux (XDG autostart), in addition to Windows.
- More reliable sign-in/sync: `Sign out` now deletes only `cursor.com` cookies where the platform supports it (keeping any Google/GitHub session in the same profile), a dedicated WebView profile for AppImage runs avoids WebKitGTK cookie-database contention, and the app no longer mistakes a merely non-empty WebView profile folder for a signed-in session.
- Sturdier persistence and sync: transient file-read errors no longer overwrite `settings.json` or `usage-samples.json`, and timer/command exceptions no longer crash the process.
- Calendar, chart, and dialog polish: selectable/copyable text, theme-aware colors, clearer sync alert banner, and chart axis labels that match the calendar's day-of-month view.
- CI/release overhaul: matrix Linux builds (x64/arm64), locked NuGet restores with Dependabot, and a release workflow that builds and verifies every platform before publishing.

## Why this release matters

This is the first release that runs on Linux and macOS in addition to Windows, backed by a safer sign-in/sync model and a more resilient release pipeline.

## Detailed Changes

See [`dev/CHANGELOG.md`](https://github.com/wsj-br/CursorPace/blob/master/dev/CHANGELOG.md#020---2026-08-30) for the full list of changes in this release.

---

## Install

Download the package for your platform from this release:

- Windows: `CursorUsageProgress-0.2.0-win-x64-setup.exe`. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**. **Sign in** needs the Microsoft Edge WebView2 Runtime; the installer offers the download page if it is missing.
- Linux: `CursorUsageProgress-0.2.0-linux-x64.AppImage` or `CursorUsageProgress-0.2.0-linux-arm64.AppImage`. Make the file executable (`chmod +x`) before running it.
- macOS: `CursorUsageProgress-0.2.0-osx-arm64.zip` or `CursorUsageProgress-0.2.0-osx-x64.zip`. Unzip, then right-click the app and choose **Open** the first time to bypass Gatekeeper (the build is unsigned).

---

## Documentation

- [Quick start](https://github.com/wsj-br/CursorUsageProgress/blob/master/QUICKSTART.md) — install, sign-in, daily use, tray, troubleshooting.
- [Development](https://github.com/wsj-br/CursorUsageProgress/blob/master/dev/DEVELOPMENT.md) — build, test, package, contribute.
- [README](https://github.com/wsj-br/CursorUsageProgress/blob/master/README.md) — product overview and source build.

---

## License

MIT © [Waldemar Scudeller Jr.](https://github.com/wsj-br/CursorUsageProgress)
