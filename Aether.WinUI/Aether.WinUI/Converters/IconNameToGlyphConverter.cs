using Aether.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Aether.WinUI.Converters;

public sealed class IconNameToGlyphConverter : IValueConverter
{
    private static ILogger<IconNameToGlyphConverter> Logger =>
        Ioc.Default.GetService<ILogger<IconNameToGlyphConverter>>() ?? NullLogger<IconNameToGlyphConverter>.Instance;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("IconNameToGlyphConverter.Convert value={Value} parameter={Parameter}", value, parameter);
        var iconName = value as string ?? string.Empty;
        var fallback = parameter as string ?? "\uE8A5";

        var mapper = Ioc.Default.GetService<IconMapService>();
        return mapper?.ToGlyph(iconName, fallback) ?? fallback;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("IconNameToGlyphConverter.ConvertBack value={Value}", value);
        // One-way mapping only
        return value;
    }
}
