using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace SmoothScrollModern.Shared.Presentation;

public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility.Visible;
    }
}

public sealed class BooleanToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? 1.0 : 0.45;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is double opacity && opacity >= 1.0;
    }
}
