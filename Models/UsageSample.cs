using System.Text.Json.Serialization;

namespace CursorPace.Models;

public sealed class UsageSample
{
    [JsonPropertyName("ts")]
    public DateTimeOffset TimestampUtc { get; set; }

    [JsonPropertyName("cursor")]
    public decimal CursorModelsPercent { get; set; }

    [JsonPropertyName("other")]
    public decimal OtherModelsPercent { get; set; }

    public decimal GetPercent(QuotaKind kind) =>
        kind switch
        {
            QuotaKind.CursorModels => CursorModelsPercent,
            QuotaKind.OtherModels => OtherModelsPercent,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
}
