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
2. Unzip and move `CursorPace.app` to Applications (or run from the download folder).
3. If Gatekeeper blocks the unsigned build, attempt to open it once, then open **System Settings → Privacy & Security** and choose **Open Anyway**.
4. Launch the app and sign in to Cursor.

## First run

**Start in notification tray** is on by default, so the first launch may show only the tray icon. Open the window from that icon, turn the setting off, or launch with `--show`. When running from source, `dev.ps1` and `dev.sh` show the window by default; use `-NoShow` or `--no-show` to use the app's normal startup visibility. The window starts with an empty state until you sign in:

1. Choose **Sign in**.
2. Complete Google, GitHub, or two-factor sign-in in the embedded window. The window closes when Cursor accepts the session. If you already see your account, choose **Continue**.
3. After a successful update, the billing cycle and usage appear on the chart. A last-updated time appears under the month heading.

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

Choose title-bar **Refresh** or Settings **Refresh now** to fetch immediately. On Linux that fetch uses a 1x1 transparent host and should not flash the sign-in window; the sign-in window still appears when the session needs it. **Sign out** clears the saved Cursor session. On Windows and macOS this keeps a Google or GitHub session stored in the app's private browser profile when possible, so signing in again may not ask for that password; on Linux the embedded WebKitGTK browser has no way to clear only the Cursor session, so **Sign out** clears the whole browser profile there, including Google/GitHub. Samples stay on disk until you delete `usage-samples.json` or uninstall.

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

The body is a usage chart for the displayed billing cycle. Three cards sit above it: cycle start and next renewal, Cursor Models and Other Models run-out times, and the latest reading with its time, Cursor %, Other %, and Expected usage %. The heading shows the cycle-start month and year, with Previous/Next chevrons to move through stored cycles (disabled on the oldest cycle and on the current one) and the last-updated time under that heading.

The chart has range buttons: **1D**, **2D**, **7D**, **1W**, **2W**, and **1M**. **1D** is the last 24 hours and **2D** the last 48 hours; both plot every sample in that window. **7D** and **1W** are the last seven days, and **2W** is the last 14 days. Those longer ranges connect the last sample of each local day. **1M** shows the whole displayed cycle. On the current cycle a short range ends at the current time. On an earlier cycle it ends at that cycle's renewal. A range never starts before the cycle start, so early in a cycle **7D**, **1W**, and **2W** show only the days that have happened. Choosing a range fits the vertical axis to the values on screen, rounded outward to the next 10% step.

Click and drag across the plot to select an interval. Releasing the pointer zooms to that interval and clears the range-button highlight. **Settings** → **Startup & Display** → **Every sample up to** chooses **2D**, **4D**, or **7D**. The default is **4D**. Changing this setting preserves an active zoom when you return to the chart. A preset or zoom up to that length plots every sample; a longer interval keeps the last sample of each local day. **1M** and **2W** always keep one sample per day. Right-click the plot to return to **1M**. Choosing **1D**, **2D**, **7D**, **1W**, **2W**, or **1M** leaves the zoom and shows that range, including the button that was already selected.

A dashed **Expected usage** line runs linearly from 0% at cycle start to 100% at next renewal. Thick solid **Cursor** and **Other Models** paths follow the samples. Intervals up to the **Every sample up to** setting draw a small circle on each measured sample, in that series color. The expected line and the estimated lines have no circles. Each path shows its last measured percent inside the selected range to the right of that point (`xx.x%`, in the series color). A dotted vertical guide at that same time rises from the X axis to the Expected usage line and labels the linear expected percent there. Thinner estimated lines run from the last sample toward next renewal, clipped to the selected range, and a gray line marks 100% when that level is inside the axis. Moving the pointer across the plot draws a vertical guide and shows that time's Cursor, Other Models, and Expected usage at the top left. While a drag zoom is active, each metric also shows its value at the start of that zoom and the change from that start to the pointer (`start 20.0%, +24.4%`). A preset range hides those start rows. A value with no surrounding measurement is shown as a dash, and its change stays blank when either side is missing. Multi-day ranges label the day of the month. Intervals that plot every sample label the time of day, and the hour grid widens so those times do not overlap.

Title bar actions:

