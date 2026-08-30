using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CursorPace.Converters;

public sealed class RunOutDayToBackgroundConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true
            ? new SolidColorBrush(Color.FromArgb(32, 250, 204, 21))
            : Brushes.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
