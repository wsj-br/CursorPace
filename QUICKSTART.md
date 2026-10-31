# Quick start

End-user guide for Cursor Pace. For building from source, see [dev/DEVELOPMENT.md](dev/DEVELOPMENT.md).

## Install

Download a build from this repository's Releases page.

**Windows.** Run `CursorPace-*-win-x64-setup.exe`. If SmartScreen warns that the app is unsigned, choose **More info**, then **Run anyway**. **Sign in** needs the [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703). The installer offers the download page when it is missing.

**Linux.** For most distros, `chmod +x CursorPace-*-linux-x64.AppImage` (or the `linux-arm64` file) and run it. On Debian or Ubuntu, `sudo apt install ./CursorPace-*-linux-x64.deb` (or `linux-arm64`) pulls in GTK and WebKit. Google sign-in may be blocked in WebKit; use GitHub, email, or sign in on Windows.

**macOS.** Open `CursorPace-*-osx-arm64.dmg` (Apple Silicon) or `*-osx-x64.dmg` (Intel) and drag `CursorPace.app` to Applications. If Gatekeeper blocks the unsigned app, right-click **Open** once, then choose **Open Anyway** in **System Settings → Privacy & Security**.

## First run

**Start in notification tray** is on by default, so the first launch may show only the tray icon. Open the window from that icon, or launch with `--show`.

1. Choose **Sign in**.
2. Finish Google, GitHub, or two-factor sign-in in the embedded window. It closes when Cursor accepts the session. If you already see your account, choose **Continue**.
3. The billing cycle and usage appear on the chart.

You can sign in later from **Settings** → **Cursor account**. After a successful update that heading reads **Cursor account (connected)**. **Refresh** in the title bar, or **Refresh now** in Settings, fetches immediately.

**Sign out** clears the Cursor session. On Windows and macOS a saved Google or GitHub login can remain, so the next sign-in may skip that password. On Linux, **Sign out** clears the whole browser profile. Usage samples stay on disk.

With **Update usage automatically** on, refreshes fall on a 1, 2, 4, 6, or 12 hour mark from local midnight. On launch, a refresh runs only after the last success is at least 20 minutes old and a scheduled slot was missed or the full interval has already elapsed.

## Main window

Three cards sit above the chart:

- **Cycle start** and **Next renewal**. On the live cycle, the time remaining sits beside the renewal date.
- A forecast for each model. Inside the cycle this is the run-out time. Above the dashed expected line it is when that line catches the reading. On or below the line it is the projected percent at renewal. On the live cycle, a date includes the wait beside it.
- **Last measure**: the newest sample in this cycle, with Cursor %, Other %, Expected usage %, and how far each series is from that pace.

Previous and Next on the month heading move through stored cycles. Under that heading, `Cursor dd-MMM HH:mm` is the latest refresh. A red dot means the account is signed out or the refresh failed. Click it to open **Cursor account**. When a sync server is configured, **Sync server** and its last success time appear there too. A red dot means the last attempt failed. Click it to open **Sync Server**.

| Range | What it shows |
| --- | --- |
| **1D** / **2D** | The last 24 or 48 hours |
| **7D** / **1W** | The last seven days |
| **2W** | The last 14 days |
| **1M** | The whole displayed cycle, one sample per local day |

On the live cycle a short range ends at the current time. On an earlier cycle it ends at that cycle's renewal. The start stays at the cycle start when the window would otherwise begin earlier.

Drag across the plot to zoom. Right-click returns to **1M**. Choosing a range button, including the one already selected, leaves the zoom and shows that range. **Every sample up to** (**2D**, **4D**, **7D**, or **14D**, default **14D**) sets how far the chart plots every sample and labels the local time. Longer ranges keep the last sample of each day and label the day of the month. The vertical axis fits the values on screen, up to 120%.

Move the pointer to read Cursor, Other Models, and Expected usage at that time. Past the last sample, Cursor and Other show the estimate, marked `estimated`. During a zoom, the readout also shows each value at the zoom start and the change from there.

| Control | Action |
| --- | --- |
| **Refresh** | Fetch Cursor usage now. Hidden on the Settings page |
| **Settings** | Open Settings in this window |
| **Back** | Return to the chart |
| **Minimize** / **Maximize** | Standard window controls. The app stays in the tray |
| Close | Hide the window. The app stays in the tray |

The window remembers its size, position, and maximized state. Informational labels can be selected and copied.

## Expected and estimated

- **Expected usage** on the chart is the dashed line from 0% at cycle start to 100% at the next renewal. Hover uses this line.
- **Cursor** and **Other Models** are the measured samples.
- **Expected** in a cycle CSV follows each sample, then paces the remaining quota to 100% at renewal.
- **Estimated** is the round-dotted projection from the last sample toward renewal. Past the last sample, hover uses this line.

Cursor Models and Other Models stay independent.

## Settings

