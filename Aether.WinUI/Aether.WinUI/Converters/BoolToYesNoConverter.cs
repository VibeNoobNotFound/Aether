using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Aether.WinUI.Converters;

public sealed class BoolToYesNoConverter : IValueConverter
{
    private static ILogger<BoolToYesNoConverter> Logger =>
        Ioc.Default.GetService<ILogger<BoolToYesNoConverter>>() ?? NullLogger<BoolToYesNoConverter>.Instance;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("BoolToYesNoConverter.Convert value={Value}", value);
        return value is bool b ? (b ? "Yes" : "No") : "No";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("BoolToYesNoConverter.ConvertBack value={Value}", value);
        return value;
    }
}
