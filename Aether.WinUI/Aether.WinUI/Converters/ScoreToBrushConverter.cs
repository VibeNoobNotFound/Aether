using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Aether.WinUI.Converters;

public sealed class ScoreToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
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
        return value;
    }
}
