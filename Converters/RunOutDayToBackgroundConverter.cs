using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace CursorUsageProgress.Converters;

public sealed class RunOutDayToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is bool isRunOut && isRunOut
            ? new SolidColorBrush(Color.FromArgb(32, 250, 204, 21))
            : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}
