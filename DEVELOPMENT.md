# Development Notes

## Project Architecture

### Core Components

**Models** (`Models/`)
- `QuotaKind` - Enum for CursorModels and OtherModels
- `QuotaDayEntry` - Represents one day in the cycle with percentages
- `QuotaCycle` - Complete cycle with start, end, and all days
- `AppSettings` - Persistent application settings

**Services** (`Services/`)
- `IClock` / `SystemClock` - Time abstraction for testing
- `ICycleCalculator` / `CycleCalculator` - Core calculation logic
- `IPlanStore` / `JsonPlanStore` - JSON persistence
- `IStartupRegistration` / `WindowsStartupRegistration` - Windows Registry startup
- `ITrayService` / `TrayService` - System tray icon management

**ViewModels** (`ViewModels/`)
- `MainViewModel` - Main window logic
- `DayRowViewModel` - Individual row in the data grid
- `ViewModelBase` - Base class with INotifyPropertyChanged
- `RelayCommand` - ICommand implementation

**Views** (`Views/`)
- `MainWindow` - Main application window
- `RenewalDayDialog` - Setup/change renewal day

## Calculation Logic

The cycle calculator implements the contract specified in the requirements:

```
Days in cycle: D = (NextRenewal - CycleStart).Days
Day k (0-indexed): defaultPercent = 100 * k / D

Example: 31-day cycle
- Day 1 (k=0): 0%
- Day 2 (k=1): 3.225...%
- Day 31 (k=30): 96.774...%
- Renewal (not shown): 100%
```

### Renewal Day Lookup

1. Check if current month has the renewal day
2. If yes and date <= today, use it as cycle start
3. Otherwise, search backwards month-by-month for valid renewal day
4. Skip months that don't have the day (e.g., Feb 30, Feb 31)

### Recalculation After Edit

When editing day `k` to value `x`:
- Previous days (0 to k-1) remain unchanged
- Remaining intervals: `D - k`
- Daily increment: `(100 - x) / (D - k)`
- Day `j` (j > k): `x + (j - k) × dailyIncrement`

## Data Persistence

Settings stored at: `%LocalAppData%\CursorQuotaProgress\settings.json`

Format:
```json
{
  "version": 1,
  "renewalDay": 15,
  "runAtStartup": false,
  "activeCycle": {
    "renewalDay": 15,
    "cycleStart": "2026-01-15T00:00:00",
    "nextRenewal": "2026-02-15T00:00:00",
    "days": [
      {
        "dayNumber": 1,
        "date": "2026-01-15T00:00:00",
        "cursorModelsPercent": 0,
        "otherModelsPercent": 0,
        "cursorModelsIsManual": false,
        "otherModelsIsManual": false
      },
      ...
    ]
  }
}
```

Atomic write using temp file + move for crash safety.

## Window Lifecycle

1. **Process Start**
   - Create mutex for single-instance
   - Initialize tray icon
   - Load settings
   - Show setup dialog if first run
   - Show main window unless `--background` flag

2. **Close Window** (X button)
   - Cancel close event
   - Hide window
   - Keep tray icon visible
   - Process continues running

3. **Quit** (explicit button or tray menu)
   - Shutdown application
   - Dispose tray icon
   - Exit process

4. **Second Instance Launch**
   - Detect existing mutex
   - Signal first instance via EventWaitHandle
   - Exit immediately

## Testing Strategy

Unit tests cover:
- Renewal day calculation across year boundaries
- Month skipping (28, 29, 30, 31)
- Leap year handling (Feb 29)
- Cycle generation with correct day counts
- Default percentage distribution
- Independent quota recalculation
- Edit propagation with full precision

Integration testing checklist:
- [ ] First-run setup flow
- [ ] Renewal day change with confirmation
- [ ] Edit cells and verify recalculation
- [ ] Invalid input rejection
- [ ] Startup registration
- [ ] Single-instance enforcement
- [ ] Close to tray behavior
- [ ] Tray icon recovery after Explorer restart
- [ ] Midnight/timezone change detection
- [ ] Theme switching (light/dark/high-contrast)

## Known Limitations

1. **Icon**: Placeholder only - needs proper multi-resolution .ico
2. **Manual Edits**: Discarded on renewal day change (by design)
3. **Historical Data**: Not retained across cycles
4. **No Cloud Sync**: Local machine only
5. **No Cursor API**: Manual percentage tracking, not actual usage

## Performance Targets

- **Startup**: < 1 second on typical hardware
- **Idle Memory**: < 100 MB private working set
- **Tray Latency**: < 100ms to show window on click
- **UI Responsiveness**: 60fps for scrolling and updates

## Future Enhancements (Out of Scope for v1)

- Historical cycle viewer
- Usage statistics and charts
- Custom themes
- Multiple quota profiles
- Export to CSV
- Cloud sync
- Cursor API integration for actual usage tracking
- Reminder notifications
- Mini mode (compact view)

## Building from Source

Requirements:
- .NET 10 SDK
- Windows 10/11 x64
- Visual Studio 2022 or VS Code (optional)

Build commands:
```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet publish CursorQuotaProgress -c Release -r win-x64 --self-contained
```

## Troubleshooting

**App won't start after update**
- Kill existing process
- Check mutex is released
- Verify .NET 10 runtime in published folder

**Tray icon missing after Explorer restart**
- App listens for SessionSwitch events
- Should auto-recover within seconds
- If not, restart the app

**Settings lost**
- Check `%LocalAppData%\CursorQuotaProgress\`
- Look for `settings.corrupt.json` backup
- JSON deserialization errors trigger regeneration

**Startup registration not working**
- Check: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- Key name: `CursorQuotaProgress`
- Value should include `--background` flag

## License

Copyright © 2026. Internal tool, no public distribution planned for v1.
