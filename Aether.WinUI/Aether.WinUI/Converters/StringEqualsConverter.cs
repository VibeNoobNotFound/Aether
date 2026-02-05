using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Aether.WinUI.Converters;

public sealed class StringEqualsConverter : IValueConverter
{
    private static ILogger<StringEqualsConverter> Logger =>
        Ioc.Default.GetService<ILogger<StringEqualsConverter>>() ?? NullLogger<StringEqualsConverter>.Instance;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("StringEqualsConverter.Convert value={Value} parameter={Parameter}", value, parameter);
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
        Logger.LogTrace("StringEqualsConverter.ConvertBack value={Value}", value);
        return DependencyProperty.UnsetValue;
    }
}
