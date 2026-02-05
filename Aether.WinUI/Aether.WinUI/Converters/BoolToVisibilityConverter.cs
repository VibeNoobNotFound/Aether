using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Aether.WinUI.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    private static ILogger<BoolToVisibilityConverter> Logger =>
        Ioc.Default.GetService<ILogger<BoolToVisibilityConverter>>() ?? NullLogger<BoolToVisibilityConverter>.Instance;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("BoolToVisibilityConverter.Convert value={Value} parameter={Parameter}", value, parameter);
        if (value is bool boolValue)
        {
            // Default: true -> Visible, false -> Collapsed
            // If parameter is "Inverse", flip it
            bool isInverse = parameter is string str && str.Equals("Inverse", StringComparison.OrdinalIgnoreCase);

            if (isInverse)
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            else
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("BoolToVisibilityConverter.ConvertBack value={Value}", value);
        return DependencyProperty.UnsetValue;
    }
}
