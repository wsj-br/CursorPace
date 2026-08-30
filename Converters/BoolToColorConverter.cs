using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;

namespace CursorUsageProgress.Converters;

public sealed class BoolToColorConverter : IValueConverter
{
    public string TrueColor { get; set; } = "#FF0000";
    public string FalseColor { get; set; } = "#00FF00";
    public string? TrueResource { get; set; }
    public string? FalseResource { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var resource = value is true ? TrueResource : FalseResource;
        if (!string.IsNullOrWhiteSpace(resource)
            && Application.Current?.TryGetResource(resource, Application.Current.ActualThemeVariant, out var found) == true
            && found is IBrush brush)
        {
            return brush;
        }

        var colorString = value is true ? TrueColor : FalseColor;
        return new SolidColorBrush(ParseColor(colorString));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return Color.FromArgb(
                255,
                System.Convert.ToByte(hex[..2], 16),
                System.Convert.ToByte(hex.Substring(2, 2), 16),
                System.Convert.ToByte(hex.Substring(4, 2), 16));
        }

        return Colors.White;
    }
}
