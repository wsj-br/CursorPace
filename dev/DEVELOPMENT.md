# Development

Contributor guide: environment, build, test, layout, and release. End-user steps are in [../QUICKSTART.md](../QUICKSTART.md). Agent and architecture constraints are in [../AGENTS.md](../AGENTS.md).

## Prerequisites

- Windows 10 or 11, x64
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Windows 10 SDK 10.0.19041 or later (comes with Visual Studio 2022/2026 with the WinUI workload, or the standalone SDK)
- [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (Evergreen x64) to exercise **Sign in**
- Optional: [Inno Setup 6](https://jrsoftware.org/isdl.php) for `.\scripts\build.ps1`
- Optional: Visual Studio or VS Code / Cursor

The app is WinUI 3 (Windows App SDK), not WPF. WebView2 types come from Windows App SDK; do not add a separate `Microsoft.Web.WebView2` package unless the SDK stops shipping them.

## Clone and restore

```powershell
git clone https://github.com/wsj-br/CursorUsageProgress.git
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

`--background` starts the tray icon without showing the main window. **Start in notification tray** does the same for a normal launch; the Run key also passes `--background` when that setting is on.

## Solution layout

```text
CursorUsageProgress/
├── App.xaml, App.xaml.cs
├── CursorUsageProgress.csproj
├── CursorUsageProgress.slnx
├── Models/
├── Services/                    # cycle math, JSON stores, WebView2 client, sync
├── ViewModels/                  # MainViewModel, calendar, UsageChartViewModel
├── Views/                       # MainWindow, Settings, chart, WebView2 host
├── Converters/
├── Assets/                      # cursor_usage_progress.ico / .png
├── Tests/
│   └── CursorUsageProgress.Tests.csproj
├── setup.iss                    # Inno Setup (checks WebView2 Runtime)
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
| Cursor session | WebView2 host window + persistent profile under LocalAppData |
| Tests | xUnit, project under `Tests/` |
| Settings | JSON under `%LocalAppData%\CursorUsageProgress\` |
| Installer | Inno Setup 6, per-user (`PrivilegesRequired=lowest`) |

Manual construction in `App.OnLaunched` wires `IClock`, `ICycleCalculator`, `IPlanStore`, `IUsageSampleStore`, `ICursorUsageClient`, `IUsageSyncService`, `IStartupRegistration`, `ITrayService`, and `MainViewModel`. There is no DI container.

Keep the usage HTTP call inside WebView2 (`fetch` with credentials). Do not copy Cursor cookies into `HttpClient`.

## Tests

| File | When to update |
| --- | --- |
| `CycleCalculatorTests.cs` | Cycle bounds, `ExpectedPercentAt`, Theil-Sen, run-out |
| `SampleEstimationTests.cs` | Sample-driven expected percents, burn, and run-out |
| `UsageChartSeriesBuilderTests.cs` | Chart seconds mapping, markers, midnight slots |
| `SyncScheduleTests.cs` | Launch skip window and clock-aligned intervals |
| `UsageSummaryParserTests.cs` | `usage-summary` JSON shape |
| `WebView2ScriptResultParserTests.cs` | Object vs JSON-string script results |
| `UsageSampleStoreTests.cs` / `UsageSampleAppenderTests.cs` | Sample file and cycle rollover |
| `CycleCsvBuilderTests.cs` / `UsageSamplesCsvBuilderTests.cs` | CSV columns |
| `MainViewModelTests.cs` / `DayRowViewModelTests.cs` / `CalendarMonthViewModelTests.cs` | Initialization, connected-account persistence, exports, calendar heading |
| `WindowPlacementTests.cs` | Restore clamped to the work area |

`CycleCalculatorTests` still covers:

- `GenerateCycleFromBounds` timed instants and calendar day rows
- Seconds axis (`AxisSeconds` / `CycleSeconds`)
- `ExpectedPercentAt` through samples then to `NextRenewal`
- Theil-Sen daily usage, uncapped burn projections, run-out instants, and independent quota estimates
- Independent Cursor Models vs Other Models

Add cases next to the existing facts when you change those areas. `dev/api_usage-summary.json` is a captured dashboard payload; `dev/api_usage-summary.ps1` fetches a live copy when `CURSOR_SESSION_TOKEN` is set. Do not commit session tokens.

## Packaging

`.\scripts\build.ps1`:

1. Runs tests (unless `-SkipTests`)
2. `dotnet publish` self-contained `win-x64` (not single-file; trimming and ReadyToRun stay off)
3. Compiles `setup.iss` unless `-SkipInstaller`
4. Writes `installer\CursorUsageProgress-<version>-win-x64-setup.exe` and a sibling `.sha256` file

The installer prompts to open the WebView2 Runtime download page when the runtime is missing. Uninstall deletes `%LocalAppData%\CursorUsageProgress`.

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

`JsonPlanStore` writes camelCase JSON to `%LocalAppData%\CursorUsageProgress\settings.json` (atomic: write `settings.json.tmp`, then move). The `Version` field is `2`. Leftover `renewalDay`, cycle `edits`, and legacy `days[]` are ignored on load.

Current `settings.json` fields (defaults on `AppSettings` / `StoredSettings` so older files still deserialize):

| Field | Role |
| --- | --- |
| `activeCycle` | `renewalDay`, `cycleStart`, `nextRenewal` |
| `runAtStartup` | Current-user Run key |
| `startInNotificationTray` | Default `true`; hide the window on launch; Run key includes `--background` |
| `autoSyncEnabled` | Default `true` |
| `syncIntervalHours` | 1, 2, 4, 6, or 12; other values clamp to 1 |
| `showChartView` | Last main-window body (calendar vs chart) |
| `cursorAccountConnected` | Last known signed-in state for launch skip |
| `lastUsageSyncUtc` | Last successful usage fetch |
| `windowX` / `windowY` | Last window position |

`usage-samples.json` is a separate document: `version`, `cycleStartUtc`, and `samples` (`ts`, `cursor`, `other`). A new Cursor billing-cycle start clears that sample list.

When you add settings fields, give them defaults on `AppSettings` / `StoredSettings` so older files still deserialize. Bump `Version` when the contract is incompatible, then migrate or regenerate the active cycle on load.

## Troubleshooting

**App will not start after an update**

- End `CursorUsageProgress.exe` so the single-instance mutex is released.
- Confirm the published folder contains the Windows App SDK payload (self-contained publish).

**Sign in fails in a local run**

- Confirm x64 (`PlatformTarget`) and the WebView2 Runtime.
- Profile folder: `%LocalAppData%\CursorUsageProgress\WebView2`. Delete it to force a fresh login. Do not delete `settings.json` unless you also want to reset the cycle.

**Tray icon missing after Explorer restart**

- `TrayService` listens for `SessionSwitch` and recreates the icon. If it does not return, restart the app.

**Settings lost**

- `%LocalAppData%\CursorUsageProgress\`
- `settings.corrupt.json` / `usage-samples.corrupt.json` are backups of files that failed to parse

**Startup registration**

- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- Value name `CursorUsageProgress`
- Command: quoted exe path; plus `--background` when **Start in notification tray** is on

**Tests or publish path wrong**

- Tests live under `Tests/`, not the repo root.
- Publish output is `bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\`.

## Contributing

1. Match existing naming, MVVM boundaries, and interface-based services. Follow [../AGENTS.md](../AGENTS.md) for cycle math, sync, and persistence.
2. Put calculation changes in `CycleCalculator` and cover them with xUnit facts. Chart mapping belongs in `UsageChartSeriesBuilder`.
3. Keep user-facing docs (`README.md`, `QUICKSTART.md`) in sync with UI changes. Log user-visible work under `## [Unreleased]` in `dev/CHANGELOG.md`.
4. Do not commit `bin/`, `obj/`, or `installer/` outputs.

The project is MIT licensed (`LICENSE`). Open an issue for bugs or proposals. There is no published code of conduct or security policy file yet.
