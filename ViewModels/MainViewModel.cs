using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Input;
using CursorQuotaProgress.Models;
using CursorQuotaProgress.Services;
using Microsoft.UI.Xaml;

namespace CursorQuotaProgress.ViewModels;

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
        ? _cycle.CycleStart.ToString("d", CultureInfo.CurrentCulture)
        : string.Empty;

    public string NextRenewalText => _cycle != null
        ? _cycle.NextRenewal.ToString("d", CultureInfo.CurrentCulture)
        : string.Empty;

    public string CursorModelsTodayText => FormatTodayPercent(QuotaKind.CursorModels);

    public string OtherModelsTodayText => FormatTodayPercent(QuotaKind.OtherModels);

    public string TrayToolTipText
    {
        get
        {
            if (!TryGetTodayPercents(out var cursor, out var other))
                return "Cursor Quota Progress";

            return $"Cursor Quota Progress\nCursor: {FormatPercent(cursor)}\nOther Models: {FormatPercent(other)}";
        }
    }

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

        foreach (var day in _cycle.Days)
        {
            var linearQuota = (double)_calculator.LinearPercent(day.DayNumber, totalDays);

            var vm = new DayRowViewModel(day, linearQuota, linearQuota);
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
        OnPropertyChanged(nameof(CursorModelsTodayText));
        OnPropertyChanged(nameof(OtherModelsTodayText));
        OnPropertyChanged(nameof(TrayToolTipText));
    }

    private string FormatTodayPercent(QuotaKind kind)
    {
        if (!TryGetTodayPercents(out var cursor, out var other))
            return "—";

        return FormatPercent(kind == QuotaKind.CursorModels ? cursor : other);
    }

    private bool TryGetTodayPercents(out decimal cursor, out decimal other)
    {
        cursor = 0;
        other = 0;

        if (_cycle == null || _currentDayNumber <= 0 || _currentDayNumber > _cycle.Days.Count)
            return false;

        var day = _cycle.Days[_currentDayNumber - 1];
        cursor = day.CursorModelsPercent;
        other = day.OtherModelsPercent;
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
