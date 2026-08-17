# Quick start

End-user guide for Cursor Quota Progress. For building from source, see [dev/DEVELOPMENT.md](dev/DEVELOPMENT.md).

## Install

1. Download `CursorQuotaProgress-*-win-x64-setup.exe` from this repository's Releases page.
2. Run the installer. If SmartScreen warns that the app is unsigned, choose **More info**, then **Run anyway**.
3. Finish the wizard. The app launches when setup completes.

## First run

A **Set renewal day** dialog appears:

1. Enter the day of the month your Cursor quota resets (1-31). Example: enter **15** if quotas reset on the 15th.
2. Confirm the cycle preview (start date, next renewal, day count). Months that do not contain that day are skipped (for example, day 31 in February).
3. Choose **OK**.

Canceling this dialog exits the app. You can change the day later in **Settings**.

## Main window

The header shows:

- **Cycle start** and **Next renewal**
- **Cursor Models run out** and **Other Models run out** (formatted dd-MMM, or — when the quota is not projected to run out before renewal)

The body is a month calendar for the current cycle. Today and the renewal day are highlighted. A projected run-out day has a subtle yellow background. Each day shows the renewal-paced value on the left and, when enough data exists, the estimated burn value on the right.

Title bar actions:

| Control | Action |
| --- | --- |
| **Reset** | Clear all manual edits and regenerate an even distribution |
| **Settings** | Startup, renewal day, reset, CSV export |
| **Quit** | Exit the process (does not keep the tray icon) |
| Window close (X) | Hide the window; the app stays in the tray |

The window is a fixed size. Selecting a day expands it to show the edit panel.

## Edit a day

1. Click a day in the calendar.
2. Set **Cursor Models Quota** and/or **Other Models Quota**.
3. Choose **Apply** to save, **Reset** to clear that day's manual values, or the close control to leave the panel without applying.

Notes:

- Each quota is independent. Editing Cursor Models never changes Other Models.
- Manual days are anchors. Days before the first edit interpolate from 0% toward that edit. Days between two edits interpolate between them. Days after the last edit interpolate toward 100% at renewal for the left-hand renewal-paced value. The right-hand estimate uses the observed burn rate and can exceed 100% before renewal.
- Unedited days are not stored; they are recomputed from cycle length and the surrounding anchors.
- **Reset** in the title bar (or **Reset cycle** in Settings) discards every manual edit for the cycle.

### Example

In a 31-day cycle, set day 15 to 35% with no other edits:

- Days 1-14 rise from 0% toward 35%.
- Day 15 is 35% (manual).
- Days 16-31 rise from 35% toward 100% at the next renewal (day 31 is still below 100%).

## Settings

Open **Settings** from the title bar.

| Setting | Effect |
| --- | --- |
| **Run at Windows sign-in** | Starts the app at sign-in, minimized to the tray (`--background`) |
| **Change renewal day** | Builds a new cycle and discards manual edits |
| **Reset cycle** | Same as title-bar Reset: even distribution, current renewal day kept |
| **Export CSV** | Writes the current cycle (linear vs recalculated percents, and which days are manual) |

## System tray

While the process is running, an icon stays in the notification area.

- **Left-click** or **Open**: show the window
- **Quit** (tray menu or title bar): exit

Hover over the tray icon to see today's renewal-paced percentage and estimated burn percentage for Cursor and Other Models. The estimate is omitted until enough manual data points exist.

If the icon is missing, expand the overflow chevron (`^`). After Explorer or a session switch (lock/unlock), the icon should return within a few seconds.

## Startup and single instance

- With **Run at Windows sign-in** on, a new sign-in starts the app in the tray. Click the icon to open the window.
- From the Start menu, the window opens immediately.
- Only one process runs. Launching again activates the existing window.

## Data

Settings live at:

```text
%LocalAppData%\CursorQuotaProgress\settings.json
```

That file holds renewal day, startup preference, and manual edits for the current cycle. Copy the folder to back up. Delete it to start over (the next launch asks for a renewal day).

If the file cannot be read, the app copies it to `settings.corrupt.json` and writes a blank settings file.

Uninstalling removes this folder. Back it up first if you want to keep edits.

## Renewal

On the next activation after midnight on renewal day (window shown, or the five-minute timer while the window is open):

1. The old cycle and its manual edits are discarded.
2. A new even distribution is generated for the new cycle.
3. Today's percentages update.

The window does not need to stay visible, but the process must be running.

## Uninstall

1. Quit from the title bar or the tray menu.
2. Windows Settings, **Apps**, **Installed apps**, **Cursor Quota Progress**, **Uninstall**.

## Troubleshooting

**App will not start**

- In Task Manager, end any `CursorQuotaProgress.exe` process, then launch again.
- If it still fails, check Windows Event Viewer for the application error.

**Tray icon disappeared**

- Expand the notification overflow (`^`).
- Lock and unlock Windows, or restart the app.

**Settings not saving**

- Confirm write access to `%LocalAppData%\CursorQuotaProgress`.
- If `settings.corrupt.json` exists, the previous file was unreadable. Delete both files to reset.

**Wrong percentage for today**

- Confirm system date, time, and time zone. The app uses local time.

**Auto-start not working**

- Confirm **Run at Windows sign-in** is on in Settings.
- Registry (current user): `Software\Microsoft\Windows\CurrentVersion\Run`, value `CursorQuotaProgress`, command including `--background`.

## Tips

- Info-card dates use dd-MMM; calendar and edit-panel dates use the system format.
- The UI follows Windows light, dark, and high-contrast themes.
- High-DPI scaling is handled by WinUI.

