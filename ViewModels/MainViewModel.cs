using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using CursorQuotaProgress.Models;
using CursorQuotaProgress.Services;

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

        Days = new ObservableCollection<DayRowViewModel>();

        ChangeRenewalDayCommand = new RelayCommand(OnChangeRenewalDay, () => _isInitialized);
        QuitCommand = new RelayCommand(OnQuit);

        if (_settings.RenewalDay.HasValue && _cycle != null)
        {
            _isInitialized = true;
            RefreshCycle();
        }
    }

    public bool IsInitialized => _isInitialized;

    public ObservableCollection<DayRowViewModel> Days { get; }

    public string CycleStartText => _cycle != null
        ? _cycle.CycleStart.ToString("d", CultureInfo.CurrentCulture)
        : string.Empty;

    public string NextRenewalText => _cycle != null
        ? _cycle.NextRenewal.ToString("d", CultureInfo.CurrentCulture)
        : string.Empty;

    public string CursorModelsTodayText => _cycle != null && _currentDayNumber > 0 && _currentDayNumber <= _cycle.Days.Count
        ? $"{_cycle.Days[_currentDayNumber - 1].CursorModelsPercent:F2}%"
        : "—";

    public string OtherModelsTodayText => _cycle != null && _currentDayNumber > 0 && _currentDayNumber <= _cycle.Days.Count
        ? $"{_cycle.Days[_currentDayNumber - 1].OtherModelsPercent:F2}%"
        : "—";

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
    public ICommand QuitCommand { get; }

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
        foreach (var day in _cycle.Days)
        {
            var vm = new DayRowViewModel(day);
            vm.CursorModelsEdited += OnCursorModelsEdited;
            vm.OtherModelsEdited += OnOtherModelsEdited;
            Days.Add(vm);
        }

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

        OnPropertyChanged(nameof(CursorModelsTodayText));
        OnPropertyChanged(nameof(OtherModelsTodayText));
    }

    private void OnCursorModelsEdited(int dayNumber, decimal newValue)
    {
        if (_cycle == null) return;

        _cycle.Days[dayNumber - 1].CursorModelsPercent = newValue;
        _cycle.Days[dayNumber - 1].CursorModelsIsManual = true;

        _calculator.RecalculateQuota(_cycle, QuotaKind.CursorModels, dayNumber);

        for (int i = dayNumber; i < _cycle.Days.Count; i++)
        {
            Days[i].UpdateFromModel(_cycle.Days[i]);
        }

        _settings.ActiveCycle = _cycle;
        _store.Save(_settings);

        OnPropertyChanged(nameof(CursorModelsTodayText));
    }

    private void OnOtherModelsEdited(int dayNumber, decimal newValue)
    {
        if (_cycle == null) return;

        _cycle.Days[dayNumber - 1].OtherModelsPercent = newValue;
        _cycle.Days[dayNumber - 1].OtherModelsIsManual = true;

        _calculator.RecalculateQuota(_cycle, QuotaKind.OtherModels, dayNumber);

        for (int i = dayNumber; i < _cycle.Days.Count; i++)
        {
            Days[i].UpdateFromModel(_cycle.Days[i]);
        }

        _settings.ActiveCycle = _cycle;
        _store.Save(_settings);

        OnPropertyChanged(nameof(OtherModelsTodayText));
    }

    private void OnChangeRenewalDay()
    {
        ChangeRenewalDayRequested?.Invoke();
    }

    public event Action? ChangeRenewalDayRequested;

    private void OnQuit()
    {
        QuitRequested?.Invoke();
    }

    public void SetRenewalDay(int renewalDay)
    {
        _settings.RenewalDay = renewalDay;
        StartNewCycle();
        _isInitialized = true;
    }

    private void StartNewCycle()
    {
        if (!_settings.RenewalDay.HasValue) return;

        _cycle = _calculator.GenerateCycle(_settings.RenewalDay.Value, _clock.Today);
        _settings.ActiveCycle = _cycle;
        _store.Save(_settings);

        RefreshCycle();
    }
}
