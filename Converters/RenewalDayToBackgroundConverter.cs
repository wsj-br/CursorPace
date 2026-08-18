using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CursorUsageProgress.Converters;

public sealed class RenewalDayToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isRenewalDay && isRenewalDay)
        {
            // Subtle violet tint for cycle start and renewal days
            return new SolidColorBrush(Color.FromArgb(28, 139, 92, 246));
        }

        return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
