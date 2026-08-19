using System.ComponentModel;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using CursorUsageProgress.Services;
using CursorUsageProgress.ViewModels;
using Windows.Graphics;

namespace CursorUsageProgress.Views;

public sealed partial class MainWindow : Window
{
    private const int DefaultWindowWidth = 760;
    private const int DefaultWindowHeight = 787;

    private readonly MainViewModel _viewModel;
    private readonly DispatcherQueueTimer _dayCheckTimer;
    private SettingsWindow? _settingsWindow;
    private PointInt32? _lastNormalPosition;
    private bool _restorePlacementPending;

    public MainViewModel ViewModel => _viewModel;

    public MainWindow(MainViewModel viewModel, bool startInTray = false)
    {
        _viewModel = viewModel;

        InitializeComponent();

        RootGrid.DataContext = _viewModel;
        UpdateViewModeIcons();

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.QuitRequested += () => DispatcherQueue.TryEnqueue(CloseForReal);

        SetupWindow();
        SetupTheme();
        if (startInTray)
            HideToTray();
        RootGrid.Loaded += (_, _) => TextBlockSelection.EnableOnLabels(RootGrid, AppTitleBar);

        _dayCheckTimer = DispatcherQueue.CreateTimer();
        _dayCheckTimer.Interval = TimeSpan.FromMinutes(5);
        _dayCheckTimer.Tick += (_, _) => _viewModel.CheckForNewDay();
        _dayCheckTimer.Start();

        _restorePlacementPending = _viewModel.TryGetSavedWindowPosition(out _, out _);
        Activated += OnWindowActivated;
        AppWindow.Changed += OnAppWindowChanged;
        AppWindow.Closing += OnAppWindowClosing;
    }

    private void SetupWindow()
    {
        Title = "Cursor Usage Progress";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "cursor_usage_progress.ico");
        if (File.Exists(iconPath))
            AppWindow.SetIcon(iconPath);

        AppWindow.Resize(new Windows.Graphics.SizeInt32(DefaultWindowWidth, DefaultWindowHeight));

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMinimizable = true;
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = AppWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            titleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        }

        if (MicaController.IsSupported())
            SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
        else
            SystemBackdrop = new DesktopAcrylicBackdrop();
    }

    private void SetupTheme()
    {
        var uiSettings = new Windows.UI.ViewManagement.UISettings();
        ApplySystemTheme(uiSettings);
        uiSettings.ColorValuesChanged += (s, _) =>
            DispatcherQueue.TryEnqueue(() => ApplySystemTheme(s));
    }

    private void ApplySystemTheme(Windows.UI.ViewManagement.UISettings settings)
    {
        var bg = settings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
        var isDark = bg.R < 128;
        RootGrid.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
        UpdateViewModeIcons();
    }

    public void HideToTray()
    {
        AppWindow.Hide();
    }

    public void BringToFront()
    {
        _restorePlacementPending = true;
        AppWindow.Show();
        Activate();
        RestoreWindowPosition();
        _viewModel.CheckForNewDay();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.IsChartView) or nameof(MainViewModel.IsCalendarView))
            UpdateViewModeIcons();
    }

    private void OnCalendarViewClick(object sender, RoutedEventArgs e) =>
        _viewModel.IsChartView = false;

    private void OnChartViewClick(object sender, RoutedEventArgs e) =>
        _viewModel.IsChartView = true;

    private void UpdateViewModeIcons()
    {
        var accent = ThemeBrush("AccentTextFillColorPrimaryBrush", Windows.UI.Color.FromArgb(255, 0, 120, 212));
        var dimmed = ThemeBrush("TextFillColorDisabledBrush", Windows.UI.Color.FromArgb(255, 138, 138, 138));
        CalendarViewIcon.Foreground = _viewModel.IsCalendarView ? accent : dimmed;
        ChartViewIcon.Foreground = _viewModel.IsChartView ? accent : dimmed;
    }

    private static Brush ThemeBrush(string key, Windows.UI.Color fallback)
    {
        if (Application.Current.Resources.TryGetValue(key, out var value) && value is Brush brush)
            return brush;
        return new SolidColorBrush(fallback);
    }

    private bool _reallyClosing;

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_reallyClosing) return;
        args.Cancel = true;
        PersistWindowPosition();
        _restorePlacementPending = true;
        AppWindow.Hide();
    }

    private void CloseForReal()
    {
        _reallyClosing = true;
        PersistWindowPosition();
        _dayCheckTimer.Stop();
        _settingsWindow?.Close();
        (Application.Current as App)?.Quit();
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
            return;

        _viewModel.CheckForNewDay();

        if (!_restorePlacementPending)
            return;

        _restorePlacementPending = false;
        RestoreWindowPosition();
        DispatcherQueue.TryEnqueue(RestoreWindowPosition);
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (!args.DidPositionChange || IsMinimized())
            return;

        var position = sender.Position;
        if (!IsPlausiblePosition(position))
            return;

        _lastNormalPosition = position;
    }

    private void PersistWindowPosition()
    {
        if (!TryGetNormalPosition(out var position))
            return;

        _viewModel.SaveWindowPosition(position.X, position.Y);
    }

    private bool TryGetNormalPosition(out PointInt32 position)
    {
        if (_lastNormalPosition is { } saved && IsPlausiblePosition(saved))
        {
            position = saved;
            return true;
        }

        position = AppWindow.Position;
        return !IsMinimized() && IsPlausiblePosition(position);
    }

    private void RestoreWindowPosition()
    {
        if (!_viewModel.TryGetSavedWindowPosition(out var x, out var y))
            return;

        try
        {
            var size = AppWindow.Size;
            var display = DisplayArea.GetFromPoint(new PointInt32(x, y), DisplayAreaFallback.Nearest);
            var work = display.WorkArea;
            var (clampedX, clampedY) = WindowPlacement.ClampToWorkArea(
                x, y, size.Width, size.Height, work.X, work.Y, work.Width, work.Height);
            AppWindow.Move(new PointInt32(clampedX, clampedY));
        }
        catch
        {
        }
    }

    private bool IsMinimized() =>
        AppWindow.Presenter is OverlappedPresenter presenter
        && presenter.State == OverlappedPresenterState.Minimized;

    private static bool IsPlausiblePosition(PointInt32 position) =>
        position.X > -10_000 && position.Y > -10_000;

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        if (_settingsWindow != null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_viewModel, AppWindow);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Activate();
    }

    private void OnQuitClick(object sender, RoutedEventArgs e)
    {
        CloseForReal();
    }
}
