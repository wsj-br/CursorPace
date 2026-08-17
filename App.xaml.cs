using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using CursorQuotaProgress.Services;
using CursorQuotaProgress.ViewModels;
using CursorQuotaProgress.Views;

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
    private DispatcherQueue? _dispatcherQueue;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        bool createdNew;
        _mutex = new Mutex(true, MutexName, out createdNew);

        if (!createdNew)
        {
            SignalExistingInstance();
            Exit();
            return;
        }

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        ListenForActivationSignal();

        var clock = new SystemClock();
        var calculator = new CycleCalculator();
        var store = new JsonPlanStore();
        var startupReg = new WindowsStartupRegistration();

        _viewModel = new MainViewModel(clock, calculator, store, startupReg);

        _trayService = new TrayService();
        _trayService.Initialize(
            onOpenRequested: ShowMainWindow,
            onQuitRequested: () => _dispatcherQueue!.TryEnqueue(Exit));

        _mainWindow = new MainWindow(_viewModel, calculator, clock);

        bool launchInBackground = Environment.GetCommandLineArgs().Contains("--background");

        if (!_viewModel.IsInitialized)
        {
            _mainWindow.Activate();
            _mainWindow.ShowRenewalDaySetup();
        }
        else if (!launchInBackground)
        {
            _mainWindow.Activate();
        }
    }

    private void ShowMainWindow()
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            if (_mainWindow != null)
            {
                _mainWindow.BringToFront();
            }
        });
    }

    private void SignalExistingInstance()
    {
        try
        {
            var handle = EventWaitHandle.OpenExisting(MutexName + "_Event");
            handle.Set();
        }
        catch { }
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
        { IsBackground = true };

        _namedEventThread.Start();
    }
}
