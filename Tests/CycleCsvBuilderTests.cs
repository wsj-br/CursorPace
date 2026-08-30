using CursorPace.Models;
using CursorPace.Services;

namespace CursorPace.Tests;

public class CycleCsvBuilderTests
{
    private readonly CycleCalculator _calculator = new();

    [Fact]
    public void Build_WritesExpectedHeadersAndExpectedPercentNotSampleOverlay()
    {
        var start = new DateTime(2026, 1, 1);
        var cycle = _calculator.GenerateCycleFromBounds(start, start.AddDays(31));
        var samples = new List<UsageSample>
        {
            SampleAt(cycle.CycleStart.AddHours(12), 9m, 11m)
        };
        _calculator.RebuildDays(cycle, samples, cycle.CycleStart.Date);

        var csv = CycleCsvBuilder.Build(cycle, _calculator, samples);
        var lines = csv.Replace("\r\n", "\n").TrimEnd().Split('\n');

        Assert.Equal(
            "day number,date,Cursor (expected),Other Models (expected),Cursor (estimated),Other Models (estimated),IsDataPoint",
            lines[0]);

        var day1 = lines[1].Split(',');
        Assert.Equal("1", day1[0]);
        Assert.Equal("2026-01-01", day1[1]);
        var expectedDay1 = _calculator.ExpectedPercent(cycle, QuotaKind.CursorModels, 1, samples);
        Assert.Equal((expectedDay1 / 100m).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture), day1[2]);
        Assert.Equal("1", day1[6]);

        var expectedDay2 = _calculator.ExpectedPercent(cycle, QuotaKind.CursorModels, 2, samples);
        var day2 = lines[2].Split(',');
        Assert.Equal((expectedDay2 / 100m).ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture), day2[2]);
        Assert.NotEqual("0.0000", day2[2]);
        Assert.Equal("0", day2[6]);
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
