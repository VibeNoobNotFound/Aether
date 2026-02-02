using Aether.WinUI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aether.WinUI.AttachedProperties;

public static class ImageCache
{
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

        if (string.IsNullOrEmpty(url))
        {
            image.Source = null;
            return;
        }

        // Optional: Set a placeholder or loading state here?
        // image.Opacity = 0.5; 

        try
        {
            var app = Application.Current as App;
            var service = app?.Services.GetService<ImageCacheService>();
            
            if (service == null) return;

            // TODO: Ideally we'd have CancellationToken handling here for rapid scrolling
            // For now, simple async load
            var bitmap = await service.GetImageAsync(url);
            
            // Verify the URL matches what we requested (handling race conditions)
            if (GetSource(image) == url)
            {
                image.Source = bitmap;
                // image.Opacity = 1.0;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ImageCache] Error loading {url}: {ex.Message}");
        }
    }
}
