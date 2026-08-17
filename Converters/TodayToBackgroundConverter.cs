using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CursorQuotaProgress.Converters;

public sealed class TodayToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isToday && isToday)
        {
            // Subtle blue tint for today - professional and understated
            return new SolidColorBrush(Color.FromArgb(25, 96, 165, 250));
        }
        return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
