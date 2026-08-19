using CursorUsageProgress.Models;
using CursorUsageProgress.Services;

namespace CursorUsageProgress.Tests;

public class UsageChartSeriesBuilderTests
{
    private readonly CycleCalculator _calculator = new();
    private readonly UsageChartSeriesBuilder _builder = new();

    [Fact]
    public void ExpectedPolyline_StartsAtOrigin_AndEndsAtRenewal100()
    {
        var cycle = MidnightCycle();
        var document = _builder.Build(cycle, _calculator, samples: null);

        Assert.Equal(0m, document.CursorExpected[0].X);
        Assert.Equal(0m, document.CursorExpected[0].Y);
        Assert.Equal(document.CycleSeconds, document.CursorExpected[^1].X);
        Assert.Equal(100m, document.CursorExpected[^1].Y);
        Assert.Equal(CycleCalculator.CycleSeconds(cycle), document.CycleSeconds);
        Assert.Equal(0m, UsageChartSeriesBuilder.ToAxisX(cycle, cycle.CycleStart));
    }

    [Fact]
    public void ExpectedPolyline_IncludesEveryInCycleSample()
    {
        var cycle = MidnightCycle();
        var samples = new List<UsageSample>
        {
            SampleAt(cycle.CycleStart.AddHours(12), 25m, 30m),
            SampleAt(cycle.CycleStart.AddDays(2), 40m, 45m)
        };

        var document = _builder.Build(cycle, _calculator, samples);

        Assert.Equal(4, document.CursorExpected.Count);
        Assert.Equal(25m, document.CursorExpected[1].Y);
        Assert.Equal(UsageChartSeriesBuilder.ToAxisX(cycle, cycle.CycleStart.AddHours(12)), document.CursorExpected[1].X);
        Assert.Equal(40m, document.CursorExpected[2].Y);
        Assert.Equal(100m, document.CursorExpected[^1].Y);
    }

    [Fact]
    public void EstimatedPolyline_OmittedWithoutEnoughPoints()
    {
        var document = _builder.Build(MidnightCycle(), _calculator, samples: null);

        Assert.Empty(document.CursorEstimated);
        Assert.Empty(document.OtherEstimated);
        Assert.False(document.HasCursorEstimated);
        Assert.False(document.HasOtherEstimated);
    }

    [Fact]
    public void EstimatedPolyline_IsTwoPointsFromLastSampleToRenewal()
    {
        var cycle = MidnightCycle();
        var last = cycle.CycleStart.AddDays(9);
        var samples = new List<UsageSample>
        {
            SampleAt(cycle.CycleStart, 0m, 0m),
            SampleAt(last, 50m, 50m)
        };

        var document = _builder.Build(cycle, _calculator, samples);

        Assert.Equal(2, document.CursorEstimated.Count);
        Assert.Equal(UsageChartSeriesBuilder.ToAxisX(cycle, last), document.CursorEstimated[0].X);
        Assert.Equal(50m, document.CursorEstimated[0].Y);
        Assert.Equal(document.CycleSeconds, document.CursorEstimated[1].X);
        Assert.Equal(
            _calculator.ProjectedPercentAt(cycle, QuotaKind.CursorModels, cycle.NextRenewal, samples),
            document.CursorEstimated[1].Y);
    }

    [Fact]
    public void TimedCycle_DomainIsCycleStartToNextRenewal()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var end = new DateTime(2026, 9, 2, 22, 19, 47);
        var cycle = _calculator.GenerateCycleFromBounds(start, end);
        var sampleLocal = start.AddDays(10);
        var samples = new List<UsageSample>
        {
            SampleAt(start, 0m, 0m),
            SampleAt(sampleLocal, 40m, 40m)
        };

        var document = _builder.Build(cycle, _calculator, samples);

