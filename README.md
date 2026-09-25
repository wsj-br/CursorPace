# Cursor Pace

<p align="center">
  <img src="Assets/cursor_pace.png" alt="Cursor Pace" width="180">
</p>

Desktop app for Windows, Linux, and macOS that tracks Cursor model quota across a billing cycle. Sign in with your Cursor account to pull usage automatically. The chart shows two independent percentages: **Cursor Models** and **Other Models**.

CSV expected percents follow each usage sample in time, then pace remaining quota to 100% at the next renewal. The chart's dashed **Expected usage** line is a straight linear pace from 0% at cycle start to 100% at next renewal. Thick solid Cursor and Other Models paths follow the last reading of each local day, or every sample on the 1-day and 2-day ranges. A separate Theil-Sen estimate projects daily burn and run-out.

Sign in uses an embedded native WebView (WebView2 on Windows, WKWebView on macOS, WebKitGTK or WPE on Linux) and your Cursor dashboard session. There is no official personal-plan API and no Team API key.

## Requirements

- Windows 10 or 11 x64, Linux x64 or ARM64, or macOS (Intel or Apple Silicon)
- For a Windows release install: the app payload is self-contained. **Sign in** also needs the [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (already on most Windows 11 PCs; the installer offers the download page if it is missing)
- Linux: WebKitGTK 4.1 (`libwebkit2gtk-4.1-0`; WPE may work depending on the Avalonia WebView build). GNOME tray icons may need the AppIndicator extension
- macOS: WKWebView (built in). Releases are unsigned; Gatekeeper may require **Open Anyway** in **System Settings → Privacy & Security**
- For building from source: [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Install

1. Download the build for your platform from this repository's Releases page:
   - **Windows**: `CursorPace-*-win-x64-setup.exe`
   - **Linux**: `CursorPace-*-linux-x64.AppImage` (x86_64) or `*-linux-arm64.AppImage` (ARM64)
   - **macOS**: `CursorPace-*-osx-arm64.zip` (Apple Silicon) or `*-osx-x64.zip` (Intel)
2. **Windows**: run the installer. If SmartScreen warns that the app is unsigned, choose **More info**, then **Run anyway**. If WebView2 Runtime is missing, open the download page the installer offers.
3. **Linux**: make the AppImage executable (`chmod +x`), then run it. First launch may take a moment while the bundle extracts.
4. **macOS**: unzip the archive, move `CursorPace.app` to Applications, then open it. If Gatekeeper blocks the unsigned app, attempt to open it once and then choose **Open Anyway** in **System Settings → Privacy & Security**.
5. Sign in to Cursor. If **Start in notification tray** is on (the default), open the window from the tray icon first, or launch with `--show`.

See [QUICKSTART.md](QUICKSTART.md) for first-run setup, Cursor account sign-in, the usage chart, tray behavior, and troubleshooting.

## Features

- Sign in to Cursor from the empty state or Settings; optional automatic updates on clock-aligned 1, 2, 4, 6, or 12 hour intervals
- Billing cycle start and next renewal come from Cursor
- Usage chart for the current cycle, with Previous/Next controls to open stored earlier cycles. Summary cards show cycle start and next renewal, both run-out times, and the latest Cursor, Other Models, and Expected usage readings. When a sync server is configured, the status line under the cycle heading shows **Sync server** and the last successful server sync time, with a red dot after a failed attempt. The Cursor time gets a red dot when the account is signed out or the latest refresh failed. Clicking the Cursor time opens **Cursor account**. Clicking the sync-server status opens **Sync Server**
- Separate **Cursor Models** and **Other Models** percentages
- Chart ranges are **1D**, **2D**, **7D**, **1W**, **2W**, and **1M**. **1M** is the whole displayed cycle. Shorter ranges end at the current time on the live cycle and at renewal on an earlier cycle, and never start before the cycle start. Drag across the plot to zoom to that interval; right-click returns to **1M**, and choosing a range button shows that range. The Y axis fits the values in view, rounded out to 10% steps. **Settings** → **Startup & Display** → **Every sample up to** chooses **2D**, **4D**, **7D**, or **14D** (default **14D**). Changing that setting preserves an active drag zoom when you return to the chart. Intervals up to that length, including a drag zoom, mark each measured Cursor and Other Models sample, and the hour grid widens so the time labels do not overlap. Moving the pointer draws a vertical guide and shows Cursor, Other Models, and Expected usage for that time. While a drag zoom is active, the readout also shows each value at the start of that zoom and the change from there
- Custom title bar shows the app name on the left and keeps Refresh, Settings, Minimize, Maximize, and Close in separate, right-aligned controls. **Refresh** runs the same Cursor fetch as Settings **Refresh now**
- System tray: closing the window hides it; **Quit** on the tray menu exits. The tooltip shows today's expected percent and the projected percent at renewal. On macOS the Dock icon is removed after the window hides and while it is minimized; open the window from the tray icon, `--show`, or a second launch
- Optional launch at login (Windows Run key, macOS `SMAppService` / attributed Launch Agent fallback, Linux XDG autostart)
- Single-instance: a second launch brings the existing window forward
- Settings uses top tabs: **Startup & Display** (startup, theme, and how long the chart plots every sample), **Cursor account** (Cursor account and refresh interval), **Sync Server** (URL, API token, machine name, **Re-sync now**, and a link to the [CursorPace Sync Server](https://github.com/wsj-br/CursorPace-SyncServer) install instructions or repository), **Export & Backup** (CSV export, backup or restore of settings plus usage samples as a zip file, **Open Folder** after local saves), and **About** (version, UTC build date and time, copyright, MIT license, and a GitHub link). Opening Settings again returns to the last tab, including after a restart
- Remembers window size, position, and maximized state; informational labels can be selected and copied
- Theme: follow the system, or force light or dark

## Build from source

On Windows (PowerShell) or Linux/macOS (bash):

```bash
dotnet restore
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
./scripts/build.sh             # Linux: AppImage; macOS: zipped .app bundle
```

Publish only (skip packaging): add `--skip-installer` / `-SkipInstaller`.

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