| Control | Action |
| --- | --- |
| **Refresh** | Fetch Cursor usage immediately (same as Settings **Refresh now**). Hidden on the Settings page |
| **Settings** | Open Settings in this window (Startup & Display, Cursor account, Sync Server, Export & Backup, About) |
| **Back** | On the Settings page, the chevron or the **Settings** heading returns to the chart |
| **Minimize** | Minimize the window; the app stays in the tray |
| **Maximize** | Maximize or restore the window |
| Window close (X) | Hide the window; the app stays in the tray |

Refresh and Settings are separated from the Minimize, Maximize, and Close controls by a small gap.

Under the cycle heading, a short status line shows the latest Cursor refresh as `Cursor dd-MMM HH:mm`. A red dot appears beside it when the account is signed out or the latest refresh failed. Clicking that time opens **Settings** on **Cursor account**. When **Sync with server** is on and a URL and API token are set, that line also shows **Sync server** and the last successful sync time in the same `dd-MMM HH:mm` format. A red dot appears beside it after a failed attempt. Clicking it opens **Settings** on **Sync Server**.

The window is resizable and can be maximized. It restores its last size, position, and maximized state on show and launch. Informational labels can be selected and copied.

## Expected vs estimated

- **Expected usage** (chart dashed): a straight line from 0% at cycle start to 100% at the next renewal. It does not pass through samples. The hover readout uses this line at the pointer time.
- **Usage** (chart thick solid): Cursor and Other Models paths. Intervals up to the **Every sample up to** setting use every sample in the window and mark each measured sample. Longer ranges use the last in-cycle sample of each local day, draw no sample circles, and are omitted until at least two local dates have samples.
- **Expected** (CSV expected columns): a continuous line from 0% at cycle start through each sample's timestamp, then remaining quota paced to 100% at the next renewal. Days before the first sample rise toward that sample.
- **Estimated** (chart thin solid): Theil-Sen daily burn from samples. It can exceed 100% before renewal. On the chart it is a straight line from the last sample to next renewal, clipped to the selected range.

Each quota is independent.

## Settings

Open **Settings** from the title bar. Settings replace the chart in the main window. A **Back** control and a **Settings** heading sit below the title bar; either one returns to the chart. Settings are grouped into **Startup & Display**, **Cursor account**, **Sync Server**, **Export & Backup**, and **About** tabs. The last tab opens again the next time you enter Settings, including after you quit and start the app.