Settings replace the chart. **Back**, or the **Settings** heading, returns to it. The last tab opens again next time, including after a restart.

**Sync Server** is optional. [CursorPace Sync Server](https://github.com/wsj-br/CursorPace-SyncServer) shares usage samples and billing-cycle bounds across machines on a local network, so a Mac, a Windows PC, and a Linux machine in the same development environment show the same chart. Install and run the server from that repository. Theme, window placement, and startup options stay on each computer.

| Tab | What you can set |
| --- | --- |
| Startup & Display | **Launch at login**, **Start in notification tray**, theme (System, Light, or Dark), and **Every sample up to** |
| Cursor account | **Sign in** (hidden while connected), **Refresh now**, **Sign out**, and automatic refresh at 1, 2, 4, 6, or 12 hours |
| Sync Server | **Sync with server**, server URL, API token, machine name (the hostname by default), and **Re-sync now**. The link opens the sync server repository |
| Export & Backup | **Export Cycle CSV** for the cycle on screen, **Export Usage** for every stored sample while signed in, and **Backup** / **Restore** of settings and samples as one zip. **Open Folder** opens the destination |
| About | Version, UTC build date and time, copyright, MIT license, and the GitHub repository |

Backup and Restore leave the Cursor sign-in on this machine.

## Tray

Click the icon, or choose **Open**, to show the window. **Quit** exits. The tooltip shows today's percent and, once there is enough data, the projected percent at renewal.

On macOS, a click opens the tray menu, and the Dock icon is hidden while the window is hidden or minimized. Show the window from the tray, with `--show`, or by launching the app again.

If the icon is missing, open the notification overflow. On GNOME, the AppIndicator extension may be required.

A second launch shows the window that is already running. A second `--background` launch leaves it hidden. `--show` forces the window open. **Launch at login** follows **Start in notification tray**.

## Data

```text
Windows: %LocalAppData%\CursorPace\
Linux:   ~/.local/share/CursorPace/
macOS:   ~/Library/Application Support/CursorPace/
```

`settings.json` holds preferences, window placement, and cycle bounds. `usage-samples.json` holds the samples. The Cursor sign-in lives in `WebView2` on Windows, `WebView` on Linux and macOS, or `WebView-AppImage` when running a Linux AppImage. A file that cannot be parsed is copied to `*.corrupt.json` and replaced with a blank one. A locked or unreadable file is left as it is.

Delete the folder to start over. On Windows, uninstalling the app removes it. On Linux and macOS, delete it yourself.

## Renewal

A fetch that reports a new cycle start archives the previous cycle and keeps its samples. Previous and Next open those cycles. The app also refreshes when the local date reaches the stored renewal. The window can stay hidden. The process has to be running for that check and for automatic updates.

## Uninstall

Quit from the tray menu. On Windows, uninstall **Cursor Pace** from **Installed apps**. On Linux and macOS, delete the app and, for a clean start, the data folder above.

## Troubleshooting

**The app will not start.** End any `CursorPace` process and launch again. On Windows, check Event Viewer. On Linux, try `journalctl --user -xe`. On macOS, check Console.app. If Linux sits at 100% CPU, or `crash.log` says `Could not create glyphTypeface`, install a release newer than 0.3.0. If **Launch at login** still points at a deleted AppImage, run the new AppImage once.

**Sign-in fails, or "The specified module could not be found".** On Windows, install the [WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (Evergreen x64). On Linux, install `libwebkit2gtk-4.1-0`. If Google blocks the embedded browser, use GitHub or email. Choose **Continue** when your Cursor account is already on screen.

**Usage does not update.** Confirm **Cursor account (connected)** and automatic updates, or choose **Refresh**. Check the system date, time, and time zone. After a Cursor rate limit, the app waits for the next interval.

**The tray or taskbar icon is missing.** Open the notification overflow. On Linux, restart the app so it can write `~/.local/share/applications/CursorPace.desktop`. If the taskbar icon is still a generic gear, log out once, or on X11 press Alt+F2, type `r`, and press Enter.

**Settings are not saving, or today's percent looks wrong.** Confirm you can write the data folder. If `settings.corrupt.json` exists, delete it and `settings.json` to reset. Then check the clock and choose **Refresh**.

**Launch at login does nothing.** Confirm the setting is on. Windows stores `CursorPace` in the current user's Run key, and adds `--background` when tray start is on. A signed macOS app appears under **Open at Login**. An unsigned build uses `~/Library/LaunchAgents/com.cursorpace.app.plist` and starts the `.app` with `/usr/bin/open`. Linux uses `~/.config/autostart/cursor-pace.desktop`. An AppImage entry has to point at the `.AppImage` file. Turn the setting off and on, or launch the new AppImage once, to rewrite it.

**Two tray icons, or signed out after a Linux reboot.** Quit from the tray and start the app once. On Linux, sign in again on a current build so the session is stored in `cookies.sqlite`.
