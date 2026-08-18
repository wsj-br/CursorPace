# Quick start

End-user guide for Cursor Usage Progress. For building from source, see [dev/DEVELOPMENT.md](dev/DEVELOPMENT.md).

## Install

1. Download `CursorUsageProgress-*-win-x64-setup.exe` from this repository's Releases page.
2. Run the installer. If SmartScreen warns that the app is unsigned, choose **More info**, then **Run anyway**.
3. If the installer reports that Microsoft Edge WebView2 Runtime is missing, open the download page it offers. You can still use the app as a manual planner without it; **Sign in** needs the runtime.
4. Finish the wizard. The app launches when setup completes.

## First run

A **Set renewal day** dialog appears:

1. Enter the day of the month your Cursor quota resets (1-31). Example: enter **15** if quotas reset on the 15th.
2. Confirm the cycle preview (start date, next renewal, day count). Months that do not contain that day are skipped (for example, day 31 in February).
3. Choose **OK**.

Canceling this dialog exits the app. After you sign in (below), Cursor supplies the billing cycle and this day is no longer editable.

## Sign in to Cursor

Open **Settings** from the title bar.

1. Under **Cursor account (disconnected)**, choose **Sign in**.
2. Complete Google, GitHub, or two-factor sign-in in the embedded window. The window closes when Cursor accepts the session. If you already see your account, choose **Continue**.
3. After a successful update, the heading shows **Cursor account (connected)** and a last-updated time appears under the info cards.

While signed in:

- Usage samples are stored locally and plotted on the chart.
- The billing cycle start and next renewal come from Cursor.
- Calendar day edits, title-bar **Reset**, **Reset cycle**, and **Change renewal day** are hidden or disabled.
- **Export Usage** appears next to **Export Cycle CSV**.

Choose **Refresh now** to fetch immediately. **Sign out** clears the saved Cursor session. Samples stay on disk and still pin expected percents until you delete `usage-samples.json` or uninstall.

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

- **Cycle start** and **Next renewal**
- **Cursor Models run out** and **Other Models run out** (formatted dd-MMM, or — when the quota is not projected to run out before renewal)
- A last-updated caption when a Cursor session exists (`Updated HH:mm`, or a short status if a fetch failed)

The body is a month calendar or a usage chart for the current cycle. Switch with the calendar and chart icons on the right of the info cards (selected is accent color, the other is dimmed). The calendar highlights today and the renewal day. A projected run-out day has a subtle yellow background.

Each calendar day shows the expected (renewal-paced) value on the left. The estimated burn value on the right appears only on days after the last sample or edit for that quota. A teal day number means that day has a synced sample; a blue day number means a manual edit.

The chart is read-only: expected percents are dashed lines from cycle start to next renewal, estimated percents are solid from the last sample or edit to next renewal, and a gray line marks 100%. Markers are the cycle-start origin, every sync sample (placed by time of day), and manual edits. Switch back to the calendar to edit a day (signed out only).

Title bar actions:

| Control | Action |
| --- | --- |
| **Reset** | Clear all manual edits and regenerate an even distribution (hidden while signed in) |
| **Settings** | Cursor account, startup, renewal day, reset, CSV export |
| **Quit** | Exit the process (does not keep the tray icon) |
| Window close (X) | Hide the window; the app stays in the tray |

The window is a fixed size. It restores its last position on show and launch. Selecting a day (when editing is allowed) expands it to show the edit panel. Informational labels can be selected and copied.

## Expected vs estimated

- **Expected** (calendar left, chart dashed): on a day with a last-of-day sample or a manual edit, that pin is the value. After a pin, remaining quota is paced to 100% at the next renewal. A later pin does not rewrite the days between pins. Days before the first pin stay on the even 0-to-100 schedule.
- **Estimated** (calendar right, chart solid): Theil-Sen daily burn from samples (or from edits when there are no samples). It can exceed 100% before renewal. On the chart it starts at the last sample or edit, not at cycle start.

Each quota is independent. A Cursor Models pin never changes Other Models.

### Example

In a 31-day cycle, set day 15 to 35% with no other edits:

