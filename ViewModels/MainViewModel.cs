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
    private QuotaCycle? _displayedCycle;
    private int _liveTodayDayNumber;
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
        _displayedCycle = _settings.ActiveCycle;

        Days = new ObservableCollection<DayRowViewModel>();
        Calendar = new CalendarMonthViewModel();
        Chart = new UsageChartViewModel();

        ShowSettingsCommand = new RelayCommand(() => IsSettingsView = true);
        HideSettingsCommand = new RelayCommand(() => IsSettingsView = false);
        GoToPreviousCycleCommand = new RelayCommand(GoToPreviousCycle, () => CanGoToPreviousCycle);
        GoToNextCycleCommand = new RelayCommand(GoToNextCycle, () => CanGoToNextCycle);
        RefreshNowCommand = new AsyncRelayCommand(() => _sync.RefreshNowAsync(false), () => !IsSyncing);
        SignInCommand = new AsyncRelayCommand(
            () => _sync.SignInAsync(),
            () => !IsSyncing && (!IsCursorConnected || _sync.Status == SyncStatus.AuthRequired));
        DisconnectCommand = new AsyncRelayCommand(() => _sync.DisconnectAsync(), () => !IsSyncing && _sync.Status != SyncStatus.SignedOut);

        _sync.StateChanged += OnSyncStateChanged;
        _sync.SnapshotReceived += OnSnapshotReceived;
        PersistCursorAccountConnected();
        PersistLastUsageSync();

        if (_displayedCycle != null)
            RefreshDisplayedCycle();

        ApplyStartupRegistration(_settings.RunAtStartup);
    }

    public bool IsInitialized => _displayedCycle != null;

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

    public string CycleStartText => _displayedCycle != null
        ? _displayedCycle.CycleStart.ToString(InfoCardDateTimeFormat, CultureInfo.CurrentCulture)
        : string.Empty;

    public string NextRenewalText => _displayedCycle != null
        ? _displayedCycle.NextRenewal.ToString(InfoCardDateTimeFormat, CultureInfo.CurrentCulture)
        : string.Empty;

    public string CursorModelsRunOutText => FormatRunOutDate(QuotaKind.CursorModels);

    public string OtherModelsRunOutText => FormatRunOutDate(QuotaKind.OtherModels);

    public string TrayToolTipText
    {
        get
        {
            var live = _settings.ActiveCycle;
            if (live == null || !TryGetLiveTodayDay(out var day))
                return "Cursor Pace";

            var samples = _sync.Samples;
            var cursorEop = _calculator.ProjectedPercentAt(live, QuotaKind.CursorModels, live.NextRenewal, samples);
            var otherEop = _calculator.ProjectedPercentAt(live, QuotaKind.OtherModels, live.NextRenewal, samples);
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

    public ICommand ShowSettingsCommand { get; }
    public ICommand HideSettingsCommand { get; }
    public ICommand GoToPreviousCycleCommand { get; }
    public ICommand GoToNextCycleCommand { get; }
    public ICommand RefreshNowCommand { get; }
    public ICommand SignInCommand { get; }
    public ICommand DisconnectCommand { get; }

    public bool CanGoToPreviousCycle => DisplayedCycleIndex > 0;

    public bool CanGoToNextCycle
    {
        get
        {
            var index = DisplayedCycleIndex;
            return index >= 0 && index < StoredCycles.Count - 1;
        }
    }

    public string SuggestedCycleFileName =>
        $"cursor-pace-{_clock.Now.ToString(ExportFileTimestampFormat, CultureInfo.InvariantCulture)}";

    public string SuggestedUsageSamplesFileName =>
        $"usage-samples-{_clock.Now.ToString(ExportFileTimestampFormat, CultureInfo.InvariantCulture)}";

    public string SuggestedBackupFileName =>
        $"cursor-pace-backup-{_clock.Now.ToString(ExportFileTimestampFormat, CultureInfo.InvariantCulture)}";

    public void CheckForNewDay()
    {
        var live = _settings.ActiveCycle;
        if (live == null) return;

        if (_clock.Today >= live.NextRenewal)
            _ = _sync.RefreshNowAsync(false);
        else if (IsViewingLiveCycle)
        {
            UpdateLiveTodayDayNumber();
            HighlightTodayOnDisplayed();
            NotifyTodayQuotaTexts();
        }
        else
        {
            RebuildLiveDays();
            NotifyTodayQuotaTexts();
        }
    }

    private void RefreshDisplayedCycle()
    {
        if (_displayedCycle == null) return;

        var samples = _sync.Samples;
        _calculator.RebuildDays(_displayedCycle, samples, _clock.Today);
        if (IsViewingLiveCycle)
            UpdateLiveTodayDayNumber();

        Days.Clear();

        var cursorRunOutDay = _calculator.EstimateRunOutDayNumber(_displayedCycle, QuotaKind.CursorModels, samples);
        var otherRunOutDay = _calculator.EstimateRunOutDayNumber(_displayedCycle, QuotaKind.OtherModels, samples);
        var hasCursorUpdate = _calculator.TryGetLastUpdate(
            _displayedCycle, QuotaKind.CursorModels, samples, out var cursorLastUpdate, out _);
        var hasOtherUpdate = _calculator.TryGetLastUpdate(
            _displayedCycle, QuotaKind.OtherModels, samples, out var otherLastUpdate, out _);

        foreach (var day in _displayedCycle.Days)
        {
            var projectedCursor = hasCursorUpdate && day.Date > cursorLastUpdate.Date
                ? _calculator.ProjectedPercent(_displayedCycle, QuotaKind.CursorModels, day.DayNumber, samples)
                : null;
            var projectedOther = hasOtherUpdate && day.Date > otherLastUpdate.Date
                ? _calculator.ProjectedPercent(_displayedCycle, QuotaKind.OtherModels, day.DayNumber, samples)
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

        Calendar.BuildCalendar(Days.ToList(), _displayedCycle.CycleStart, _displayedCycle.NextRenewal);
        Chart.Replace(new UsageChartSeriesBuilder().Build(_displayedCycle, _calculator, samples));

        HighlightTodayOnDisplayed();
        OnPropertyChanged(nameof(IsInitialized));
        OnPropertyChanged(nameof(CycleStartText));
        OnPropertyChanged(nameof(NextRenewalText));
        NotifyTodayQuotaTexts();
        RaiseCycleNavChanged();
    }

    private void HighlightTodayOnDisplayed()
    {
        if (_displayedCycle == null) return;

        var today = _clock.Today;
        for (int i = 0; i < _displayedCycle.Days.Count && i < Days.Count; i++)
            Days[i].IsToday = _displayedCycle.Days[i].Date.Date == today;

        var todayCell = Calendar.GetCellForDate(today);
        if (todayCell != null)
            todayCell.IsToday = true;
    }

    private void RebuildLiveDays()
    {
        var live = _settings.ActiveCycle;
        if (live == null) return;

        _calculator.RebuildDays(live, _sync.Samples, _clock.Today);
        UpdateLiveTodayDayNumber();
    }

    private void UpdateLiveTodayDayNumber()
    {
        _liveTodayDayNumber = 0;
        var live = _settings.ActiveCycle;
        if (live == null) return;

        var today = _clock.Today;
        for (int i = 0; i < live.Days.Count; i++)
        {
            if (live.Days[i].Date.Date == today)
            {
                _liveTodayDayNumber = i + 1;
                break;
            }
        }
    }

    private void PersistSettings()
    {
        _store.Save(_settings);
        NotifyTodayQuotaTexts();
    }

    private void NotifyTodayQuotaTexts()
    {
        OnPropertyChanged(nameof(CursorModelsRunOutText));
        OnPropertyChanged(nameof(OtherModelsRunOutText));
        OnPropertyChanged(nameof(TrayToolTipText));
    }

    private string FormatRunOutDate(QuotaKind kind)
    {
        if (_displayedCycle == null)
            return "—";

        var instant = _calculator.EstimateRunOutInstant(_displayedCycle, kind, _sync.Samples);
        return instant.HasValue
            ? instant.Value.ToString(InfoCardDateTimeFormat, CultureInfo.CurrentCulture)
            : "—";
    }

    private bool TryGetLiveTodayDay(out QuotaDayEntry day)
    {
        day = null!;
        var live = _settings.ActiveCycle;
        if (live == null || _liveTodayDayNumber <= 0 || _liveTodayDayNumber > live.Days.Count)
            return false;

        day = live.Days[_liveTodayDayNumber - 1];
        return true;
    }

    private bool IsViewingLiveCycle =>
        _displayedCycle != null
        && _settings.ActiveCycle != null
        && _displayedCycle.CycleStart.Date == _settings.ActiveCycle.CycleStart.Date;

    private List<QuotaCycle> StoredCycles
    {
        get
        {
            var cycles = new List<QuotaCycle>(_settings.CycleHistory.Count + 1);
            cycles.AddRange(_settings.CycleHistory);
            if (_settings.ActiveCycle != null)
                cycles.Add(_settings.ActiveCycle);
            return cycles;
        }
    }

    private int DisplayedCycleIndex
    {
        get
        {
            if (_displayedCycle == null)
                return -1;

            var cycles = StoredCycles;
            for (int i = 0; i < cycles.Count; i++)
            {
                if (cycles[i].CycleStart.Date == _displayedCycle.CycleStart.Date)
                    return i;
            }

            return cycles.Count - 1;
        }
    }

    private void GoToPreviousCycle()
    {
        var index = DisplayedCycleIndex;
        if (index <= 0)
            return;

        _displayedCycle = StoredCycles[index - 1];
        RefreshDisplayedCycle();
    }

    private void GoToNextCycle()
    {
        var cycles = StoredCycles;
        var index = DisplayedCycleIndex;
        if (index < 0 || index >= cycles.Count - 1)
            return;

        _displayedCycle = cycles[index + 1];
        RefreshDisplayedCycle();
    }

    private void RaiseCycleNavChanged()
    {
        OnPropertyChanged(nameof(CanGoToPreviousCycle));
        OnPropertyChanged(nameof(CanGoToNextCycle));
        ((RelayCommand)GoToPreviousCycleCommand).RaiseCanExecuteChanged();
        ((RelayCommand)GoToNextCycleCommand).RaiseCanExecuteChanged();
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
        var live = _settings.ActiveCycle;
        var viewingLive = _displayedCycle == null || IsViewingLiveCycle;

        if (live == null || live.CycleStart.Date != startLocal.Date)
        {
            CycleHistory.ArchiveIfNewStartDate(_settings, startLocal);
            var created = _calculator.GenerateCycleFromBounds(startLocal, endLocal);
            _settings.ActiveCycle = created;
            if (viewingLive)
                _displayedCycle = created;
        }
        else if (live.CycleStart != startLocal || live.NextRenewal != endLocal)
        {
            var updated = new QuotaCycle
            {
                RenewalDay = startLocal.Day,
                CycleStart = startLocal,
                NextRenewal = endLocal
            };
            _settings.ActiveCycle = updated;
            if (viewingLive)
                _displayedCycle = updated;
        }

        PersistSettings();
        if (viewingLive)
        {
            _displayedCycle = _settings.ActiveCycle;
            RefreshDisplayedCycle();
        }
        else
        {
            RebuildLiveDays();
            NotifyTodayQuotaTexts();
            RaiseCycleNavChanged();
        }
    }

    private static string FormatPercent(decimal value) =>
        $"{(int)Math.Round(value, MidpointRounding.AwayFromZero)}%";

    public void SaveWindowPlacement(int x, int y, int width, int height, bool maximized)
    {
        if (_settings.WindowX == x
            && _settings.WindowY == y
            && _settings.WindowWidth == width
            && _settings.WindowHeight == height
            && _settings.WindowMaximized == maximized)
            return;

        _settings.WindowX = x;
        _settings.WindowY = y;
        _settings.WindowWidth = width;
        _settings.WindowHeight = height;
        _settings.WindowMaximized = maximized;
        _store.Save(_settings);
    }

    public bool TryGetSavedWindowPlacement(
        out int? x,
        out int? y,
        out int? width,
        out int? height,
        out bool maximized)
    {
        x = _settings.WindowX;
        y = _settings.WindowY;
        width = _settings.WindowWidth is int savedWidth && savedWidth > 0 ? savedWidth : null;
        height = _settings.WindowHeight is int savedHeight && savedHeight > 0 ? savedHeight : null;
        maximized = _settings.WindowMaximized;
        return x is not null || y is not null || width is not null || height is not null || maximized;
    }

    public bool TryBuildCycleCsv(out string csv)
    {
        csv = string.Empty;
        if (_displayedCycle == null || Days.Count == 0)
            return false;

        csv = CycleCsvBuilder.Build(_displayedCycle, _calculator, _sync.Samples);
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

        _displayedCycle = _settings.ActiveCycle;
        if (_displayedCycle != null)
        {
            RefreshDisplayedCycle();
        }
        else
        {
            Days.Clear();
            Calendar.BuildCalendar([], default, default);
            Chart.Replace(null);
            _liveTodayDayNumber = 0;
            OnPropertyChanged(nameof(IsInitialized));
            OnPropertyChanged(nameof(CycleStartText));
            OnPropertyChanged(nameof(NextRenewalText));
            NotifyTodayQuotaTexts();
            RaiseCycleNavChanged();
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
