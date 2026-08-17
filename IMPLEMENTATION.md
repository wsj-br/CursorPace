# Architecture

Design and calculation reference for Cursor Quota Progress. Build and test commands are in [DEVELOPMENT.md](DEVELOPMENT.md).

## Purpose

A WinUI 3 desktop app that plans two independent quota series (Cursor Models and Other Models) over the billing cycle defined by a monthly renewal day. Percentages are a local plan, not live Cursor usage.

## Layers

| Layer | Role |
| --- | --- |
| **Models** | Data types only: `QuotaKind`, `QuotaDayEntry`, `QuotaDayEdit`, `QuotaCycle`, `AppSettings` |
| **Services** | Interfaces plus implementations: clock, cycle math, JSON store, startup registry, tray |
| **ViewModels** | MVVM: `MainViewModel`, calendar VMs, `DayRowViewModel`, `RelayCommand` |
| **Views** | WinUI windows and `CalendarControl`; dialogs in code-behind where needed |
| **Converters** | XAML value converters for today, renewal day, and edit styling |

Time, persistence, and calculations go through `IClock`, `IPlanStore`, and `ICycleCalculator` so unit tests do not need the UI or the real clock.

## Design decisions

**Decimal percents.** Stored and computed as `decimal`. The main window shows whole percents (rounded half away from zero). CSV export uses a finer ratio format.

**Edits as anchors, not a full day list.** `QuotaCycle.Edits` is what is saved. `Days` is rebuilt in memory. A later edit is another anchor; it is not wiped when you edit an earlier day.

**Independent quotas.** Cursor Models and Other Models have separate optional values on each `QuotaDayEdit`.

**Atomic JSON.** Write `settings.json.tmp`, then move over `settings.json`. On deserialize failure, copy the bad file to `settings.corrupt.json` and start from blank settings.

**Single instance.** Mutex `CursorQuotaProgress_SingleInstance`. A second process signals `CursorQuotaProgress_SingleInstance_Event` and exits; the first process shows the window.

**Tray-first lifetime.** The tray icon lasts for the process. Closing the window hides it. **Quit** disposes the icon and calls `Application.Exit`.

**Per-user install.** Inno Setup uses `{localappdata}\Programs` and `PrivilegesRequired=lowest`. Uninstall deletes `%LocalAppData%\CursorQuotaProgress`.

## Cycle lookup

Renewal day is an integer 1-31.

```text
FindCycleStart(renewalDay, today):
  if this month contains renewalDay and that date <= today:
    return that date
  else:
    walk months backward until the month contains renewalDay

FindNextRenewal(renewalDay, cycleStart):
  start at cycleStart + 1 month
  walk months forward until the month contains renewalDay

GenerateCycle:
  start = FindCycleStart(...)
  end = FindNextRenewal(...)
  D = (end - start).Days
  for dayNumber 1..D:
    default percent = 100 * (dayNumber - 1) / D
```

Months that do not contain the day (31 February, 31 April, 29 February in a common year) are skipped. Example: renewal day 31 with a January start yields a cycle through 31 March.

Day 1 of the cycle is 0%. The last stored day is the day before next renewal, so it is below 100%. Renewal itself is the start of the next cycle.

## Interpolation

Anchors for one quota kind:

- Implicit start: day 1 at 0% unless day 1 is edited
- Each `QuotaDayEdit` that has a value for that kind
- Implicit end: next renewal at 100% (one day past the last cycle day)

For day `d` between previous anchor `(startDay, startPercent)` and next anchor `(endDay, endPercent)`:

```text
span = endDay - startDay
percent = startPercent + (d - startDay) * (endPercent - startPercent) / span
```

`SetManual` writes or updates an edit and rebuilds `Days`. `ClearManual` removes that kind from the edit (and the edit row if both kinds are empty), then rebuilds.

Editing one kind never changes the other.

## Persistence

Path: `%LocalAppData%\CursorQuotaProgress\settings.json`

Current shape (camelCase JSON):

```json
{
  "version": 1,
  "renewalDay": 15,
  "runAtStartup": false,
  "activeCycle": {
    "renewalDay": 15,
    "cycleStart": "2026-01-15T00:00:00",
    "nextRenewal": "2026-02-15T00:00:00",
    "edits": [
      {
        "dayNumber": 15,
        "cursorModelsPercent": 35
      }
    ]
  }
}
```

`JsonPlanStore` still reads a legacy `days` array with `cursorModelsIsManual` / `otherModelsIsManual` and converts flagged rows into `edits`.

`Days` on `QuotaCycle` is not written.

## Process lifecycle

1. **Start:** take the mutex, create tray and view model, load settings. If there is no renewal day, show the window and the setup dialog. If `--background` and already initialized, keep the window hidden.
2. **Close (X):** cancel close, hide `AppWindow`.
3. **Quit:** dispose tray, interrupt the named-event thread, release the mutex, exit.
4. **Second instance:** set the named event, exit.
5. **Day change:** on window activation and on a five-minute timer, `MainViewModel.CheckForNewDay` regenerates the cycle when the calendar day (or timezone-adjusted date) has moved.

## UI map

| Surface | Responsibility |
| --- | --- |
| `MainWindow` | Custom title bar, info cards, `CalendarControl`, day edit panel |
| `CalendarControl` | Month grid bound to `CalendarMonthViewModel` |
| `SettingsWindow` | Run at sign-in, change renewal day, reset cycle, CSV export |
| `RenewalDayDialog` | ContentDialog for first run and renewal-day change |
| `TrayService` | NotifyIcon, Open/Quit, tooltip, SessionSwitch recovery |

Startup registration writes `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\CursorQuotaProgress` to `"<exe>" --background`.

## Out of scope

- Cursor API or any live usage feed
- History of past cycles (only the active cycle is kept)
- Cloud sync
- Code signing (SmartScreen will warn)
- Automatic updates

CSV export of the current cycle is implemented in Settings.

## Packaging notes

Self-contained `win-x64` publish, Windows App SDK self-contained, not single-file. See `build.ps1` and `setup.iss`.
