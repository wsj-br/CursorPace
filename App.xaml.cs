using System.Threading;
using System.Windows;
using CursorQuotaProgress.Services;
using CursorQuotaProgress.ViewModels;
using CursorQuotaProgress.Views;
using Application = System.Windows.Application;

namespace CursorQuotaProgress;

public partial class App : Application
{
    private const string MutexName = "CursorQuotaProgress_SingleInstance";

    private Mutex? _mutex;
    private EventWaitHandle? _eventWaitHandle;
    private Thread? _namedEventThread;

    private ITrayService? _trayService;
    private MainWindow? _mainWindow;
    private MainViewModel? _viewModel;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        bool createdNew;
        _mutex = new Mutex(true, MutexName, out createdNew);

        if (!createdNew)
        {
            SignalExistingInstance();
            Shutdown();
            return;
        }

        ListenForActivationSignal();

        var clock = new SystemClock();
        var calculator = new CycleCalculator();
        var store = new JsonPlanStore();
        var startupReg = new WindowsStartupRegistration();

        _viewModel = new MainViewModel(clock, calculator, store, startupReg);

        _trayService = new TrayService();
        _trayService.Initialize(
            onOpenRequested: ShowMainWindow,
            onQuitRequested: () => Shutdown());

        _mainWindow = new MainWindow(_viewModel, calculator, clock);

        bool launchInBackground = e.Args.Contains("--background");

        // Show initial setup if no renewal day is configured
        if (!_viewModel.IsInitialized)
        {
            // Show main window first so it can be the owner of the dialog
            _mainWindow.Show();

            var dialog = new RenewalDayDialog(calculator, clock) { Owner = _mainWindow };
            if (dialog.ShowDialog() == true && dialog.RenewalDay.HasValue)
            {
                _viewModel.SetRenewalDay(dialog.RenewalDay.Value);
            }
            else
            {
                Shutdown();
                return;
            }
        }
        else if (!launchInBackground)
        {
            _mainWindow.Show();
        }
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _trayService?.Dispose();

        _namedEventThread?.Interrupt();
        _eventWaitHandle?.Close();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow != null)
        {
            Dispatcher.Invoke(() =>
            {
                _mainWindow.Show();
                _mainWindow.WindowState = WindowState.Normal;
                _mainWindow.Activate();
            });
        }
    }

    private void SignalExistingInstance()
    {
        try
        {
            var eventWaitHandle = EventWaitHandle.OpenExisting(MutexName + "_Event");
            eventWaitHandle.Set();
        }
        catch
        {
        }
    }

    private void ListenForActivationSignal()
    {
        _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, MutexName + "_Event");

        _namedEventThread = new Thread(() =>
        {
            while (true)
            {
                try
                {
                    _eventWaitHandle.WaitOne();
                    ShowMainWindow();
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
            }
        })
        {
            IsBackground = true
        };

        _namedEventThread.Start();
    }
}

