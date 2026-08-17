using System;

namespace CursorQuotaProgress.ViewModels;

/// <summary>
/// Represents one cell in the calendar grid.
/// May contain quota data (DayData != null) or be empty for grid alignment.
/// </summary>
public sealed class CalendarCellViewModel : ViewModelBase
{
    private readonly DateTime? _date;
    private readonly DayRowViewModel? _dayData;
    private readonly bool _isRenewalDay;
    private bool _isToday;

    public CalendarCellViewModel(DateTime? date, DayRowViewModel? dayData, bool isRenewalDay = false)
    {
        _date = date;
        _dayData = dayData;
        _isRenewalDay = isRenewalDay;

        // Subscribe to property changes from the underlying day data
        if (_dayData != null)
        {
            _dayData.PropertyChanged += OnDayDataPropertyChanged;
        }
    }

    private void OnDayDataPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Re-raise property changed notifications for properties that depend on DayRowViewModel
        if (e.PropertyName == nameof(DayRowViewModel.IsModified))
        {
            OnPropertyChanged(nameof(IsModified));
        }
        else if (e.PropertyName == nameof(DayRowViewModel.CursorModelsValue) ||
                 e.PropertyName == nameof(DayRowViewModel.LinearQuotaCursor))
        {
            OnPropertyChanged(nameof(IsCursorOverLinear));
            OnPropertyChanged(nameof(CurrentCursorText));
            OnPropertyChanged(nameof(LinearCursorText));
            OnPropertyChanged(nameof(ShowRecalculatedQuotas));
        }
        else if (e.PropertyName == nameof(DayRowViewModel.OtherModelsValue) ||
                 e.PropertyName == nameof(DayRowViewModel.LinearQuotaOther))
        {
            OnPropertyChanged(nameof(IsOtherOverLinear));
            OnPropertyChanged(nameof(CurrentOtherText));
            OnPropertyChanged(nameof(LinearOtherText));
            OnPropertyChanged(nameof(ShowRecalculatedQuotas));
        }
    }

    /// <summary>
    /// The date this cell represents (null for empty padding cells).
    /// </summary>
    public DateTime? Date => _date;

    /// <summary>
    /// Reference to the underlying day data (null if no quota data exists for this date).
    /// </summary>
    public DayRowViewModel? DayData => _dayData;

    /// <summary>
    /// Whether this cell contains actual quota data.
    /// </summary>
    public bool HasData => _dayData != null;

    /// <summary>
    /// Day number to display (e.g., "1", "15", "31").
    /// </summary>
    public string DayNumberText => _date?.Day.ToString() ?? string.Empty;

    /// <summary>
    /// Whether this cell represents today's date.
    /// </summary>
    public bool IsToday
    {
        get => _isToday;
        set => SetProperty(ref _isToday, value);
    }

    /// <summary>
    /// Whether this cell is a renewal day (cycle start or end).
    /// </summary>
    public bool IsRenewalDay => _isRenewalDay;

    /// <summary>
    /// Whether this cell is a weekend (Saturday or Sunday).
    /// </summary>
    public bool IsWeekend => _date?.DayOfWeek == DayOfWeek.Saturday
                              || _date?.DayOfWeek == DayOfWeek.Sunday;

    /// <summary>
    /// Opacity for empty/out-of-cycle cells (0.3) vs data cells (1.0).
    /// </summary>
    public double CellOpacity => HasData ? 1.0 : 0.3;

    /// <summary>
    /// Linear (expected) quota for Cursor Models at this day.
    /// </summary>
    public string LinearCursorText => _dayData != null
        ? $"{(int)_dayData.LinearQuotaCursor}%"
        : string.Empty;

    /// <summary>
    /// Linear (expected) quota for Other Models at this day.
    /// </summary>
    public string LinearOtherText => _dayData != null
        ? $"{(int)_dayData.LinearQuotaOther}%"
        : string.Empty;

    /// <summary>
    /// Current (actual) quota for Cursor Models at this day.
    /// </summary>
    public string CurrentCursorText => _dayData != null
        ? $"{(int)_dayData.CursorModelsValue}%"
        : string.Empty;

    /// <summary>
    /// Current (actual) quota for Other Models at this day.
    /// </summary>
    public string CurrentOtherText => _dayData != null
        ? $"{(int)_dayData.OtherModelsValue}%"
        : string.Empty;

    /// <summary>
    /// Show both right-hand values when either quota has diverged from linear.
    /// </summary>
    public bool ShowRecalculatedQuotas => _dayData != null
        && ((int)_dayData.CursorModelsValue != (int)_dayData.LinearQuotaCursor
            || (int)_dayData.OtherModelsValue != (int)_dayData.LinearQuotaOther);

    /// <summary>
    /// Whether current Cursor quota is over linear (needs red/warning color).
    /// </summary>
    public bool IsCursorOverLinear => _dayData != null && _dayData.CursorModelsValue > _dayData.LinearQuotaCursor;

    /// <summary>
    /// Whether current Other quota is over linear (needs red/warning color).
    /// </summary>
    public bool IsOtherOverLinear => _dayData != null && _dayData.OtherModelsValue > _dayData.LinearQuotaOther;

    /// <summary>
    /// Whether user has modified this day's quota (for blue accent on day number).
    /// </summary>
    public bool IsModified => _dayData?.IsModified ?? false;
}

