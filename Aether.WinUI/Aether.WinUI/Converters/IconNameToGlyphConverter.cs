using Aether.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Data;
using System;

namespace Aether.WinUI.Converters;

public sealed class IconNameToGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var iconName = value as string ?? string.Empty;
        var fallback = parameter as string ?? "\uE8A5";

        var app = App.Current;
        var mapper = app?.Services.GetService<IconMapService>();
        return mapper?.ToGlyph(iconName, fallback) ?? fallback;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        // One-way mapping only
        return value;
    }
}
