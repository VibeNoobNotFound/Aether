using global::Aether.Protos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Aether.WinUI.Converters;

public class GameStateToVisibilityConverter : IValueConverter
{
    private static ILogger<GameStateToVisibilityConverter> Logger =>
        Ioc.Default.GetService<ILogger<GameStateToVisibilityConverter>>() ?? NullLogger<GameStateToVisibilityConverter>.Instance;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("GameStateToVisibilityConverter.Convert value={Value} parameter={Parameter}", value, parameter);
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
    private static ILogger<GameStateToBoolConverter> Logger =>
        Ioc.Default.GetService<ILogger<GameStateToBoolConverter>>() ?? NullLogger<GameStateToBoolConverter>.Instance;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("GameStateToBoolConverter.Convert value={Value}", value);
        if (value is GameState state)
        {
            // return true if Launching
            return state == GameState.Launching;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
