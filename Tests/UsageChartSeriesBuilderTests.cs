using CursorUsageProgress.Models;
using CursorUsageProgress.Services;

namespace CursorUsageProgress.Tests;

public class UsageChartSeriesBuilderTests
{
    private readonly CycleCalculator _calculator = new();
    private readonly UsageChartSeriesBuilder _builder = new();

    [Fact]
    public void ExpectedPolyline_MatchesExpectedPercent_AndEndsAtRenewal100()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        _calculator.SetManual(cycle, QuotaKind.CursorModels, 10, 40m);

        var document = _builder.Build(cycle, _calculator, samples: null);
        var totalDays = _calculator.TotalDays(cycle);

        Assert.Equal(totalDays + 1, document.CursorExpected.Count);
        for (var day = 1; day <= totalDays; day++)
        {
            Assert.Equal(day, document.CursorExpected[day - 1].X);
            Assert.Equal(
                _calculator.ExpectedPercent(cycle, QuotaKind.CursorModels, day),
                document.CursorExpected[day - 1].Y);
        }

        var last = document.CursorExpected[^1];
        Assert.Equal(UsageChartSeriesBuilder.ToAxisX(cycle, cycle.NextRenewal), last.X);
        Assert.Equal(100m, last.Y);
        Assert.Equal(last.X, document.RenewalX);
        Assert.Equal(totalDays + 1, document.PlotEndX);
        Assert.Equal(totalDays + 1, document.SlotEndX);
        Assert.Equal(totalDays, document.DayTicks.Count);
        Assert.Equal(totalDays, document.DayTicks[^1].X);
        Assert.Equal(totalDays, document.DayTicks[^1].DayNumber);
    }

    [Fact]
    public void ExpectedPolyline_UsesLastOfDaySamples()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        var samples = new List<UsageSample>
        {
            SampleAt(cycle.CycleStart.AddHours(12), 25m, 30m),
            SampleAt(cycle.CycleStart.AddDays(2), 40m, 45m)
        };

        var document = _builder.Build(cycle, _calculator, samples);

        Assert.Equal(25m, document.CursorExpected[0].Y);
        Assert.Equal(
            _calculator.ExpectedPercent(cycle, QuotaKind.CursorModels, 1, samples),
            document.CursorExpected[0].Y);
        Assert.Equal(
            _calculator.ExpectedPercent(cycle, QuotaKind.CursorModels, 3, samples),
            document.CursorExpected[2].Y);
        Assert.Equal(40m, document.CursorExpected[2].Y);
    }

    [Fact]
    public void EstimatedPolyline_OmittedWithoutEnoughPoints()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));

        var document = _builder.Build(cycle, _calculator, samples: null);

        Assert.Empty(document.CursorEstimated);
        Assert.Empty(document.OtherEstimated);
        Assert.False(document.HasCursorEstimated);
        Assert.False(document.HasOtherEstimated);
    }

    [Fact]
    public void EstimatedPolyline_StartsAtLastUpdate_AndEndsAtRenewal()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        _calculator.SetManual(cycle, QuotaKind.CursorModels, 5, 20m);
        _calculator.SetManual(cycle, QuotaKind.CursorModels, 10, 50m);

        var document = _builder.Build(cycle, _calculator, samples: null);
        var totalDays = _calculator.TotalDays(cycle);

        Assert.Equal(10m, document.CursorEstimated[0].X);
        Assert.Equal(50m, document.CursorEstimated[0].Y);
        Assert.DoesNotContain(document.CursorEstimated, p => p.X < 10m);

        for (var day = 11; day <= totalDays; day++)
        {
            var point = document.CursorEstimated[day - 10];
            var midnight = cycle.CycleStart.Date.AddDays(day - 1);
            Assert.Equal(day, point.X);
            Assert.Equal(
                _calculator.ProjectedPercentAt(cycle, QuotaKind.CursorModels, midnight, samples: null),
                point.Y);
        }

        var last = document.CursorEstimated[^1];
        Assert.Equal(document.RenewalX, last.X);
        Assert.Equal(
            _calculator.ProjectedPercentAt(cycle, QuotaKind.CursorModels, cycle.NextRenewal, samples: null),
            last.Y);
    }

    [Fact]
    public void EstimatedPolyline_OmitsDaysOnOrBeforeLastUpdate()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        var sampleLocal = cycle.CycleStart.AddDays(4).AddHours(20);
        var samples = new List<UsageSample>
        {
            SampleAt(cycle.CycleStart, 0m, 0m),
            SampleAt(sampleLocal, 20m, 20m)
        };

        var document = _builder.Build(cycle, _calculator, samples);

        Assert.Equal(UsageChartSeriesBuilder.ToAxisX(cycle, sampleLocal), document.CursorEstimated[0].X);
        Assert.Equal(20m, document.CursorEstimated[0].Y);
        Assert.DoesNotContain(document.CursorEstimated, p => p.X < document.CursorEstimated[0].X);
        Assert.DoesNotContain(document.CursorEstimated, p => p.X == 5m);
        Assert.Equal(document.RenewalX, document.CursorEstimated[^1].X);
    }

    [Fact]
    public void TimedCycle_SeriesStartAtCycleStart_AndEndAtNextRenewal()
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
        var startX = UsageChartSeriesBuilder.ToAxisX(cycle, start);
        var renewalX = UsageChartSeriesBuilder.ToAxisX(cycle, end);

        Assert.Equal(startX, document.CursorExpected[0].X);
        Assert.True(document.CursorExpected[0].X > 1m);
        Assert.DoesNotContain(document.CursorExpected, p => p.X == 1m);
        Assert.Equal(renewalX, document.CursorExpected[^1].X);
        Assert.Equal(100m, document.CursorExpected[^1].Y);
        Assert.Equal(renewalX, document.PlotEndX);
        Assert.Equal(renewalX, document.RenewalX);
        Assert.Equal(_calculator.TotalDays(cycle) + 1m, document.SlotEndX);
        Assert.True(document.SlotEndX < document.RenewalX);
        Assert.Equal(_calculator.TotalDays(cycle), document.DayTicks.Count);
        Assert.Equal(_calculator.TotalDays(cycle), document.DayTicks[^1].X);

        Assert.Equal(UsageChartSeriesBuilder.ToAxisX(cycle, sampleLocal), document.CursorEstimated[0].X);
        Assert.Equal(40m, document.CursorEstimated[0].Y);
        Assert.Equal(renewalX, document.CursorEstimated[^1].X);
        Assert.Equal(
            _calculator.ProjectedPercentAt(cycle, QuotaKind.CursorModels, end, samples),
            document.CursorEstimated[^1].Y);
    }

    [Fact]
    public void EstimatedPolyline_MidnightVerticesFollowElapsedTime()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var end = new DateTime(2026, 9, 2, 22, 19, 47);
        var cycle = _calculator.GenerateCycleFromBounds(start, end);
        var sampleLocal = new DateTime(2026, 8, 18, 20, 11, 0);
        var samples = new List<UsageSample>
        {
            SampleAt(start, 0m, 0m),
            SampleAt(sampleLocal, 75.425m, 72.56m)
        };

        var document = _builder.Build(cycle, _calculator, samples);
        var nextMidnight = sampleLocal.Date.AddDays(1);
        var nextMidnightX = UsageChartSeriesBuilder.ToAxisX(cycle, nextMidnight);
        var atMidnight = _calculator.ProjectedPercentAt(cycle, QuotaKind.CursorModels, nextMidnight, samples);
        var dayNumber = (nextMidnight - cycle.CycleStart.Date).Days + 1;
        var atDayIndex = _calculator.ProjectedPercent(cycle, QuotaKind.CursorModels, dayNumber, samples);

        Assert.NotEqual(atDayIndex, atMidnight);

        var vertex = Assert.Single(document.CursorEstimated, p => p.X == nextMidnightX);
        Assert.Equal(atMidnight, vertex.Y);
        Assert.Equal(75.425m, document.CursorEstimated[0].Y);

        var dtHours = (decimal)(nextMidnight - sampleLocal).TotalHours;
        var slopePerHour = (vertex.Y - 75.425m) / dtHours;
        var endY = document.CursorEstimated[^1].Y;
        var endHours = (decimal)(end - sampleLocal).TotalHours;
        Assert.Equal(75.425m + slopePerHour * endHours, endY, precision: 10);
    }

    [Fact]
    public void OriginMarker_MidnightCycle_IsAtDayOne()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));

        var document = _builder.Build(cycle, _calculator, samples: null);
        var origin = Assert.Single(document.Markers, m => m.MarkerKind == ChartMarkerKind.Origin);

        Assert.Equal(1m, origin.X);
        Assert.Equal(0m, origin.Y);
        Assert.Null(origin.QuotaKind);
    }

    [Fact]
    public void OriginMarker_TimedCycle_UsesFractionalX()
    {
        var start = new DateTime(2026, 8, 2, 22, 19, 47);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddMonths(1));

        var document = _builder.Build(cycle, _calculator, samples: null);
        var origin = Assert.Single(document.Markers, m => m.MarkerKind == ChartMarkerKind.Origin);

        Assert.Equal(UsageChartSeriesBuilder.ToAxisX(cycle, start), origin.X);
        Assert.Equal(0m, origin.Y);
        Assert.True(origin.X > 1.9m);
        Assert.True(origin.X < 2m);
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
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
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
    public void SampleAt3am_SitsNearDayTick_SampleAt11pm_SitsNearNextDay()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        var at3am = cycle.CycleStart.Date.AddHours(3);
        var at11pm = cycle.CycleStart.Date.AddHours(23);
        var samples = new List<UsageSample>
        {
            SampleAt(at3am, 4m, 4m),
            SampleAt(at11pm, 7m, 7m)
        };

        var document = _builder.Build(cycle, _calculator, samples);
        var xs = document.Markers
            .Where(m => m.MarkerKind == ChartMarkerKind.Sample && m.QuotaKind == QuotaKind.CursorModels)
            .Select(m => m.X)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(2, xs.Count);
        Assert.True(xs[0] - 1m < 2m - xs[0]);
        Assert.True(2m - xs[1] < xs[1] - 1m);
    }

    [Fact]
    public void Edits_AppearAtIntegerDayNumbers()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        _calculator.SetManual(cycle, QuotaKind.CursorModels, 10, 42m);
        _calculator.SetManual(cycle, QuotaKind.OtherModels, 10, 55m);

        var document = _builder.Build(cycle, _calculator, samples: null);
        var edits = document.Markers.Where(m => m.MarkerKind == ChartMarkerKind.Edit).ToList();

        Assert.Equal(2, edits.Count);
        Assert.All(edits, e => Assert.Equal(10m, e.X));
        Assert.Contains(edits, e => e.QuotaKind == QuotaKind.CursorModels && e.Y == 42m);
        Assert.Contains(edits, e => e.QuotaKind == QuotaKind.OtherModels && e.Y == 55m);
    }

    [Fact]
    public void UsageLimit_IsGuideNotASeries()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        var document = _builder.Build(cycle, _calculator, samples: null);

        Assert.Equal(100m, document.UsageLimitPercent);
        Assert.DoesNotContain(document.CursorExpected, p => p.X == 0 && p.Y == 100m);
    }

    [Fact]
    public void YMax_ExtendsPast120WhenEstimatedExceeds()
    {
        var cycle = _calculator.GenerateCycle(1, new DateTime(2026, 1, 10));
        _calculator.SetManual(cycle, QuotaKind.CursorModels, 2, 80m);

        var document = _builder.Build(cycle, _calculator, samples: null);
        var lastProjected = document.CursorEstimated[^1].Y;

        Assert.True(lastProjected > 120m);
        Assert.True(document.YMax >= lastProjected);
        Assert.Equal(0m, document.YMax % 20m);
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