| Tab | Setting | Effect |
| --- | --- | --- |
| Startup & Display | **Launch at login** | Starts the app at OS login (Windows Run key, macOS `SMAppService` / attributed Launch Agent fallback, or Linux XDG autostart) |
| Startup & Display | **Start in notification tray** | Start with only the tray icon. Off opens the window. `--background` does the same. `--show` forces the window open |
| Startup & Display | **Theme** | System (default), Light, or Dark. Overrides the Fluent theme variant for the app |
| Startup & Display | **Every sample up to** | **2D**, **4D** (default), or **7D**. Chart intervals up to that length plot every sample and mark each measured point. Longer intervals keep the last sample of each day. **2W** and **1M** always keep one sample per day |
| Cursor account | **Sign in** | Open the Cursor session window (disabled while already signed in) |
| Cursor account | **Refresh now** | Fetch usage immediately (same action as title-bar **Refresh**) |
| Cursor account | **Sign out** | Clear the saved Cursor session (keeps a Google/GitHub session in the browser profile on Windows/macOS when possible; clears the whole profile on Linux) |
| Cursor account | **Update usage automatically** | Clock-aligned refreshes at the interval below |
| Cursor account | **Refresh interval (hours)** | 1, 2, 4, 6, or 12 |
| Sync Server | **Sync with server** | Optional. Share usage samples and billing-cycle bounds with other machines through a CursorPace sync server. Syncs on launch, after each new Cursor sample, every 10 minutes, and when you click **Re-sync now** |
| Sync Server | **Server URL** | Sync server origin, for example `http://127.0.0.1:8000` |
| Sync Server | **API token** | Token from the **Tokens** page of your CursorPace Sync server. The eye control shows or hides the value |
| Sync Server | **Machine name** | Label shown on the server. Defaults to this computer's hostname |
| Sync Server | **Re-sync now** | Push local data, then pull the merged canonical state |
| Sync Server | **Repository** | When the server URL and API token are not set, points at the [CursorPace Sync Server](https://github.com/wsj-br/CursorPace-SyncServer) install instructions. After those fields are set, the same card shows that repository link |
| Export & Backup | **Export Cycle CSV** | Writes each day of the cycle currently shown on the chart: expected and estimated percents, and whether the day is a data point. The suggested name includes the current date and time (`yyyy-MM-dd-HH_mm_ss`) |
| Export & Backup | **Export Usage** | Writes all retained sample timestamps and percents across stored cycles (shown while signed in). The suggested name includes the current date and time (`yyyy-MM-dd-HH_mm_ss`) |
| Export & Backup | **Backup** | Writes `manifest.json`, `settings.json`, and `usage-samples.json` as one `.zip` file (suggested name `cursor-pace-backup-yyyy-MM-dd-HH_mm_ss`) |
| Export & Backup | **Restore** | Replaces local settings and samples from a backup zip. The Cursor sign-in session is not changed |
| About | **About** | Version from the build, UTC compile date and time (`dd-MMM-yyyy HH:mm:ss UTC`), copyright, MIT license, and a link to the GitHub repository |

After a local export or backup completes, choose **Open Folder** on the left side of the completion dialog to open its destination folder, or choose **OK** on the right to close it.

## System tray

While the process is running, an icon stays in the notification area.

- **Left-click** or **Open**: show the window
- **Quit** (tray menu): exit

Hover over the tray icon to see today's expected percentage and the projected percent at the next renewal for Cursor and Other Models. The renewal projection is omitted until enough data exists.

If the icon is missing, expand the overflow chevron (`^`). On Linux, GNOME may need the AppIndicator extension. On macOS, left-click opens the tray menu; choose **Open** from that menu. macOS also removes the Dock icon after the main window hides and while it is minimized; restore the window from the tray, `--show`, or a second launch of the app.

## Startup and single instance

- With **Launch at login** on, a new OS login session starts the app. **Start in notification tray** (on by default) keeps the window hidden; turn that off to open the window. Click the tray icon to open the window. On macOS that hidden state also removes the Dock icon until the window is shown.
- From the Start menu or app launcher, the window opens unless **Start in notification tray** is on. Pass `--show` to force the window open regardless of that setting or `--background`.
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
| `settings.json` | Startup, theme, last Settings tab, Cursor refresh interval, last window position and size, connection flag, last successful Cursor sync time, optional sync-server URL/key/machine name, the current cycle bounds, and previous cycle bounds (`cycleHistory`) |
| `usage-samples.json` | Collected usage samples for stored Cursor billing cycles |
| `WebView2\` | Windows embedded browser profile (Cursor session cookies) |
| `WebView\` | Linux and macOS embedded browser profile |
| `WebView-AppImage\` | Linux only: used instead of `WebView\` when running from an AppImage, so its bundled WebKit never shares a cookie store with a non-AppImage run |

**Backup** in Settings writes `settings.json` and `usage-samples.json` as one zip. It does not include the WebView profile, so Restore does not sign you in on another machine. Copy the whole folder to back up the Cursor session as well. Delete the folder to start over (the next launch asks you to sign in).

If `settings.json` cannot be parsed, the app copies it to `settings.corrupt.json` and writes a blank settings file. A locked or unreadable file is left in place so a transient I/O error cannot wipe settings. The same backup naming is used for `usage-samples.json`.

On Windows, uninstalling via the Inno installer removes this folder. On Linux and macOS, delete the folder yourself if you want a clean start. Back it up first if you want to keep samples.

## Renewal

The next successful fetch that reports a new billing-cycle start archives the previous cycle bounds and keeps its samples. Previous/Next on the month heading opens those stored cycles. Short chart ranges on an earlier cycle end at that cycle's renewal. If the local date reaches the stored next renewal before that fetch, the app requests a refresh.

The window does not need to stay visible, but the process must be running for midnight checks and for automatic usage updates.

## Uninstall

1. Quit from the tray menu.
2. Windows: Settings, **Apps**, **Installed apps**, **Cursor Pace**, **Uninstall**. That removes the app and the data folder.
3. Linux/macOS: delete the published folder (or app bundle) and, if you want a clean start, the data folder listed above.

## Troubleshooting

**App will not start**

- End any `CursorPace` process, then launch again.
- If it still fails on Windows, check Event Viewer for the application error. On Linux try `journalctl --user -xe`; on macOS check Console.app.
- macOS Console `Invalid view geometry: y is NaN` from `WKWebView` was caused by both early host layout and an ABI mismatch in the released Avalonia WebView macOS interop. Current builds use a compatible local WebView build and attach it only after finite arrange. Rebuild from this tree if you still see that report.
- Linux **Refresh** should not open a full-size sign-in window. If it does, you are on a build that still maps the macOS-style off-screen host on Linux; rebuild from this tree.

**Sign in fails or "The specified module could not be found"**

- Windows: install the [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (Evergreen x64).
- Linux: install WebKitGTK 4.1 (`libwebkit2gtk-4.1-0`). Google may block embedded WebKit login; use GitHub or email, or sign in on Windows.
- Retry **Sign in**. The window should close when Cursor accepts the session; use **Continue** if you already see your account.

**Usage does not update**

- Confirm **Cursor account (connected)** in Settings and that **Update usage automatically** is on, or choose title-bar **Refresh** / Settings **Refresh now**.
- Confirm system date, time, and time zone. The app uses local time for the chart and clock-aligned intervals.
- If Cursor rate-limits the request, the app waits until the next interval.

**Tray icon disappeared**

- Expand the notification overflow (`^`).
- On Windows, lock and unlock the session, or restart the app. On Linux/macOS, restart the app or the desktop session.

**Taskbar icon missing (Linux)**

- Restart the app. A launch writes `~/.local/share/applications/CursorPace.desktop` so GNOME can match the open window to the app icon. The window stays in the taskbar while it is open; closing it hides to the tray. If the icon is still the generic gear, log out of the desktop session once, or press Alt+F2, type `r`, and Enter (X11 sessions).

**Settings not saving**

- Confirm write access to the data folder for your OS (`%LocalAppData%\CursorPace`, `~/.local/share/CursorPace`, or `~/Library/Application Support/CursorPace`).
- If `settings.corrupt.json` exists, the previous file was unreadable. Delete both files to reset.

**Wrong percentage for today**

- Confirm system date, time, and time zone.
- Check the last-updated caption and title-bar **Refresh** or Settings **Refresh now**.

**Auto-start not working**

- Confirm **Launch at login** is on in Settings.
- Windows registry (current user): `Software\Microsoft\Windows\CurrentVersion\Run`, value `CursorPace`. With **Start in notification tray** the command includes `--background`.
- macOS 13+: a signed bundle appears under System Settings → General → Login Items → **Open at Login** as `CursorPace`. Unsigned builds use `~/Library/LaunchAgents/com.cursorpace.app.plist` as a compatibility fallback; its `AssociatedBundleIdentifiers` must contain `com.cursorpace.app` and its `ProgramArguments` must start `/usr/bin/open -a` on the `.app` bundle, not `Contents/MacOS/CursorPace`. Launch the app once after updating, or turn **Launch at login** off and on, to rewrite the registration.
- Linux: `~/.config/autostart/cursor-pace.desktop`. For an AppImage, `Exec` must be the `.AppImage` file, not a path under `/tmp/.mount_*`. Launch the app once after updating, or turn **Launch at login** off and on, to rewrite the file.

**Two tray icons or a crash at login**

- The session started two copies (login autostart plus a restored session, or an AppImage helper). Current builds keep the first process and exit the second before creating another tray icon. Quit from the tray menu, then start the app once.

**Signed out after a Linux reboot**

- Sign in once more on a build that persists WebKit cookies (`cookies.sqlite` under `WebView/` or `WebView-AppImage/`). WebView2 on Windows already keeps the session across reboots.

## Tips

- Info-card dates use dd-MMM HH:mm. Chart day labels use the day of the month, and the 1-day and 2-day ranges use the local time.
- The UI theme defaults to the system light or dark setting. Override it under **Settings** → **Startup & Display** → **Theme** (System, Light, or Dark).
