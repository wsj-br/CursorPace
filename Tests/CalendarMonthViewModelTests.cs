using System.Globalization;
using CursorUsageProgress.ViewModels;

namespace CursorUsageProgress.Tests;

public class CalendarMonthViewModelTests
{
    [Fact]
    public void FormatMonthHeading_UsesCycleStartMonthAndYear()
    {
        var start = new DateTime(2026, 8, 2);
        var text = CalendarMonthViewModel.FormatMonthHeading(start);
        Assert.Equal(start.ToString("MMMM yyyy", CultureInfo.CurrentCulture), text);
    }
}
