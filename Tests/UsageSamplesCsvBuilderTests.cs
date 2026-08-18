using CursorUsageProgress.Models;
using CursorUsageProgress.Services;

namespace CursorUsageProgress.Tests;

public class UsageSamplesCsvBuilderTests
{
    [Fact]
    public void Build_WritesHeadersRatiosAndUtcTimestampsInOrder()
    {
        var later = DateTimeOffset.Parse("2026-08-03T14:30:00Z");
        var earlier = DateTimeOffset.Parse("2026-08-03T08:00:00Z");
        var csv = UsageSamplesCsvBuilder.Build(
        [
            new UsageSample
            {
                TimestampUtc = later,
                CursorModelsPercent = 20m,
                OtherModelsPercent = 25m
            },
            new UsageSample
            {
                TimestampUtc = earlier,
                CursorModelsPercent = 4.5m,
                OtherModelsPercent = 6m
            }
        ]);
        var lines = csv.Replace("\r\n", "\n").TrimEnd().Split('\n');

        Assert.Equal("timestamp,Cursor,Other Models", lines[0]);
        Assert.Equal("2026-08-03T08:00:00Z,0.0450,0.0600", lines[1]);
        Assert.Equal("2026-08-03T14:30:00Z,0.2000,0.2500", lines[2]);
    }
}