- Days 1-14 keep the even schedule (the same values they had with no edits).
- Day 15 is 35% (manual).
- Days 16-31 rise from 35% toward 100% at the next renewal (day 31 is still below 100%).

## Edit a day

Editing is available only while the Cursor account is disconnected.

1. Click a day in the calendar.
2. Set **Cursor Models Quota** and/or **Other Models Quota**.
3. Choose **Apply** to save, **Reset** to clear that day's manual values, or the close control to leave the panel without applying.

Unedited days are not stored; they are recomputed from cycle length and the surrounding pins. **Reset** in the title bar (or **Reset cycle** in Settings) discards every manual edit for the cycle.

## Settings

Open **Settings** from the title bar.

| Setting | Effect |
| --- | --- |
| **Sign in** | Open the Cursor session window (disabled while already signed in) |
| **Refresh now** | Fetch usage immediately |
| **Sign out** | Clear the saved Cursor session |
| **Update usage automatically** | Clock-aligned refreshes at the interval below |
| **Refresh interval (hours)** | 1, 2, 4, 6, or 12 |
| **Run at Windows sign-in** | Starts the app at sign-in, minimized to the tray (`--background`) |
| **Change renewal day** | Builds a new cycle and discards manual edits (hidden while signed in) |
| **Reset cycle** | Same as title-bar Reset (hidden while signed in) |
| **Export Cycle CSV** | Writes each day: expected and estimated percents, and whether the day is a data point |
| **Export Usage** | Writes collected sample timestamps and percents (shown while signed in) |

## System tray

While the process is running, an icon stays in the notification area.

- **Left-click** or **Open**: show the window
- **Quit** (tray menu or title bar): exit

Hover over the tray icon to see today's expected percentage and the end-of-period (`EOP`) projection for Cursor and Other Models. `EOP` is the estimate at the next renewal instant. It is omitted until enough data exists.

If the icon is missing, expand the overflow chevron (`^`). After Explorer or a session switch (lock/unlock), the icon should return within a few seconds.

## Startup and single instance

- With **Run at Windows sign-in** on, a new sign-in starts the app in the tray. Click the icon to open the window.
- From the Start menu, the window opens immediately.
- Only one process runs. Launching again activates the existing window.

## Data

Files live under:

```text
%LocalAppData%\CursorUsageProgress\
```

| Path | Contents |
| --- | --- |
| `settings.json` | Renewal day, startup, sync interval, last window position, connection flag, last successful sync time, and manual edits for the current cycle |
| `usage-samples.json` | Collected usage samples for the current Cursor billing cycle |
| `WebView2\` | Embedded browser profile (Cursor session cookies) |

Copy the folder to back up. Delete it to start over (the next launch asks for a renewal day).

If `settings.json` cannot be read, the app copies it to `settings.corrupt.json` and writes a blank settings file. The same backup naming is used for `usage-samples.json`.

Uninstalling removes this folder. Back it up first if you want to keep edits and samples.

## Renewal

**Signed in:** the next successful fetch that reports a new billing-cycle start replaces the cycle and drops samples from the previous cycle.

**Signed out:** on the next activation after midnight on renewal day (window shown, or the five-minute timer while the window is open):

1. The old cycle and its manual edits are discarded.
2. A new even distribution is generated for the new cycle.
3. Today's percentages update.

The window does not need to stay visible, but the process must be running for midnight rollover and for automatic usage updates.

## Uninstall

1. Quit from the title bar or the tray menu.
2. Windows Settings, **Apps**, **Installed apps**, **Cursor Usage Progress**, **Uninstall**.

## Troubleshooting

**App will not start**

- In Task Manager, end any `CursorUsageProgress.exe` process, then launch again.
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
- If you are signed in, check the last-updated caption and **Refresh now**.
- If you are signed out, confirm any manual pins on earlier days; later pins do not rewrite the gap.

**Auto-start not working**

- Confirm **Run at Windows sign-in** is on in Settings.
- Registry (current user): `Software\Microsoft\Windows\CurrentVersion\Run`, value `CursorUsageProgress`, command including `--background`.

## Tips

- Info-card dates use dd-MMM; calendar and edit-panel dates use the system format.
- The UI follows Windows light, dark, and high-contrast themes.
- High-DPI scaling is handled by WinUI.
