# Development

Contributor guide: environment, build, test, layout, and release. End-user steps are in [../QUICKSTART.md](../QUICKSTART.md). Agent and architecture constraints are in [../AGENTS.md](../AGENTS.md).

## Prerequisites

- Windows 10 or 11 x64, Linux x64, or macOS (Intel or Apple Silicon)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Native WebView runtime for **Sign in** (see per-OS install below)
- Optional: [Inno Setup 6](https://jrsoftware.org/isdl.php) for `.\scripts\build.ps1` / `./scripts/build.sh` (Windows installer only)
- Optional: Visual Studio or VS Code / Cursor

The app is Avalonia 12 on `net10.0`. Usage fetch stays inside `NativeWebView` (`fetch` with credentials). Do not add `HttpClient` cookie export, CEF, or `WebAuthenticationBroker`.

NuGet packages come from the project files via `dotnet restore` after clone. System dependencies below are what you install with the OS package manager or an installer.

## Install dependencies

Confirm the SDK after install:

```text
dotnet --list-sdks
```

You need a `10.0.x` SDK listed. `global.json` pins `10.0.111` for CI; locally, `rollForward: latestFeature` allows any newer installed 10.0 SDK (for example `10.0.400` on Windows). Full install docs: [Windows](https://learn.microsoft.com/dotnet/core/install/windows), [Linux](https://learn.microsoft.com/dotnet/core/install/linux), [macOS](https://learn.microsoft.com/dotnet/core/install/macos).

### Windows

1. **.NET 10 SDK** (pick one):
   - Download the x64 SDK installer from [.NET 10 downloads](https://dotnet.microsoft.com/download/dotnet/10.0), or
   - `winget install Microsoft.DotNet.SDK.10`
2. **WebView2 Runtime** (Evergreen x64) for **Sign in**. Already present on most Windows 11 PCs. If missing, install from [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703).
3. **Inno Setup 6** (optional, for the Windows installer): download from [jrsoftware.org/isdl.php](https://jrsoftware.org/isdl.php). `scripts/build.ps1` / `scripts/build.sh` find `ISCC.exe` on `PATH` or in the usual Program Files / LocalAppData install folders.

### Linux

1. **.NET 10 SDK** (examples; use your distro’s docs if packages differ):

```bash
# Debian / Ubuntu (after Microsoft’s package feed is configured, or when the distro ships 10.0)
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0

# Fedora
sudo dnf install dotnet-sdk-10.0

# Or any distro: install script into a user directory
curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0
# Then put ~/.dotnet on PATH, e.g. export DOTNET_ROOT=$HOME/.dotnet && export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools
```

2. **WebKitGTK 4.1** (and GTK 3 / libsoup 3) for **Sign in**. WPE is optional; WebKitGTK is the baseline.

```bash
# Debian / Ubuntu
sudo apt install libgtk-3-0 libwebkit2gtk-4.1-0 libsoup-3.0-0

# Fedora
sudo dnf install gtk3 webkit2gtk4.1 libsoup3

# Arch
sudo pacman -S gtk3 webkit2gtk-4.1 libsoup3
```

3. **GNOME tray** (optional): install the AppIndicator extension if the tray icon does not appear.

4. **AppImage tooling** (optional, for `./scripts/build.sh`): WebKitGTK/GTK runtime libraries on the build host (`libgtk-3-0`, `libwebkit2gtk-4.1-0`, `libsoup-3.0-0`), plus ImageMagick (`imagemagick`) to resize the tray icon. `linuxdeploy` is downloaded on first run.

### macOS

1. **.NET 10 SDK** (pick one):
   - Download the macOS SDK installer (x64 or Arm64) from [.NET 10 downloads](https://dotnet.microsoft.com/download/dotnet/10.0), or
   - Install script: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 10.0` (then put `~/.dotnet` on `PATH` as on Linux).
2. **WKWebView** is built into macOS; no separate WebView package is required for **Sign in**.
3. Unsigned local builds may need **Open** from Finder the first time Gatekeeper blocks the binary.

## Clone and restore

```bash
git clone https://github.com/wsj-br/CursorPace.git
cd CursorPace
dotnet restore
```

## Everyday commands

| Task | PowerShell (Windows) | Bash (Linux / macOS) |
| --- | --- | --- |
| Build | `dotnet build` | `dotnet build` |
| Tests | `dotnet test .\Tests\CursorPace.Tests.csproj` | `dotnet test ./Tests/CursorPace.Tests.csproj` |
| Run (window) | `.\scripts\dev.ps1` | `./scripts/dev.sh` |
| Run (tray only) | `.\scripts\dev.ps1 -Background` | `./scripts/dev.sh --background` |
| Run (Release) | `.\scripts\dev.ps1 -Configuration Release` | `./scripts/dev.sh --configuration Release` |
| Tests via script | `.\scripts\dev.ps1 -Test` | `./scripts/dev.sh --test` |
| Publish + installer | `.\scripts\build.ps1` | `./scripts/build.sh` (Linux AppImage or macOS app bundle) |
| Publish only | `.\scripts\build.ps1 -SkipInstaller` | `./scripts/build.sh --skip-installer` |
| Publish, skip tests | `.\scripts\build.ps1 -SkipTests` | `./scripts/build.sh --skip-tests` |
| Clean artifacts | `.\scripts\clean.ps1` | `./scripts/clean.sh` |
| Clean (no delete) | `.\scripts\clean.ps1 -DryRun` | `./scripts/clean.sh --dry-run` |
| Clean, keep NuGet cache | `.\scripts\clean.ps1 -PurgeNuGetCache:$false` | `./scripts/clean.sh --no-purge-nuget` |
| GitHub release from HEAD | `.\scripts\release.ps1` | `./scripts/release.sh` |
| Dry-run release | `.\scripts\release.ps1 -DryRun` | `./scripts/release.sh --dry-run` |
| Release without clean-tree check | `.\scripts\release.ps1 -VerifyClean:$false` | `./scripts/release.sh --no-verify-clean` |

Launch flags after `--`:

```bash
dotnet run --project ./CursorPace.csproj -- --background
```

`--background` starts the tray icon without showing the main window. **Start in notification tray** does the same for a normal launch; Windows Run, macOS Launch Agent, and Linux XDG autostart also pass `--background` when that setting is on.

Maintainer scripts ship as PowerShell (`.ps1`) and bash (`.sh`) with the same behavior. Use `.ps1` on Windows PowerShell and `.sh` on Linux/macOS (no PowerShell install required).

## Solution layout

```text
CursorPace/
├── Program.cs
├── App.axaml, App.axaml.cs
├── CursorPace.csproj
├── CursorPace.slnx
├── Models/
├── Services/                    # cycle math, JSON stores, NativeWebView client, sync
├── ViewModels/                  # MainViewModel, calendar, UsageChartViewModel
├── Views/                       # MainWindow, SettingsView, chart, WebView host
├── Converters/
├── Assets/                      # cursor_pace.ico / .png
├── Tests/
│   └── CursorPace.Tests.csproj
├── setup.iss                    # Inno Setup (Windows only; checks WebView2 Runtime)
├── packaging/
│   ├── cursor-pace.desktop
│   └── io.github.wsj_br.CursorPace.appdata.xml
├── scripts/
│   ├── build.ps1 / build.sh
│   ├── build-appimage.sh
│   ├── build-appbundle.sh
│   ├── clean.ps1 / clean.sh
│   ├── dev.ps1 / dev.sh
│   └── release.ps1 / release.sh
└── dev/
    ├── CHANGELOG.md
    ├── DEVELOPMENT.md
    └── release-new-version-prompt.md
```

Open `CursorPace.slnx` in Visual Studio, or build the `.csproj` files directly.

## Stack

| Area | Choice |
| --- | --- |
| UI | Avalonia 12 (`net10.0`) |
| Tray | Avalonia `TrayIcon` |
| Cursor session | `NativeWebView` host window + persistent profile under LocalApplicationData |
| Tests | xUnit, project under `Tests/` |
| Settings | JSON under LocalApplicationData `CursorPace` |
| Installer | Inno Setup 6 (Windows), AppImage (Linux), zipped `.app` bundle (macOS) |

Manual construction in `App.OnFrameworkInitializationCompleted` wires `IClock`, `ICycleCalculator`, `IPlanStore`, `IUsageSampleStore`, `ICursorUsageClient`, `IUsageSyncService`, `IDataBackupService`, `IStartupRegistration`, `ITrayService`, and `MainViewModel`. There is no DI container.

Keep the usage HTTP call inside `NativeWebView` (`fetch` with credentials). Do not copy Cursor cookies into `HttpClient`.

## Tests

| File | When to update |
| --- | --- |
| `CycleCalculatorTests.cs` | Cycle bounds, `ExpectedPercentAt`, Theil-Sen, run-out |
| `SampleEstimationTests.cs` | Sample-driven expected percents, burn, and run-out |
| `UsageChartSeriesBuilderTests.cs` | Chart seconds mapping, markers, midnight slots |
| `SyncScheduleTests.cs` | Launch skip window and clock-aligned intervals |
| `UsageSummaryParserTests.cs` | `usage-summary` JSON shape |
| `WebView2ScriptResultParserTests.cs` | Object vs JSON-string script results |
| `JsonPlanStoreTests.cs` / `UsageSampleStoreTests.cs` / `UsageSampleAppenderTests.cs` | Settings/sample file load, corruption vs I/O errors, cycle rollover |
| `UsageSyncServiceTests.cs` | Sign-in state on startup, launch/interval refresh skip rules, `StateChanged` / `SnapshotReceived` |
| `CycleCsvBuilderTests.cs` / `UsageSamplesCsvBuilderTests.cs` | CSV columns |
| `MainViewModelTests.cs` / `DayRowViewModelTests.cs` / `CalendarMonthViewModelTests.cs` | Initialization, connected-account persistence, exports, calendar heading, settings page, backup restore |
| `DataBackupArchiveTests.cs` | Zip backup format, missing entries, restore into stores |
| `WindowPlacementTests.cs` | Restore clamped to the work area |
| `LaunchModeTests.cs` | `--background` and **Start in notification tray** hide the window on launch |
| `AsyncRelayCommandTests.cs` | Async command reentrancy guard and exception handling |

`CycleCalculatorTests` still covers:

- `GenerateCycleFromBounds` timed instants and calendar day rows
- Seconds axis (`AxisSeconds` / `CycleSeconds`)
- `ExpectedPercentAt` through samples then to `NextRenewal`
- Theil-Sen daily usage, uncapped burn projections, run-out instants, and independent quota estimates
- Independent Cursor Models vs Other Models

Add cases next to the existing facts when you change those areas. Do not commit session tokens.

## Packaging

### Windows (`.\scripts\build.ps1`)

1. Runs tests (unless `-SkipTests`)
2. `dotnet publish` self-contained `win-x64` (not single-file; trimming and ReadyToRun stay off)
3. Compiles `setup.iss` unless `-SkipInstaller`
4. Writes `installer\CursorPace-<version>-win-x64-setup.exe` and a sibling `.sha256` file

### Linux and macOS (`./scripts/build.sh`)

1. Runs tests (unless `--skip-tests`)
2. Detects the host RID (`linux-x64`, `linux-arm64`, `osx-arm64`, or `osx-x64`) and publishes self-contained output
3. Unless `--skip-installer`:
   - **Linux**: `./scripts/build-appimage.sh` writes `installer/CursorPace-<version>-<rid>.AppImage` (+ `.sha256`) for `linux-x64` or `linux-arm64`. Needs WebKitGTK/GTK libraries on the build host matching the target architecture; [linuxdeploy](https://github.com/linuxdeploy/linuxdeploy) tooling is downloaded automatically for that architecture. AppImage packaging must run on a host whose architecture matches `--rid` because linuxdeploy bundles the host's native libraries.
   - **macOS**: `./scripts/build-appbundle.sh` writes `installer/CursorPace-<version>-<rid>.zip` containing `Cursor Pace.app` (+ `.sha256`)

Publish output is under `bin/Release/net10.0/<rid>/publish/`. Trimming, ReadyToRun, and PublishSingleFile stay off.

Manual publish (no packaging):

```text
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r linux-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=false
```

The Windows installer prompts to open the WebView2 Runtime download page when the runtime is missing. Uninstall deletes `%LocalAppData%\CursorPace`.

`installer/` is gitignored. Release packages are intentionally unsigned; their SHA-256 checksum files detect download corruption but are not code-signing identities.

Do not commit built binaries.

## Version bumps

Keep these in sync:

1. `<Version>` in `CursorPace.csproj` (`scripts/build.*` and `scripts/release.*` read this)
2. Default `MyAppVersion` in `setup.iss` (overridden by `scripts/build.*` with `/DMyAppVersion=...`)
3. `dev/CHANGELOG.md`: when releasing, move `[Unreleased]` bullets into `## [x.y.z] - YYYY-MM-DD` using `dev/release-new-version-prompt.md`
4. `release-notes/RELEASE_NOTES_<version>.md` (required by `scripts/release.*`)
5. Git tag `v<version>`

`.\scripts\release.ps1` / `./scripts/release.sh` recreates and pushes the annotated tag from HEAD. The tag starts `.github/workflows/dotnet-desktop.yml`, which validates the version, tests once, builds unsigned Windows x64, Linux x64, Linux ARM64, macOS ARM64, and macOS x64 packages, verifies all checksums, and creates the GitHub Release only after every build succeeds. The Linux ARM64 job runs on the `ubuntu-24.04-arm` hosted runner. A manual workflow dispatch builds the same artifacts without creating a release.

The workflow pins hosted runner generations, the .NET SDK, and GitHub Actions major versions. NuGet restores use committed `packages.lock.json` files in locked mode on CI; Dependabot proposes action and NuGet updates.

## Settings format

`JsonPlanStore` writes camelCase JSON to `%LocalAppData%\CursorPace\settings.json` (atomic: write `settings.json.tmp`, then move). The `Version` field is `2`. Leftover `renewalDay`, cycle `edits`, and legacy `days[]` are ignored on load.

Current `settings.json` fields (defaults on `AppSettings` / `StoredSettings` so older files still deserialize):

| Field | Role |
| --- | --- |
| `activeCycle` | `renewalDay`, `cycleStart`, `nextRenewal` |
| `runAtStartup` | Launch at login (Windows Run key, macOS Launch Agent, Linux XDG autostart) |
| `startInNotificationTray` | Default `true`; hide the window on launch; startup registration includes `--background` |
| `themeMode` | `System` (default), `Light`, or `Dark`; sets Avalonia `RequestedThemeVariant` |
| `autoSyncEnabled` | Default `true` |
| `syncIntervalHours` | 1, 2, 4, 6, or 12; other values clamp to 1 |
| `showChartView` | Last main-window body (calendar vs chart) |
| `cursorAccountConnected` | Last known signed-in state for launch skip |
| `lastUsageSyncUtc` | Last successful usage fetch |
| `windowX` / `windowY` | Last window position |

`usage-samples.json` is a separate document: `version`, `cycleStartUtc`, and `samples` (`ts`, `cursor`, `other`). A new Cursor billing-cycle start clears that sample list.

Settings **Backup** writes a zip (`manifest.json`, `settings.json`, `usage-samples.json`). It does not include the WebView profile. **Restore** replaces those two JSON files and reloads the cycle in the running app.

When you add settings fields, give them defaults on `AppSettings` / `StoredSettings` so older files still deserialize. Bump `Version` when the contract is incompatible, then migrate or regenerate the active cycle on load.

## Troubleshooting

**App will not start after an update**

- End `CursorPace` so the single-instance mutex or lock file is released.
- Confirm the published folder contains the self-contained Avalonia payload.

**Sign in fails in a local run**

- Windows: confirm the WebView2 Runtime. Profile folder: `%LocalAppData%\CursorPace\WebView2`.
- Linux/macOS: profile folder is `WebView` under LocalApplicationData; a Linux AppImage run uses `WebView-AppImage` instead (see below). Google may block WebKit login.
- After a successful Cursor session, the sign-in window should close on its own (or after **Continue**). An `Unsupported result type` banner meant the usage script returned a non-string value to WebKit; current builds stringify the fetch result.
- Delete the profile folder to force a fresh login. Do not delete `settings.json` unless you also want to reset the cycle.

**"Lost" Cursor login that keeps recurring on Linux**

An AppImage bundles its own WebKitGTK build via `linuxdeploy --plugin gtk`. If that bundled WebKit and the system WebKitGTK used by a `dotnet run`/`dev.sh` build ever wrote cookies to the *same* profile folder, one build's WebKit can fail to read the other's cookie database, and the fetch returns `AuthRequired` even though nothing actually signed you out. `WebViewProfilePaths` detects an AppImage run via the `APPIMAGE` environment variable (set by AppImage's `AppRun`) and gives it a separate `WebView-AppImage` profile folder so a dev run and an AppImage run never share one cookie store. If you still see recurring `AuthRequired` after this, compare `~/.local/share/CursorPace/WebView/` and `.../WebView-AppImage/` timestamps to confirm which build wrote which profile, and check whether a newer AppImage build picked up a different bundled WebKitGTK version than a previous one (that scenario is not covered by the folder split, since both are "AppImage" runs).

**App shows `(connected)` right after Sign in even though Cursor never accepted the session**

Do not resurrect a `HasPersistedProfile`-style check that treats the WebView profile folder existing/being non-empty as evidence of a signed-in session: WebKitGTK writes `Cache/hsts-storage.sqlite`, `Cache/WebKitCache/`, `localstorage/`, `storage/`, and `mediakeys/` to that folder as soon as the embedded browser is first initialized, before any cookie is ever set. Verified locally (WSLg + `libwebkit2gtk-4.1-0` 2.52.3): the folder was ~40 MB with those files after only opening the sign-in window, with zero Cursor cookies. `IsSignedIn` must come only from `cursorAccountConnected`/prior-sync evidence in `UsageSyncService`'s constructor and from actual `FetchAsync` results (`Ok` / `SignedOut`); other statuses (`AuthRequired`, `Syncing`, `RateLimited`, `Error`) must leave `IsSignedIn` unchanged rather than deriving it from the profile folder.

**Sign out on Linux also signs out Google/GitHub**

`NativeWebViewCursorUsageClient.DisconnectAsync` tries `NativeWebView.TryGetCookieManager()` to delete only `cursor.com` cookies, and only falls back to deleting the whole profile folder (which also clears Google/GitHub) if no cookie manager is available. Verified locally: on Linux with the WebKitGTK backend (`AdapterInfo.Type == WebKitGtk`, WebKit 2.52.3), `TryGetCookieManager()` returns `null`, so Sign out always takes the full-wipe fallback there. This is a limitation of `Avalonia.Controls.WebView` 12.1.0's WebKitGTK adapter, not a bug in this app's logic; re-check if a future `Avalonia.Controls.WebView` release adds cookie-manager support for that backend. Windows (`ICoreWebView2CookieManager`) and macOS (`WKHTTPCookieStore`) have their own cookie-manager implementations in the same package and are expected to support the scoped delete, but that has not been verified on those platforms.

**Tray icon missing**

- Restart the app. On GNOME, install the AppIndicator extension.

**System theme wrong on Linux or WSL (Settings → Theme = System)**

When `themeMode` is `System`, the app sets Avalonia `RequestedThemeVariant` to `Default`. Avalonia does not read GNOME `gsettings`, GTK theme files, or the Windows host theme directly. On Linux it queries the XDG Desktop Portal over D-Bus:

- Service: `org.freedesktop.portal.Desktop`
- Interface: `org.freedesktop.portal.Settings`
- Key: `org.freedesktop.appearance` / `color-scheme` (`0` = no preference, `1` = dark, `2` = light)

If that read fails, Avalonia falls back to **Light**. The app then stays light even when Ubuntu reports dark mode.

This often shows up on **Ubuntu inside WSL** (WSLg): D-Bus and `xdg-desktop-portal-gtk` may be running, but the public `org.freedesktop.portal.Settings` interface is missing when `XDG_CURRENT_DESKTOP` is unset and no portal backend is configured. The GTK implementation backend may still know the scheme (`org.freedesktop.impl.portal.desktop.gtk`), but Avalonia only talks to the public portal API.

Verify:

```bash
# GNOME preference (informational; Avalonia does not read this directly)
gsettings get org.gnome.desktop.interface color-scheme

# What Avalonia needs (should list org.freedesktop.portal.Settings)
gdbus introspect --session \
  --dest org.freedesktop.portal.Desktop \
  --object-path /org/freedesktop/portal/desktop | grep Settings

# Backend often has the value even when the public portal does not
dbus-send --session --print-reply \
  --dest=org.freedesktop.impl.portal.desktop.gtk \
  /org/freedesktop/portal/desktop \
  org.freedesktop.impl.portal.Settings.Read \
  string:org.freedesktop.appearance string:color-scheme
```

Fix on WSL or minimal Linux sessions: create `~/.config/xdg-desktop-portal/portals.conf`:

```ini
[preferred]
default=gtk
org.freedesktop.impl.portal.Settings=gtk
```

Restart the portal, then relaunch the app:

```bash
systemctl --user restart xdg-desktop-portal xdg-desktop-portal-gtk
export XDG_CURRENT_DESKTOP=GNOME   # optional hint when no full desktop session
```

If auto-detection still fails, set **Theme** to **Light** or **Dark** in Settings (stored as `themeMode` in `settings.json`).

**Settings lost**

- `%LocalAppData%\CursorPace\`
- `settings.corrupt.json` / `usage-samples.corrupt.json` are backups of files that failed to parse

**Startup registration**

- Windows: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, value `CursorPace`
- macOS: `~/Library/LaunchAgents/com.cursorpace.app.plist`
- Linux: `~/.config/autostart/cursor-pace.desktop`
- Command includes `--background` when **Start in notification tray** is on

**Tests or publish path wrong**

- Tests live under `Tests/`, not the repo root.
- Windows publish output is `bin\Release\net10.0\win-x64\publish\`.

## Contributing

1. Match existing naming, MVVM boundaries, and interface-based services. Follow [../AGENTS.md](../AGENTS.md) for cycle math, sync, and persistence.
2. Put calculation changes in `CycleCalculator` and cover them with xUnit facts. Chart mapping belongs in `UsageChartSeriesBuilder`.
3. Keep user-facing docs (`README.md`, `QUICKSTART.md`) in sync with UI changes. Log user-visible work under `## [Unreleased]` in `dev/CHANGELOG.md`.
4. Do not commit `bin/`, `obj/`, or `installer/` outputs.

The project is MIT licensed (`LICENSE`). Open an issue for bugs or proposals. There is no published code of conduct or security policy file yet.
