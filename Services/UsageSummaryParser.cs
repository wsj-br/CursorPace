using System.Globalization;
using System.Text.Json;
using CursorUsageProgress.Models;

namespace CursorUsageProgress.Services;

public static class UsageSummaryParser
{
    public static bool TryParse(string body, DateTimeOffset fetchedAt, out UsageSnapshot? snapshot)
    {
        snapshot = null;
        if (string.IsNullOrWhiteSpace(body))
            return false;

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (!TryGetDateTimeOffset(root, "billingCycleStart", out var cycleStart))
                return false;
            if (!TryGetDateTimeOffset(root, "billingCycleEnd", out var cycleEnd))
                return false;
            if (cycleEnd <= cycleStart)
                return false;

            if (!root.TryGetProperty("individualUsage", out var usage)
                || usage.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!usage.TryGetProperty("plan", out var plan)
                || plan.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (!TryGetDecimal(plan, "autoPercentUsed", out var cursorPercent))
                return false;
            if (!TryGetDecimal(plan, "apiPercentUsed", out var otherPercent))
                return false;

            snapshot = new UsageSnapshot
            {
                BillingCycleStartUtc = cycleStart.ToUniversalTime(),
                BillingCycleEndUtc = cycleEnd.ToUniversalTime(),
                CursorModelsPercent = cursorPercent,
                OtherModelsPercent = otherPercent,
                FetchedAtUtc = fetchedAt.ToUniversalTime()
            };
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryGetDateTimeOffset(JsonElement parent, string name, out DateTimeOffset value)
    {
        value = default;
        if (!parent.TryGetProperty(name, out var element))
            return false;

        if (element.ValueKind == JsonValueKind.String)
        {
            return DateTimeOffset.TryParse(
                element.GetString(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out value);
        }

        return element.TryGetDateTimeOffset(out value);
    }

    private static bool TryGetDecimal(JsonElement parent, string name, out decimal value)
    {
        value = default;
        if (!parent.TryGetProperty(name, out var element))
            return false;

        if (element.ValueKind == JsonValueKind.Number)
            return element.TryGetDecimal(out value);

        if (element.ValueKind == JsonValueKind.String)
        {
            return decimal.TryParse(
                element.GetString(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value);
        }

        return false;
    }
}
