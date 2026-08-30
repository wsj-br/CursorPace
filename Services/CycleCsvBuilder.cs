using System.Globalization;
using System.Text;
using CursorPace.Models;

namespace CursorPace.Services;

public static class CycleCsvBuilder
{
    public static string Build(
        QuotaCycle cycle,
        ICycleCalculator calculator,
        IReadOnlyList<UsageSample>? samples)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            "day number,date,Cursor (expected),Other Models (expected),Cursor (estimated),Other Models (estimated),IsDataPoint");

        foreach (var day in cycle.Days)
        {
            var expectedCursor = calculator.ExpectedPercent(cycle, QuotaKind.CursorModels, day.DayNumber, samples);
            var expectedOther = calculator.ExpectedPercent(cycle, QuotaKind.OtherModels, day.DayNumber, samples);
            var estimatedCursor = calculator.ProjectedPercent(cycle, QuotaKind.CursorModels, day.DayNumber, samples);
            var estimatedOther = calculator.ProjectedPercent(cycle, QuotaKind.OtherModels, day.DayNumber, samples);
            var isDataPoint = day.CursorModelsIsActual || day.OtherModelsIsActual ? 1 : 0;

            builder.Append(day.DayNumber);
            builder.Append(',');
            builder.Append(day.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(FormatCsvRatio(expectedCursor));
            builder.Append(',');
            builder.Append(FormatCsvRatio(expectedOther));
            builder.Append(',');
            builder.Append(FormatCsvRatio(estimatedCursor));
            builder.Append(',');
            builder.Append(FormatCsvRatio(estimatedOther));
            builder.Append(',');
            builder.Append(isDataPoint);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string FormatCsvRatio(decimal? percent) =>
        percent is decimal value
            ? (value / 100m).ToString("0.0000", CultureInfo.InvariantCulture)
            : string.Empty;
}
