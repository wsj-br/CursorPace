using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CursorPace.Models;

namespace CursorPace.Services;

public sealed class RemoteSyncService : IRemoteSyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new LenientDecimalConverter() }
    };

    private readonly HttpClient _http;

    public RemoteSyncService()
        : this(new HttpClient { Timeout = TimeSpan.FromSeconds(30) })
    {
    }

    public RemoteSyncService(HttpClient httpClient)
    {
        _http = httpClient;
    }

    public async Task<RemoteSyncResult> SyncAsync(RemoteSyncLocalState local, CancellationToken cancellationToken = default)
    {
        string baseUrl;
        try
        {
            baseUrl = NormalizeBaseUrl(local.BaseUrl);
        }
        catch (Exception ex)
        {
            return Fail(ex.Message);
        }

        using var pushContent = new StringContent(
            JsonSerializer.Serialize(ToPushBody(local), JsonOptions),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage pushResponse;
        try
        {
            using var pushRequest = new HttpRequestMessage(HttpMethod.Post, baseUrl + "/api/v1/push")
            {
                Content = pushContent
            };
            pushRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", local.ApiKey);
            pushResponse = await _http.SendAsync(pushRequest, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Fail(TransportMessage(ex));
        }

        int pushedNew = 0;
        int serverTotal = 0;
        using (pushResponse)
        {
            var pushJson = await ReadBodyAsync(pushResponse, cancellationToken).ConfigureAwait(false);
            if (pushResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return Fail("The sync server rejected the API token.");
            if (!pushResponse.IsSuccessStatusCode)
                return Fail(FormatHttpError("push", pushResponse.StatusCode, pushJson));

            try
            {
                var pushResult = JsonSerializer.Deserialize<PushResponse>(pushJson, JsonOptions);
                pushedNew = pushResult?.Accepted ?? 0;
                serverTotal = pushResult?.TotalSamples ?? 0;
            }
            catch
            {
            }
        }

        HttpResponseMessage pullResponse;
        try
        {
            using var pullRequest = new HttpRequestMessage(HttpMethod.Get, baseUrl + "/api/v1/pull");
            pullRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", local.ApiKey);
            pullResponse = await _http.SendAsync(pullRequest, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Fail(TransportMessage(ex));
        }

        using (pullResponse)
        {
            var pullJson = await ReadBodyAsync(pullResponse, cancellationToken).ConfigureAwait(false);
            if (pullResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return Fail("The sync server rejected the API token.");
            if (!pullResponse.IsSuccessStatusCode)
                return Fail(FormatHttpError("pull", pullResponse.StatusCode, pullJson));

            PullResponse? pull;
            try
            {
                pull = JsonSerializer.Deserialize<PullResponse>(pullJson, JsonOptions);
            }
            catch (JsonException)
            {
                return Fail("The sync server returned an unreadable response.");
            }

            if (pull == null)
                return Fail("The sync server returned an empty response.");

            return new RemoteSyncResult(true, null, ToCanonicalState(pull), pushedNew, serverTotal);
        }
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        var trimmed = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (trimmed.Length == 0)
            throw new InvalidOperationException("Set the sync server URL first.");
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("The sync server URL must start with http:// or https://.");
        return trimmed;
    }

    private static async Task<string> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return string.Empty;
        }
    }

    private static string FormatHttpError(string operation, System.Net.HttpStatusCode status, string body)
    {
        var detail = TryReadDetail(body);
        if (!string.IsNullOrWhiteSpace(detail))
            return detail;

        return $"The sync server returned {(int)status} for {operation}.";
    }

    private static string? TryReadDetail(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("detail", out var detail))
                return null;
            if (detail.ValueKind == JsonValueKind.String)
                return detail.GetString();
            if (detail.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var item in detail.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    return item.GetString();
                if (item.ValueKind == JsonValueKind.Object
                    && item.TryGetProperty("msg", out var msg)
                    && msg.ValueKind == JsonValueKind.String)
                    return msg.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private static RemoteSyncResult Fail(string message) =>
        new(false, message, null, 0, 0);

    private static string TransportMessage(Exception ex) =>
        ex is TaskCanceledException
            ? "The sync server did not answer in time."
            : "Could not reach the sync server.";

    private static PushBody ToPushBody(RemoteSyncLocalState local) =>
        new()
        {
            MachineName = local.MachineName,
            CycleStartUtc = local.CycleStartUtc,
            ActiveCycle = ToCycleBody(local.ActiveCycle),
            CycleHistory = local.CycleHistory.Select(ToCycleBody).Where(c => c != null).Select(c => c!).ToList(),
            Samples = local.Samples.Select(s => new SampleBody
            {
                Ts = s.TimestampUtc,
                Cursor = s.CursorModelsPercent,
                Other = s.OtherModelsPercent
            }).ToList()
        };

    private static CycleBody? ToCycleBody(QuotaCycle? cycle) =>
        cycle == null
            ? null
            : new CycleBody { CycleStart = cycle.CycleStart, NextRenewal = cycle.NextRenewal };

    private static RemoteSyncCanonicalState ToCanonicalState(PullResponse pull)
    {
        var samples = new List<UsageSample>();
        if (pull.Samples != null)
        {
            foreach (var item in pull.Samples)
            {
                if (item == null)
                    continue;
                samples.Add(new UsageSample
                {
                    TimestampUtc = item.Ts,
                    CursorModelsPercent = item.Cursor,
                    OtherModelsPercent = item.Other
                });
            }
        }

        var history = new List<QuotaCycle>();
        if (pull.CycleHistory != null)
        {
            foreach (var item in pull.CycleHistory)
            {
                var cycle = ToQuotaCycle(item);
                if (cycle != null)
                    history.Add(cycle);
            }
        }

        return new RemoteSyncCanonicalState(
            pull.CycleStartUtc,
            ToQuotaCycle(pull.ActiveCycle),
            history,
            samples);
    }

    private static QuotaCycle? ToQuotaCycle(CycleBody? body)
    {
        if (body == null || body.NextRenewal <= body.CycleStart)
            return null;

        return new QuotaCycle
        {
            RenewalDay = body.CycleStart.Day,
            CycleStart = body.CycleStart,
            NextRenewal = body.NextRenewal
        };
    }

    private sealed class PushBody
    {
        [JsonPropertyName("machine_name")]
        public string MachineName { get; set; } = string.Empty;
        [JsonPropertyName("cycle_start_utc")]
        public DateTimeOffset? CycleStartUtc { get; set; }
        [JsonPropertyName("active_cycle")]
        public CycleBody? ActiveCycle { get; set; }
        [JsonPropertyName("cycle_history")]
        public List<CycleBody> CycleHistory { get; set; } = new();
        [JsonPropertyName("samples")]
        public List<SampleBody> Samples { get; set; } = new();
    }

    private sealed class CycleBody
    {
        [JsonPropertyName("cycle_start")]
        public DateTime CycleStart { get; set; }
        [JsonPropertyName("next_renewal")]
        public DateTime NextRenewal { get; set; }
    }

    private sealed class SampleBody
    {
        [JsonPropertyName("ts")]
        public DateTimeOffset Ts { get; set; }
        [JsonPropertyName("cursor")]
        public decimal Cursor { get; set; }
        [JsonPropertyName("other")]
        public decimal Other { get; set; }
    }

    private sealed class PushResponse
    {
        [JsonPropertyName("accepted")]
        public int Accepted { get; set; }
        [JsonPropertyName("total_samples")]
        public int TotalSamples { get; set; }
    }

    private sealed class PullResponse
    {
        [JsonPropertyName("cycle_start_utc")]
        public DateTimeOffset? CycleStartUtc { get; set; }
        [JsonPropertyName("active_cycle")]
        public CycleBody? ActiveCycle { get; set; }
        [JsonPropertyName("cycle_history")]
        public List<CycleBody>? CycleHistory { get; set; }
        [JsonPropertyName("samples")]
        public List<SampleBody>? Samples { get; set; }
    }

    /// <summary>
    /// Accepts percentages as JSON numbers or numeric strings without float rounding.
    /// </summary>
    private sealed class LenientDecimalConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.Number => reader.GetDecimal(),
                JsonTokenType.String => decimal.Parse(
                    reader.GetString() ?? "0",
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture),
                _ => throw new JsonException("Expected a decimal number or numeric string.")
            };

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
        {
            var rounded = decimal.Round(value, 4, MidpointRounding.AwayFromZero);
            writer.WriteStringValue(rounded.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}
