using CursorPace.Models;
using CursorPace.Services;

namespace CursorPace.Tests;

public class RemoteSyncMergeTests
{
    [Fact]
    public void UnionSamples_MergesBothSidesSortedWithoutDuplicates()
    {
        var local = new List<UsageSample>
        {
            SampleAt(new DateTime(2026, 9, 2, 10, 0, 0), 20, 5),
            SampleAt(new DateTime(2026, 9, 1, 10, 0, 0), 10, 2)
        };
        var remote = new List<UsageSample>
        {
            SampleAt(new DateTime(2026, 9, 1, 10, 0, 0), 10, 2),
            SampleAt(new DateTime(2026, 9, 3, 10, 0, 0), 30, 8)
        };

        var merged = RemoteSyncMerge.UnionSamples(local, remote);

        Assert.Equal(3, merged.Count);
        Assert.True(merged[0].TimestampUtc < merged[1].TimestampUtc);
        Assert.True(merged[1].TimestampUtc < merged[2].TimestampUtc);
    }

    [Fact]
    public void UnionSamples_DeduplicatesByInstantAcrossOffsets()
    {
        var instant = new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero);
        var local = new List<UsageSample> { Sample(instant, 10, 2) };
        var remote = new List<UsageSample> { Sample(instant.ToOffset(TimeSpan.FromHours(2)), 10, 2) };

        var merged = RemoteSyncMerge.UnionSamples(local, remote);

        Assert.Single(merged);
        Assert.Same(local[0], merged[0]);
    }

    [Fact]
    public void LatestCycleStartUtc_ReturnsMax()
    {
        var older = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var newer = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

        Assert.Equal(newer, RemoteSyncMerge.LatestCycleStartUtc(older, newer));
        Assert.Equal(newer, RemoteSyncMerge.LatestCycleStartUtc(newer, older));
        Assert.Equal(newer, RemoteSyncMerge.LatestCycleStartUtc(null, newer));
        Assert.Equal(older, RemoteSyncMerge.LatestCycleStartUtc(older, null));
        Assert.Null(RemoteSyncMerge.LatestCycleStartUtc(null, null));
    }

    [Fact]
    public void NewestCycle_LaterStartWins()
    {
        var local = Cycle(new DateTime(2026, 8, 1), new DateTime(2026, 9, 1));
        var remote = Cycle(new DateTime(2026, 9, 1), new DateTime(2026, 10, 1));

        Assert.Same(remote, RemoteSyncMerge.NewestCycle(local, remote));
        Assert.Same(remote, RemoteSyncMerge.NewestCycle(remote, local));
    }

    [Fact]
    public void NewestCycle_SameStartLaterRenewalWinsThenLocal()
    {
        var local = Cycle(new DateTime(2026, 9, 1), new DateTime(2026, 10, 1, 1, 0, 0));
        var remote = Cycle(new DateTime(2026, 9, 1), new DateTime(2026, 10, 1, 2, 0, 0));

        Assert.Same(remote, RemoteSyncMerge.NewestCycle(local, remote));
        Assert.Same(local, RemoteSyncMerge.NewestCycle(local, local));
    }

    [Fact]
    public void NewestCycle_NullSideReturnsOther()
    {
        var cycle = Cycle(new DateTime(2026, 9, 1), new DateTime(2026, 10, 1));

        Assert.Same(cycle, RemoteSyncMerge.NewestCycle(null, cycle));
        Assert.Same(cycle, RemoteSyncMerge.NewestCycle(cycle, null));
        Assert.Null(RemoteSyncMerge.NewestCycle(null, null));
    }

    [Fact]
    public void UnionHistory_DeduplicatesByStartDateKeepingLaterRenewal()
    {
        var local = new List<QuotaCycle> { Cycle(new DateTime(2026, 7, 1), new DateTime(2026, 8, 1)) };
        var remote = new List<QuotaCycle>
        {
            Cycle(new DateTime(2026, 7, 1), new DateTime(2026, 8, 1, 1, 0, 0)),
            Cycle(new DateTime(2026, 8, 1), new DateTime(2026, 9, 1))
        };

        var merged = RemoteSyncMerge.UnionHistory(local, remote);

        Assert.Equal(2, merged.Count);
        Assert.Equal(new DateTime(2026, 8, 1, 1, 0, 0), merged[0].NextRenewal);
    }

    [Fact]
    public void MergeCycles_SupersededLocalActiveMovesToHistory()
    {
        var localActive = Cycle(new DateTime(2026, 8, 1), new DateTime(2026, 9, 1));
        var remoteActive = Cycle(new DateTime(2026, 9, 1), new DateTime(2026, 10, 1));

        var (active, history) = RemoteSyncMerge.MergeCycles(
            localActive, [], remoteActive, []);

        Assert.Same(remoteActive, active);
        Assert.Single(history);
        Assert.Same(localActive, history[0]);
    }

    [Fact]
    public void MergeCycles_ActiveNeverDuplicatedInHistory()
    {
        var localActive = Cycle(new DateTime(2026, 9, 1), new DateTime(2026, 10, 1));
        var remoteHistory = new List<QuotaCycle>
        {
            Cycle(new DateTime(2026, 9, 1), new DateTime(2026, 10, 1, 5, 0, 0))
        };

        var (active, history) = RemoteSyncMerge.MergeCycles(
            localActive, [], localActive, remoteHistory);

        Assert.Same(localActive, active);
        Assert.Empty(history);
    }

    private static UsageSample Sample(DateTimeOffset ts, decimal cursor, decimal other) =>
        new() { TimestampUtc = ts, CursorModelsPercent = cursor, OtherModelsPercent = other };

    private static UsageSample SampleAt(DateTime local, decimal cursor, decimal other)
    {
        var offset = TimeZoneInfo.Local.GetUtcOffset(local);
        return Sample(
            new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), offset),
            cursor,
            other);
    }

    private static QuotaCycle Cycle(DateTime start, DateTime end) =>
        new() { RenewalDay = start.Day, CycleStart = start, NextRenewal = end };
}
