using System.Text.Json;
using System.Text.Json.Serialization;

namespace CursorUsageProgress.Services;

public static class WebView2ScriptResultParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool TryParse(string? encoded, out int status, out string body)
    {
        status = 0;
        body = string.Empty;
        if (string.IsNullOrWhiteSpace(encoded) || encoded == "null")
            return false;

        try
        {
            using var document = JsonDocument.Parse(encoded);
            var json = Unwrap(document.RootElement);
            if (json == null)
                return false;

            var dto = JsonSerializer.Deserialize<Dto>(json, Options);
            if (dto == null)
                return false;

            status = dto.Status;
            body = dto.Body ?? string.Empty;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? Unwrap(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.String)
        {
            var inner = root.GetString();
            if (string.IsNullOrWhiteSpace(inner) || inner == "null")
                return null;

            using var nested = JsonDocument.Parse(inner);
            if (nested.RootElement.ValueKind != JsonValueKind.Object
                || !nested.RootElement.TryGetProperty("status", out _))
            {
                return null;
            }

            return inner;
        }

        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("status", out _))
            return null;

        return root.GetRawText();
    }

    private sealed class Dto
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;
    }
}
