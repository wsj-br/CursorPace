# Quick start

End-user guide for Cursor Usage Progress. For building from source, see [dev/DEVELOPMENT.md](dev/DEVELOPMENT.md).

## Install

1. Download `CursorUsageProgress-*-win-x64-setup.exe` from this repository's Releases page.
2. Run the installer. If SmartScreen warns that the app is unsigned, choose **More info**, then **Run anyway**.
3. If the installer reports that Microsoft Edge WebView2 Runtime is missing, open the download page it offers. **Sign in** needs the runtime.
4. Finish the wizard. The app launches when setup completes.

## First run

The main window opens with an empty state until you sign in:

1. Choose **Sign in**.
2. Complete Google, GitHub, or two-factor sign-in in the embedded window. The window closes when Cursor accepts the session. If you already see your account, choose **Continue**.
3. After a successful update, the billing cycle and usage appear on the calendar and chart. A last-updated time appears under the info cards.

You can also sign in later from **Settings**.

## Sign in to Cursor

Open **Settings** from the title bar if you are not already signed in.

1. Under **Cursor account (disconnected)**, choose **Sign in**.
2. Complete sign-in in the embedded window as above.
3. After a successful update, the heading shows **Cursor account (connected)**.

While signed in:

- Usage samples are stored locally and plotted on the chart.
- The billing cycle start and next renewal come from Cursor.
- **Export Usage** appears next to **Export Cycle CSV**.

Choose **Refresh now** to fetch immediately. **Sign out** clears the saved Cursor session. Samples stay on disk until you delete `usage-samples.json` or uninstall.

### Automatic updates

With **Update usage automatically** on, refreshes run on the clock hour aligned to the interval:

| Interval | Local times |
| --- | --- |
| 1h | 00:00, 01:00, 02:00, … |
| 2h | 00:00, 02:00, 04:00, … |
| 4h | 00:00, 04:00, 08:00, … |
| 6h / 12h | same pattern from midnight |

Launch skips a refresh when the last successful update is under 20 minutes old, unless that last update is already older than the interval or a clock-aligned slot was missed (for example last update 19:55, start 20:05, interval 1h).

## Main window

The header shows:

- **Cycle start** and **Next renewal** (dd-MMM HH:mm)
- **Cursor Models run out** and **Other Models run out** (dd-MMM HH:mm, or — when the quota is not projected to run out before renewal)
- A last-updated caption when a Cursor session exists (`Updated HH:mm`, or a short status if a fetch failed)

The body is a month calendar or a usage chart for the current cycle. Switch with the calendar and chart icons on the right of the info cards (selected is accent color, the other is dimmed). The calendar shows the month name above the weekday row, highlights today and the renewal day, and includes the renewal date when the cycle still has time left that day. A projected run-out day has a subtle yellow background.

Each calendar day shows two percents. On the left, a day with a synced sample shows that day's last reading (teal day number); a day without a sample shows the expected (renewal-paced) value at that day's midnight. The estimated burn value on the right appears only on days after the last sample for that quota.

The chart is read-only: expected percents are dashed lines from 0% at cycle start through each sample to 100% at next renewal, estimated percents are solid from the last sample to next renewal, and a gray line marks 100%. The left and right edges are the cycle start and next renewal instants. Vertical gridlines sit at midnight. Axis labels are the day of the month: the truncated slot before the first midnight is unlabeled, and every other slot (including the renewal-date slot) is labelled. Markers are the cycle-start origin and every sync sample (placed by timestamp).

Title bar actions:

| Control | Action |
| --- | --- |
| **Settings** | Cursor account, startup, CSV export |
| **Quit** | Exit the process (does not keep the tray icon) |
| Window close (X) | Hide the window; the app stays in the tray |

The window is a fixed size. It restores its last position on show and launch. Informational labels can be selected and copied.

## Expected vs estimated

- **Expected** (chart dashed; calendar left on days without a sample): a continuous line from 0% at cycle start through each sample's timestamp, then remaining quota paced to 100% at the next renewal. Days before the first sample rise toward that sample. On the calendar, a day with a synced sample shows that day's last reading on the left instead of this interpolated value.
- **Estimated** (calendar right, chart solid): Theil-Sen daily burn from samples. It can exceed 100% before renewal. On the chart it is a straight line from the last sample to next renewal. The calendar shows it only after the last-update date.

Each quota is independent.

## Settings

Open **Settings** from the title bar.

