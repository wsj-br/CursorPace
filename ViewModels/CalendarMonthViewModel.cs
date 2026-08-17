using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace CursorQuotaProgress.ViewModels;

/// <summary>
/// Organizes cycle days into a calendar grid structure with weeks and cells.
/// </summary>
public sealed class CalendarMonthViewModel : ViewModelBase
{
    private readonly Dictionary<DateTime, CalendarCellViewModel> _cellLookup;

    public CalendarMonthViewModel()
    {
        Weeks = new ObservableCollection<CalendarWeekViewModel>();
        _cellLookup = new Dictionary<DateTime, CalendarCellViewModel>();
    }

    /// <summary>
    /// Week rows for the calendar grid (typically 5-6 rows).
    /// </summary>
    public ObservableCollection<CalendarWeekViewModel> Weeks { get; }

    /// <summary>
    /// Builds the calendar grid from a flat list of day view models.
    /// </summary>
    /// <param name="days">Days from the current quota cycle.</param>
    /// <param name="cycleStart">The cycle start date.</param>
    /// <param name="cycleEnd">The cycle end date (next renewal - 1 day).</param>
    public void BuildCalendar(List<DayRowViewModel> days, DateTime cycleStart, DateTime cycleEnd)
    {
        Weeks.Clear();
        _cellLookup.Clear();

        if (days == null || days.Count == 0)
            return;

        // Find the first day of the calendar grid (start of week containing cycle start)
        var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
        var calendarStart = cycleStart;
        while (calendarStart.DayOfWeek != firstDayOfWeek)
        {
            calendarStart = calendarStart.AddDays(-1);
        }

        // Find the last day of the calendar grid (end of week containing cycle end)
        var calendarEndDate = cycleEnd;
        var lastDayOfWeek = (DayOfWeek)(((int)firstDayOfWeek + 6) % 7);
        while (calendarEndDate.DayOfWeek != lastDayOfWeek)
        {
            calendarEndDate = calendarEndDate.AddDays(1);
        }

        // Create lookup for quick access to day data by date
        var dayLookup = days.ToDictionary(
            d => DateTime.ParseExact(d.DateText, "d", CultureInfo.CurrentCulture).Date);

        // Build calendar grid
        var currentDate = calendarStart;
        var currentWeek = new List<CalendarCellViewModel>();

        while (currentDate <= calendarEndDate)
        {
            // Create cell for this date
            var hasData = dayLookup.TryGetValue(currentDate.Date, out var dayData);
            // Mark both cycle start and the NextRenewal date (not NextRenewal - 1)
            var isRenewalDay = currentDate.Date == cycleStart.Date || currentDate.Date == cycleEnd.AddDays(1).Date;

            var cell = new CalendarCellViewModel(
                currentDate,
                hasData ? dayData : null,
                isRenewalDay);

            _cellLookup[currentDate.Date] = cell;
            currentWeek.Add(cell);

            // If week is complete (7 days), add to collection
            if (currentWeek.Count == 7)
            {
                Weeks.Add(new CalendarWeekViewModel(currentWeek));
                currentWeek = new List<CalendarCellViewModel>();
            }

            currentDate = currentDate.AddDays(1);
        }
    }

    /// <summary>
    /// Gets the cell for a specific date (for updating IsToday, etc.).
    /// </summary>
    public CalendarCellViewModel? GetCellForDate(DateTime date)
    {
        _cellLookup.TryGetValue(date.Date, out var cell);
        return cell;
    }
}


