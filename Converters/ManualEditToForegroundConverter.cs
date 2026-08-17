using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CursorQuotaProgress.Converters;

public sealed class ManualEditToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isManuallyEdited && isManuallyEdited)
        {
            return new SolidColorBrush(Color.FromArgb(255, 91, 156, 230));
        }

        return null!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
