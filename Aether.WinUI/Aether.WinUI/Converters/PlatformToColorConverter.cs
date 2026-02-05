using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace Aether.WinUI.Converters;

public class PlatformToColorConverter : IValueConverter
{
    private static ILogger<PlatformToColorConverter> Logger =>
        Ioc.Default.GetService<ILogger<PlatformToColorConverter>>() ?? NullLogger<PlatformToColorConverter>.Instance;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("PlatformToColorConverter.Convert value={Value}", value);
        if (value is string platform)
        {
            var color = GetColorForPlatform(platform);
            return new SolidColorBrush(color);
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("PlatformToColorConverter.ConvertBack value={Value}", value);
        throw new NotImplementedException();
    }

    private Color GetColorForPlatform(string platform)
    {
        Logger.LogTrace("PlatformToColorConverter.GetColorForPlatform platform={Platform}", platform);
        return platform.ToLowerInvariant() switch
        {
            "steam" => Color.FromArgb(255, 0, 122, 255), // Blue
            "epic games" or "epic" => Color.FromArgb(255, 175, 82, 222), // Purple
            "app store" => Color.FromArgb(255, 50, 173, 230), // Cyan
            "gog" => Color.FromArgb(255, 255, 59, 48), // Red
            "crossover" => Color.FromArgb(255, 255, 204, 0), // Yellow
            _ => Color.FromArgb(255, 142, 142, 147) // Gray
        };
    }
}
