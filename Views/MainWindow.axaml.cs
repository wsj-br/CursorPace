using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using CursorUsageProgress.Services;
using CursorUsageProgress.ViewModels;

namespace CursorUsageProgress.Views;

public partial class MainWindow : Window
{
    private const int DefaultWindowWidth = 760;
    private const int DefaultWindowHeight = 787;

    private readonly MainViewModel _viewModel = null!;
    private readonly DispatcherTimer _dayCheckTimer = null!;
    private SettingsWindow? _settingsWindow;
    private PixelPoint? _lastNormalPosition;
    private bool _restorePlacementPending;
    private bool _reallyClosing;

    public MainViewModel ViewModel => _viewModel;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel, bool startInTray = false)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
        InitializeComponent();
        UpdateViewModeIcons();

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.QuitRequested += () => Dispatcher.UIThread.Post(CloseForReal);

        SetupWindow();
        if (startInTray)
            HideToTray();
        RootGrid.Loaded += (_, _) => TextBlockSelection.EnableOnLabels(RootGrid, AppTitleBar);

        _dayCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        _dayCheckTimer.Tick += (_, _) => _viewModel.CheckForNewDay();
        _dayCheckTimer.Start();

        _restorePlacementPending = _viewModel.TryGetSavedWindowPosition(out _, out _);
        Activated += OnWindowActivated;
        PositionChanged += OnPositionChanged;
        Closing += OnWindowClosing;
    }

    private void SetupWindow()
    {
        Title = "Cursor Usage Progress";
        Width = DefaultWindowWidth;
        Height = DefaultWindowHeight;
        CanResize = false;
        if (OperatingSystem.IsMacOS())
            TitleBarContent.Padding = new Thickness(78, 0, 16, 0);
    }

    public void HideToTray() => Hide();

    public void BringToFront()
    {
        _restorePlacementPending = true;
        Show();
        WindowState = WindowState.Normal;
        Activate();
        RestoreWindowPosition();
        _viewModel.CheckForNewDay();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.IsChartView) or nameof(MainViewModel.IsCalendarView))
            UpdateViewModeIcons();
    }

    private void OnCalendarViewClick(object? sender, RoutedEventArgs e) =>
        _viewModel.IsChartView = false;

    private void OnChartViewClick(object? sender, RoutedEventArgs e) =>
        _viewModel.IsChartView = true;

    private void UpdateViewModeIcons()
    {
        var accent = ThemeBrush("ThemeAccentBrush", Color.FromArgb(255, 0, 120, 212));
        var dimmed = ThemeBrush("ThemeForegroundLowBrush", Color.FromArgb(255, 138, 138, 138));
        CalendarViewIcon.Foreground = _viewModel.IsCalendarView ? accent : dimmed;
        ChartViewIcon.Foreground = _viewModel.IsChartView ? accent : dimmed;
    }

    private IBrush ThemeBrush(string key, Color fallback)
    {
        if (Application.Current?.TryGetResource(key, ActualThemeVariant, out var value) == true
            && value is IBrush brush)
        {
            return brush;
        }

        return new SolidColorBrush(fallback);
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_reallyClosing)
            return;
        e.Cancel = true;
        PersistWindowPosition();
        _restorePlacementPending = true;
        Hide();
    }

    private void CloseForReal()
    {
        _reallyClosing = true;
        PersistWindowPosition();
        _dayCheckTimer.Stop();
        _settingsWindow?.Close();
        (Application.Current as App)?.Quit();
    }

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        _viewModel.CheckForNewDay();
        if (!_restorePlacementPending)
            return;

        _restorePlacementPending = false;
        RestoreWindowPosition();
        Dispatcher.UIThread.Post(RestoreWindowPosition, DispatcherPriority.Loaded);
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (WindowState != WindowState.Normal)
            return;
        if (!IsPlausiblePosition(Position))
            return;
        _lastNormalPosition = Position;
    }

    private void PersistWindowPosition()
    {
        if (!TryGetNormalPosition(out var position))
            return;
        _viewModel.SaveWindowPosition(position.X, position.Y);
    }

    private bool TryGetNormalPosition(out PixelPoint position)
    {
        if (_lastNormalPosition is { } saved && IsPlausiblePosition(saved))
        {
            position = saved;
            return true;
        }

        position = Position;
        return WindowState != WindowState.Minimized && IsPlausiblePosition(position);
    }

    private void RestoreWindowPosition()
    {
        if (!_viewModel.TryGetSavedWindowPosition(out var x, out var y))
            return;

        try
        {
            var width = (int)Math.Round(FrameSize?.Width ?? Width);
            var height = (int)Math.Round(FrameSize?.Height ?? Height);
            var screen = Screens.ScreenFromPoint(new PixelPoint(x, y)) ?? Screens.Primary;
            if (screen == null)
                return;
            var work = screen.WorkingArea;
            var (clampedX, clampedY) = WindowPlacement.ClampToWorkArea(
                x, y, width, height, work.X, work.Y, work.Width, work.Height);
            Position = new PixelPoint(clampedX, clampedY);
        }
        catch
        {
        }
    }

    private static bool IsPlausiblePosition(PixelPoint position) =>
        position.X > -10_000 && position.Y > -10_000;

    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (_settingsWindow != null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_viewModel);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show(this);
    }

    private void OnQuitClick(object? sender, RoutedEventArgs e) => CloseForReal();
}
