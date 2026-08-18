using CursorUsageProgress.Models;
using CursorUsageProgress.Services;

namespace CursorUsageProgress.Tests;

public class SampleEstimationTests
{
    private readonly CycleCalculator _calculator = new();

    [Fact]
    public void GenerateCycleFromBounds_UsesExactInstantsAndLocalDay()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var end = new DateTime(2026, 9, 2, 22, 19, 47);

        var cycle = _calculator.GenerateCycleFromBounds(start, end);

        Assert.Equal(2, cycle.RenewalDay);
        Assert.Equal(start, cycle.CycleStart);
        Assert.Equal(end, cycle.NextRenewal);
        Assert.Equal(31, cycle.Days.Count);
        Assert.Equal(new DateTime(2026, 8, 2), cycle.Days[0].Date);
        Assert.Equal(new DateTime(2026, 9, 1), cycle.Days[^1].Date);
        Assert.Equal(0m, cycle.Days[0].CursorModelsPercent);
    }

    [Fact]
    public void RebuildDays_UsesLastSampleOfEachLocalDate()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddMonths(1));
        var day1 = new DateTime(2026, 8, 2);
        var day2 = new DateTime(2026, 8, 3);
        var samples = new List<UsageSample>
        {
            SampleAt(new DateTime(2026, 8, 2, 10, 0, 0), 4m, 5m),
            SampleAt(new DateTime(2026, 8, 2, 23, 50, 0), 6m, 7m),
            SampleAt(new DateTime(2026, 8, 3, 10, 0, 0), 10m, 12m)
        };

        _calculator.RebuildDays(cycle, samples, day2);

        var first = cycle.Days.Single(d => d.Date == day1);
        var second = cycle.Days.Single(d => d.Date == day2);
        var third = cycle.Days.Single(d => d.Date == day2.AddDays(1));

        Assert.Equal(6m, first.CursorModelsPercent);
        Assert.Equal(7m, first.OtherModelsPercent);
        Assert.True(first.CursorModelsIsActual);
        Assert.Equal(10m, second.CursorModelsPercent);
        Assert.True(second.CursorModelsIsActual);
        Assert.False(third.CursorModelsIsActual);
        Assert.Equal(_calculator.ExpectedPercent(cycle, QuotaKind.CursorModels, third.DayNumber), third.CursorModelsPercent);
    }

    [Fact]
    public void EstimateDailyUsage_WithSamples_UsesFractionalDayOffsets()
    {
        var start = new DateTime(2026, 8, 2, 22, 0, 0);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddMonths(1));
        var samples = new List<UsageSample>
        {
            SampleAt(start, 0m, 0m),
            SampleAt(start.AddHours(12), 6m, 8m)
        };

        var rate = _calculator.EstimateDailyUsage(cycle, QuotaKind.CursorModels, samples);

        Assert.Equal(12m, rate);
        Assert.Equal(0m, _calculator.ProjectedPercent(cycle, QuotaKind.CursorModels, 1, samples));
        Assert.Equal(12m, _calculator.ProjectedPercent(cycle, QuotaKind.CursorModels, 2, samples));
    }

    [Fact]
    public void EstimateDailyUsage_FewerThanTwoSamples_FallsBackToEdits()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        _calculator.SetManual(cycle, QuotaKind.CursorModels, 10, 45m);
        var samples = new List<UsageSample>
        {
            SampleAt(cycle.CycleStart.Date.AddHours(12), 20m, 20m)
        };

        Assert.Equal(45m / 9m, _calculator.EstimateDailyUsage(cycle, QuotaKind.CursorModels, samples));
        Assert.Equal(21, _calculator.EstimateRunOutDayNumber(cycle, QuotaKind.CursorModels, samples));
    }

    [Fact]
    public void EstimateDailyUsage_SameDayCluster_UsesOriginAndLastSample()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddMonths(1));
        var lastLocal = new DateTime(2026, 8, 18, 13, 1, 24);
        var other = 52.5636m;
        var samples = new List<UsageSample>
        {
            SampleAt(new DateTime(2026, 8, 18, 12, 40, 13), 33.89625m, other),
            SampleAt(new DateTime(2026, 8, 18, 12, 52, 6), 33.955m, other),
            SampleAt(new DateTime(2026, 8, 18, 12, 54, 20), 33.955m, other),
            SampleAt(lastLocal, 34.0425m, other)
        };

        var elapsedDays = (decimal)(lastLocal - start).TotalDays;
        var cursorRate = _calculator.EstimateDailyUsage(cycle, QuotaKind.CursorModels, samples);
        var otherRate = _calculator.EstimateDailyUsage(cycle, QuotaKind.OtherModels, samples);
        Assert.NotNull(cursorRate);
        Assert.NotNull(otherRate);
        Assert.Equal(34.0425m / elapsedDays, cursorRate.Value, precision: 10);
        Assert.Equal(other / elapsedDays, otherRate.Value, precision: 10);
        var cursorDay1 = _calculator.ProjectedPercent(cycle, QuotaKind.CursorModels, 1, samples);
        var otherDay1 = _calculator.ProjectedPercent(cycle, QuotaKind.OtherModels, 1, samples);
        Assert.NotNull(cursorDay1);
        Assert.NotNull(otherDay1);
        Assert.True(cursorDay1.Value >= 0m);
        Assert.True(otherDay1.Value >= 0m);
        Assert.Equal(0m, cursorDay1.Value, precision: 10);
        Assert.Equal(0m, otherDay1.Value, precision: 10);
    }

    [Fact]
    public void EstimateDailyUsage_TwoSamplesOnSameDate_CollapsesToLastAndOrigin()
    {
        var start = new DateTime(2026, 8, 2, 22, 0, 0);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddMonths(1));
        var noon = new DateTime(2026, 8, 18, 12, 0, 0);
        var evening = noon.AddHours(6);
        var samples = new List<UsageSample>
        {
            SampleAt(noon, 30m, 40m),
            SampleAt(evening, 36m, 40m)
        };

        var elapsedDays = (decimal)(evening - start).TotalDays;
        var rate = _calculator.EstimateDailyUsage(cycle, QuotaKind.CursorModels, samples);
        Assert.NotNull(rate);
        Assert.Equal(36m / elapsedDays, rate.Value, precision: 10);
    }

    [Fact]
    public void EstimateDailyUsage_RealSampleOnCycleStartDate_DoesNotPrependOrigin()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddMonths(1));
        var day1 = start.AddMinutes(40);
        var later = start.AddDays(10);
        var samples = new List<UsageSample>
        {
            SampleAt(day1, 5m, 5m),
            SampleAt(later, 25m, 25m)
        };

        var span = (decimal)(later - day1).TotalDays;
        var rate = _calculator.EstimateDailyUsage(cycle, QuotaKind.CursorModels, samples);
        Assert.NotNull(rate);
        Assert.Equal(20m / span, rate.Value, precision: 10);
    }

    [Fact]
    public void EstimateRunOutDayNumber_WithSamples_ProjectsFromLastTimestamp()
    {
        var start = new DateTime(2026, 8, 2, 0, 0, 0);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddDays(31));
        var samples = new List<UsageSample>
        {
            SampleAt(start, 0m, 0m),
            SampleAt(start.AddDays(10), 50m, 50m)
        };

        Assert.Equal(5m, _calculator.EstimateDailyUsage(cycle, QuotaKind.CursorModels, samples));
        Assert.Equal(21, _calculator.EstimateRunOutDayNumber(cycle, QuotaKind.CursorModels, samples));
    }

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
