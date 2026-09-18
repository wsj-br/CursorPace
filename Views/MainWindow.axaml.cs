using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
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
    private readonly WindowResizeDrag _resizeDrag = null!;
    private PixelPoint? _lastNormalPosition;
    private Size? _lastNormalSize;
    private WindowState _restoreWindowState = WindowState.Normal;
    private bool _restorePlacementPending;
    private bool _concealUntilPlaced;

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
        _resizeDrag = new WindowResizeDrag(this);
        UpdateViewModeIcons();

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        SetupWindow();
        ApplySavedWindowPlacement();
        ConcealUntilPlaced();
        UpdateMaximizeCaption();

        _dayCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
        _dayCheckTimer.Tick += (_, _) => _viewModel.CheckForNewDay();
        _dayCheckTimer.Start();

        _restorePlacementPending = _viewModel.TryGetSavedWindowPlacement(out _, out _, out _, out _, out _);
        Opened += OnWindowOpened;
        Activated += OnWindowActivated;
        PositionChanged += OnPositionChanged;
        SizeChanged += OnWindowSizeChanged;
        PropertyChanged += OnWindowPropertyChanged;
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
        MinWidth = DefaultWindowWidth;
        MinHeight = DefaultWindowHeight;
        Width = DefaultWindowWidth;
        Height = DefaultWindowHeight;
        CanResize = true;
        CanMaximize = true;
        _lastNormalSize = new Size(DefaultWindowWidth, DefaultWindowHeight);
    }

    public void BringToFront()
    {
        MacDesktopIntegration.ApplyDockVisibility(true);
        if (!IsVisible)
        {
            ApplySavedWindowPlacement();
            ConcealUntilPlaced();
            _restorePlacementPending = _viewModel.TryGetSavedWindowPlacement(out _, out _, out _, out _, out _);
        }

        Show();
        WindowState = _restoreWindowState == WindowState.Maximized
            ? WindowState.Maximized
            : WindowState.Normal;
        Activate();
        // GNOME/Mutter often ignores Activate() for a window that is already
        // mapped behind others. Toggling Topmost forces a raise without
        // leaving the window always-on-top.
        Topmost = true;
        Topmost = false;
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
        PersistWindowPlacement();
        if (e.CloseReason is WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown)
        {
            _dayCheckTimer.Stop();
            return;
        }

        e.Cancel = true;
        _restorePlacementPending = true;
        Hide();
        MacDesktopIntegration.ApplyDockVisibility(false);
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        if (!_restorePlacementPending && !_concealUntilPlaced)
            return;

        // Linux WMs (especially Mutter) often ignore PPosition and map at the
        // default top-left, then honor a later move. Keep the window invisible
        // until that second apply so the jump is not visible. Do not toggle
        // ShowInTaskbar: GNOME/Zorin drop the taskbar icon when the window maps
        // with skip-taskbar, and clearing that hint later does not restore it.
        if (OperatingSystem.IsLinux() && _concealUntilPlaced)
        {
            ApplySavedWindowPlacement();
            _dispatcher.Post(() =>
            {
                ApplySavedWindowPlacement();
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

    private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (WindowState != WindowState.Normal)
            return;

        var size = CurrentFrameSize();
        if (size.Width > 0 && size.Height > 0)
            _lastNormalSize = size;
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != WindowStateProperty)
            return;

        UpdateMaximizeCaption();
        if (WindowState is WindowState.Normal or WindowState.Maximized)
            _restoreWindowState = WindowState;
        SyncMacDockVisibility();
    }

    private void SyncMacDockVisibility() =>
        MacDesktopIntegration.ApplyDockVisibility(IsVisible, WindowState == WindowState.Minimized);

    private void PersistWindowPlacement()
    {
        if (!TryGetNormalPosition(out var position))
            return;
        if (!TryGetNormalSize(out var width, out var height))
            return;

        var maximized = WindowState == WindowState.Maximized
            || (WindowState == WindowState.Minimized && _restoreWindowState == WindowState.Maximized);
        _viewModel.SaveWindowPlacement(position.X, position.Y, width, height, maximized);
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

    private bool TryGetNormalSize(out int width, out int height)
    {
        if (_lastNormalSize is { } saved && saved.Width > 0 && saved.Height > 0)
        {
            width = (int)Math.Round(saved.Width);
            height = (int)Math.Round(saved.Height);
            return true;
        }

        var size = CurrentFrameSize();
        width = (int)Math.Round(size.Width);
        height = (int)Math.Round(size.Height);
        return width > 0 && height > 0;
    }

    private Size CurrentFrameSize()
    {
        if (FrameSize is { Width: > 0, Height: > 0 } frame)
            return frame;
        return new Size(Width, Height);
    }

    private void ApplySavedWindowPlacement()
    {
        if (!_viewModel.TryGetSavedWindowPlacement(out var savedX, out var savedY, out var savedWidth, out var savedHeight, out var maximized))
            return;

        try
        {
            var width = savedWidth ?? (int)Math.Round(CurrentFrameSize().Width);
            var height = savedHeight ?? (int)Math.Round(CurrentFrameSize().Height);
            var x = savedX ?? Position.X;
            var y = savedY ?? Position.Y;

            var screen = Screens.ScreenFromPoint(new PixelPoint(x, y)) ?? Screens.Primary;
            if (screen == null)
            {
                ApplyNormalBounds(x, y, width, height);
                ApplyMaximizedState(maximized);
                return;
            }

            var work = screen.WorkingArea;
            var clamped = WindowPlacement.ClampToWorkArea(
                x, y, width, height, work.X, work.Y, work.Width, work.Height);
            ApplyNormalBounds(clamped.X, clamped.Y, clamped.Width, clamped.Height);
            ApplyMaximizedState(maximized);
        }
        catch
        {
        }
    }

    private void ApplyNormalBounds(int x, int y, int width, int height)
    {
        if (WindowState == WindowState.Maximized)
            WindowState = WindowState.Normal;

        Width = width;
        Height = height;
        Position = new PixelPoint(x, y);
        _lastNormalSize = new Size(width, height);
        if (IsPlausiblePosition(Position))
            _lastNormalPosition = Position;
    }

    private void ApplyMaximizedState(bool maximized)
    {
        WindowState = maximized ? WindowState.Maximized : WindowState.Normal;
        _restoreWindowState = WindowState;
        UpdateMaximizeCaption();
    }

    private void ConcealUntilPlaced()
    {
        if (!OperatingSystem.IsLinux())
            return;
        if (!_viewModel.TryGetSavedWindowPlacement(out var x, out var y, out _, out _, out _))
            return;
        if (x is null || y is null)
            return;

        Opacity = 0;
        _concealUntilPlaced = true;
    }

    private void RevealPlacedWindow()
    {
        ApplySavedWindowPlacement();
        if (_concealUntilPlaced)
        {
            Opacity = 1;
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

    private void OnResizeGripPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control grip)
            return;
        _resizeDrag.OnPointerPressed(ResizeEdge(grip), grip, e);
    }

    private void OnResizeGripMoved(object? sender, PointerEventArgs e) =>
        _resizeDrag.OnPointerMoved(e);

    private void OnResizeGripReleased(object? sender, PointerReleasedEventArgs e) =>
        _resizeDrag.OnPointerReleased(e);

    private void OnResizeGripCaptureLost(object? sender, PointerCaptureLostEventArgs e) =>
        _resizeDrag.OnPointerCaptureLost(e);

    private static WindowEdge ResizeEdge(Control grip) => grip.Name switch
    {
        "ResizeGripN" => WindowEdge.North,
        "ResizeGripS" => WindowEdge.South,
        "ResizeGripW" => WindowEdge.West,
        "ResizeGripE" => WindowEdge.East,
        "ResizeGripNW" => WindowEdge.NorthWest,
        "ResizeGripNE" => WindowEdge.NorthEast,
        "ResizeGripSW" => WindowEdge.SouthWest,
        "ResizeGripSE" => WindowEdge.SouthEast,
        _ => WindowEdge.SouthEast
    };

    private void OnMinimizeClick(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void UpdateMaximizeCaption()
    {
        if (MaximizeButton is null || MaximizeIconControl is null || RestoreIconControl is null)
            return;

        var maximized = WindowState == WindowState.Maximized;
        MaximizeIconControl.IsVisible = !maximized;
        RestoreIconControl.IsVisible = maximized;
        var caption = maximized ? "Restore" : "Maximize";
        ToolTip.SetTip(MaximizeButton, caption);
        AutomationProperties.SetName(MaximizeButton, caption);
    }

    private void OnWindowCloseClick(object? sender, RoutedEventArgs e) =>
        Close();
}
