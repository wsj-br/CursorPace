using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using CursorPace.Services;
using CursorPace.ViewModels;

namespace CursorPace.Views;

public partial class MainWindow : Window
{
    private const int DefaultWindowWidth = 760;
    private const int DefaultWindowHeight = 787;

    private readonly MainViewModel _viewModel = null!;
    private readonly IUiDispatcher _dispatcher = null!;
    private readonly DispatcherTimer _dayCheckTimer = null!;
    private readonly TitleBarDrag _titleBarDrag = null!;
    private PixelPoint? _lastNormalPosition;
    private bool _restorePlacementPending;
    private bool _concealUntilPlaced;
    private bool _reallyClosing;

    public MainViewModel ViewModel => _viewModel;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel)
        : this(viewModel, new AvaloniaUiDispatcher())
    {
    }

    public MainWindow(MainViewModel viewModel, IUiDispatcher dispatcher)
    {
        _viewModel = viewModel;
        _dispatcher = dispatcher;
        DataContext = _viewModel;
        InitializeComponent();
        _titleBarDrag = new TitleBarDrag(this, TitleBarContent);
        UpdateViewModeIcons();

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.QuitRequested += () => _dispatcher.Post(CloseForReal);

        SetupWindow();
        ApplySavedWindowPosition();
        ConcealUntilPlaced();

        _dayCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        _dayCheckTimer.Tick += (_, _) => _viewModel.CheckForNewDay();
        _dayCheckTimer.Start();

        _restorePlacementPending = _viewModel.TryGetSavedWindowPosition(out _, out _);
        Opened += OnWindowOpened;
        Activated += OnWindowActivated;
        PositionChanged += OnPositionChanged;
        Closing += OnWindowClosing;
        KeyDown += OnWindowKeyDown;
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (SelectableTextCopy.TryHandleCopyKey(this, e))
            e.Handled = true;
    }

    private void SetupWindow()
    {
        Title = "Cursor Pace";
        Width = DefaultWindowWidth;
        Height = DefaultWindowHeight;
        CanResize = false;
    }

    public void BringToFront()
    {
        if (!IsVisible)
        {
            ApplySavedWindowPosition();
            ConcealUntilPlaced();
            _restorePlacementPending = _viewModel.TryGetSavedWindowPosition(out _, out _);
        }

        Show();
        WindowState = WindowState.Normal;
        Activate();
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
        (Application.Current as App)?.Quit();
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        if (!_restorePlacementPending && !_concealUntilPlaced)
            return;

        // Linux WMs (especially Mutter) often ignore PPosition and map at the
        // default top-left, then honor a later move. Keep the window invisible
        // until that second apply so the jump is not visible.
        if (OperatingSystem.IsLinux() && _concealUntilPlaced)
        {
            ApplySavedWindowPosition();
            _dispatcher.Post(() =>
            {
                ApplySavedWindowPosition();
                _dispatcher.Post(RevealPlacedWindow);
            });
            return;
        }

        _restorePlacementPending = false;
    }

    private void OnWindowActivated(object? sender, EventArgs e) =>
        _viewModel.CheckForNewDay();

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

    private void ApplySavedWindowPosition()
    {
        if (!_viewModel.TryGetSavedWindowPosition(out var x, out var y))
            return;

        try
        {
            var width = (int)Math.Round(FrameSize?.Width ?? Width);
            var height = (int)Math.Round(FrameSize?.Height ?? Height);
            var screen = Screens.ScreenFromPoint(new PixelPoint(x, y)) ?? Screens.Primary;
            if (screen == null)
            {
                Position = new PixelPoint(x, y);
                return;
            }

            var work = screen.WorkingArea;
            var (clampedX, clampedY) = WindowPlacement.ClampToWorkArea(
                x, y, width, height, work.X, work.Y, work.Width, work.Height);
            Position = new PixelPoint(clampedX, clampedY);
        }
        catch
        {
        }
    }

    private void ConcealUntilPlaced()
    {
        if (!OperatingSystem.IsLinux())
            return;
        if (!_viewModel.TryGetSavedWindowPosition(out _, out _))
            return;

        Opacity = 0;
        ShowInTaskbar = false;
        _concealUntilPlaced = true;
    }

    private void RevealPlacedWindow()
    {
        ApplySavedWindowPosition();
        if (_concealUntilPlaced)
        {
            Opacity = 1;
            ShowInTaskbar = true;
            _concealUntilPlaced = false;
        }

        _restorePlacementPending = false;
    }

    private static bool IsPlausiblePosition(PixelPoint position) =>
        position.X > -10_000 && position.Y > -10_000;

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e) =>
        _titleBarDrag.OnPointerPressed(e);

    private void OnTitleBarPointerMoved(object? sender, PointerEventArgs e) =>
        _titleBarDrag.OnPointerMoved(e);

    private void OnTitleBarPointerReleased(object? sender, PointerReleasedEventArgs e) =>
        _titleBarDrag.OnPointerReleased(e);

    private void OnTitleBarPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) =>
        _titleBarDrag.OnPointerCaptureLost(e);

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void OnWindowCloseClick(object? sender, RoutedEventArgs e) =>
        Close();

    private void OnQuitClick(object? sender, RoutedEventArgs e) => CloseForReal();
}
