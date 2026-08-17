using System.Windows;
using System.Windows.Threading;
using CursorQuotaProgress.Services;
using CursorQuotaProgress.ViewModels;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace CursorQuotaProgress.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DispatcherTimer _dayCheckTimer;
    private readonly ICycleCalculator _calculator;
    private readonly IClock _clock;

    public MainWindow(MainViewModel viewModel, ICycleCalculator calculator, IClock clock)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _calculator = calculator;
        _clock = clock;
        DataContext = _viewModel;

        _viewModel.QuitRequested += OnQuitRequested;
        _viewModel.ChangeRenewalDayRequested += OnChangeRenewalDayRequested;

        _dayCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(5)
        };
        _dayCheckTimer.Tick += (s, e) => _viewModel.CheckForNewDay();
        _dayCheckTimer.Start();

        Loaded += OnLoaded;
        Activated += (s, e) => _viewModel.CheckForNewDay();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Days.Count > 0)
        {
            var todayRow = _viewModel.Days.FirstOrDefault(d => d.IsToday);
            if (todayRow != null)
            {
                CycleDataGrid.ScrollIntoView(todayRow);
                CycleDataGrid.SelectedItem = todayRow;
            }
        }
    }

    private void OnQuitRequested()
    {
        Application.Current.Shutdown();
    }

    private void OnChangeRenewalDayRequested()
    {
        var result = MessageBox.Show(
            "Changing the renewal day will discard any manual edits and generate a new cycle. Continue?",
            "Change Renewal Day",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            var dialog = new RenewalDayDialog(_calculator, _clock) { Owner = this };
            if (dialog.ShowDialog() == true && dialog.RenewalDay.HasValue)
            {
                _viewModel.SetRenewalDay(dialog.RenewalDay.Value);
            }
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }

    public new void Show()
    {
        base.Show();
        Activate();
        _viewModel.CheckForNewDay();
    }
}
