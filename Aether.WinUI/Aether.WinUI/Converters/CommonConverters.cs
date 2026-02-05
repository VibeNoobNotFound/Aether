using Aether.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Aether.WinUI.Converters;

public class ConnectionStateToVisibilityConverter : IValueConverter
{
    private static ILogger<ConnectionStateToVisibilityConverter> Logger =>
        Ioc.Default.GetService<ILogger<ConnectionStateToVisibilityConverter>>() ?? NullLogger<ConnectionStateToVisibilityConverter>.Instance;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("ConnectionStateToVisibilityConverter.Convert value={Value}", value);
        if (value is ConnectionState state)
        {
            // Show if NOT connected (i.e. Connecting, Error, Disconnected)
            return state != ConnectionState.Connected ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("ConnectionStateToVisibilityConverter.ConvertBack value={Value}", value);
        throw new NotImplementedException();
    }
}

public class ConnectionStateToBoolConverter : IValueConverter
{
    private static ILogger<ConnectionStateToBoolConverter> Logger =>
        Ioc.Default.GetService<ILogger<ConnectionStateToBoolConverter>>() ?? NullLogger<ConnectionStateToBoolConverter>.Instance;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("ConnectionStateToBoolConverter.Convert value={Value}", value);
        if (value is ConnectionState state)
        {
            // True if Connecting
            return state == ConnectionState.Connecting;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("ConnectionStateToBoolConverter.ConvertBack value={Value}", value);
        throw new NotImplementedException();
    }
}

public class ErrorStateToVisibilityConverter : IValueConverter
{
    private static ILogger<ErrorStateToVisibilityConverter> Logger =>
        Ioc.Default.GetService<ILogger<ErrorStateToVisibilityConverter>>() ?? NullLogger<ErrorStateToVisibilityConverter>.Instance;

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("ErrorStateToVisibilityConverter.Convert value={Value}", value);
        if (value is ConnectionState state)
        {
            // Visible only on Error
            return state == ConnectionState.Error ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        Logger.LogTrace("ErrorStateToVisibilityConverter.ConvertBack value={Value}", value);
        throw new NotImplementedException();
    }
}
