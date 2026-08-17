# Cursor Quota Progress

A Windows 11 x64 desktop application for tracking Cursor quota allowances across renewal cycles.

## Status

✅ **Core Implementation Complete**
- ✅ Cycle calculation logic with proper renewal-day handling
- ✅ Full MVVM architecture with testable services
- ✅ Data persistence with JSON storage and corruption recovery
- ✅ System tray integration with close-to-tray behavior
- ✅ Single-instance enforcement with inter-process signaling
- ✅ Windows startup registration (user-level)
- ✅ Theme-aware WPF UI following system colors
- ✅ Comprehensive unit tests (10/10 passing)
- ✅ Initial setup dialog for renewal day selection
- ✅ Renewal day change with confirmation dialog
- ✅ Independent quota editing with live recalculation
- ✅ Locale-aware decimal input and date formatting
- ✅ Today-row highlighting and auto-scroll
- ✅ Midnight/timezone/resume detection

🚧 **Remaining for Production Release**
- ⚠️ Application icon (placeholder exists, needs actual multi-res .ico)
- ⚠️ Inno Setup installer testing
- ⚠️ Windows Sandbox integration test
- ⚠️ Memory footprint measurement (<100 MB target)
- ⚠️ Code signing (optional for v1)

## Project Structure

```
Cursor-progress/
├── Models/           - Domain types (QuotaCycle, QuotaDayEntry, AppSettings)
├── Services/         - Core services with interfaces
│   ├── IClock, ICycleCalculator, IPlanStore
│   ├── IStartupRegistration, ITrayService
│   └── Implementations
├── ViewModels/       - MVVM view models
├── Views/            - WPF windows and controls
├── Assets/           - Icons and resources
├── CursorQuotaProgress.csproj      - Main application project
├── CursorQuotaProgress.Tests.csproj - Unit tests
├── CycleCalculatorTests.cs         - Test suite
└── *.md              - Documentation files
```

## Building

Requires .NET 10 SDK:

```bash
dotnet build
dotnet test
```

## Running

```bash
dotnet run --project CursorQuotaProgress.csproj
```

Launch in background mode:
```bash
dotnet run --project CursorQuotaProgress.csproj -- --background
```

## Publishing

Self-contained release:
```bash
dotnet publish CursorQuotaProgress.csproj -c Release -r win-x64 --self-contained
```

Output: `bin/Release/net10.0-windows/win-x64/publish/`

## Key Features Implemented

### Calculation Contract ✅
- Renewal days 1-31 with month-skipping logic
- Even distribution: day 1 = 0%, last day ~96%+
- Independent quota editing with recalculation
- Full decimal precision internally
- Locale-aware decimal input/display

### Application Architecture ✅
- Testable boundaries with dependency injection
- Atomic JSON persistence with corruption recovery
- System tray icon with Open/Quit menu
- Single-instance with inter-process activation
- Startup registration (user-level, no elevation)

### UI ✅
- Compact header with today's available quotas
- Full cycle table with editable percentages
- Today-row highlighting
- Run-at-startup checkbox
- Quit button for explicit shutdown

## Technical Highlights

- **No trimming/ReadyToRun** - Safe self-contained deployment
- **Proper WPF + Windows Forms** - Tray icon via NotifyIcon
- **Culture-aware** - Uses system short-date format
- **DPI scaling** - Native WPF support
- **Theme following** - Leverages system brushes
- **Midnight detection** - Timer + activation checks

## Testing

10 unit tests cover:
- Renewal-day lookup across year boundaries
- Month skipping (day 29, 30, 31, February)
- Leap year handling
- Cycle generation and daily increment calculation
- Independent quota recalculation
- Edit propagation with full precision

All tests passing with .NET 10.

## Next Steps

1. Add initial setup dialog for first-run renewal day selection
2. Implement renewal day change with confirmation
3. Create proper icon.ico with multiple resolutions
4. Write Inno Setup script for installer
5. Test in Windows Sandbox without .NET runtime
6. Measure idle memory footprint (target: <100 MB)

## License

Copyright © 2026
