using Avalonia;
using Avalonia.Controls;
using CursorPace.ViewModels;

namespace CursorPace.Services;

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
        // macOS native exporter throws if TrayIcon.Menu is replaced after export.
        // Keep the App.axaml menu instance and only set item commands.
        if (_icon.Menu is { } menu)
        {
            foreach (var item in menu.Items)
            {
                if (item is not NativeMenuItem menuItem)
                    continue;
                if (menuItem.CommandParameter as string == "tray-open")
                    menuItem.Command = openCommand;
                else if (menuItem.CommandParameter as string == "tray-quit")
                    menuItem.Command = quitCommand;
            }
        }

        LocaleChanged += ApplyMenuHeaders;
        ApplyMenuHeaders();
    }

    private void ApplyMenuHeaders()
    {
        if (_icon?.Menu is not { } menu)
            return;

        foreach (var item in menu.Items)
        {
            if (item is not NativeMenuItem menuItem)
                continue;
            if (menuItem.CommandParameter as string == "tray-open")
                menuItem.Header = Tp("tray", "Open");
            else if (menuItem.CommandParameter as string == "tray-quit")
                menuItem.Header = Tp("tray", "Quit");
        }
    }

    public void ShowWindow() => _onOpenRequested?.Invoke();

    public void UpdateToolTip(string text)
    {
        if (_icon == null)
            return;
        _icon.ToolTipText = string.IsNullOrWhiteSpace(text)
            ? T("CursorPace")
            : text;
    }

    public void Dispose()
    {
        if (_icon == null)
            return;
        LocaleChanged -= ApplyMenuHeaders;
        _icon.IsVisible = false;
        _icon = null;
    }
}
