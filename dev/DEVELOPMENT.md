# Development

Contributor guide: environment, build, test, layout, and release. End-user steps are in [../QUICKSTART.md](../QUICKSTART.md).

## Prerequisites

- Windows 10 or 11, x64
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Windows 10 SDK 10.0.19041 or later (comes with Visual Studio 2022/2026 with the WinUI workload, or the standalone SDK)
- Optional: [Inno Setup 6](https://jrsoftware.org/isdl.php) for `.\scripts\build.ps1`
- Optional: Visual Studio or VS Code / Cursor

The app is WinUI 3 (Windows App SDK), not WPF.

## Clone and restore

```powershell
git clone <repository-url>
cd CursorUsageProgress
dotnet restore
```

## Everyday commands

| Task | Command |
| --- | --- |
| Build | `dotnet build` |
| Tests | `dotnet test .\Tests\CursorUsageProgress.Tests.csproj` |
| Run (window) | `.\scripts\dev.ps1` or `dotnet run --project .\CursorUsageProgress.csproj` |
| Run (tray only) | `.\scripts\dev.ps1 -Background` |
| Tests via script | `.\scripts\dev.ps1 -Test` |
| Publish + installer | `.\scripts\build.ps1` |
| Publish only | `.\scripts\build.ps1 -SkipInstaller` |
| Publish, skip tests | `.\scripts\build.ps1 -SkipTests` |
| Clean artifacts | `.\scripts\clean.ps1` |
| GitHub release from HEAD | `.\scripts\release.ps1` |
| Dry-run release | `.\scripts\release.ps1 -DryRun` |

Launch flags after `--`:

```powershell
dotnet run --project .\CursorUsageProgress.csproj -- --background
```

`--background` starts the tray icon without showing the main window (used by the Run-at-sign-in registry value).

## Solution layout

```text
CursorUsageProgress/
├── App.xaml, App.xaml.cs
├── CursorUsageProgress.csproj
├── CursorUsageProgress.slnx
├── Models/
├── Services/
├── ViewModels/
├── Views/
├── Converters/
├── Assets/                      # cursor_usage_progress.ico / .png
├── Tests/
│   ├── CursorUsageProgress.Tests.csproj
│   └── CycleCalculatorTests.cs
├── setup.iss                    # Inno Setup
├── scripts/
│   ├── build.ps1
│   ├── clean.ps1
│   ├── dev.ps1
│   └── release.ps1
└── dev/
    ├── CHANGELOG.md
    ├── DEVELOPMENT.md
    └── release-new-version-prompt.md
```

Open `CursorUsageProgress.slnx` in Visual Studio, or build the `.csproj` files directly.

## Stack

| Area | Choice |
| --- | --- |
| UI | WinUI 3, Windows App SDK (`net10.0-windows10.0.19041.0`) |
| Tray | `H.NotifyIcon.WinUI` |
| Tests | xUnit, project under `Tests/` |
| Settings | JSON under `%LocalAppData%\CursorUsageProgress\` |
| Installer | Inno Setup 6, per-user (`PrivilegesRequired=lowest`) |

Manual construction in `App.OnLaunched` wires `IClock`, `ICycleCalculator`, `IPlanStore`, `IStartupRegistration`, `ITrayService`, and `MainViewModel`. There is no DI container.

## Tests

`Tests/CycleCalculatorTests.cs` covers:

- Cycle start and next renewal across year boundaries
- Months that lack the renewal day (28, 29, 30, 31)
- Leap-year 29 February
- Default linear percents
- Manual edits as interpolation anchors (including later edits and days before the first edit)
- Theil-Sen daily usage, uncapped burn projections, run-out dates, and independent quota estimates
- Independent Cursor Models vs Other Models
- `ClearManual` restoring computed values

Add cases next to the existing facts when you change `CycleCalculator`.

## Packaging

`.\scripts\build.ps1`:

1. Runs tests (unless `-SkipTests`)
2. `dotnet publish` self-contained `win-x64` (not single-file; trimming and ReadyToRun stay off)
3. Compiles `setup.iss` unless `-SkipInstaller`
4. Writes `installer\CursorUsageProgress-<version>-win-x64-setup.exe` and a sibling `.sha256` file

`installer/` is gitignored. Attach the exe and checksum to a GitHub Release.

Do not commit built binaries.

## Version bumps

Keep these in sync:

1. `<Version>` in `CursorUsageProgress.csproj` (`scripts/build.ps1` and `scripts/release.ps1` read this)
2. Default `MyAppVersion` in `setup.iss` (overridden by `scripts/build.ps1` with `/DMyAppVersion=...`)
3. `dev/CHANGELOG.md`: when releasing, move `[Unreleased]` bullets into `## [x.y.z] - YYYY-MM-DD` using `dev/release-new-version-prompt.md`
4. `release-notes/RELEASE_NOTES_<version>.md` (required by `scripts/release.ps1`)
5. Git tag `v<version>`

`.\scripts\release.ps1` creates the annotated tag and GitHub Release from HEAD. The Release workflow (`.github/workflows/dotnet-desktop.yml`) then runs `scripts/build.ps1` and attaches the installer.

## Settings format

On-disk cycle data stores **edits only**. `JsonPlanStore` writes camelCase JSON to `%LocalAppData%\CursorUsageProgress\settings.json` (atomic: write `settings.json.tmp`, then move). Full day lists from older files are migrated on load, then dropped on save. The `Version` field on `AppSettings` / `StoredSettings` is currently `1`.

When you add settings fields, give them defaults on `AppSettings` / `StoredSettings` so older files still deserialize. Bump `Version` when the contract is incompatible, then migrate or regenerate the active cycle on load.

## Troubleshooting

**App will not start after an update**

- End `CursorUsageProgress.exe` so the single-instance mutex is released.
- Confirm the published folder contains the Windows App SDK payload (self-contained publish).

**Tray icon missing after Explorer restart**

- `TrayService` listens for `SessionSwitch` and recreates the icon. If it does not return, restart the app.

**Settings lost**

- `%LocalAppData%\CursorUsageProgress\`
- `settings.corrupt.json` is a backup of a file that failed to parse

**Startup registration**

- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- Value name `CursorUsageProgress`
- Command: quoted exe path plus `--background`

**Tests or publish path wrong**

- Tests live under `Tests/`, not the repo root.
- Publish output is `bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\`.

## Contributing

1. Match existing naming, MVVM boundaries, and interface-based services.
2. Put calculation changes in `CycleCalculator` and cover them with xUnit facts.
3. Keep user-facing docs (`README.md`, `QUICKSTART.md`) in sync with UI changes. Log user-visible work under `## [Unreleased]` in `dev/CHANGELOG.md`.
4. Do not commit `bin/`, `obj/`, or `installer/` outputs.

The project is MIT licensed (`LICENSE`). Open an issue for bugs or proposals. There is no published code of conduct or security policy file yet.

