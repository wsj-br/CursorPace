using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CursorUsageProgress.Services;
using CursorUsageProgress.ViewModels;
using CursorUsageProgress.Views;

namespace CursorUsageProgress;

public partial class App : Application
{
    private ISingleInstance? _singleInstance;
    private ITrayService? _trayService;
    private MainWindow? _mainWindow;
    private MainViewModel? _viewModel;
    private IUsageSyncService? _syncService;
    private IUiDispatcher? _dispatcher;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            base.OnFrameworkInitializationCompleted();
            return;
        }

        desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _singleInstance = SingleInstance.Create();
        if (!_singleInstance.TryAcquire())
        {
            _singleInstance.SignalExisting();
            _singleInstance.Dispose();
            _singleInstance = null;
            desktop.Shutdown();
            return;
        }

        _dispatcher = new AvaloniaUiDispatcher();
        _singleInstance.Listen(ShowMainWindow);

        var clock = new SystemClock();
        var calculator = new CycleCalculator();
        var store = new JsonPlanStore();
        var startupReg = StartupRegistration.Create();
        var sampleStore = new JsonUsageSampleStore();
        var usageClient = new NativeWebViewCursorUsageClient(_dispatcher);
        var sync = new UsageSyncService(_dispatcher, usageClient, sampleStore, clock, store);
        var backup = new DataBackupService(store, sampleStore);
        _syncService = sync;

        _viewModel = new MainViewModel(clock, calculator, store, startupReg, sync, backup);

        _trayService = new TrayService();
        _trayService.Initialize(
            onOpenRequested: ShowMainWindow,
            onQuitRequested: Quit);
        _trayService.UpdateToolTip(_viewModel.TrayToolTipText);
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        var launchInBackground = Environment.GetCommandLineArgs().Contains("--background")
            || _viewModel.StartInNotificationTray;

        _mainWindow = new MainWindow(_viewModel, _dispatcher, startInTray: launchInBackground);
        desktop.MainWindow = _mainWindow;

        if (!launchInBackground)
            _mainWindow.Show();

        _ = _viewModel.StartSyncAsync();

        base.OnFrameworkInitializationCompleted();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.TrayToolTipText))
            _trayService?.UpdateToolTip(_viewModel!.TrayToolTipText);
    }

    public void Quit()
    {
        if (_dispatcher != null)
            _dispatcher.Post(QuitCore);
        else
            QuitCore();
    }

    private void QuitCore()
    {
        _syncService?.Dispose();
        _syncService = null;
        _trayService?.Dispose();
        _trayService = null;
        _singleInstance?.Dispose();
        _singleInstance = null;
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void ShowMainWindow()
    {
        _dispatcher?.Post(() => _mainWindow?.BringToFront());
    }
}
