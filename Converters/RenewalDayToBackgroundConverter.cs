using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CursorUsageProgress.Converters;

public sealed class RenewalDayToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is true)
            return new SolidColorBrush(Color.FromArgb(28, 139, 92, 246));
        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
