using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace CursorUsageProgress.Converters;

public sealed class TodayToFontWeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isToday && isToday)
        {
            return new Windows.UI.Text.FontWeight { Weight = 600 }; // SemiBold
        }
        return new Windows.UI.Text.FontWeight { Weight = 400 }; // Normal
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
