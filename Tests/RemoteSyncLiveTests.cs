using CursorPace.Models;
using CursorPace.Services;

namespace CursorPace.Tests;

public class RemoteSyncLiveTests
{
    [Fact]
    public async Task LiveServer_PushThenPull_RoundTripsWhenConfigured()
    {
        var token = Environment.GetEnvironmentVariable("CP_SYNC_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
            return;

        var url = Environment.GetEnvironmentVariable("CP_SYNC_URL") ?? "http://127.0.0.1:8000";
        var service = new RemoteSyncService();
        var marker = new DateTimeOffset(2026, 1, 2, 3, 4, 5, 123, TimeSpan.Zero);
        var local = new RemoteSyncLocalState(
            url,
            token.Trim('"'),
            "live-test",
            new DateTimeOffset(2026, 8, 15, 0, 0, 0, TimeSpan.Zero),
            new QuotaCycle
            {
                RenewalDay = 15,
                CycleStart = new DateTime(2026, 8, 15, 1, 0, 0),
                NextRenewal = new DateTime(2026, 9, 15, 1, 0, 0)
            },
            [],
            [
                new UsageSample
                {
                    TimestampUtc = marker,
                    CursorModelsPercent = 25.16583333333333m,
                    OtherModelsPercent = 61.1818m
                }
            ]);

        var result = await service.SyncAsync(local);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.NotNull(result.Canonical);
        var canonical = result.Canonical;
        Assert.Contains(canonical.Samples, s => s.TimestampUtc.UtcTicks == marker.UtcTicks);
        Assert.NotNull(canonical.ActiveCycle);
        Assert.Equal(new DateTime(2026, 8, 15, 1, 0, 0), canonical.ActiveCycle.CycleStart);

        var again = await service.SyncAsync(local);
        Assert.True(again.Success, again.ErrorMessage);
        Assert.Equal(0, again.PushedNewCount);
    }
}
