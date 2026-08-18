using CursorUsageProgress.Models;

namespace CursorUsageProgress.Tests;

public class SyncScheduleTests
{
    [Fact]
    public void ShouldRefreshOnStart_WhenAutoSyncOff_ReturnsFalse()
    {
        var now = DateTimeOffset.Parse("2026-08-18T12:00:00Z");

        Assert.False(SyncSchedule.ShouldRefreshOnStart(false, true, now, null, 1));
    }

    [Fact]
    public void ShouldRefreshOnStart_WhenDisconnected_ReturnsFalse()
    {
        var now = DateTimeOffset.Parse("2026-08-18T12:00:00Z");

        Assert.False(SyncSchedule.ShouldRefreshOnStart(true, false, now, null, 1));
    }

    [Fact]
    public void ShouldRefreshOnStart_WhenConnectedAndNeverUpdated_ReturnsTrue()
    {
        var now = DateTimeOffset.Parse("2026-08-18T12:00:00Z");

        Assert.True(SyncSchedule.ShouldRefreshOnStart(true, true, now, null, 1));
    }

    [Theory]
    [InlineData(19, false)]
    [InlineData(20, true)]
    [InlineData(21, true)]
    public void ShouldRefreshOnStart_UsesTwentyMinuteWindow(int minutesAgo, bool expected)
    {
        var now = new DateTimeOffset(2026, 8, 18, 12, 30, 0, TimeSpan.Zero);
        var last = now.AddMinutes(-minutesAgo);

        Assert.Equal(expected, SyncSchedule.ShouldRefreshOnStart(true, true, now, last, 1));
    }

    [Fact]
    public void ShouldRefreshOnStart_WhenAlignedHourWasMissed_ReturnsTrue()
    {
        var now = new DateTimeOffset(2026, 8, 18, 20, 5, 0, TimeSpan.Zero);
        var last = new DateTimeOffset(2026, 8, 18, 19, 55, 0, TimeSpan.Zero);

        Assert.True(SyncSchedule.ShouldRefreshOnStart(true, true, now, last, 1));
    }

    [Fact]
    public void ShouldRefreshOnStart_WhenStillInSameHour_SkipsRecentUpdate()
    {
        var now = new DateTimeOffset(2026, 8, 18, 20, 5, 0, TimeSpan.Zero);
        var last = new DateTimeOffset(2026, 8, 18, 20, 2, 0, TimeSpan.Zero);

        Assert.False(SyncSchedule.ShouldRefreshOnStart(true, true, now, last, 1));
    }

    [Fact]
    public void ShouldRefreshOnStart_WhenTwoHourSlotWasMissed_ReturnsTrue()
    {
        var now = new DateTimeOffset(2026, 8, 18, 20, 5, 0, TimeSpan.Zero);
        var last = new DateTimeOffset(2026, 8, 18, 19, 55, 0, TimeSpan.Zero);

        Assert.True(SyncSchedule.ShouldRefreshOnStart(true, true, now, last, 2));
    }

    [Fact]
    public void ShouldRefreshOnStart_WhenLastUpdateOlderThanInterval_ReturnsTrue()
    {
        var now = new DateTimeOffset(2026, 8, 18, 20, 5, 0, TimeSpan.Zero);
        var last = new DateTimeOffset(2026, 8, 18, 18, 50, 0, TimeSpan.Zero);

        Assert.True(SyncSchedule.ShouldRefreshOnStart(true, true, now, last, 1));
    }

    [Theory]
    [InlineData(1, 0, 0, 1, 0)]
    [InlineData(1, 0, 30, 1, 0)]
    [InlineData(1, 1, 0, 2, 0)]
    [InlineData(1, 2, 0, 3, 0)]
    [InlineData(1, 23, 15, 0, 1)]
    [InlineData(2, 0, 0, 2, 0)]
    [InlineData(2, 1, 15, 2, 0)]
    [InlineData(2, 2, 0, 4, 0)]
    [InlineData(2, 3, 59, 4, 0)]
    [InlineData(2, 22, 0, 0, 1)]
    [InlineData(4, 0, 0, 4, 0)]
    [InlineData(4, 3, 0, 4, 0)]
    [InlineData(4, 4, 0, 8, 0)]
    [InlineData(4, 8, 0, 12, 0)]
    [InlineData(4, 23, 0, 0, 1)]
    [InlineData(6, 0, 0, 6, 0)]
    [InlineData(6, 5, 59, 6, 0)]
    [InlineData(6, 6, 0, 12, 0)]
    [InlineData(6, 18, 0, 0, 1)]
    [InlineData(12, 0, 0, 12, 0)]
    [InlineData(12, 11, 59, 12, 0)]
    [InlineData(12, 12, 0, 0, 1)]
    public void NextAlignedLocal_UsesClockHoursFromMidnight(
        int intervalHours,
        int hour,
        int minute,
        int expectedHour,
        int addDays)
    {
        var now = new DateTime(2026, 8, 18, hour, minute, 0);
        var expected = new DateTime(2026, 8, 18, expectedHour, 0, 0).AddDays(addDays);

        Assert.Equal(expected, SyncSchedule.NextAlignedLocal(now, intervalHours));
    }

    [Fact]
    public void NextAlignedLocal_ClampsUnknownIntervalToOneHour()
    {
        var now = new DateTime(2026, 8, 18, 10, 15, 0);

        Assert.Equal(new DateTime(2026, 8, 18, 11, 0, 0), SyncSchedule.NextAlignedLocal(now, 3));
    }
}
