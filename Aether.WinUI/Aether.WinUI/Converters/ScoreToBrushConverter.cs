using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Aether.WinUI.Converters;

public sealed class ScoreToBrushConverter : IValueConverter
{
    private static ILogger<ScoreToBrushConverter> Logger =>
        Ioc.Default.GetService<ILogger<ScoreToBrushConverter>>() ?? NullLogger<ScoreToBrushConverter>.Instance;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("ScoreToBrushConverter.Convert value={Value}", value);
        if (value is double score)
        {
            if (score >= 75)
            {
                return new SolidColorBrush(Colors.LawnGreen);
            }
            if (score >= 50)
            {
                return new SolidColorBrush(Colors.Gold);
            }
            if (score > 0)
            {
                return new SolidColorBrush(Colors.OrangeRed);
            }
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("ScoreToBrushConverter.ConvertBack value={Value}", value);
        return value;
    }
}
