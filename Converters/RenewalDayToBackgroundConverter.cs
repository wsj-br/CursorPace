using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CursorQuotaProgress.Converters;

public sealed class RenewalDayToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isRenewalDay && isRenewalDay)
        {
            // Subtle amber/gold tint for renewal days - professional
            return new SolidColorBrush(Color.FromArgb(25, 251, 191, 36));
        }

        return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
