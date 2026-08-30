# Quick start

End-user guide for Cursor Pace. For building from source, see [dev/DEVELOPMENT.md](dev/DEVELOPMENT.md).

## Install

### Windows

1. Download `CursorPace-*-win-x64-setup.exe` from this repository's Releases page.
2. Run the installer. If SmartScreen warns that the app is unsigned, choose **More info**, then **Run anyway**.
3. If the installer reports that Microsoft Edge WebView2 Runtime is missing, open the download page it offers. **Sign in** on Windows needs the runtime.
4. Finish the wizard. The app launches when setup completes.

### Linux

1. Download `CursorPace-*-linux-x64.AppImage` (x86_64) or `*-linux-arm64.AppImage` (ARM64) from Releases.
2. Make it executable: `chmod +x CursorPace-*.AppImage`
3. Run the AppImage (double-click or from a terminal). The bundle includes GTK/WebKit dependencies from the build host; most recent distros work without extra packages.
4. Google sign-in may be blocked in WebKit; use GitHub, email, or sign in on Windows if needed.

### macOS

1. Download `CursorPace-*-osx-arm64.zip` (Apple Silicon) or `*-osx-x64.zip` (Intel) from Releases.
2. Unzip and move `Cursor Pace.app` to Applications (or run from the download folder).
3. If Gatekeeper blocks the unsigned build, attempt to open it once, then open **System Settings → Privacy & Security** and choose **Open Anyway**.
4. Launch the app and sign in to Cursor.

## First run

The main window opens with an empty state until you sign in:

1. Choose **Sign in**.
2. Complete Google, GitHub, or two-factor sign-in in the embedded window. The window closes when Cursor accepts the session. If you already see your account, choose **Continue**.
3. After a successful update, the billing cycle and usage appear on the calendar and chart. A last-updated time appears under the month heading (or above the chart).

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

Choose **Refresh now** to fetch immediately. **Sign out** clears the saved Cursor session. On Windows and macOS this keeps a Google or GitHub session stored in the app's private browser profile when possible, so signing in again may not ask for that password; on Linux the embedded WebKitGTK browser has no way to clear only the Cursor session, so **Sign out** clears the whole browser profile there, including Google/GitHub. Samples stay on disk until you delete `usage-samples.json` or uninstall.

### Automatic updates

With **Update usage automatically** on, refreshes run on the clock hour aligned to the interval:

| Interval | Local times |
| --- | --- |
| 1h | 00:00, 01:00, 02:00, … |
| 2h | 00:00, 02:00, 04:00, … |
| 4h | 00:00, 04:00, 08:00, … |
| 6h / 12h | same pattern from midnight |

Launch never refreshes when the last successful update is under 20 minutes old. After that, a launch refresh runs only if a clock-aligned slot was missed or the last update is already older than the interval; otherwise the next refresh waits for the aligned timer.

## Main window

The body is a month calendar or a usage chart for the current cycle. Switch with the calendar and chart icons on the right of the info cards (selected is accent color, the other is dimmed). The calendar shows the month name above the weekday row, highlights today and the renewal day, and includes the renewal date when the cycle still has time left that day. A projected run-out day has a subtle yellow background.

Each calendar day shows two percents. On the left, a day with a synced sample shows that day's last reading (teal day number); a day without a sample shows the expected (renewal-paced) value at that day's midnight. The estimated burn value on the right appears only on days after the last sample for that quota (green when ≤100%, red when >100%).

The chart is read-only: expected percents are dashed lines from 0% at cycle start through each sample to 100% at next renewal, estimated percents are solid from the last sample to next renewal, and a gray line marks 100%. The left and right edges are the cycle start and next renewal instants. Vertical gridlines sit at midnight. Axis labels are the day of the month: the truncated slot before the first midnight is unlabeled, and every other slot (including the renewal-date slot) is labelled. Markers are the cycle-start origin and every sync sample (placed by timestamp).

