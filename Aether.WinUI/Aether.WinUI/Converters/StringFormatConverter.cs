using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Aether.WinUI.Converters;

public class StringFormatConverter : IValueConverter
{
    private static ILogger<StringFormatConverter> Logger =>
        Ioc.Default.GetService<ILogger<StringFormatConverter>>() ?? NullLogger<StringFormatConverter>.Instance;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("StringFormatConverter.Convert value={Value} parameter={Parameter}", value, parameter);
        if (parameter is string format && value != null)
        {
            return string.Format(format, value);
        }
        return value ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("StringFormatConverter.ConvertBack value={Value}", value);
        throw new NotImplementedException();
    }
}