        Assert.Equal(0m, document.CursorExpected[0].X);
        Assert.Equal(document.CycleSeconds, document.CursorExpected[^1].X);
        Assert.Equal(100m, document.CursorExpected[^1].Y);
        Assert.Equal(UsageChartSeriesBuilder.ToAxisX(cycle, sampleLocal), document.CursorEstimated[0].X);
        Assert.Equal(document.CycleSeconds, document.CursorEstimated[^1].X);
    }

    [Fact]
    public void Slots_MidnightCycle_EveryDayIsLabelledAndFillsTheDomain()
    {
        var cycle = MidnightCycle();
        var document = _builder.Build(cycle, _calculator, samples: null);

        Assert.Equal(31, document.Slots.Count);
        Assert.Equal(new DateTime(2026, 1, 1), document.Slots[0].Date);
        Assert.Equal(0m, document.Slots[0].StartX);
        Assert.Equal(new DateTime(2026, 1, 31), document.Slots[^1].Date);
        Assert.Equal(document.CycleSeconds, document.Slots[^1].EndX);
        Assert.All(document.Slots, slot => Assert.False(slot.IsLeadingPartial));
    }

    [Fact]
    public void Slots_TimedCycle_LeadingPartialIsUnlabelledAndTrailingDayKeepsItsDate()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var end = new DateTime(2026, 9, 2, 22, 19, 47);
        var cycle = _calculator.GenerateCycleFromBounds(start, end);
        var document = _builder.Build(cycle, _calculator, samples: null);

        Assert.Equal(32, document.Slots.Count);

        var leading = document.Slots[0];
        Assert.True(leading.IsLeadingPartial);
        Assert.Equal(new DateTime(2026, 8, 2), leading.Date);
        Assert.Equal(0m, leading.StartX);
        Assert.Equal(UsageChartSeriesBuilder.ToAxisX(cycle, new DateTime(2026, 8, 3)), leading.EndX);

        Assert.False(document.Slots[1].IsLeadingPartial);
        Assert.Equal(new DateTime(2026, 8, 3), document.Slots[1].Date);

        var trailing = document.Slots[^1];
        Assert.False(trailing.IsLeadingPartial);
        Assert.Equal(new DateTime(2026, 9, 2), trailing.Date);
        Assert.Equal(UsageChartSeriesBuilder.ToAxisX(cycle, new DateTime(2026, 9, 2)), trailing.StartX);
        Assert.Equal(document.CycleSeconds, trailing.EndX);

        // Calendar rows include the leading partial date; chart labels skip that slot.
        var labelled = document.Slots.Where(s => !s.IsLeadingPartial).ToList();
        Assert.Equal(32, _calculator.TotalDays(cycle));
        Assert.Equal(31, labelled.Count);
        Assert.Equal([3, 4, 5], labelled.Take(3).Select(s => s.Date.Day));
        Assert.Equal([31, 1, 2], labelled.TakeLast(3).Select(s => s.Date.Day));
    }

    [Fact]
    public void OriginMarker_IsAtZero()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddMonths(1));
        var document = _builder.Build(cycle, _calculator, samples: null);
        var origin = Assert.Single(document.Markers, m => m.MarkerKind == ChartMarkerKind.Origin);

        Assert.Equal(0m, origin.X);
        Assert.Equal(0m, origin.Y);
        Assert.Equal(start, origin.Instant);
        Assert.Null(origin.QuotaKind);
    }

    [Fact]
    public void Samples_OutsideCycle_AreOmitted()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var end = start.AddMonths(1);
        var cycle = _calculator.GenerateCycleFromBounds(start, end);
        var samples = new List<UsageSample>
        {
            SampleAt(start.AddHours(-1), 1m, 1m),
            SampleAt(end, 90m, 90m),
            SampleAt(start.AddHours(1), 10m, 12m)
        };

        var document = _builder.Build(cycle, _calculator, samples);
        var sampleMarkers = document.Markers.Where(m => m.MarkerKind == ChartMarkerKind.Sample).ToList();

        Assert.Equal(2, sampleMarkers.Count);
        Assert.Contains(sampleMarkers, m => m.QuotaKind == QuotaKind.CursorModels && m.Y == 10m);
        Assert.Contains(sampleMarkers, m => m.QuotaKind == QuotaKind.OtherModels && m.Y == 12m);
    }

    [Fact]
    public void Samples_SameLocalDate_KeepDistinctFractionalX()
    {
        var cycle = MidnightCycle();
        var morning = cycle.CycleStart.AddHours(3);
        var evening = cycle.CycleStart.AddHours(23);
        var samples = new List<UsageSample>
        {
            SampleAt(morning, 5m, 6m),
            SampleAt(evening, 8m, 9m)
        };

        var document = _builder.Build(cycle, _calculator, samples);
        var cursorSamples = document.Markers
            .Where(m => m.MarkerKind == ChartMarkerKind.Sample && m.QuotaKind == QuotaKind.CursorModels)
            .OrderBy(m => m.X)
            .ToList();

        Assert.Equal(2, cursorSamples.Count);
        Assert.Equal(UsageChartSeriesBuilder.ToAxisX(cycle, morning), cursorSamples[0].X);
        Assert.Equal(UsageChartSeriesBuilder.ToAxisX(cycle, evening), cursorSamples[1].X);
        Assert.NotEqual(cursorSamples[0].X, cursorSamples[1].X);
    }

    [Fact]
    public void UsageLimit_IsGuideNotASeries()
    {
        var document = _builder.Build(MidnightCycle(), _calculator, samples: null);

        Assert.Equal(100m, document.UsageLimitPercent);
        Assert.DoesNotContain(document.CursorExpected, p => p.X == 0 && p.Y == 100m);
    }

    [Fact]
    public void YMax_ExtendsPast120WhenEstimatedExceeds()
    {
        var cycle = MidnightCycle();
        var samples = new List<UsageSample>
        {
            SampleAt(cycle.CycleStart, 0m, 0m),
            SampleAt(cycle.CycleStart.AddDays(1), 80m, 80m)
        };

        var document = _builder.Build(cycle, _calculator, samples);
        var lastProjected = document.CursorEstimated[^1].Y;

        Assert.True(lastProjected > 120m);
        Assert.True(document.YMax >= lastProjected);
        Assert.Equal(0m, document.YMax % 20m);
    }

    private QuotaCycle MidnightCycle() =>
        _calculator.GenerateCycleFromBounds(new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));

    private static UsageSample SampleAt(DateTime local, decimal cursor, decimal other)
    {
        var offset = TimeZoneInfo.Local.GetUtcOffset(local);
        return new UsageSample
        {
            TimestampUtc = new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), offset),
            CursorModelsPercent = cursor,
            OtherModelsPercent = other
        };
    }
}
