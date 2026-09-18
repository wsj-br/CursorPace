using System.Net;
using System.Text;
using System.Text.Json;
using CursorPace.Models;
using CursorPace.Services;

namespace CursorPace.Tests;

public class RemoteSyncServiceTests
{
    [Fact]
    public async Task SyncAsync_PushesLocalThenPullsCanonical()
    {
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/api/v1/push")
            {
                Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
                Assert.Equal("test-key", request.Headers.Authorization.Parameter);
                return JsonResponse("""{"accepted":2,"duplicates":1,"total_samples":3}""");
            }

            if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath == "/api/v1/pull")
                return JsonResponse("""
                    {
                      "cycle_start_utc": "2026-09-01T00:00:00.000000Z",
                      "active_cycle": {"cycle_start": "2026-09-01T01:00:00", "next_renewal": "2026-10-01T01:00:00"},
                      "cycle_history": [{"cycle_start": "2026-08-01T01:00:00", "next_renewal": "2026-09-01T01:00:00"}],
                      "samples": [
                        {"ts": "2026-09-02T10:00:00.000000Z", "cursor": 20.5, "other": "7.25"}
                      ],
                      "server_time": "2026-09-18T10:00:00.000000Z"
                    }
                    """);

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var service = new RemoteSyncService(new HttpClient(handler));

