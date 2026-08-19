using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace CursorUsageProgress.ViewModels;

/// <summary>
/// Organizes cycle days into a calendar grid structure with weeks and cells.
/// </summary>
public sealed class CalendarMonthViewModel : ViewModelBase
{
    private readonly Dictionary<DateTime, CalendarCellViewModel> _cellLookup;
    private string _monthHeading = string.Empty;

    public CalendarMonthViewModel()
    {
        Weeks = new ObservableCollection<CalendarWeekViewModel>();
        _cellLookup = new Dictionary<DateTime, CalendarCellViewModel>();
    }

    /// <summary>
    /// Week rows for the calendar grid (typically 5-6 rows).
    /// </summary>
    public ObservableCollection<CalendarWeekViewModel> Weeks { get; }

    public string MonthHeading
    {
        get => _monthHeading;
        private set => SetProperty(ref _monthHeading, value);
    }

    /// <summary>
    /// Builds the calendar grid from a flat list of day view models.
    /// </summary>
    /// <param name="days">Days from the current quota cycle.</param>
    /// <param name="cycleStart">The cycle start instant.</param>
    /// <param name="nextRenewal">The next renewal instant.</param>
    public void BuildCalendar(List<DayRowViewModel> days, DateTime cycleStart, DateTime nextRenewal)
    {
        Weeks.Clear();
        _cellLookup.Clear();
        MonthHeading = string.Empty;

        if (days == null || days.Count == 0)
            return;

        MonthHeading = FormatMonthHeading(cycleStart.Date);

        // Find the first day of the calendar grid (start of week containing cycle start)
        var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        var calendarStart = cycleStart.Date;
        while (calendarStart.DayOfWeek != firstDayOfWeek)
        {
            calendarStart = calendarStart.AddDays(-1);
        }

        // Pad through the end of the week that contains the renewal date
        var calendarEndDate = nextRenewal.Date;
        var lastDayOfWeek = (DayOfWeek)(((int)firstDayOfWeek + 6) % 7);
        while (calendarEndDate.DayOfWeek != lastDayOfWeek)
        {
            calendarEndDate = calendarEndDate.AddDays(1);
        }

        var dayLookup = days.ToDictionary(d => d.Date.Date);

        var currentDate = calendarStart;
        var currentWeek = new List<CalendarCellViewModel>();

        while (currentDate <= calendarEndDate)
        {
            var hasData = dayLookup.TryGetValue(currentDate.Date, out var dayData);
            var isRenewalDay = currentDate.Date == cycleStart.Date || currentDate.Date == nextRenewal.Date;

            var cell = new CalendarCellViewModel(
                currentDate,
                hasData ? dayData : null,
                isRenewalDay,
                hasData && dayData!.IsRunOutDay);

            _cellLookup[currentDate.Date] = cell;
            currentWeek.Add(cell);

            if (currentWeek.Count == 7)
            {
                Weeks.Add(new CalendarWeekViewModel(currentWeek));
                currentWeek = new List<CalendarCellViewModel>();
            }

            currentDate = currentDate.AddDays(1);
        }
    }

    public static string FormatMonthHeading(DateTime startDate) =>
        startDate.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

    /// <summary>
    /// Gets the cell for a specific date (for updating IsToday, etc.).
    /// </summary>
    public CalendarCellViewModel? GetCellForDate(DateTime date)
    {
        _cellLookup.TryGetValue(date.Date, out var cell);
        return cell;
    }
}
