using CursorUsageProgress.Models;
using CursorUsageProgress.Services;

namespace CursorUsageProgress.Tests;

public class CycleCalculatorTests
{
    private readonly CycleCalculator _calculator = new();

    [Fact]
    public void GenerateCycleFromBounds_UsesExactInstantsAndLocalDays()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var end = new DateTime(2026, 9, 2, 22, 19, 47);

        var cycle = _calculator.GenerateCycleFromBounds(start, end);

        Assert.Equal(2, cycle.RenewalDay);
        Assert.Equal(start, cycle.CycleStart);
        Assert.Equal(end, cycle.NextRenewal);
        Assert.Equal(32, cycle.Days.Count);
        Assert.Equal(new DateTime(2026, 8, 2), cycle.Days[0].Date);
        Assert.Equal(new DateTime(2026, 9, 2), cycle.Days[^1].Date);
        Assert.Equal(0m, cycle.Days[0].CursorModelsPercent);
    }

    [Fact]
    public void GenerateCycleFromBounds_MidnightEnd_OmitsRenewalDate()
    {
        var cycle = _calculator.GenerateCycleFromBounds(new DateTime(2026, 1, 1), new DateTime(2026, 2, 1));

        Assert.Equal(31, cycle.Days.Count);
        Assert.Equal(new DateTime(2026, 1, 31), cycle.Days[^1].Date);
    }

    [Fact]
    public void ExpectedPercent_TimedCycle_LastCalendarDateIsMidnightNotRenewal()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var end = new DateTime(2026, 9, 2, 22, 19, 47);
        var cycle = _calculator.GenerateCycleFromBounds(start, end);

        var last = _calculator.ExpectedPercent(cycle, QuotaKind.CursorModels, 32);
        Assert.Equal(
            _calculator.ExpectedPercentAt(cycle, QuotaKind.CursorModels, new DateTime(2026, 9, 2), samples: null),
            last);
        Assert.True(last > 0m);
        Assert.True(last < 100m);
        Assert.Equal(100m, _calculator.ExpectedPercentAt(cycle, QuotaKind.CursorModels, end, samples: null));
    }

    [Fact]
    public void GenerateCycleFromBounds_RejectsEndOnOrBeforeStart()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _calculator.GenerateCycleFromBounds(start, start));
    }

    [Fact]
    public void AxisSeconds_IsZeroAtCycleStart()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddMonths(1));

        Assert.Equal(0m, CycleCalculator.AxisSeconds(cycle, start));
        Assert.Equal(CycleCalculator.CycleSeconds(cycle), CycleCalculator.AxisSeconds(cycle, cycle.NextRenewal));
        Assert.Equal(
            (decimal)(cycle.NextRenewal - start).Ticks / TimeSpan.TicksPerSecond,
            CycleCalculator.CycleSeconds(cycle));
    }

    [Fact]
    public void ExpectedPercent_WithNoSamples_IsLinearInElapsedTime()
    {
        var start = new DateTime(2026, 1, 1);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddMonths(1));
        var totalSeconds = CycleCalculator.CycleSeconds(cycle);

        Assert.Equal(0m, _calculator.ExpectedPercent(cycle, QuotaKind.CursorModels, 1));
        Assert.Equal(100m * 86400m / totalSeconds, _calculator.ExpectedPercent(cycle, QuotaKind.CursorModels, 2));
        Assert.Equal(100m, _calculator.ExpectedPercentAt(cycle, QuotaKind.CursorModels, cycle.NextRenewal, samples: null));
        Assert.Equal(50m, _calculator.ExpectedPercentAt(cycle, QuotaKind.CursorModels, start.AddDays(15.5), samples: null));
    }

    [Fact]
    public void ExpectedPercentAt_TimedCycle_DayOneMidnightClampsToCycleStart()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddMonths(1));

        Assert.Equal(0m, _calculator.ExpectedPercent(cycle, QuotaKind.CursorModels, 1));
        Assert.Equal(0m, _calculator.ExpectedPercentAt(cycle, QuotaKind.CursorModels, start.Date, samples: null));
    }

    [Fact]
    public void ExpectedPercentAt_WithSample_InterpolatesTowardSampleThenToRenewal()
    {
        var start = new DateTime(2026, 1, 1);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddDays(31));
        var sampleAt = start.AddDays(10);
        var samples = new List<UsageSample> { SampleAt(sampleAt, 40m, 40m) };

        Assert.Equal(20m, _calculator.ExpectedPercentAt(cycle, QuotaKind.CursorModels, start.AddDays(5), samples));
        Assert.Equal(40m, _calculator.ExpectedPercentAt(cycle, QuotaKind.CursorModels, sampleAt, samples));
        var after = start.AddDays(20.5);
        var spanAfter = (decimal)(cycle.NextRenewal - sampleAt).Ticks / TimeSpan.TicksPerSecond;
        var elapsedAfter = (decimal)(after - sampleAt).Ticks / TimeSpan.TicksPerSecond;
        Assert.Equal(40m + elapsedAfter * 60m / spanAfter, _calculator.ExpectedPercentAt(cycle, QuotaKind.CursorModels, after, samples));
    }

    [Fact]
    public void ExpectedPercent_DaysBeforeFirstSample_AimAtThatSample()
    {
        var start = new DateTime(2026, 1, 1);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddDays(31));
        var samples = new List<UsageSample>
        {
            SampleAt(start.AddDays(16).AddHours(12), 75.425m, 72.56m)
        };

        var atDay16 = _calculator.ExpectedPercent(cycle, QuotaKind.CursorModels, 16, samples);
        Assert.True(atDay16 > 0m);
        Assert.True(atDay16 < 75.425m);
        Assert.NotEqual(100m * 15 / 31, atDay16);
        Assert.Equal(75.425m, _calculator.ExpectedPercentAt(cycle, QuotaKind.CursorModels, start.AddDays(16).AddHours(12), samples));
    }

    [Fact]
    public void ExpectedPercent_TwoSamples_FollowsObservedPathThenPacesToRenewal()
    {
        var start = new DateTime(2026, 1, 1);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddDays(31));
        var first = start.AddDays(9);
        var second = start.AddDays(21);
        var samples = new List<UsageSample>
        {
            SampleAt(first, 20m, 20m),
            SampleAt(second, 50m, 50m)
        };

        var mid = start.AddDays(15);
        Assert.Equal(20m, _calculator.ExpectedPercentAt(cycle, QuotaKind.CursorModels, first, samples));
        Assert.Equal(35m, _calculator.ExpectedPercentAt(cycle, QuotaKind.CursorModels, mid, samples));
        Assert.Equal(50m, _calculator.ExpectedPercentAt(cycle, QuotaKind.CursorModels, second, samples));

        var after = start.AddDays(26);
        var spanAfter = (decimal)(cycle.NextRenewal - second).Ticks / TimeSpan.TicksPerSecond;
        var elapsedAfter = (decimal)(after - second).Ticks / TimeSpan.TicksPerSecond;
        Assert.Equal(
            50m + elapsedAfter * 50m / spanAfter,
            _calculator.ExpectedPercentAt(cycle, QuotaKind.CursorModels, after, samples));
    }

    [Fact]
    public void EstimateDailyUsage_WithNoSamples_ReturnsNull()
    {
        var cycle = MidnightCycle();

        Assert.Null(_calculator.EstimateDailyUsage(cycle, QuotaKind.CursorModels));
        Assert.Null(_calculator.EstimateRunOutDayNumber(cycle, QuotaKind.CursorModels));
        Assert.Null(_calculator.ProjectedPercent(cycle, QuotaKind.CursorModels, 10));
        Assert.Null(_calculator.EstimateRunOutInstant(cycle, QuotaKind.CursorModels, samples: null));
    }

    [Fact]
    public void EstimateDailyUsage_TwoSamples_UsesElapsedSeconds()
    {
        var start = new DateTime(2026, 8, 2, 22, 0, 0);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddMonths(1));
        var samples = new List<UsageSample>
        {
            SampleAt(start, 0m, 0m),
            SampleAt(start.AddHours(12), 6m, 8m)
        };

        Assert.Equal(12m, _calculator.EstimateDailyUsage(cycle, QuotaKind.CursorModels, samples)!.Value, precision: 10);
        Assert.Equal(6m, _calculator.ProjectedPercentAt(cycle, QuotaKind.CursorModels, start.AddHours(12), samples)!.Value, precision: 10);
        Assert.Equal(12m, _calculator.ProjectedPercentAt(cycle, QuotaKind.CursorModels, start.AddDays(1), samples)!.Value, precision: 10);
    }

    [Fact]
    public void EstimateDailyUsage_IndependentQuotas()
    {
        var start = new DateTime(2026, 1, 1);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddDays(31));
        var samples = new List<UsageSample>
        {
            SampleAt(start, 0m, 0m),
            SampleAt(start.AddDays(9), 45m, 0m)
        };

        Assert.Equal(5m, _calculator.EstimateDailyUsage(cycle, QuotaKind.CursorModels, samples)!.Value, precision: 10);
        Assert.Equal(0m, _calculator.EstimateDailyUsage(cycle, QuotaKind.OtherModels, samples)!.Value, precision: 10);
        Assert.Equal(21, _calculator.EstimateRunOutDayNumber(cycle, QuotaKind.CursorModels, samples));
        Assert.Null(_calculator.EstimateRunOutDayNumber(cycle, QuotaKind.OtherModels, samples));
    }

    [Fact]
    public void EstimateRunOutInstant_ProjectsFromLastTimestamp()
    {
        var start = new DateTime(2026, 8, 2);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddDays(31));
        var samples = new List<UsageSample>
        {
            SampleAt(start, 0m, 0m),
            SampleAt(start.AddDays(10), 50m, 50m)
        };

        var runOut = _calculator.EstimateRunOutInstant(cycle, QuotaKind.CursorModels, samples);
        Assert.Equal(start.AddDays(20), runOut);
        Assert.Equal(21, _calculator.EstimateRunOutDayNumber(cycle, QuotaKind.CursorModels, samples));
    }

    [Fact]
    public void EstimateRunOutInstant_ZeroOrNegativeRate_ReturnsNull()
    {
        var start = new DateTime(2026, 1, 1);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddDays(31));
        var flat = new List<UsageSample>
        {
            SampleAt(start, 40m, 40m),
            SampleAt(start.AddDays(9), 40m, 40m)
        };
        var falling = new List<UsageSample>
        {
            SampleAt(start, 50m, 50m),
            SampleAt(start.AddDays(9), 20m, 20m)
        };

        Assert.Equal(0m, _calculator.EstimateDailyUsage(cycle, QuotaKind.CursorModels, flat));
        Assert.Null(_calculator.EstimateRunOutInstant(cycle, QuotaKind.CursorModels, flat));
        Assert.True(_calculator.EstimateDailyUsage(cycle, QuotaKind.CursorModels, falling) < 0m);
        Assert.Null(_calculator.EstimateRunOutInstant(cycle, QuotaKind.CursorModels, falling));
    }

    [Fact]
    public void EstimateRunOutInstant_LastPointAt100_ReturnsThatInstant()
    {
        var start = new DateTime(2026, 1, 1);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddDays(31));
        var at = start.AddDays(9);
        var samples = new List<UsageSample>
        {
            SampleAt(start, 0m, 0m),
            SampleAt(at, 100m, 100m)
        };

        Assert.Equal(at, _calculator.EstimateRunOutInstant(cycle, QuotaKind.CursorModels, samples));
        Assert.Equal(10, _calculator.EstimateRunOutDayNumber(cycle, QuotaKind.CursorModels, samples));
    }

    [Fact]
    public void EstimateRunOutInstant_Hits100AtRenewal_ReturnsNull()
    {
        var start = new DateTime(2026, 1, 1);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddDays(31));
        var at = start.AddDays(15);
        var linear = 100m * 15m / 31m;
        var samples = new List<UsageSample>
        {
            SampleAt(start, 0m, 0m),
            SampleAt(at, linear, linear)
        };

        Assert.Null(_calculator.EstimateRunOutInstant(cycle, QuotaKind.CursorModels, samples));
        Assert.True(_calculator.ProjectedPercentAt(cycle, QuotaKind.CursorModels, cycle.NextRenewal.AddTicks(-1), samples) < 100m);
    }

    [Fact]
    public void EstimateRunOutDayNumber_TimedCycle_CanLandOnRenewalDate()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var end = new DateTime(2026, 9, 2, 22, 19, 47);
        var cycle = _calculator.GenerateCycleFromBounds(start, end);
        var samples = new List<UsageSample>
        {
            SampleAt(start, 0m, 0m),
            SampleAt(start.AddDays(30), 97m, 97m)
        };

        var instant = _calculator.EstimateRunOutInstant(cycle, QuotaKind.CursorModels, samples);
        Assert.NotNull(instant);
        Assert.Equal(new DateTime(2026, 9, 2), instant.Value.Date);
        Assert.True(instant.Value < end);
        Assert.Equal(32, _calculator.EstimateRunOutDayNumber(cycle, QuotaKind.CursorModels, samples));
    }

    [Fact]
    public void ProjectedPercent_AfterRunOut_ContinuesPast100()
    {
        var start = new DateTime(2026, 1, 1);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddDays(31));
        var samples = new List<UsageSample>
        {
            SampleAt(start, 0m, 0m),
            SampleAt(start.AddDays(9), 50m, 50m)
        };

        Assert.Equal(19, _calculator.EstimateRunOutDayNumber(cycle, QuotaKind.CursorModels, samples));
        Assert.Equal(100m, _calculator.ProjectedPercentAt(cycle, QuotaKind.CursorModels, start.AddDays(18), samples)!.Value, precision: 10);
        Assert.True(_calculator.ProjectedPercentAt(cycle, QuotaKind.CursorModels, start.AddDays(19), samples) > 100m);
        Assert.True(_calculator.ProjectedPercentAt(cycle, QuotaKind.CursorModels, cycle.NextRenewal, samples) > 100m);
    }

    [Fact]
    public void TryGetLastUpdate_UsesLatestInCycleSample()
    {
        var start = new DateTime(2026, 1, 1);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddDays(31));
        var later = start.AddDays(9).AddHours(20);
        var samples = new List<UsageSample>
        {
            SampleAt(start.AddDays(2), 10m, 11m),
            SampleAt(later, 40m, 41m)
        };

        Assert.True(_calculator.TryGetLastUpdate(cycle, QuotaKind.CursorModels, samples, out var instant, out var percent));
        Assert.Equal(later, instant);
        Assert.Equal(40m, percent);
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
