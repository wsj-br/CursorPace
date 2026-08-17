using Microsoft.UI.Xaml.Data;

namespace CursorQuotaProgress.Converters;

public sealed class BoolToOpacityConverter : IValueConverter
{
    public double TrueOpacity { get; set; } = 1.0;
    public double FalseOpacity { get; set; } = 0.5;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool boolValue)
        {
            return boolValue ? TrueOpacity : FalseOpacity;
        }
        return FalseOpacity;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