| Setting | Effect |
| --- | --- |
| **Sign in** | Open the Cursor session window (disabled while already signed in) |
| **Refresh now** | Fetch usage immediately |
| **Sign out** | Clear the saved Cursor session |
| **Update usage automatically** | Clock-aligned refreshes at the interval below |
| **Refresh interval (hours)** | 1, 2, 4, 6, or 12 |
| **Run at Windows sign-in** | Starts the app at sign-in |
| **Start in notification tray** | Start with only the tray icon. Off opens the window. `--background` does the same |
| **Export Cycle CSV** | Writes each day: expected and estimated percents, and whether the day is a data point |
| **Export Usage** | Writes collected sample timestamps and percents (shown while signed in) |

## System tray

While the process is running, an icon stays in the notification area.

- **Left-click** or **Open**: show the window
- **Quit** (tray menu or title bar): exit

Hover over the tray icon to see today's expected percentage and the end-of-period (`EOP`) projection for Cursor and Other Models. `EOP` is the estimate at the next renewal instant. It is omitted until enough data exists.

If the icon is missing, expand the overflow chevron (`^`). After Explorer or a session switch (lock/unlock), the icon should return within a few seconds.

## Startup and single instance

- With **Run at Windows sign-in** on, a new sign-in starts the app. **Start in notification tray** (on by default) keeps the window hidden; turn that off to open the window. Click the tray icon to open the window.
- From the Start menu, the window opens unless **Start in notification tray** is on.
- Only one process runs. Launching again activates the existing window.

## Data

Files live under:

```text
%LocalAppData%\CursorUsageProgress\
```

| Path | Contents |
| --- | --- |
| `settings.json` | Startup, sync interval, last window position, connection flag, last successful sync time, and the current cycle bounds |
| `usage-samples.json` | Collected usage samples for the current Cursor billing cycle |
| `WebView2\` | Embedded browser profile (Cursor session cookies) |

Copy the folder to back up. Delete it to start over (the next launch asks you to sign in).

If `settings.json` cannot be read, the app copies it to `settings.corrupt.json` and writes a blank settings file. The same backup naming is used for `usage-samples.json`.

Uninstalling removes this folder. Back it up first if you want to keep samples.

## Renewal

The next successful fetch that reports a new billing-cycle start replaces the cycle and drops samples from the previous cycle. If the local date reaches the stored next renewal before that fetch, the app requests a refresh.

The window does not need to stay visible, but the process must be running for midnight checks and for automatic usage updates.

## Uninstall

1. Quit from the title bar or the tray menu.
2. Windows Settings, **Apps**, **Installed apps**, **Cursor Usage Progress**, **Uninstall**.

## Troubleshooting

**App will not start**

- In Task Manager, end any `CursorUsageProgress.exe` process, then launch again.
- If the process appears and exits immediately, Event Viewer shows `Microsoft.UI.Xaml.dll` with exception `0xc000027b` when the install is missing `resources.pri`. Use an installer built after that packaging fix.
- If it still fails, check Windows Event Viewer for the application error.

**Sign in fails or "The specified module could not be found"**

- Install the [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (Evergreen x64).
- Retry **Sign in**. The window should close when Cursor accepts the session; use **Continue** if you already see your account.

**Usage does not update**

- Confirm **Cursor account (connected)** in Settings and that **Update usage automatically** is on, or choose **Refresh now**.
- Confirm system date, time, and time zone. The app uses local time for the calendar, chart, and clock-aligned intervals.
- If Cursor rate-limits the request, the app waits until the next interval.

**Tray icon disappeared**

- Expand the notification overflow (`^`).
- Lock and unlock Windows, or restart the app.

**Settings not saving**

- Confirm write access to `%LocalAppData%\CursorUsageProgress`.
- If `settings.corrupt.json` exists, the previous file was unreadable. Delete both files to reset.

**Wrong percentage for today**

- Confirm system date, time, and time zone.
- Check the last-updated caption and **Refresh now**.

**Auto-start not working**

- Confirm **Run at Windows sign-in** is on in Settings.
- Registry (current user): `Software\Microsoft\Windows\CurrentVersion\Run`, value `CursorUsageProgress`. With **Start in notification tray** the command includes `--background`.

## Tips

- Info-card dates use dd-MMM HH:mm; calendar dates use the system format.
- The UI follows Windows light, dark, and high-contrast themes.
- High-DPI scaling is handled by WinUI.
