using System.ComponentModel;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using CursorUsageProgress;
using CursorUsageProgress.Services;
using CursorUsageProgress.ViewModels;
using Windows.Globalization.NumberFormatting;
using Windows.Graphics;

namespace CursorUsageProgress.Views;

public sealed partial class MainWindow : Window
{
    private const int DefaultWindowWidth = 760;
    private const int DefaultWindowHeight = 749;
    private const int EditPanelWidth = 220;
    private const int EditPanelGap = 16;

    private readonly MainViewModel _viewModel;
    private readonly ICycleCalculator _calculator;
    private readonly IClock _clock;
    private readonly DispatcherQueueTimer _dayCheckTimer;
    private SettingsWindow? _settingsWindow;
    private PointInt32? _lastNormalPosition;
    private bool _restorePlacementPending;

    public MainViewModel ViewModel => _viewModel;

    public MainWindow(MainViewModel viewModel, ICycleCalculator calculator, IClock clock)
    {
        _viewModel = viewModel;
        _calculator = calculator;
        _clock = clock;

        InitializeComponent();

        RootGrid.DataContext = _viewModel;
        ConfigureIntegerNumberBoxes();
        UpdateViewModeIcons();

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.QuitRequested += () => DispatcherQueue.TryEnqueue(CloseForReal);
        _viewModel.ChangeRenewalDayRequested += OnChangeRenewalDayRequested;
        _viewModel.ResetCycleRequested += OnResetCycleRequested;

        SetupWindow();
        SetupTheme();
        RootGrid.Loaded += (_, _) => TextBlockSelection.EnableOnLabels(RootGrid, AppTitleBar);

        _dayCheckTimer = DispatcherQueue.CreateTimer();
        _dayCheckTimer.Interval = TimeSpan.FromMinutes(5);
        _dayCheckTimer.Tick += (_, _) => _viewModel.CheckForNewDay();
        _dayCheckTimer.Start();

        _restorePlacementPending = _viewModel.TryGetSavedWindowPosition(out _, out _);
        Activated += OnWindowActivated;
        AppWindow.Changed += OnAppWindowChanged;
        AppWindow.Closing += OnAppWindowClosing;

        // Scroll to today after window is ready
        DispatcherQueue.TryEnqueue(() => ScrollToToday());
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

        // Make window non-resizable
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMinimizable = true;
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }

        // Account for caption button overlays
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = AppWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;

            // Set button colors to transparent so they work with the custom design
            titleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            titleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        }

        // Mica backdrop (Windows 11), Acrylic fallback on Windows 10
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

    private void ConfigureIntegerNumberBoxes()
    {
        var formatter = new DecimalFormatter
        {
            FractionDigits = 0,
            NumberRounder = new IncrementNumberRounder
            {
                Increment = 1,
                RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp
            }
        };

        CursorQuotaBox.NumberFormatter = formatter;
        OtherQuotaBox.NumberFormatter = formatter;
    }

    private void ApplySystemTheme(Windows.UI.ViewManagement.UISettings settings)
    {
        var bg = settings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
        var isDark = bg.R < 128;
        RootGrid.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
        UpdateViewModeIcons();
    }

    public void BringToFront()
    {
        _restorePlacementPending = true;
        AppWindow.Show();
        Activate();
        RestoreWindowPosition();
        _viewModel.CheckForNewDay();
        ScrollToToday();
    }

    public void ShowRenewalDaySetup()
    {
        if (RootGrid.XamlRoot != null)
        {
            _ = ShowRenewalDayDialogAsync();
            return;
        }

        RoutedEventHandler? handler = null;
        handler = (_, _) =>
        {
            RootGrid.Loaded -= handler;
            _ = ShowRenewalDayDialogAsync();
        };
        RootGrid.Loaded += handler;
    }

    private async Task ShowRenewalDayDialogAsync()
    {
        if (_viewModel.IsInitialized)
            return;

        if (RootGrid.XamlRoot == null)
            return;

        var dialog = new RenewalDayDialog(_calculator, _clock) { XamlRoot = RootGrid.XamlRoot };
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialog.RenewalDay.HasValue)
        {
            _viewModel.SetRenewalDay(dialog.RenewalDay.Value);
            ScrollToToday();
        }
        else
        {
            CloseForReal();
        }
    }

    private void ScrollToToday()
    {
        // Calendar grid doesn't need scrolling - it's always visible
        // The today highlighting is handled by the ViewModel
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsEditingDay))
            UpdateWindowSizeForEditPanel();
        else if (e.PropertyName is nameof(MainViewModel.IsChartView) or nameof(MainViewModel.IsCalendarView))
            UpdateViewModeIcons();
    }

    private void UpdateWindowSizeForEditPanel()
    {
        var extra = _viewModel.IsEditingDay ? EditPanelWidth + EditPanelGap : 0;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(DefaultWindowWidth + extra, DefaultWindowHeight));
    }

    private void OnCellSelected(object sender, CalendarCellViewModel cell)
    {
        if (!_viewModel.CanEditDays)
            return;

        if (cell?.DayData != null)
        {
            _viewModel.StartEditingDay(cell.DayData);
        }
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

    private void OnApplyClick(object sender, RoutedEventArgs e)
    {
        // NumberBox TwoWay binding often does not flush until lost focus.
        if (!double.IsNaN(CursorQuotaBox.Value))
            _viewModel.EditingCursorQuota = CursorQuotaBox.Value;
        if (!double.IsNaN(OtherQuotaBox.Value))
            _viewModel.EditingOtherQuota = OtherQuotaBox.Value;

        if (_viewModel.ApplyEditCommand.CanExecute(null))
            _viewModel.ApplyEditCommand.Execute(null);
    }

    private void OnResetDayClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.ResetDayCommand.CanExecute(null))
            _viewModel.ResetDayCommand.Execute(null);
    }

    private async void OnChangeRenewalDayRequested()
    {
        if (RootGrid.XamlRoot == null)
            return;

        if (_viewModel.IsInitialized)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Change renewal day",
                Content = TextBlockSelection.Message(
                    "Changing the renewal day will discard any manual edits and generate a new cycle. Continue?"),
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                XamlRoot = RootGrid.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };

            var result = await confirmDialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;
        }

        var dialog = new RenewalDayDialog(_calculator, _clock, _viewModel.RenewalDay == 0 ? 15 : _viewModel.RenewalDay)
        {
            XamlRoot = RootGrid.XamlRoot
        };
        var dialogResult = await dialog.ShowAsync();

        if (dialogResult == ContentDialogResult.Primary && dialog.RenewalDay.HasValue)
        {
            _viewModel.SetRenewalDay(dialog.RenewalDay.Value);
            ScrollToToday();
        }
    }

    private bool _reallyClosing;

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_reallyClosing) return;
        // Hide instead of close — keep app alive in tray
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

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.ResetCycleCommand.CanExecute(null))
            OnResetCycleRequested();
    }

    private void OnQuitClick(object sender, RoutedEventArgs e)
    {
        CloseForReal();
    }

    private async void OnResetCycleRequested()
    {
        var confirmDialog = new ContentDialog
        {
            Title = "Reset cycle",
            Content = TextBlockSelection.Message(
                "This will clear all manual edits and regenerate the cycle from scratch. Continue?"),
            PrimaryButtonText = "Reset",
            CloseButtonText = "Cancel",
            XamlRoot = RootGrid.XamlRoot,
            DefaultButton = ContentDialogButton.Close
        };

        var result = await confirmDialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            _viewModel.SetRenewalDay(_viewModel.RenewalDay);
            ScrollToToday();
        }
    }
}
