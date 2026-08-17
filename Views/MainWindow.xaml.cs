using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using CursorQuotaProgress.Services;
using CursorQuotaProgress.ViewModels;

namespace CursorQuotaProgress.Views;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ICycleCalculator _calculator;
    private readonly IClock _clock;
    private readonly DispatcherQueueTimer _dayCheckTimer;
    private DayRowViewModel? _selectedDay;

    public MainWindow(MainViewModel viewModel, ICycleCalculator calculator, IClock clock)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _calculator = calculator;
        _clock = clock;

        RootGrid.DataContext = _viewModel;

        _viewModel.QuitRequested += () => DispatcherQueue.TryEnqueue(CloseForReal);
        _viewModel.ChangeRenewalDayRequested += OnChangeRenewalDayRequested;

        SetupWindow();
        SetupTheme();

        _dayCheckTimer = DispatcherQueue.CreateTimer();
        _dayCheckTimer.Interval = TimeSpan.FromMinutes(5);
        _dayCheckTimer.Tick += (_, _) => _viewModel.CheckForNewDay();
        _dayCheckTimer.Start();

        Activated += (_, _) => _viewModel.CheckForNewDay();
        AppWindow.Closing += OnAppWindowClosing;
    }

    private void SetupWindow()
    {
        Title = "Cursor Quota Progress";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.Resize(new Windows.Graphics.SizeInt32(860, 620));

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

    private void ApplySystemTheme(Windows.UI.ViewManagement.UISettings settings)
    {
        var bg = settings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
        var isDark = bg.R < 128;
        RootGrid.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;
    }

    public void BringToFront()
    {
        AppWindow.Show();
        Activate();
        _viewModel.CheckForNewDay();
        ScrollToToday();
    }

    public void ShowRenewalDaySetup()
    {
        _ = ShowRenewalDayDialogAsync();
    }

    private async Task ShowRenewalDayDialogAsync()
    {
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
        var today = _viewModel.Days.FirstOrDefault(d => d.IsToday);
        if (today != null)
        {
            DaysList.ScrollIntoView(today);
            DaysList.SelectedItem = today;
        }
    }

    private void OnDaySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DaysList.SelectedItem is DayRowViewModel day)
        {
            _selectedDay = day;
            EditPanelTitle.Text = $"Edit Day {day.DayNumber}";

            if (double.TryParse(day.CursorModelsText, out var cm))
                CursorModelsBox.Value = cm;
            if (double.TryParse(day.OtherModelsText, out var om))
                OtherModelsBox.Value = om;

            EditPanel.Visibility = Visibility.Visible;
        }
    }

    private void OnApplyEdit(object sender, RoutedEventArgs e)
    {
        if (_selectedDay == null) return;

        if (!double.IsNaN(CursorModelsBox.Value))
            _selectedDay.CursorModelsText = CursorModelsBox.Value.ToString("F2");

        if (!double.IsNaN(OtherModelsBox.Value))
            _selectedDay.OtherModelsText = OtherModelsBox.Value.ToString("F2");

        EditPanel.Visibility = Visibility.Collapsed;
        _selectedDay = null;
        DaysList.SelectedItem = null;
    }

    private void OnCancelEdit(object sender, RoutedEventArgs e)
    {
        EditPanel.Visibility = Visibility.Collapsed;
        _selectedDay = null;
        DaysList.SelectedItem = null;
    }

    private async void OnChangeRenewalDayRequested()
    {
        var confirmDialog = new ContentDialog
        {
            Title = "Change renewal day",
            Content = "Changing the renewal day will discard any manual edits and generate a new cycle. Continue?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            XamlRoot = RootGrid.XamlRoot,
            DefaultButton = ContentDialogButton.Close
        };

        var result = await confirmDialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        var dialog = new RenewalDayDialog(_calculator, _clock) { XamlRoot = RootGrid.XamlRoot };
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
        AppWindow.Hide();
    }

    private void CloseForReal()
    {
        _reallyClosing = true;
        _dayCheckTimer.Stop();
        Close();
    }
}
