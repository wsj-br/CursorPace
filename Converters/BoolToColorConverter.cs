using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CursorQuotaProgress.Converters;

public sealed class BoolToColorConverter : IValueConverter
{
    public string TrueColor { get; set; } = "#FF0000";
    public string FalseColor { get; set; } = "#00FF00";

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var colorString = value is bool boolValue && boolValue ? TrueColor : FalseColor;
        return new SolidColorBrush(ParseColor(colorString));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    private Color ParseColor(string hex)
    {
        hex = hex.TrimStart('#');

        if (hex.Length == 6)
        {
            return Color.FromArgb(
                255,
                System.Convert.ToByte(hex.Substring(0, 2), 16),
                System.Convert.ToByte(hex.Substring(2, 2), 16),
                System.Convert.ToByte(hex.Substring(4, 2), 16)
            );
        }

        return Colors.White;
    }
}
