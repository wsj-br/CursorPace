using CursorUsageProgress.Models;
using CursorUsageProgress.Services;

namespace CursorUsageProgress.Tests;

public class UsageSummaryParserTests
{
    private const string SampleBody = """
        {
          "billingCycleStart": "2026-08-02T21:19:47Z",
          "billingCycleEnd": "2026-09-02T21:19:47Z",
          "membershipType": "pro_plus",
          "individualUsage": {
            "plan": {
              "autoPercentUsed": 32.78375,
              "apiPercentUsed": 51.5,
              "totalPercentUsed": 35.04615384615384
            }
          }
        }
        """;

    [Fact]
    public void TryParse_MapsAutoToCursorAndApiToOther()
    {
        var fetchedAt = DateTimeOffset.Parse("2026-08-18T01:00:00Z");

        Assert.True(UsageSummaryParser.TryParse(SampleBody, fetchedAt, out var snapshot));
        Assert.NotNull(snapshot);
        Assert.Equal(DateTimeOffset.Parse("2026-08-02T21:19:47Z"), snapshot!.BillingCycleStartUtc);
        Assert.Equal(DateTimeOffset.Parse("2026-09-02T21:19:47Z"), snapshot.BillingCycleEndUtc);
        Assert.Equal(32.78375m, snapshot.CursorModelsPercent);
        Assert.Equal(51.5m, snapshot.OtherModelsPercent);
        Assert.Equal(fetchedAt, snapshot.FetchedAtUtc);
    }

    [Fact]
    public void TryParse_MissingPlan_ReturnsFalse()
    {
        Assert.False(UsageSummaryParser.TryParse("""{"billingCycleStart":"2026-08-02T21:19:47Z","billingCycleEnd":"2026-09-02T21:19:47Z","individualUsage":{}}""", DateTimeOffset.UtcNow, out var snapshot));
        Assert.Null(snapshot);
    }

    [Fact]
    public void TryParse_InvalidJson_ReturnsFalse()
    {
        Assert.False(UsageSummaryParser.TryParse("not json", DateTimeOffset.UtcNow, out _));
    }
}
