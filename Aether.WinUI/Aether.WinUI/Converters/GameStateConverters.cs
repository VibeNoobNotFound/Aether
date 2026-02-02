using global::Aether.Protos;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Aether.WinUI.Converters;

public class GameStateToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is GameState state)
        {
            var targetState = GameState.Running;
            if (parameter is string paramStr && Enum.TryParse<GameState>(paramStr, out var parsed))
            {
                targetState = parsed;
            }

            if (state == targetState) return Visibility.Visible;

            // "Active" parameter means either launching or running
            if (parameter as string == "Active" && (state == GameState.Launching || state == GameState.Running))
            {
                return Visibility.Visible;
            }
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

public class GameStateToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is GameState state)
        {
            // return true if Launching
            return state == GameState.Launching;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
