using Avalonia;
using Avalonia.Controls;
using CursorUsageProgress.ViewModels;

namespace CursorUsageProgress.Services;

public sealed class TrayService : ITrayService
{
    private TrayIcon? _icon;
    private Action? _onOpenRequested;
    private Action? _onQuitRequested;

    public void Initialize(Action onOpenRequested, Action onQuitRequested)
    {
        _onOpenRequested = onOpenRequested;
        _onQuitRequested = onQuitRequested;

        var icons = TrayIcon.GetIcons(Application.Current!);
        if (icons == null || icons.Count == 0)
            return;

        _icon = icons[0];
        var openCommand = new RelayCommand(() => _onOpenRequested?.Invoke());
        var quitCommand = new RelayCommand(() => _onQuitRequested?.Invoke());
        _icon.Command = openCommand;

        if (_icon.Menu is { } menu)
        {
            foreach (var item in menu.Items)
            {
                if (item is not NativeMenuItem native)
                    continue;
                if (native.Header == "Open")
                    native.Command = openCommand;
                else if (native.Header == "Quit")
                    native.Command = quitCommand;
            }
        }
    }

    public void ShowWindow() => _onOpenRequested?.Invoke();

    public void UpdateToolTip(string text)
    {
        if (_icon == null)
            return;
        _icon.ToolTipText = string.IsNullOrWhiteSpace(text)
            ? "Cursor Usage Progress"
            : text;
    }

    public void Dispose()
    {
        if (_icon == null)
            return;
        _icon.IsVisible = false;
        _icon = null;
    }
}
