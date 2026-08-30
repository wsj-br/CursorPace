using System;

namespace CursorPace.ViewModels;

public sealed class CalendarCellViewModel : ViewModelBase
{
    private readonly DateTime? _date;
    private readonly DayRowViewModel? _dayData;
    private readonly bool _isRenewalDay;
    private readonly bool _isRunOutDay;
    private bool _isToday;

    public CalendarCellViewModel(DateTime? date, DayRowViewModel? dayData, bool isRenewalDay = false, bool isRunOutDay = false)
    {
        _date = date;
        _dayData = dayData;
        _isRenewalDay = isRenewalDay;
        _isRunOutDay = isRunOutDay;

        if (_dayData != null)
            _dayData.PropertyChanged += OnDayDataPropertyChanged;
    }

    private void OnDayDataPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DayRowViewModel.IsActual))
        {
            OnPropertyChanged(nameof(ShowAsActual));
            OnPropertyChanged(nameof(HasAccentDayNumber));
        }
    }

    public DateTime? Date => _date;
    public DayRowViewModel? DayData => _dayData;
    public bool HasData => _dayData != null;
    public string DayNumberText => _date?.Day.ToString() ?? string.Empty;

    public bool IsToday
    {
        get => _isToday;
        set => SetProperty(ref _isToday, value);
    }

    public bool IsRenewalDay => _isRenewalDay;
    public bool IsRunOutDay => _isRunOutDay;
    public bool IsWeekend => _date?.DayOfWeek == DayOfWeek.Saturday || _date?.DayOfWeek == DayOfWeek.Sunday;
    public double CellOpacity => HasData ? 1.0 : 0.3;

    public string ExpectedCursorText => _dayData != null ? $"{_dayData.ShownExpectedCursor}%" : string.Empty;
    public string ExpectedOtherText => _dayData != null ? $"{_dayData.ShownExpectedOther}%" : string.Empty;
    public string ProjectedCursorText =>
        _dayData?.ShownProjectedCursor is int cursorPercent ? $"{cursorPercent}%" : string.Empty;
    public string ProjectedOtherText =>
        _dayData?.ShownProjectedOther is int otherPercent ? $"{otherPercent}%" : string.Empty;

    public bool HasCursorProjection => _dayData?.HasCursorProjection == true;
    public bool HasOtherProjection => _dayData?.HasOtherProjection == true;
    public bool CursorProjectedAbove100 =>
        _dayData?.ProjectedQuotaCursor is double cursorValue && cursorValue > 100;
    public bool OtherProjectedAbove100 =>
        _dayData?.ProjectedQuotaOther is double otherValue && otherValue > 100;
    public bool CursorWillRunOut => _dayData?.CursorWillRunOut == true;
    public bool OtherWillRunOut => _dayData?.OtherWillRunOut == true;
    public bool ShowAsActual => _dayData?.IsActual ?? false;
    public bool HasAccentDayNumber => ShowAsActual;
}
