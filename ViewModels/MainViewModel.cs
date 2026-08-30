using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using CursorPace.Models;
using CursorPace.Services;

namespace CursorPace.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly IClock _clock;
    private readonly ICycleCalculator _calculator;
    private readonly IPlanStore _store;
    private readonly IStartupRegistration _startupReg;
    private readonly IUsageSyncService _sync;
    private readonly IDataBackupService _backup;

    private const string InfoCardDateTimeFormat = "dd-MMM HH:mm";
    private const string ExportFileTimestampFormat = "yyyy-MM-dd-HH_mm_ss";

    private AppSettings _settings;
    private QuotaCycle? _cycle;
    private int _currentDayNumber;
    private bool _isSettingsView;

    public MainViewModel(
        IClock clock,
        ICycleCalculator calculator,
        IPlanStore store,
        IStartupRegistration startupReg,
        IUsageSyncService sync,
        IDataBackupService backup)
    {
        _clock = clock;
        _calculator = calculator;
        _store = store;
        _startupReg = startupReg;
        _sync = sync;
        _backup = backup;

        _settings = _store.Load();
        _cycle = _settings.ActiveCycle;
        if (_cycle != null)
            _calculator.RebuildDays(_cycle, _sync.Samples, _clock.Today);

        Days = new ObservableCollection<DayRowViewModel>();
        Calendar = new CalendarMonthViewModel();
        Chart = new UsageChartViewModel();

        QuitCommand = new RelayCommand(OnQuit);
        ShowSettingsCommand = new RelayCommand(() => IsSettingsView = true);
        HideSettingsCommand = new RelayCommand(() => IsSettingsView = false);
        RefreshNowCommand = new AsyncRelayCommand(() => _sync.RefreshNowAsync(false), () => !IsSyncing);
        SignInCommand = new AsyncRelayCommand(
            () => _sync.SignInAsync(),
            () => !IsSyncing && (!IsCursorConnected || _sync.Status == SyncStatus.AuthRequired));
        DisconnectCommand = new AsyncRelayCommand(() => _sync.DisconnectAsync(), () => !IsSyncing && _sync.Status != SyncStatus.SignedOut);

        _sync.StateChanged += OnSyncStateChanged;
        _sync.SnapshotReceived += OnSnapshotReceived;
        PersistCursorAccountConnected();
        PersistLastUsageSync();

        if (_cycle != null)
            RefreshCycle();

        ApplyStartupRegistration(_settings.RunAtStartup);
    }

    public bool IsInitialized => _cycle != null;

    public bool IsSettingsView
    {
        get => _isSettingsView;
        private set
        {
            if (!SetProperty(ref _isSettingsView, value))
                return;
            OnPropertyChanged(nameof(IsMainView));
        }
    }

    public bool IsMainView => !_isSettingsView;

    public ObservableCollection<DayRowViewModel> Days { get; }

    public CalendarMonthViewModel Calendar { get; }

    public UsageChartViewModel Chart { get; }

    public bool IsChartView
    {
        get => _settings.ShowChartView;
        set
        {
            if (_settings.ShowChartView == value) return;
            _settings.ShowChartView = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCalendarView));
            _store.Save(_settings);
        }
    }

    public bool IsCalendarView => !_settings.ShowChartView;

    public string CycleStartText => _cycle != null
        ? _cycle.CycleStart.ToString(InfoCardDateTimeFormat, CultureInfo.CurrentCulture)
        : string.Empty;

    public string NextRenewalText => _cycle != null
        ? _cycle.NextRenewal.ToString(InfoCardDateTimeFormat, CultureInfo.CurrentCulture)
        : string.Empty;

    public string CursorModelsRunOutText => FormatRunOutDate(QuotaKind.CursorModels);

    public string OtherModelsRunOutText => FormatRunOutDate(QuotaKind.OtherModels);

    public string TrayToolTipText
    {
        get
        {
            if (!TryGetTodayDay(out var day))
                return "Cursor Pace";

            var samples = _sync.Samples;
            var cursorEop = _calculator.ProjectedPercentAt(_cycle!, QuotaKind.CursorModels, _cycle!.NextRenewal, samples);
            var otherEop = _calculator.ProjectedPercentAt(_cycle, QuotaKind.OtherModels, _cycle.NextRenewal, samples);
            return $"Cursor Pace\nCursor: {FormatPercent(day.CursorModelsPercent)}{FormatEop(cursorEop)}\nOther models: {FormatPercent(day.OtherModelsPercent)}{FormatEop(otherEop)}";
        }
    }

    private static string FormatEop(decimal? value) =>
        value.HasValue ? $" - projected at renewal {FormatPercent(value.Value)}" : string.Empty;

    public bool RunAtStartup
    {
        get => _settings.RunAtStartup;
        set
        {
            var oldValue = _settings.RunAtStartup;
            if (oldValue == value) return;

            _settings.RunAtStartup = value;
            OnPropertyChanged();

            ApplyStartupRegistration(value);

            _store.Save(_settings);
        }
    }

    public bool StartInNotificationTray
    {
        get => _settings.StartInNotificationTray;
        set
        {
            if (_settings.StartInNotificationTray == value) return;

            _settings.StartInNotificationTray = value;
            OnPropertyChanged();

            if (_settings.RunAtStartup)
                ApplyStartupRegistration(true);

            _store.Save(_settings);
        }
    }

    public UiThemeMode ThemeMode
    {
        get => UiTheme.Clamp(_settings.ThemeMode);
        set
        {
            var mode = UiTheme.Clamp(value);
            if (_settings.ThemeMode == mode) return;
            _settings.ThemeMode = mode;
            OnPropertyChanged();
            _store.Save(_settings);
        }
    }

    public IReadOnlyList<UiThemeMode> ThemeModeOptions => UiTheme.AllowedModes;

    public bool AutoSyncEnabled
    {
        get => _settings.AutoSyncEnabled;
        set
        {
            if (_settings.AutoSyncEnabled == value) return;
            _settings.AutoSyncEnabled = value;
            OnPropertyChanged();
            _store.Save(_settings);
            _sync.SetAutoSyncEnabled(value);
        }
    }

    public int SyncIntervalHours
    {
        get => SyncInterval.Clamp(_settings.SyncIntervalHours);
        set
        {
            var hours = SyncInterval.Clamp(value);
            if (_settings.SyncIntervalHours == hours) return;
            _settings.SyncIntervalHours = hours;
            OnPropertyChanged();
            _store.Save(_settings);
            _sync.SetIntervalHours(hours);
        }
    }

    public IReadOnlyList<int> SyncIntervalOptions => SyncInterval.AllowedHours;

    public string SyncStatusText => _sync.StatusText;

    public string LastSyncText => _sync.StatusText;

    public bool IsSyncing => _sync.Status == SyncStatus.Syncing;

    public bool HasSyncAlert =>
        _sync.Status is SyncStatus.Error or SyncStatus.AuthRequired or SyncStatus.RateLimited;

    public bool ShowSyncAlertSignInActions =>
        _sync.Status == SyncStatus.AuthRequired && !IsSyncing;

    public bool ShowSignInButton =>
        !IsSyncing && (!IsCursorConnected || _sync.Status == SyncStatus.AuthRequired);

    public string EmptyStateText =>
        IsSyncing && IsCursorConnected
            ? "Connected — loading usage…"
            : IsSyncing
                ? "Signing in…"
                : "Sign in to Cursor to load your billing cycle and usage.";

    public bool ShowEmptySignIn => !IsInitialized && !IsSyncing && !IsCursorConnected;

    public bool IsCursorConnected => _sync.IsSignedIn;

    public string CursorAccountTitle =>
        IsCursorConnected ? "Cursor account (connected)" : "Cursor account (disconnected)";

    public string AboutVersion => AppInfo.Current.Version;

    public string AboutBuildDate => AppInfo.Current.FormatBuildDate(CultureInfo.CurrentCulture);

    public string AboutCopyright => AppInfo.Current.Copyright;

    public string AboutLicense => AppInfo.LicenseName;

    public string AboutRepositoryUrl => AppInfo.RepositoryUrl;

    public Uri AboutRepositoryUri => AppInfo.RepositoryUri;

    public ICommand QuitCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand HideSettingsCommand { get; }
    public ICommand RefreshNowCommand { get; }
    public ICommand SignInCommand { get; }
    public ICommand DisconnectCommand { get; }

    public string SuggestedCycleFileName =>
        $"cursor-pace-{_clock.Now.ToString(ExportFileTimestampFormat, CultureInfo.InvariantCulture)}";

    public string SuggestedUsageSamplesFileName =>
        $"usage-samples-{_clock.Now.ToString(ExportFileTimestampFormat, CultureInfo.InvariantCulture)}";

    public string SuggestedBackupFileName =>
        $"cursor-pace-backup-{_clock.Now.ToString(ExportFileTimestampFormat, CultureInfo.InvariantCulture)}";

    public event Action? QuitRequested;

    public void CheckForNewDay()
    {
        if (_cycle == null) return;

        if (_clock.Today >= _cycle.NextRenewal)
            _ = _sync.RefreshNowAsync(false);
        else
            RefreshCurrentDay();
    }

    private void RefreshCycle()
    {
        if (_cycle == null) return;

        _calculator.RebuildDays(_cycle, _sync.Samples, _clock.Today);

        Days.Clear();
        var samples = _sync.Samples;

        var cursorRunOutDay = _calculator.EstimateRunOutDayNumber(_cycle, QuotaKind.CursorModels, samples);
        var otherRunOutDay = _calculator.EstimateRunOutDayNumber(_cycle, QuotaKind.OtherModels, samples);
        var hasCursorUpdate = _calculator.TryGetLastUpdate(
            _cycle, QuotaKind.CursorModels, samples, out var cursorLastUpdate, out _);
        var hasOtherUpdate = _calculator.TryGetLastUpdate(
            _cycle, QuotaKind.OtherModels, samples, out var otherLastUpdate, out _);

        foreach (var day in _cycle.Days)
        {
            var projectedCursor = hasCursorUpdate && day.Date > cursorLastUpdate.Date
                ? _calculator.ProjectedPercent(_cycle, QuotaKind.CursorModels, day.DayNumber, samples)
                : null;
            var projectedOther = hasOtherUpdate && day.Date > otherLastUpdate.Date
                ? _calculator.ProjectedPercent(_cycle, QuotaKind.OtherModels, day.DayNumber, samples)
                : null;
            Days.Add(new DayRowViewModel(
                day,
                (double)day.CursorModelsPercent,
                (double)day.OtherModelsPercent,
                projectedCursor,
                projectedOther,
                cursorRunOutDay == day.DayNumber,
                otherRunOutDay == day.DayNumber));
        }

        Calendar.BuildCalendar(Days.ToList(), _cycle.CycleStart, _cycle.NextRenewal);
        Chart.Replace(new UsageChartSeriesBuilder().Build(_cycle, _calculator, samples));

        RefreshCurrentDay();
        OnPropertyChanged(nameof(IsInitialized));
        OnPropertyChanged(nameof(CycleStartText));
        OnPropertyChanged(nameof(NextRenewalText));
    }

    private void RefreshCurrentDay()
    {
        if (_cycle == null) return;

        var today = _clock.Today;
        _currentDayNumber = 0;

        for (int i = 0; i < _cycle.Days.Count; i++)
        {
            var isToday = _cycle.Days[i].Date.Date == today;
            Days[i].IsToday = isToday;

            if (isToday)
                _currentDayNumber = i + 1;
        }

        var todayCell = Calendar.GetCellForDate(today);
        if (todayCell != null)
            todayCell.IsToday = true;

        NotifyTodayQuotaTexts();
    }

    private void PersistCycle()
    {
        if (_cycle == null) return;
        _settings.ActiveCycle = _cycle;
        _store.Save(_settings);
        NotifyTodayQuotaTexts();
    }

    private void OnQuit()
    {
        QuitRequested?.Invoke();
    }

    private void NotifyTodayQuotaTexts()
    {
        OnPropertyChanged(nameof(CursorModelsRunOutText));
        OnPropertyChanged(nameof(OtherModelsRunOutText));
        OnPropertyChanged(nameof(TrayToolTipText));
    }

    private string FormatRunOutDate(QuotaKind kind)
    {
        if (_cycle == null)
            return "—";

        var instant = _calculator.EstimateRunOutInstant(_cycle, kind, _sync.Samples);
        return instant.HasValue
            ? instant.Value.ToString(InfoCardDateTimeFormat, CultureInfo.CurrentCulture)
            : "—";
    }

    private bool TryGetTodayDay(out QuotaDayEntry day)
    {
        day = null!;
        if (_cycle == null || _currentDayNumber <= 0 || _currentDayNumber > _cycle.Days.Count)
            return false;

        day = _cycle.Days[_currentDayNumber - 1];
        return true;
    }

    public Task StartSyncAsync() =>
        _sync.StartAsync(_settings.AutoSyncEnabled, _settings.SyncIntervalHours);

    private void OnSyncStateChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(SyncStatusText));
        OnPropertyChanged(nameof(LastSyncText));
        OnPropertyChanged(nameof(IsSyncing));
        OnPropertyChanged(nameof(HasSyncAlert));
        OnPropertyChanged(nameof(ShowSyncAlertSignInActions));
        OnPropertyChanged(nameof(ShowSignInButton));
        OnPropertyChanged(nameof(EmptyStateText));
        OnPropertyChanged(nameof(ShowEmptySignIn));
        OnPropertyChanged(nameof(IsCursorConnected));
        OnPropertyChanged(nameof(CursorAccountTitle));
        OnPropertyChanged(nameof(TrayToolTipText));
        ((AsyncRelayCommand)RefreshNowCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)SignInCommand).RaiseCanExecuteChanged();
        ((AsyncRelayCommand)DisconnectCommand).RaiseCanExecuteChanged();
        PersistCursorAccountConnected();
        PersistLastUsageSync();
    }

    private void PersistCursorAccountConnected()
    {
        if (_settings.CursorAccountConnected == IsCursorConnected)
            return;

        _settings.CursorAccountConnected = IsCursorConnected;
        _store.Save(_settings);
    }

    private void PersistLastUsageSync()
    {
        if (_sync.LastSuccessUtc is not { } last)
            return;
        if (_settings.LastUsageSyncUtc == last)
            return;

        _settings.LastUsageSyncUtc = last;
        _store.Save(_settings);
    }

    private void OnSnapshotReceived(object? sender, UsageSnapshot snapshot)
    {
        var startLocal = snapshot.BillingCycleStartUtc.LocalDateTime;
        var endLocal = snapshot.BillingCycleEndUtc.LocalDateTime;

        if (_cycle == null || _cycle.CycleStart.Date != startLocal.Date)
        {
            _cycle = _calculator.GenerateCycleFromBounds(startLocal, endLocal);
        }
        else if (_cycle.CycleStart != startLocal || _cycle.NextRenewal != endLocal)
        {
            _cycle = new QuotaCycle
            {
                RenewalDay = startLocal.Day,
                CycleStart = startLocal,
                NextRenewal = endLocal
            };
        }

        PersistCycle();
        RefreshCycle();
    }

    private static string FormatPercent(decimal value) =>
        $"{(int)Math.Round(value, MidpointRounding.AwayFromZero)}%";

    public void SaveWindowPosition(int x, int y)
    {
        if (_settings.WindowX == x && _settings.WindowY == y)
            return;

        _settings.WindowX = x;
        _settings.WindowY = y;
        _store.Save(_settings);
    }

    public bool TryGetSavedWindowPosition(out int x, out int y)
    {
        if (_settings.WindowX is int savedX && _settings.WindowY is int savedY)
        {
            x = savedX;
            y = savedY;
            return true;
        }

        x = 0;
        y = 0;
        return false;
    }

    public bool TryBuildCycleCsv(out string csv)
    {
        csv = string.Empty;
        if (_cycle == null || Days.Count == 0)
            return false;

        csv = CycleCsvBuilder.Build(_cycle, _calculator, _sync.Samples);
        return true;
    }

    public bool TryBuildUsageSamplesCsv(out string csv)
    {
        csv = string.Empty;
        if (_sync.Samples.Count == 0)
            return false;

        csv = UsageSamplesCsvBuilder.Build(_sync.Samples);
        return true;
    }

    public bool TryWriteBackup(Stream destination, out string? error)
    {
        try
        {
            _backup.WriteBackup(destination, new DateTimeOffset(_clock.Now));
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = string.IsNullOrWhiteSpace(ex.Message)
                ? "Could not write the backup file."
                : ex.Message;
            return false;
        }
    }

    public bool TryRestoreBackup(Stream source, out string? error)
    {
        try
        {
            var result = _backup.RestoreBackup(source);
            if (!result.Success)
            {
                error = result.ErrorMessage ?? "Could not restore the backup file.";
                return false;
            }

            ReloadAfterRestore();
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = string.IsNullOrWhiteSpace(ex.Message)
                ? "Could not restore the backup file."
                : ex.Message;
            return false;
        }
    }

    private void ApplyStartupRegistration(bool enabled)
    {
        try
        {
            if (enabled)
                _startupReg.Register(_settings.StartInNotificationTray);
            else
                _startupReg.Unregister();
        }
        catch
        {
        }
    }

    private void ReloadAfterRestore()
    {
        _settings = _store.Load();
        _sync.ReloadPersistedUsage(_settings.LastUsageSyncUtc);
        _sync.SetAutoSyncEnabled(_settings.AutoSyncEnabled);
        _sync.SetIntervalHours(_settings.SyncIntervalHours);

        ApplyStartupRegistration(_settings.RunAtStartup);

        _cycle = _settings.ActiveCycle;
        if (_cycle != null)
        {
            RefreshCycle();
        }
        else
        {
            Days.Clear();
            Calendar.BuildCalendar([], default, default);
            Chart.Replace(null);
            OnPropertyChanged(nameof(IsInitialized));
            OnPropertyChanged(nameof(CycleStartText));
            OnPropertyChanged(nameof(NextRenewalText));
            NotifyTodayQuotaTexts();
        }

        OnPropertyChanged(nameof(IsChartView));
        OnPropertyChanged(nameof(IsCalendarView));
        OnPropertyChanged(nameof(RunAtStartup));
        OnPropertyChanged(nameof(StartInNotificationTray));
        OnPropertyChanged(nameof(ThemeMode));
        OnPropertyChanged(nameof(AutoSyncEnabled));
        OnPropertyChanged(nameof(SyncIntervalHours));
        PersistCursorAccountConnected();
        PersistLastUsageSync();
    }
}
