using CursorUsageProgress.Models;
using CursorUsageProgress.ViewModels;

namespace CursorUsageProgress.Tests;

public class DayRowViewModelTests
{
    [Fact]
    public void ShownPercents_MatchCalendarTruncationAndRounding()
    {
        var row = new DayRowViewModel(
            new QuotaDayEntry { DayNumber = 2, Date = new DateTime(2026, 8, 3) },
            expectedQuotaCursor: 32.9,
            expectedQuotaOther: 51.1,
            projectedQuotaCursor: 99.5m,
            projectedQuotaOther: null,
            cursorWillRunOut: false,
            otherWillRunOut: false);

        Assert.Equal(32, row.ShownExpectedCursor);
        Assert.Equal(51, row.ShownExpectedOther);
        Assert.Equal(100, row.ShownProjectedCursor);
        Assert.Null(row.ShownProjectedOther);
    }
}
