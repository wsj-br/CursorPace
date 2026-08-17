using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CursorQuotaProgress.Services;

public sealed class TrayService : ITrayService
{
    private NotifyIcon? _notifyIcon;
    private Action? _onOpenRequested;
    private Action? _onQuitRequested;

    public void Initialize(Action onOpenRequested, Action onQuitRequested)
    {
        _onOpenRequested = onOpenRequested;
        _onQuitRequested = onQuitRequested;

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Cursor Quota Progress",
            Visible = true
        };

        _notifyIcon.MouseClick += OnIconClick;

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open", null, (s, e) => _onOpenRequested?.Invoke());
        contextMenu.Items.Add("Quit", null, (s, e) => _onQuitRequested?.Invoke());
        _notifyIcon.ContextMenuStrip = contextMenu;

        Microsoft.Win32.SystemEvents.SessionSwitch += OnSessionSwitch;
    }

    public void ShowWindow()
    {
        _onOpenRequested?.Invoke();
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            Microsoft.Win32.SystemEvents.SessionSwitch -= OnSessionSwitch;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }

    private void OnIconClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _onOpenRequested?.Invoke();
        }
    }

    private void OnSessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
    {
        if (e.Reason == Microsoft.Win32.SessionSwitchReason.SessionUnlock && _notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Visible = true;
        }
    }

    private static Icon LoadIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
        if (File.Exists(iconPath))
            return new Icon(iconPath);

        return SystemIcons.Application;
    }
}