Title bar actions:

| Control | Action |
| --- | --- |
| **Settings** | Open Settings in this window (account, appearance, startup, CSV, backup) |
| **Back** | On the Settings page, the chevron or the **Settings** heading returns to the calendar or chart |
| **Quit** | Exit the process (does not keep the tray icon) |
| **Minimize** | Minimize the window; the app stays in the tray |
| Window close (X) | Hide the window; the app stays in the tray |

Quit is separated from the Minimize and Close controls by a small gap.

The window is a fixed size. It restores its last position on show and launch. Informational labels can be selected and copied.

## Expected vs estimated

- **Expected** (chart dashed; calendar left on days without a sample): a continuous line from 0% at cycle start through each sample's timestamp, then remaining quota paced to 100% at the next renewal. Days before the first sample rise toward that sample. On the calendar, a day with a synced sample shows that day's last reading on the left instead of this interpolated value.
- **Estimated** (calendar right, chart solid): Theil-Sen daily burn from samples. It can exceed 100% before renewal. On the chart it is a straight line from the last sample to next renewal. The calendar shows it only after the last-update date.

Each quota is independent.

## Settings

Open **Settings** from the title bar. Settings replace the calendar or chart in the main window. A **Back** control and a **Settings** heading sit below the title bar; either one returns to the calendar or chart.

| Setting | Effect |
| --- | --- |
| **Sign in** | Open the Cursor session window (disabled while already signed in) |
| **Refresh now** | Fetch usage immediately |
| **Sign out** | Clear the saved Cursor session (keeps a Google/GitHub session in the browser profile on Windows/macOS when possible; clears the whole profile on Linux) |
| **Update usage automatically** | Clock-aligned refreshes at the interval below |
| **Refresh interval (hours)** | 1, 2, 4, 6, or 12 |
| **Launch at login** | Starts the app at OS login (Windows Run key, macOS Launch Agent, or Linux XDG autostart) |
| **Start in notification tray** | Start with only the tray icon. Off opens the window. `--background` does the same |
| **Theme** | System (default), Light, or Dark. Overrides the Fluent theme variant for the app |
| **Export Cycle CSV** | Writes each day: expected and estimated percents, and whether the day is a data point. The suggested name includes the current date and time (`yyyy-MM-dd-HH_mm_ss`) |
| **Export Usage** | Writes collected sample timestamps and percents (shown while signed in). The suggested name includes the current date and time (`yyyy-MM-dd-HH_mm_ss`) |
| **Backup** | Writes `manifest.json`, `settings.json`, and `usage-samples.json` as one `.zip` file (suggested name `cursor-pace-backup-yyyy-MM-dd-HH_mm_ss`) |
| **Restore** | Replaces local settings and samples from a backup zip. The Cursor sign-in session is not changed |

After a local export or backup completes, choose **Open Folder** on the left side of the completion dialog to open its destination folder, or choose **OK** on the right to close it.

## System tray

While the process is running, an icon stays in the notification area.

- **Left-click** or **Open**: show the window
- **Quit** (tray menu or title bar): exit

Hover over the tray icon to see today's expected percentage and the projected percent at the next renewal for Cursor and Other Models. The renewal projection is omitted until enough data exists.

If the icon is missing, expand the overflow chevron (`^`). On Linux, GNOME may need the AppIndicator extension. On macOS, left-click opens the tray menu; choose **Open** from that menu.

## Startup and single instance

- With **Launch at login** on, a new OS login session starts the app. **Start in notification tray** (on by default) keeps the window hidden; turn that off to open the window. Click the tray icon to open the window.
- From the Start menu or app launcher, the window opens unless **Start in notification tray** is on.
- Only one process runs. Launching again activates the existing window.

## Data

Files live under the OS local app-data folder:

```text
Windows: %LocalAppData%\CursorPace\
Linux:   ~/.local/share/CursorPace/
macOS:   ~/Library/Application Support/CursorPace/
```

