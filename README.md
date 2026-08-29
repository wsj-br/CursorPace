# Cursor Usage Progress

<p align="center">
  <img src="Assets/cursor_usage_progress.png" alt="Cursor Usage Progress" width="180">
</p>

Desktop app for Windows, Linux, and macOS that tracks Cursor model quota across a billing cycle. Sign in with your Cursor account to pull usage automatically. The calendar and chart show two independent percentages: **Cursor Models** and **Other Models**.

Expected percents follow each usage sample in time, then pace remaining quota to 100% at the next renewal. A separate Theil-Sen estimate projects daily burn and run-out.

Sign in uses an embedded native WebView (WebView2 on Windows, WKWebView on macOS, WebKitGTK or WPE on Linux) and your Cursor dashboard session. There is no official personal-plan API and no Team API key.

## Requirements

- Windows 10 or 11 x64, Linux x64, or macOS (Intel or Apple Silicon)
- For a Windows release install: the app payload is self-contained. **Sign in** also needs the [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (already on most Windows 11 PCs; the installer offers the download page if it is missing)
- Linux: WebKitGTK 4.1 (`libwebkit2gtk-4.1-0`; WPE may work depending on the Avalonia WebView build). GNOME tray icons may need the AppIndicator extension
- macOS: WKWebView (built in). Unsigned builds may need **Open** from Finder after Gatekeeper
- For building from source: [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Install

1. Download `CursorUsageProgress-*-win-x64-setup.exe` from this repository's Releases page.
2. Run the installer. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**.
3. After setup, the app launches and asks you to sign in to Cursor.

Linux and macOS have no installer yet. See [Build from source](#build-from-source) and [QUICKSTART.md](QUICKSTART.md) for `dotnet publish` commands.

See [QUICKSTART.md](QUICKSTART.md) for first-run setup, Cursor account sign-in, the calendar and chart, tray behavior, and troubleshooting.

## Features

- Sign in to Cursor from the empty state or Settings; optional automatic updates on clock-aligned 1, 2, 4, 6, or 12 hour intervals
- Billing cycle start and next renewal come from Cursor
- Calendar or chart for the current cycle, with today, renewal, and projected run-out days highlighted. Calendar left is the day's last sample when one exists, otherwise the interpolated expected percent; estimated on the right appears only after the last sample date (green ≤100%, red >100%)
- Separate **Cursor Models** and **Other Models** percentages
- Chart axis runs from cycle start to next renewal in elapsed seconds; midnight ticks are day markers, and labels use the day of the month (the truncated first slot is unlabeled)
- Fixed-size custom title bar shows the app name on the left and keeps Settings, Quit, Minimize, and Close in separate, right-aligned controls
- System tray: closing the window hides it; **Quit** exits. The tooltip shows today's expected percent and the projected percent at renewal
- Optional launch at login (Windows Run key, macOS Launch Agent, Linux XDG autostart)
- Single-instance: a second launch brings the existing window forward
- Settings: Cursor account, startup, timestamped CSV exports, backup or restore of settings plus usage samples as a zip file, and an **Open Folder** action on the left after local saves
- Remembers window position; informational labels can be selected and copied
- Follows the system light or dark theme

## Build from source

On Windows (PowerShell) or Linux/macOS (same `dotnet` commands in your shell):

```powershell
dotnet restore
dotnet build
dotnet test .\Tests\CursorUsageProgress.Tests.csproj
dotnet run --project .\CursorUsageProgress.csproj
```

Self-contained publish (keep `PublishSingleFile=false`):

```powershell
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
```

Linux sign-in needs WebKitGTK 4.1 installed on the machine that runs the published binary.

Release installer (Windows only; needs [Inno Setup 6](https://jrsoftware.org/isdl.php)):

```powershell
.\scripts\build.ps1
```

Full contributor workflow is in [dev/DEVELOPMENT.md](dev/DEVELOPMENT.md).

## Documentation

| Document | Audience |
| --- | --- |
| [QUICKSTART.md](QUICKSTART.md) | Install, sign-in, daily use, troubleshooting |
| [dev/DEVELOPMENT.md](dev/DEVELOPMENT.md) | Build, test, run, package, contribute |
| [dev/CHANGELOG.md](dev/CHANGELOG.md) | User-visible changes |

## License

Copyright 2026 Waldemar Scudeller Jr.

This software is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
