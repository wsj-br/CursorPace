using CursorPace.Models;
using CursorPace.Services;

namespace CursorPace.Tests;

public class CycleHistoryTests
{
    [Fact]
    public void ArchiveIfNewStartDate_WhenNewDate_CopiesPreviousToHistory()
    {
        var previous = Bounds(new DateTime(2026, 7, 2, 8, 0, 0), new DateTime(2026, 8, 2, 8, 0, 0));
        var settings = new AppSettings { ActiveCycle = previous };

        Assert.True(CycleHistory.ArchiveIfNewStartDate(settings, new DateTime(2026, 8, 2, 8, 0, 0)));

        var archived = Assert.Single(settings.CycleHistory);
        Assert.Equal(previous.CycleStart, archived.CycleStart);
        Assert.Equal(previous.NextRenewal, archived.NextRenewal);
        Assert.Equal(2, archived.RenewalDay);
        Assert.Empty(archived.Days);
    }

    [Fact]
    public void ArchiveIfNewStartDate_WhenSameDate_DoesNotArchive()
    {
        var previous = Bounds(new DateTime(2026, 8, 2, 8, 0, 0), new DateTime(2026, 9, 2, 8, 0, 0));
        var settings = new AppSettings { ActiveCycle = previous };

        Assert.False(CycleHistory.ArchiveIfNewStartDate(settings, new DateTime(2026, 8, 2, 22, 0, 0)));
        Assert.Empty(settings.CycleHistory);
    }

    [Fact]
    public void ArchiveIfNewStartDate_WhenDuplicateStartDate_ReplacesExisting()
    {
        var first = Bounds(new DateTime(2026, 7, 2, 8, 0, 0), new DateTime(2026, 8, 1, 8, 0, 0));
        var previous = Bounds(new DateTime(2026, 7, 2, 9, 0, 0), new DateTime(2026, 8, 2, 9, 0, 0));
        var settings = new AppSettings
        {
            ActiveCycle = previous,
            CycleHistory = [first]
        };

        Assert.True(CycleHistory.ArchiveIfNewStartDate(settings, new DateTime(2026, 8, 2, 9, 0, 0)));

        var archived = Assert.Single(settings.CycleHistory);
        Assert.Equal(previous.CycleStart, archived.CycleStart);
        Assert.Equal(previous.NextRenewal, archived.NextRenewal);
    }

    private static QuotaCycle Bounds(DateTime start, DateTime end) =>
        new()
        {
            RenewalDay = start.Day,
            CycleStart = start,
            NextRenewal = end
        };
}
