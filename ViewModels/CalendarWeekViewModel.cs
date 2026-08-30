using System.Collections.Generic;

namespace CursorPace.ViewModels;

/// <summary>
/// Represents one week row in the calendar grid (7 cells).
/// </summary>
public sealed class CalendarWeekViewModel
{
    public CalendarWeekViewModel(List<CalendarCellViewModel> cells)
    {
        Cells = cells;
    }

    /// <summary>
    /// Always 7 cells (Sunday through Saturday or Monday through Sunday based on culture).
    /// </summary>
    public List<CalendarCellViewModel> Cells { get; }
}
