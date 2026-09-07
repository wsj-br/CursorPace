using CursorPace.Models;
using CursorPace.Services;

namespace CursorPace.Tests;

public class UsageChartSeriesBuilderTests
{
    private readonly CycleCalculator _calculator = new();
    private readonly UsageChartSeriesBuilder _builder = new();

    [Fact]
    public void ExpectedPolyline_StartsAtOrigin_AndEndsAtRenewal100()
    {
        var cycle = MidnightCycle();
        var document = _builder.Build(cycle, _calculator, samples: null);

        Assert.Equal(2, document.ExpectedUsage.Count);
        Assert.Equal(0m, document.ExpectedUsage[0].X);
        Assert.Equal(0m, document.ExpectedUsage[0].Y);
        Assert.Equal(document.CycleSeconds, document.ExpectedUsage[^1].X);
        Assert.Equal(100m, document.ExpectedUsage[^1].Y);
        Assert.Equal(CycleCalculator.CycleSeconds(cycle), document.CycleSeconds);
        Assert.Equal(0m, UsageChartSeriesBuilder.ToAxisX(cycle, cycle.CycleStart));
    }

    [Fact]
    public void ExpectedPolyline_IgnoresSamples_StaysTwoPoints()
    {
        var cycle = MidnightCycle();
        var samples = new List<UsageSample>
        {
            SampleAt(cycle.CycleStart.AddHours(12), 25m, 30m),
            SampleAt(cycle.CycleStart.AddDays(2), 40m, 45m)
        };

        var document = _builder.Build(cycle, _calculator, samples);

        Assert.Equal(2, document.ExpectedUsage.Count);
        Assert.Equal(0m, document.ExpectedUsage[0].Y);
        Assert.Equal(100m, document.ExpectedUsage[^1].Y);
        Assert.Equal(document.CycleSeconds, document.ExpectedUsage[^1].X);
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

        Assert.Equal(0m, document.ExpectedUsage[0].X);
        Assert.Equal(document.CycleSeconds, document.ExpectedUsage[^1].X);
        Assert.Equal(100m, document.ExpectedUsage[^1].Y);
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
    public void UsagePolyline_OmittedWithFewerThanTwoDays()
    {
        var cycle = MidnightCycle();
        var samples = new List<UsageSample>
        {
            SampleAt(cycle.CycleStart.AddHours(1), 10m, 12m)
        };

        var document = _builder.Build(cycle, _calculator, samples);

        Assert.Empty(document.CursorUsage);
        Assert.Empty(document.OtherUsage);
        Assert.False(document.HasCursorUsage);
        Assert.False(document.HasOtherUsage);
    }

    [Fact]
    public void UsagePolyline_OmitsOutsideCycle_AndKeepsInCycleDays()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var end = start.AddMonths(1);
        var cycle = _calculator.GenerateCycleFromBounds(start, end);
        var day1 = start.AddHours(1);
        var day2 = start.AddDays(1).AddHours(1);
        var samples = new List<UsageSample>
        {
            SampleAt(start.AddHours(-1), 1m, 1m),
            SampleAt(end, 90m, 90m),
            SampleAt(day1, 10m, 12m),
            SampleAt(day2, 20m, 22m)
        };

        var document = _builder.Build(cycle, _calculator, samples);

        Assert.True(document.HasCursorUsage);
        Assert.Equal(2, document.CursorUsage.Count);
        Assert.Equal(10m, document.CursorUsage[0].Y);
        Assert.Equal(12m, document.OtherUsage[0].Y);
        Assert.Equal(20m, document.CursorUsage[1].Y);
        Assert.Equal(22m, document.OtherUsage[1].Y);
        Assert.Equal(UsageChartSeriesBuilder.ToAxisX(cycle, day1), document.CursorUsage[0].X);
        Assert.Equal(UsageChartSeriesBuilder.ToAxisX(cycle, day2), document.CursorUsage[1].X);
    }

    [Fact]
    public void UsagePolyline_SameLocalDate_CollapsesToLastSample()
    {
        var cycle = MidnightCycle();
        var morning = cycle.CycleStart.AddHours(3);
        var evening = cycle.CycleStart.AddHours(23);
        var nextDay = cycle.CycleStart.AddDays(1).AddHours(12);
        var samples = new List<UsageSample>
        {
            SampleAt(morning, 5m, 6m),
            SampleAt(evening, 8m, 9m),
            SampleAt(nextDay, 15m, 16m)
        };

        var document = _builder.Build(cycle, _calculator, samples);

        Assert.Equal(2, document.CursorUsage.Count);
        Assert.Equal(UsageChartSeriesBuilder.ToAxisX(cycle, evening), document.CursorUsage[0].X);
        Assert.Equal(8m, document.CursorUsage[0].Y);
        Assert.Equal(9m, document.OtherUsage[0].Y);
        Assert.Equal(UsageChartSeriesBuilder.ToAxisX(cycle, nextDay), document.CursorUsage[1].X);
        Assert.Equal(15m, document.CursorUsage[1].Y);
    }

    [Fact]
    public void UsageLimit_IsGuideNotASeries()
    {
        var document = _builder.Build(MidnightCycle(), _calculator, samples: null);

        Assert.Equal(100m, document.UsageLimitPercent);
        Assert.DoesNotContain(document.ExpectedUsage, p => p.X == 0 && p.Y == 100m);
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
