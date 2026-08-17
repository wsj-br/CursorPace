using H.NotifyIcon;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using System.Windows.Input;
using Windows.UI;

namespace CursorQuotaProgress.Services;

public sealed class TrayService : ITrayService
{
    private TaskbarIcon? _taskbarIcon;
    private Action? _onOpenRequested;
    private Action? _onQuitRequested;

    public void Initialize(Action onOpenRequested, Action onQuitRequested)
    {
        _onOpenRequested = onOpenRequested;
        _onQuitRequested = onQuitRequested;

        var openCommand = new ActionCommand(() => _onOpenRequested?.Invoke());
        var quitCommand = new ActionCommand(() => _onQuitRequested?.Invoke());

        var openItem = new MenuFlyoutItem { Text = "Open", Command = openCommand };
        var quitItem = new MenuFlyoutItem { Text = "Quit", Command = quitCommand };
        var menu = new MenuFlyout();
        menu.Items.Add(openItem);
        menu.Items.Add(quitItem);

        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "Cursor Quota Progress",
            LeftClickCommand = openCommand,
            DoubleClickCommand = openCommand,
            NoLeftClickDelay = true,
            ContextMenuMode = ContextMenuMode.SecondWindow,
            ContextFlyout = menu,
            Icon = LoadTrayIcon(),
        };

        if (_taskbarIcon.Icon == null)
            _taskbarIcon.IconSource = CreateFallbackIconSource();

        // Created in code, so Loaded never fires — register the shell icon now.
        _taskbarIcon.ForceCreate(enablesEfficiencyMode: false);

        SystemEvents.SessionSwitch += OnSessionSwitch;
    }

    public void ShowWindow() => _onOpenRequested?.Invoke();

    public void UpdateToolTip(string text)
    {
        if (_taskbarIcon == null) return;
        _taskbarIcon.ToolTipText = string.IsNullOrWhiteSpace(text)
            ? "Cursor Quota Progress"
            : text;
    }

    public void Dispose()
    {
        if (_taskbarIcon != null)
        {
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            _taskbarIcon.Dispose();
            _taskbarIcon = null;
        }
    }

    private static System.Drawing.Icon? LoadTrayIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "cursor_quota_progress.ico");
        if (!File.Exists(iconPath))
            return null;

        return new System.Drawing.Icon(iconPath, 32, 32);
    }

    private static ImageSource CreateFallbackIconSource()
    {
        return new GeneratedIconSource
        {
            Text = "%",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(255, 125, 211, 252)),
            Background = new SolidColorBrush(Color.FromArgb(255, 30, 41, 59)),
        };
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionUnlock && _taskbarIcon != null)
            _taskbarIcon.ForceCreate(enablesEfficiencyMode: false);
    }

    private sealed class ActionCommand : ICommand
    {
        private readonly Action _action;

        public ActionCommand(Action action) => _action = action;

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _action();
    }
}
