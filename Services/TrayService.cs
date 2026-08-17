using H.NotifyIcon;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System.Windows.Input;

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

        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "Cursor Quota Progress",
            LeftClickCommand = openCommand,
            DoubleClickCommand = openCommand,
            NoLeftClickDelay = true,
        };

        var openItem = new MenuFlyoutItem { Text = "Open", Command = openCommand };
        var quitItem = new MenuFlyoutItem { Text = "Quit", Command = quitCommand };
        var menu = new MenuFlyout();
        menu.Items.Add(openItem);
        menu.Items.Add(quitItem);
        _taskbarIcon.ContextFlyout = menu;

        var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
        if (System.IO.File.Exists(iconPath))
            _taskbarIcon.SetValue(TaskbarIcon.IconSourceProperty, new BitmapIconSource { UriSource = new Uri(iconPath) });

        SystemEvents.SessionSwitch += OnSessionSwitch;
    }

    public void ShowWindow() => _onOpenRequested?.Invoke();

    public void Dispose()
    {
        if (_taskbarIcon != null)
        {
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            _taskbarIcon.Dispose();
            _taskbarIcon = null;
        }
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionUnlock && _taskbarIcon != null)
            _taskbarIcon.ForceCreate();
    }

    private sealed class ActionCommand : ICommand
    {
        private readonly Action _action;
        public ActionCommand(Action action) => _action = action;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _action();
    }
}
