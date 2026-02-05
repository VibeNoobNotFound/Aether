using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using System;

namespace Aether.WinUI.Converters;

public class BoolToEyeIconConverter : IValueConverter
{
    private static ILogger<BoolToEyeIconConverter> Logger =>
        Ioc.Default.GetService<ILogger<BoolToEyeIconConverter>>() ?? NullLogger<BoolToEyeIconConverter>.Instance;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("BoolToEyeIconConverter.Convert value={Value}", value);
        if (value is bool isVisible && isVisible)
        {
            return "\uE7B3"; // Eye Open (Redial)
        }
        return "\uED1A"; // Eye Hide (Hide)
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("BoolToEyeIconConverter.ConvertBack value={Value}", value);
        throw new NotImplementedException();
    }
}
