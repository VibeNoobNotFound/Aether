using Microsoft.UI.Xaml.Data;
using System;

namespace Aether.WinUI.Converters;

public class BoolToEyeIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isVisible && isVisible)
        {
            return "\uE7B3"; // Eye Open (Redial)
        }
        return "\uED1A"; // Eye Hide (Hide)
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
