using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Input;
using CursorUsageProgress.Models;
using CursorUsageProgress.Services;
using Microsoft.UI.Xaml;

namespace CursorUsageProgress.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly IClock _clock;
    private readonly ICycleCalculator _calculator;
    private readonly IPlanStore _store;
    private readonly IStartupRegistration _startupReg;

    private AppSettings _settings;
    private QuotaCycle? _cycle;
    private bool _isInitialized;
    private int _currentDayNumber;

    public MainViewModel(
        IClock clock,
        ICycleCalculator calculator,
        IPlanStore store,
        IStartupRegistration startupReg)
    {
        _clock = clock;
        _calculator = calculator;
        _store = store;
        _startupReg = startupReg;

        _settings = _store.Load();
        _cycle = _settings.ActiveCycle;
        if (_cycle != null)
            _calculator.RebuildDays(_cycle);

        Days = new ObservableCollection<DayRowViewModel>();
        Calendar = new CalendarMonthViewModel();

        ChangeRenewalDayCommand = new RelayCommand(OnChangeRenewalDay);
        ResetCycleCommand = new RelayCommand(OnResetCycle, () => _isInitialized);
        QuitCommand = new RelayCommand(OnQuit);
        ApplyEditCommand = new RelayCommand(OnApplyEdit);
        ResetDayCommand = new RelayCommand(OnResetEditingDay);
        CancelEditCommand = new RelayCommand(StopEditing);

        if (_settings.RenewalDay.HasValue && _cycle != null)
        {
            _isInitialized = true;
            RefreshCycle();
        }
    }

    public bool IsInitialized => _isInitialized;

    public int RenewalDay => _settings.RenewalDay ?? 0;

    public ObservableCollection<DayRowViewModel> Days { get; }

    public CalendarMonthViewModel Calendar { get; }

    public string CycleStartText => _cycle != null
        ? _cycle.CycleStart.ToString("dd-MMM", CultureInfo.CurrentCulture)
        : string.Empty;

    public string NextRenewalText => _cycle != null
        ? _cycle.NextRenewal.ToString("dd-MMM", CultureInfo.CurrentCulture)
        : string.Empty;

    public string CursorModelsRunOutText => FormatRunOutDate(QuotaKind.CursorModels);

    public string OtherModelsRunOutText => FormatRunOutDate(QuotaKind.OtherModels);

    public string TrayToolTipText
    {
        get
        {
            if (!TryGetTodayDay(out var day))
                return "Cursor Usage Progress";

            var lastDay = _calculator.TotalDays(_cycle!);
            var cursorEop = _calculator.ProjectedPercent(_cycle!, QuotaKind.CursorModels, lastDay);
            var otherEop = _calculator.ProjectedPercent(_cycle!, QuotaKind.OtherModels, lastDay);
            return $"Cursor Usage Progress\nCursor: {FormatPercent(day.CursorModelsPercent)}{FormatEop(cursorEop)}\nOther models: {FormatPercent(day.OtherModelsPercent)}{FormatEop(otherEop)}";
        }
    }

    private static string FormatEop(decimal? value) =>
        value.HasValue ? $" - EOP {FormatPercent(value.Value)}" : string.Empty;

    public bool RunAtStartup
    {
        get => _settings.RunAtStartup;
        set
        {
            var oldValue = _settings.RunAtStartup;
            if (oldValue == value) return;

            _settings.RunAtStartup = value;
            OnPropertyChanged();

            if (value)
                _startupReg.Register();
            else
                _startupReg.Unregister();

            _store.Save(_settings);
        }
    }

    public ICommand ChangeRenewalDayCommand { get; }
    public ICommand ResetCycleCommand { get; }
    public ICommand QuitCommand { get; }
    public ICommand ApplyEditCommand { get; }
    public ICommand ResetDayCommand { get; }
    public ICommand CancelEditCommand { get; }

    private bool _isEditingDay;
    private DayRowViewModel? _editingDay;
    private double _editingCursorQuota;
    private double _editingOtherQuota;

    public bool IsEditingDay
    {
        get => _isEditingDay;
        private set
        {
            if (_isEditingDay != value)
            {
                _isEditingDay = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EditPanelVisibility));
            }
        }
    }

    public Visibility EditPanelVisibility => _isEditingDay ? Visibility.Visible : Visibility.Collapsed;

    public string EditingDayText => _editingDay != null
        ? $"Day {_editingDay.DayNumber} - {_editingDay.DateText}"
        : string.Empty;

    public double EditingCursorQuota
    {
        get => _editingCursorQuota;
        set
        {
            if (_editingCursorQuota != value)
            {
                _editingCursorQuota = value;
                OnPropertyChanged();
            }
        }
    }

    public double EditingOtherQuota
    {
        get => _editingOtherQuota;
        set
        {
            if (_editingOtherQuota != value)
            {
                _editingOtherQuota = value;
                OnPropertyChanged();
            }
        }
    }

    public event Action? QuitRequested;

    public void CheckForNewDay()
    {
        if (_cycle == null) return;

        var today = _clock.Today;
        if (today >= _cycle.NextRenewal)
        {
            StartNewCycle();
        }
        else
        {
            RefreshCurrentDay();
        }
    }

    private void RefreshCycle()
    {
        if (_cycle == null) return;

        Days.Clear();
        var totalDays = _calculator.TotalDays(_cycle);

        var cursorRunOutDay = _calculator.EstimateRunOutDayNumber(_cycle, QuotaKind.CursorModels);
        var otherRunOutDay = _calculator.EstimateRunOutDayNumber(_cycle, QuotaKind.OtherModels);

        foreach (var day in _cycle.Days)
        {
            var projectedCursor = _calculator.ProjectedPercent(_cycle, QuotaKind.CursorModels, day.DayNumber);
            var projectedOther = _calculator.ProjectedPercent(_cycle, QuotaKind.OtherModels, day.DayNumber);
            var vm = new DayRowViewModel(
                day,
                (double)day.CursorModelsPercent,
                (double)day.OtherModelsPercent,
                projectedCursor,
                projectedOther,
                cursorRunOutDay == day.DayNumber,
                otherRunOutDay == day.DayNumber);
            vm.CursorModelsEdited += OnCursorModelsEdited;
            vm.OtherModelsEdited += OnOtherModelsEdited;
            Days.Add(vm);
        }

        // Build calendar grid from days with cycle start/end dates
        // NextRenewal is the actual renewal date, so pass NextRenewal.AddDays(-1) as the last day of data
        Calendar.BuildCalendar(Days.ToList(), _cycle.CycleStart, _cycle.NextRenewal.AddDays(-1));

        RefreshCurrentDay();
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

        // Update IsToday in calendar cells
        var todayCell = Calendar.GetCellForDate(today);
        if (todayCell != null)
        {
            todayCell.IsToday = true;
        }

        NotifyTodayQuotaTexts();
    }

    private void OnCursorModelsEdited(int dayNumber, decimal newValue)
    {
        if (_cycle == null) return;

        _calculator.SetManual(_cycle, QuotaKind.CursorModels, dayNumber, newValue);
        PersistCycle();
        RefreshCycle();
    }

    private void OnOtherModelsEdited(int dayNumber, decimal newValue)
    {
        if (_cycle == null) return;

        _calculator.SetManual(_cycle, QuotaKind.OtherModels, dayNumber, newValue);
        PersistCycle();
        RefreshCycle();
    }

    private void OnChangeRenewalDay()
    {
        ChangeRenewalDayRequested?.Invoke();
    }

    public event Action? ChangeRenewalDayRequested;
    public event Action? ResetCycleRequested;

    public void StartEditingDay(DayRowViewModel day)
    {
        _editingDay = day;
        EditingCursorQuota = (int)day.CursorModelsValue;
        EditingOtherQuota = (int)day.OtherModelsValue;
        IsEditingDay = true;
        OnPropertyChanged(nameof(EditingDayText));
    }

    public void StopEditing()
    {
        _editingDay = null;
        IsEditingDay = false;
        OnPropertyChanged(nameof(EditingDayText));
    }

    private void OnApplyEdit()
    {
        if (_editingDay == null || _cycle == null) return;

        var dayNumber = _editingDay.DayNumber;
        var newCursor = (int)Math.Round(EditingCursorQuota);
        var newOther = (int)Math.Round(EditingOtherQuota);
        var cursorChanged = newCursor != (int)_editingDay.CursorModelsValue;
        var otherChanged = newOther != (int)_editingDay.OtherModelsValue;

        if (cursorChanged)
            _calculator.SetManual(_cycle, QuotaKind.CursorModels, dayNumber, newCursor);

        if (otherChanged)
            _calculator.SetManual(_cycle, QuotaKind.OtherModels, dayNumber, newOther);

        if (cursorChanged || otherChanged)
            PersistCycle();

        StopEditing();
        RefreshCycle();
    }

    private void OnResetEditingDay()
    {
        if (_editingDay == null || _cycle == null) return;

        var dayNumber = _editingDay.DayNumber;
        _calculator.ClearManual(_cycle, QuotaKind.CursorModels, dayNumber);
        _calculator.ClearManual(_cycle, QuotaKind.OtherModels, dayNumber);

        PersistCycle();
        StopEditing();
        RefreshCycle();
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

    private void OnResetCycle()
    {
        ResetCycleRequested?.Invoke();
    }

    public void SetRenewalDay(int renewalDay)
    {
        _settings.RenewalDay = renewalDay;
        StartNewCycle();
        _isInitialized = true;
        OnPropertyChanged(nameof(IsInitialized));
        ((RelayCommand)ResetCycleCommand).RaiseCanExecuteChanged();
    }

    private void StartNewCycle()
    {
        if (!_settings.RenewalDay.HasValue) return;

        StopEditing();

        _cycle = _calculator.GenerateCycle(_settings.RenewalDay.Value, _clock.Today);
        _settings.ActiveCycle = _cycle;
        _store.Save(_settings);

        RefreshCycle();
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

        var dayNumber = _calculator.EstimateRunOutDayNumber(_cycle, kind);
        return dayNumber.HasValue
            ? _cycle.CycleStart.AddDays(dayNumber.Value - 1).ToString("dd-MMM", CultureInfo.CurrentCulture)
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
    private static string FormatPercent(decimal value) =>
        $"{(int)Math.Round(value, MidpointRounding.AwayFromZero)}%";

    public bool TryBuildCycleCsv(out string csv)
    {
        csv = string.Empty;
        if (_cycle == null || _cycle.Days.Count == 0)
            return false;

        var totalDays = _calculator.TotalDays(_cycle);
        var builder = new StringBuilder();
        builder.AppendLine(
            "day number,date,Cursor (linear),Other Models (linear),Cursor (recalculated),Other Models (recalculated),IsDataPoint");

        foreach (var day in _cycle.Days)
        {
            var linear = _calculator.LinearPercent(day.DayNumber, totalDays);
            var isDataPoint = day.CursorModelsIsManual || day.OtherModelsIsManual ? 1 : 0;

            builder.Append(day.DayNumber);
            builder.Append(',');
            builder.Append(day.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(FormatCsvRatio(linear));
            builder.Append(',');
            builder.Append(FormatCsvRatio(linear));
            builder.Append(',');
            builder.Append(FormatCsvRatio(day.CursorModelsPercent));
            builder.Append(',');
            builder.Append(FormatCsvRatio(day.OtherModelsPercent));
            builder.Append(',');
            builder.Append(isDataPoint);
            builder.AppendLine();
        }

        csv = builder.ToString();
        return true;
    }

    private static string FormatCsvRatio(decimal percent) =>
        (percent / 100m).ToString("0.0000", CultureInfo.InvariantCulture);
}
