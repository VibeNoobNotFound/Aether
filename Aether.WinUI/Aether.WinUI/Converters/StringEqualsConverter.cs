using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Aether.WinUI.Converters;

public sealed class StringEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var left = value?.ToString() ?? string.Empty;
        var right = parameter?.ToString() ?? string.Empty;
        var isMatch = string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

        if (targetType == typeof(Visibility))
        {
            return isMatch ? Visibility.Visible : Visibility.Collapsed;
        }

        return isMatch;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return DependencyProperty.UnsetValue;
    }
}
