using System.Globalization;
using System.Text;
using CursorUsageProgress.Models;

namespace CursorUsageProgress.Services;

public static class UsageSamplesCsvBuilder
{
    public static string Build(IReadOnlyList<UsageSample> samples)
    {
        var builder = new StringBuilder();
        builder.AppendLine("timestamp,Cursor,Other Models");

        foreach (var sample in samples.OrderBy(s => s.TimestampUtc))
        {
            builder.Append(FormatTimestamp(sample.TimestampUtc));
            builder.Append(',');
            builder.Append(FormatCsvRatio(sample.CursorModelsPercent));
            builder.Append(',');
            builder.Append(FormatCsvRatio(sample.OtherModelsPercent));
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string FormatTimestamp(DateTimeOffset timestamp) =>
        timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    private static string FormatCsvRatio(decimal percent) =>
        (percent / 100m).ToString("0.0000", CultureInfo.InvariantCulture);
}
