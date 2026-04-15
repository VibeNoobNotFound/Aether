using Aether.WinUI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aether.WinUI.AttachedProperties;

public static class ImageCache
{
    private static ILogger Logger =>
        Ioc.Default.GetService<ILoggerFactory>()?.CreateLogger("ImageCache")
        ?? NullLogger.Instance;

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.RegisterAttached(
            "Source",
            typeof(string),
            typeof(ImageCache),
            new PropertyMetadata(null, OnSourceChanged));

    public static string GetSource(DependencyObject obj)
    {
        return (string)obj.GetValue(SourceProperty);
    }

    public static void SetSource(DependencyObject obj, string value)
    {
        obj.SetValue(SourceProperty, value);
    }

    private static async void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Image image) return;

        var url = e.NewValue as string;
        Logger.LogDebug("ImageCache Source changed: {Url}", url);

        if (string.IsNullOrEmpty(url))
        {
            image.Source = null;
            return;
        }

        // Optional: Set a placeholder or loading state here?
        // image.Opacity = 0.5; 

        try
        {
            var service = Ioc.Default.GetService<ImageCacheService>();
            
            if (service == null) return;

            // TODO: Ideally we'd have CancellationToken handling here for rapid scrolling
            // For now, simple async load
            var bitmap = await service.GetImageAsync(url);
            
            // Verify the URL matches what we requested (handling race conditions)
            if (GetSource(image) == url)
            {
                Logger.LogDebug("ImageCache applied image: {Url}", url);
                image.Source = bitmap;
                // image.Opacity = 1.0;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "ImageCache error loading {Url}", url);
        }
    }
}
