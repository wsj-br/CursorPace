# Cursor Usage Progress

Windows desktop app that tracks Cursor model quota across a billing cycle. Sign in with your Cursor account to pull usage automatically, or pin values by hand. The calendar and chart show two independent percentages: **Cursor Models** and **Other Models**.

Expected percents follow the last sample or manual edit, then pace remaining quota to 100% at the next renewal. A separate Theil-Sen estimate projects daily burn and run-out.

Sign in uses an embedded Microsoft Edge WebView2 window and your Cursor dashboard session. There is no official personal-plan API and no Team API key.

## Requirements

- Windows 10 or 11, x64
- For a release install: the app payload is self-contained. **Sign in** also needs the [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (already on most Windows 11 PCs; the installer offers the download page if it is missing)
- For building from source: [.NET 10 SDK](https://dotnet.microsoft.com/download) and Windows 10 SDK 10.0.19041 or later

## Install

1. Download `CursorUsageProgress-*-win-x64-setup.exe` from this repository's Releases page.
2. Run the installer. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**.
3. After setup, the app launches and asks for your renewal day (1-31). You can sign in to Cursor later from **Settings**.

See [QUICKSTART.md](QUICKSTART.md) for first-run setup, sign-in, the calendar and chart, tray behavior, and troubleshooting.

## Features

- Sign in to Cursor from Settings; optional automatic updates on clock-aligned 1, 2, 4, 6, or 12 hour intervals
- While signed in, the billing cycle comes from Cursor; calendar edits, **Reset**, and **Change renewal day** are unavailable
- Calendar or chart for the current cycle, with today, renewal, and projected run-out days highlighted
- Separate **Cursor Models** and **Other Models** percentages
- When signed out, manual day edits pin expected percents; Theil-Sen estimates daily usage and run-out
- System tray: closing the window hides it; **Quit** exits. The tooltip shows today's expected percent and end-of-period (`EOP`) projection
- Optional start at Windows sign-in (user-level, no elevation)
- Single-instance: a second launch brings the existing window forward
- Settings: Cursor account, startup, CSV export of the cycle and (when connected) collected samples
- Remembers window position; informational labels can be selected and copied
- Follows Windows light, dark, and high-contrast themes

## Build from source

```powershell
dotnet restore
dotnet build
dotnet test .\Tests\CursorUsageProgress.Tests.csproj
dotnet run --project .\CursorUsageProgress.csproj
```

Release installer (needs [Inno Setup 6](https://jrsoftware.org/isdl.php)):

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
