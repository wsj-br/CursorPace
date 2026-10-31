# Cursor Pace

![Cursor Pace](Assets/cursor_pace.png)

Desktop app for Windows, Linux, and macOS that tracks Cursor model quota across a billing cycle. Sign in with your Cursor account to pull usage automatically. The chart shows two independent percentages: **Cursor Models** and **Other Models**.

The dashed **Expected usage** line is a straight pace from 0% at cycle start to 100% at the next renewal. Cursor and Other Models follow your samples. A separate estimate projects daily burn and run-out. [QUICKSTART.md](QUICKSTART.md) covers the chart, the tray, and settings.

Sign in uses an embedded native WebView (WebView2 on Windows, WKWebView on macOS, WebKitGTK on Linux) and your Cursor dashboard session. There is no official personal-plan API and no Team API key.

## Requirements

- Windows 10 or 11 x64, Linux x64 or ARM64, or macOS (Intel or Apple Silicon)
- For a Windows release install: the app payload is self-contained. **Sign in** also needs the [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (already on most Windows 11 PCs; the installer offers the download page if it is missing)
- Linux: WebKitGTK 4.1 (`libwebkit2gtk-4.1-0`; WPE may work depending on the Avalonia WebView build). GNOME tray icons may need the AppIndicator extension. The app uses bundled Inter and ignores system `.woff` fonts so a WOFF default face cannot hang or crash startup.
- macOS: WKWebView (built in). Releases are unsigned; Gatekeeper may require **Open Anyway** in **System Settings → Privacy & Security**
- For building from source: [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Install

1. Download the build for your platform from this repository's Releases page:
   - **Windows**: `CursorPace-*-win-x64-setup.exe`
   - **Linux**: `CursorPace-*-linux-x64.AppImage` or `*-linux-x64.deb` (x86_64), or the matching `linux-arm64` files (ARM64)
   - **macOS**: `CursorPace-*-osx-arm64.dmg` (Apple Silicon) or `*-osx-x64.dmg` (Intel)
2. **Windows**: run the installer. If SmartScreen warns that the app is unsigned, choose **More info**, then **Run anyway**. If WebView2 Runtime is missing, open the download page the installer offers.
3. **Linux**: make the AppImage executable (`chmod +x`), then run it. First launch may take a moment while the bundle extracts. On Debian or Ubuntu, `sudo apt install ./CursorPace-*.deb` installs the package and pulls GTK/WebKit dependencies.
4. **macOS**: open the DMG, drag `CursorPace.app` to Applications, then open it. If Gatekeeper blocks the unsigned app, right-click **Open** once, then choose **Open Anyway** in **System Settings → Privacy & Security**.
5. Sign in to Cursor. If **Start in notification tray** is on (the default), open the window from the tray icon first, or launch with `--show`.

See [QUICKSTART.md](QUICKSTART.md) for first-run setup, Cursor account sign-in, the usage chart, tray behavior, and troubleshooting.

## Features

- Sign in to Cursor and refresh usage on demand, or automatically every 1, 2, 4, 6, or 12 hours
- Billing cycle start and next renewal come from Cursor. Previous and Next open earlier stored cycles
- Summary cards for the cycle dates, each model's forecast, and the latest reading versus the expected pace
- Chart ranges **1D**, **2D**, **7D**, **1W**, **2W**, and **1M**, with drag-to-zoom and a pointer readout
- Closes to the notification tray. Optional launch at login. A second launch opens the window that is already running
- Light, dark, or system theme. The window remembers its size, position, and maximized state
- Export usage as CSV, and back up or restore settings and samples
- **Optional.** [CursorPace Sync Server](https://github.com/wsj-br/CursorPace-SyncServer) keeps Cursor Pace installs on a local network synchronized. It shares usage samples and billing-cycle bounds, so a Mac, a Windows PC, and a Linux machine in the same development environment show the same chart. Install and run the server from that repository.

## Build from source

On Windows (PowerShell) or Linux/macOS (bash):

```bash
dotnet restore[cursor.com
dotnet build
dotnet test ./Tests/CursorPace.Tests.csproj
dotnet run --project ./CursorPace.csproj
```

Or use the maintainer scripts: `.\scripts\dev.ps1` / `./scripts/dev.sh`. These scripts show the window by default; use `.\scripts\dev.ps1 -NoShow` or `./scripts/dev.sh --no-show` to use the app's normal startup visibility.

Self-contained publish (keep `PublishSingleFile=false`):

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r linux-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
```

Linux sign-in needs WebKitGTK 4.1 installed on the machine that runs the published binary.
A background usage refresh on Linux uses a 1x1 transparent WebView host so it does not
flash the sign-in window; macOS and Windows keep that host at login size off-screen.

The project carries a small compatibility build of Avalonia.Controls.WebView 12.1.0 under
`vendor/` for macOS `CGRect`/`CGSize` ABI correctness. No separate macOS WebView runtime is
required.

Release packaging:

```powershell
.\scripts\build.ps1          # Windows: Inno Setup installer
```

```bash
./scripts/build.sh             # Linux: AppImage and .deb; macOS: DMG
```

Publish only (skip packaging): add `--skip-installer` / `-SkipInstaller`.

Full contributor workflow is in [dev/DEVELOPMENT.md](dev/DEVELOPMENT.md).

## Documentation

| Document | Audience |
| --- | --- |
| [QUICKSTART.md](QUICKSTART.md) | Install, sign-in, daily use, troubleshooting |
| [dev/DEVELOPMENT.md](dev/DEVELOPMENT.md) | Build, test, run, package, contribute |
| [dev/CHANGELOG.md](dev/CHANGELOG.md) | User-visible changes |

## Disclaimer

[Cursor](https://cursor.com/home) is a product of Anysphere, Inc. Cursor Pace is an independent project and is not associated with or endorsed by Cursor or Anysphere.

## License

Copyright 2026 Waldemar Scudeller Jr.

This software is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.


