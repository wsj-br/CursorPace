using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CursorPace.Converters;

public sealed class TodayToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
            return new SolidColorBrush(Color.FromArgb(25, 96, 165, 250));
        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
