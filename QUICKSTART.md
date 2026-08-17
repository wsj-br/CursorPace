# Cursor Quota Progress - Quick Start Guide

## Installation

1. Download `CursorQuotaProgress-1.0.0-win-x64-setup.exe` from the releases page
2. Run the installer (SmartScreen may warn since the app is unsigned - click "More info" → "Run anyway")
3. Follow the installation wizard
4. The app will launch automatically after installation

## First Run

On first launch, you'll see the **Set Renewal Day** dialog:

1. Enter your Cursor quota renewal day (1-31)
   - This is the day of the month when your Cursor quotas reset
   - Example: If your quota renews on the 15th of each month, enter **15**

2. The dialog shows a preview of your current cycle:
   - Current cycle dates
   - Number of days until next renewal

3. Click **OK** to save and continue

## Main Window

The main window displays:

### Header
- **Current Cycle**: Start date of current cycle
- **Next Renewal**: When quotas reset to 0%
- **Available Today**: Current day's quota allowances for both models

### Quota Table
Shows the complete cycle with columns:
- **Day**: Day number (1, 2, 3...)
- **Date**: Calendar date
- **Cursor Models %**: Available quota percentage for Cursor models
- **Other Models %**: Available quota percentage for other models

The current day is **highlighted** and automatically scrolled into view.

### Bottom Controls
- **Run at Windows sign-in**: Check to start the app automatically
- **Change Renewal Day**: Update your renewal day (discards manual edits)
- **Quit**: Exit the application

## Using the App

### Viewing Today's Allowance

The header shows your current quota percentages:
- **Available Today → Cursor Models: 45.16%**
- **Available Today → Other Models: 45.16%**

These update automatically when crossing midnight or changing time zones.

### Editing Quotas

You can manually adjust quota percentages:

1. Click on any percentage cell in the table
2. Type a new value (0-100)
3. Press **Enter** or click outside the cell
4. The app recalculates all future days from that point

**Notes:**
- Earlier days remain unchanged
- Later days recalculate to reach 100% by renewal
- Each quota (Cursor Models / Other Models) is independent
- Invalid values are rejected and the previous value is retained

### Example Edit

In a 31-day cycle, if you edit Day 15 to `35%`:
- Days 1-14 remain as originally calculated
- Day 15 becomes `35%`
- Days 16-31 recalculate to reach ~96% on day 31
- Daily increment: `(100 - 35) / 17 = 3.82%` per day

### Changing Renewal Day

1. Click **Change Renewal Day**
2. Confirm you want to discard manual edits
3. Enter new renewal day
4. App generates new cycle immediately

**Warning:** This discards all manual edits and creates a fresh even distribution.

## System Tray

The app lives in the Windows notification area (system tray) when closed:

### Closing the Window
- Click the **X** button to hide the window
- The app continues running in the tray
- Your settings and edits are preserved

### Opening from Tray
- **Left-click** the tray icon to restore the window
- Or **right-click** → **Open**

### Quitting
- Click **Quit** button in the main window
- Or **right-click** tray icon → **Quit**
- This fully exits the application

## Startup Behavior

### Auto-Start at Sign-In
When enabled (checkbox in main window):
- App starts when you sign in to Windows
- Launches minimized to tray (window hidden)
- Click tray icon to show the window

### Manual Launch
If auto-start is disabled:
- Use Start menu shortcut
- Window opens immediately

### Single Instance
The app allows only one running copy:
- Launching while already running just shows the existing window
- No duplicate processes created

## Data Storage

Your settings are stored at:
```
%LocalAppData%\CursorQuotaProgress\settings.json
```

This includes:
- Renewal day
- Current cycle data
- Manual edits
- Startup preference

**Backup:** Copy this folder to preserve your settings.

**Reset:** Delete this folder to start fresh (app will ask for renewal day again).

## Renewal Behavior

At midnight on your renewal day:
1. App detects the new cycle
2. Discards old cycle and manual edits
3. Generates fresh even distribution for new cycle
4. Today's percentages update to the new values

The window doesn't need to be open - detection happens on next app activation.

## Troubleshooting

### App won't start
- Check Task Manager for existing `CursorQuotaProgress.exe` process
- End the process and try again
- If still failing, check Windows Event Viewer for error details

### Tray icon disappeared
- Windows sometimes hides overflow icons
- Click the up arrow (^) in the system tray to expand
- The icon should reappear after locking/unlocking Windows

### Settings not saving
- Check folder permissions on `%LocalAppData%\CursorQuotaProgress`
- If `settings.corrupt.json` exists, your file was malformed
- Delete both files to reset

### Wrong today's percentage
- Verify your system date/time is correct
- Check your timezone setting
- App uses local time, not UTC

### Auto-start not working
- Open Registry Editor (regedit)
- Navigate to: `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
- Verify `CursorQuotaProgress` key exists with correct path

## Uninstallation

1. Quit the application (tray → Quit)
2. Use Windows Settings → Apps → Installed apps
3. Find "Cursor Quota Progress"
4. Click three dots → Uninstall
5. Choose whether to keep settings folder

## Support

For issues or questions:
- Check `DEVELOPMENT.md` for technical details
- Review `IMPLEMENTATION.md` for architecture info
- Submit issues on the GitHub repository

## Tips

- **Keyboard Navigation**: Tab through cells, Enter to edit
- **Locale Support**: Dates use your system's short date format
- **Decimal Separator**: Accepts locale-specific decimal separator (. or ,)
- **Theme**: Follows Windows light/dark/high-contrast theme automatically
- **DPI Scaling**: Properly scales on high-DPI displays

## Example Scenarios

### Scenario 1: Conservative Usage
Edit early days to higher percentages:
- Day 1: Change from 0% to 10%
- Day 5: Change from 13% to 25%
- Later days automatically increase daily increment

### Scenario 2: Frontload Usage
Don't edit - the default even distribution works perfectly.

### Scenario 3: Quota Tracking
Check "Available Today" each morning to know your current allowance.

---

**Version:** 1.0.0  
**Last Updated:** August 2026  
**System Requirements:** Windows 10/11 x64
