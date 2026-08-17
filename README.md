# Cursor Quota Progress

Windows desktop app that plans Cursor model quota across a monthly renewal cycle. It spreads each cycle from 0% on renewal day to 100% at the next renewal, and lets you pin manual checkpoints so the rest of the calendar interpolates between them.

This is a local planner. It does not read Cursor usage or call any Cursor API.

## Requirements

- Windows 10 or 11, x64
- For a release install: no extra runtime (the installer is self-contained)
- For building from source: [.NET 10 SDK](https://dotnet.microsoft.com/download) and Windows 10 SDK 10.0.19041 or later

## Install

1. Download `CursorQuotaProgress-*-win-x64-setup.exe` from this repository's Releases page.
2. Run the installer. The build is unsigned, so SmartScreen may ask you to choose **More info**, then **Run anyway**.
3. After setup, the app launches and asks for your renewal day (1-31).

See [QUICKSTART.md](QUICKSTART.md) for first-run setup, the calendar, tray behavior, and troubleshooting.

## Features

- Calendar for the current cycle, with today and the renewal day highlighted
- Separate **Cursor Models** and **Other Models** percentages
- Manual day edits that act as interpolation anchors (other days stay computed)
- System tray: closing the window hides it; **Quit** exits
- Optional start at Windows sign-in (user-level, no elevation)
- Single-instance: a second launch brings the existing window forward
- Settings: renewal day, reset cycle, CSV export
- Follows Windows light, dark, and high-contrast themes

## Build from source

```powershell
dotnet restore
dotnet build
dotnet test .\Tests\CursorQuotaProgress.Tests.csproj
dotnet run --project .\CursorQuotaProgress.csproj
```

Release installer (needs [Inno Setup 6](https://jrsoftware.org/isdl.php)):

```powershell
.\build.ps1
```

Full contributor workflow is in [DEVELOPMENT.md](DEVELOPMENT.md). Design and calculation details are in [IMPLEMENTATION.md](IMPLEMENTATION.md).

## Documentation

| Document | Audience |
| --- | --- |
| [QUICKSTART.md](QUICKSTART.md) | Install, daily use, troubleshooting |
| [DEVELOPMENT.md](DEVELOPMENT.md) | Build, test, run, package, contribute |
| [IMPLEMENTATION.md](IMPLEMENTATION.md) | Architecture and calculation contract |

## License

Copyright 2026. All rights reserved unless a `LICENSE` file is added to this repository.
