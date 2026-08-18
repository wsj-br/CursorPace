using CursorUsageProgress.Models;
using CursorUsageProgress.Services;

namespace CursorUsageProgress.Tests;

public class UsageSampleAppenderTests
{
    [Fact]
    public void ApplySnapshot_FirstSample_Appends()
    {
        var document = new UsageSampleDocument();
        var snapshot = Snapshot(DateTimeOffset.Parse("2026-08-02T21:19:47Z"), 10m, 12m);

        Assert.True(UsageSampleAppender.ApplySnapshot(document, snapshot, TimeSpan.FromSeconds(30), out var rolled));
        Assert.False(rolled);
        Assert.Single(document.Samples);
        Assert.Equal(10m, document.Samples[0].CursorModelsPercent);
        Assert.Equal(snapshot.BillingCycleStartUtc, document.CycleStartUtc);
    }

    [Fact]
    public void ApplySnapshot_WithinMinGap_SkipsDuplicate()
    {
        var start = DateTimeOffset.Parse("2026-08-02T21:19:47Z");
        var document = new UsageSampleDocument();
        UsageSampleAppender.ApplySnapshot(document, Snapshot(start, 10m, 12m, start.AddHours(1)), TimeSpan.FromSeconds(30), out _);

        var skipped = UsageSampleAppender.ApplySnapshot(
            document,
            Snapshot(start, 11m, 13m, start.AddHours(1).AddSeconds(10)),
            TimeSpan.FromSeconds(30),
            out var rolled);

        Assert.False(skipped);
        Assert.False(rolled);
        Assert.Single(document.Samples);
        Assert.Equal(10m, document.Samples[0].CursorModelsPercent);
    }

    [Fact]
    public void ApplySnapshot_NewCycleStart_ClearsPreviousSamples()
    {
        var oldStart = DateTimeOffset.Parse("2026-07-02T21:19:47Z");
        var newStart = DateTimeOffset.Parse("2026-08-02T21:19:47Z");
        var document = new UsageSampleDocument();
        UsageSampleAppender.ApplySnapshot(document, Snapshot(oldStart, 80m, 80m, oldStart.AddDays(20)), TimeSpan.FromSeconds(30), out _);

        var changed = UsageSampleAppender.ApplySnapshot(
            document,
            Snapshot(newStart, 1m, 2m, newStart.AddMinutes(5)),
            TimeSpan.FromSeconds(30),
            out var rolled);

        Assert.True(changed);
        Assert.True(rolled);
        Assert.Single(document.Samples);
        Assert.Equal(1m, document.Samples[0].CursorModelsPercent);
        Assert.Equal(newStart, document.CycleStartUtc);
    }

    private static UsageSnapshot Snapshot(
        DateTimeOffset cycleStart,
        decimal cursor,
        decimal other,
        DateTimeOffset? fetchedAt = null) =>
        new()
        {
            BillingCycleStartUtc = cycleStart,
            BillingCycleEndUtc = cycleStart.AddMonths(1),
            CursorModelsPercent = cursor,
            OtherModelsPercent = other,
            FetchedAtUtc = fetchedAt ?? cycleStart.AddHours(1)
        };
}