        var result = await service.SyncAsync(LocalState());

        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.PushedNewCount);
        Assert.Equal(3, result.ServerTotal);
        Assert.NotNull(result.Canonical);
        var canonical = result.Canonical;
        Assert.Equal(new DateTime(2026, 9, 1, 1, 0, 0), canonical.ActiveCycle!.CycleStart);
        Assert.Single(canonical.CycleHistory);
        var sample = Assert.Single(canonical.Samples);
        Assert.Equal(20.5m, sample.CursorModelsPercent);
        Assert.Equal(7.25m, sample.OtherModelsPercent);
    }

    [Fact]
    public async Task SyncAsync_PushBodyUsesSnakeCaseContract()
    {
        string? pushBody = null;
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Post)
            {
                pushBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return JsonResponse("""{"accepted":0,"total_samples":0}""");
            }

            return JsonResponse("""{"cycle_start_utc":null,"active_cycle":null,"cycle_history":[],"samples":[]}""");
        });
        var service = new RemoteSyncService(new HttpClient(handler));

        var result = await service.SyncAsync(LocalState());

        Assert.True(result.Success);
        using var document = JsonDocument.Parse(pushBody!);
        var root = document.RootElement;
        Assert.Equal("test-machine", root.GetProperty("machine_name").GetString());
        Assert.True(root.TryGetProperty("cycle_start_utc", out _));
        Assert.True(root.TryGetProperty("active_cycle", out _));
        Assert.True(root.TryGetProperty("cycle_history", out _));
        var samples = root.GetProperty("samples");
        Assert.Equal(2, samples.GetArrayLength());
        Assert.True(samples[0].TryGetProperty("ts", out _));
        Assert.Equal(JsonValueKind.String, samples[0].GetProperty("cursor").ValueKind);
        Assert.Equal("10", samples[0].GetProperty("cursor").GetString());
    }

    [Fact]
    public async Task SyncAsync_RoundsPercentsToFourFractionDigitsAsStrings()
    {
        string? pushBody = null;
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Post)
            {
                pushBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return JsonResponse("""{"accepted":1,"total_samples":1}""");
            }

            return JsonResponse("""{"cycle_start_utc":null,"active_cycle":null,"cycle_history":[],"samples":[]}""");
        });
        var service = new RemoteSyncService(new HttpClient(handler));
        var local = LocalState() with
        {
            Samples =
            [
                new UsageSample
                {
                    TimestampUtc = new DateTimeOffset(2026, 8, 18, 11, 40, 13, TimeSpan.Zero),
                    CursorModelsPercent = 25.16583333333333m,
                    OtherModelsPercent = 61.18181818181818m
                }
            ]
        };

        var result = await service.SyncAsync(local);

        Assert.True(result.Success);
        using var document = JsonDocument.Parse(pushBody!);
        var sample = document.RootElement.GetProperty("samples")[0];
        Assert.Equal("25.1658", sample.GetProperty("cursor").GetString());
        Assert.Equal("61.1818", sample.GetProperty("other").GetString());
    }

    [Fact]
    public async Task SyncAsync_IncludesServerDetailOnBadRequest()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """{"detail":"samples[40]: invalid decimal"}""",
                Encoding.UTF8,
                "application/json")
        });
        var service = new RemoteSyncService(new HttpClient(handler));

        var result = await service.SyncAsync(LocalState());

        Assert.False(result.Success);
        Assert.Equal("samples[40]: invalid decimal", result.ErrorMessage);
    }

    [Fact]
    public async Task SyncAsync_TrimsTrailingSlashFromBaseUrl()
    {
        string? pushPath = null;
        var handler = new StubHandler(request =>
        {
            if (request.Method == HttpMethod.Post)
            {
                pushPath = request.RequestUri!.AbsolutePath;
                return JsonResponse("""{"accepted":0,"total_samples":0}""");
            }

            return JsonResponse("""{"cycle_start_utc":null,"active_cycle":null,"cycle_history":[],"samples":[]}""");
        });
        var service = new RemoteSyncService(new HttpClient(handler));
        var local = LocalState() with { BaseUrl = "http://server:8000/" };

        var result = await service.SyncAsync(local);

        Assert.True(result.Success);
        Assert.Equal("/api/v1/push", pushPath);
    }

    [Fact]
    public async Task SyncAsync_InvalidUrlReturnsFailure()
    {
        var service = new RemoteSyncService(new HttpClient(new StubHandler(_ =>
            JsonResponse("{}"))));
        var local = LocalState() with { BaseUrl = "not-a-url" };

        var result = await service.SyncAsync(local);

        Assert.False(result.Success);
        Assert.Contains("http", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SyncAsync_UnauthorizedReturnsKeyError()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var service = new RemoteSyncService(new HttpClient(handler));

        var result = await service.SyncAsync(LocalState());

        Assert.False(result.Success);
        Assert.Contains("API token", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SyncAsync_TransportFailureReturnsFailure()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("down"));
        var service = new RemoteSyncService(new HttpClient(handler));

        var result = await service.SyncAsync(LocalState());

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task SyncAsync_InvalidPullJsonReturnsFailure()
    {
        var handler = new StubHandler(request =>
            request.Method == HttpMethod.Post
                ? JsonResponse("""{"accepted":0,"total_samples":0}""")
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("not json", Encoding.UTF8, "application/json")
                });
        var service = new RemoteSyncService(new HttpClient(handler));

        var result = await service.SyncAsync(LocalState());

        Assert.False(result.Success);
        Assert.Contains("unreadable", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SyncAsync_SkipsInvalidCycleBounds()
    {
        var handler = new StubHandler(request =>
            request.Method == HttpMethod.Post
                ? JsonResponse("""{"accepted":0,"total_samples":0}""")
                : JsonResponse("""
                    {
                      "cycle_start_utc": null,
                      "active_cycle": {"cycle_start": "2026-10-01T01:00:00", "next_renewal": "2026-09-01T01:00:00"},
                      "cycle_history": [],
                      "samples": []
                    }
                    """));
        var service = new RemoteSyncService(new HttpClient(handler));

        var result = await service.SyncAsync(LocalState());

        Assert.True(result.Success);
        Assert.NotNull(result.Canonical);
        Assert.Null(result.Canonical.ActiveCycle);
    }

    private static RemoteSyncLocalState LocalState() =>
        new(
            "http://server:8000",
            "test-key",
            "test-machine",
            new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
            new QuotaCycle
            {
                RenewalDay = 1,
                CycleStart = new DateTime(2026, 9, 1, 1, 0, 0),
                NextRenewal = new DateTime(2026, 10, 1, 1, 0, 0)
            },
            [],
            [
                new UsageSample
                {
                    TimestampUtc = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero),
                    CursorModelsPercent = 10,
                    OtherModelsPercent = 2
                },
                new UsageSample
                {
                    TimestampUtc = new DateTimeOffset(2026, 9, 3, 10, 0, 0, TimeSpan.Zero),
                    CursorModelsPercent = 12,
                    OtherModelsPercent = 3
                }
            ]);

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