| Path | Contents |
| --- | --- |
| `settings.json` | Startup, theme, sync interval, last window position, connection flag, last successful sync time, and the current cycle bounds |
| `usage-samples.json` | Collected usage samples for the current Cursor billing cycle |
| `WebView2\` | Windows embedded browser profile (Cursor session cookies) |
| `WebView\` | Linux and macOS embedded browser profile |
| `WebView-AppImage\` | Linux only: used instead of `WebView\` when running from an AppImage, so its bundled WebKit never shares a cookie store with a non-AppImage run |

**Backup** in Settings writes `settings.json` and `usage-samples.json` as one zip. It does not include the WebView profile, so Restore does not sign you in on another machine. Copy the whole folder to back up the Cursor session as well. Delete the folder to start over (the next launch asks you to sign in).

If `settings.json` cannot be parsed, the app copies it to `settings.corrupt.json` and writes a blank settings file. A locked or unreadable file is left in place so a transient I/O error cannot wipe settings. The same backup naming is used for `usage-samples.json`.

On Windows, uninstalling via the Inno installer removes this folder. On Linux and macOS, delete the folder yourself if you want a clean start. Back it up first if you want to keep samples.

## Renewal

The next successful fetch that reports a new billing-cycle start replaces the cycle and drops samples from the previous cycle. If the local date reaches the stored next renewal before that fetch, the app requests a refresh.

The window does not need to stay visible, but the process must be running for midnight checks and for automatic usage updates.

## Uninstall

1. Quit from the title bar or the tray menu.
2. Windows: Settings, **Apps**, **Installed apps**, **Cursor Pace**, **Uninstall**. That removes the app and the data folder.
3. Linux/macOS: delete the published folder (or app bundle) and, if you want a clean start, the data folder listed above.

## Troubleshooting

**App will not start**

- End any `CursorPace` process, then launch again.
- If it still fails on Windows, check Event Viewer for the application error. On Linux try `journalctl --user -xe`; on macOS check Console.app.

**Sign in fails or "The specified module could not be found"**

- Windows: install the [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (Evergreen x64).
- Linux: install WebKitGTK 4.1 (`libwebkit2gtk-4.1-0`). Google may block embedded WebKit login; use GitHub or email, or sign in on Windows.
- Retry **Sign in**. The window should close when Cursor accepts the session; use **Continue** if you already see your account.

**Usage does not update**

- Confirm **Cursor account (connected)** in Settings and that **Update usage automatically** is on, or choose **Refresh now**.
- Confirm system date, time, and time zone. The app uses local time for the calendar, chart, and clock-aligned intervals.
- If Cursor rate-limits the request, the app waits until the next interval.

**Tray icon disappeared**

- Expand the notification overflow (`^`).
- On Windows, lock and unlock the session, or restart the app. On Linux/macOS, restart the app or the desktop session.

**Settings not saving**

- Confirm write access to the data folder for your OS (`%LocalAppData%\CursorPace`, `~/.local/share/CursorPace`, or `~/Library/Application Support/CursorPace`).
- If `settings.corrupt.json` exists, the previous file was unreadable. Delete both files to reset.

**Wrong percentage for today**

- Confirm system date, time, and time zone.
- Check the last-updated caption and **Refresh now**.

**Auto-start not working**

- Confirm **Launch at login** is on in Settings.
- Windows registry (current user): `Software\Microsoft\Windows\CurrentVersion\Run`, value `CursorPace`. With **Start in notification tray** the command includes `--background`.
- macOS: `~/Library/LaunchAgents/com.cursorpace.app.plist`
- Linux: `~/.config/autostart/cursor-pace.desktop`

## Tips

- Info-card dates use dd-MMM HH:mm; calendar dates use the system format.
- The UI theme defaults to the system light or dark setting. Override it under **Settings** → **Theme** (System, Light, or Dark).
