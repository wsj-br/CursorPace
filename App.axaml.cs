using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using CursorPace.Models;
using CursorPace.Services;
using CursorPace.ViewModels;
using CursorPace.Views;

namespace CursorPace;

public partial class App : Application
{
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

        _dispatcher = new AvaloniaUiDispatcher();
        SingleInstance.Current?.Listen(ShowMainWindow);

        var clock = new SystemClock();
        var calculator = new CycleCalculator();
        var store = new JsonPlanStore();
        var startupReg = StartupRegistration.Create();
        var sampleStore = new JsonUsageSampleStore();
        var usageClient = new NativeWebViewCursorUsageClient(_dispatcher);
        var sync = new UsageSyncService(_dispatcher, usageClient, sampleStore, clock, store);
        var backup = new DataBackupService(store, sampleStore);
        var remoteSync = new RemoteSyncService();
        _syncService = sync;

        _viewModel = new MainViewModel(clock, calculator, store, startupReg, sync, backup, remoteSync, _dispatcher);
        ApplyTheme(_viewModel.ThemeMode);
        LinuxDesktopIntegration.EnsureUserEntry();
        MacDesktopIntegration.EnsureDockIcon();

        _trayService = new TrayService();
        _trayService.Initialize(
            onOpenRequested: ShowMainWindow,
            onQuitRequested: Quit);
        _trayService.UpdateToolTip(_viewModel.TrayToolTipText);
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        var launchInBackground = LaunchMode.HideMainWindow(
            _viewModel.StartInNotificationTray,
            Environment.GetCommandLineArgs());

        _mainWindow = new MainWindow(_viewModel, _dispatcher);

        // ClassicDesktopStyleApplicationLifetime.StartCore always calls
        // MainWindow.Show() after this method returns. Leave it unset when
        // starting in the tray so that Show is skipped.
        if (!launchInBackground)
            desktop.MainWindow = _mainWindow;
        MacDesktopIntegration.ApplyDockVisibility(!launchInBackground);
        if (launchInBackground && OperatingSystem.IsMacOS())
            _dispatcher.Post(() => MacDesktopIntegration.ApplyDockVisibility(false));

        _ = _viewModel.StartSyncAsync();

        base.OnFrameworkInitializationCompleted();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.TrayToolTipText))
            _trayService?.UpdateToolTip(_viewModel!.TrayToolTipText);
        else if (e.PropertyName == nameof(MainViewModel.ThemeMode))
            ApplyTheme(_viewModel!.ThemeMode);
    }

    private void ApplyTheme(UiThemeMode mode)
    {
        RequestedThemeVariant = mode switch
        {
            UiThemeMode.Light => ThemeVariant.Light,
            UiThemeMode.Dark => ThemeVariant.Dark,
            UiThemeMode.System => ThemeVariant.Default,
            _ => ThemeVariant.Default
        };
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
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void ShowMainWindow()
    {
        _dispatcher?.Post(() =>
        {
            if (_mainWindow == null)
                return;
            MacDesktopIntegration.ApplyDockVisibility(true);
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow ??= _mainWindow;
            _mainWindow.BringToFront();
        });
    }
}
