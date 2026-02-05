using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Aether.WinUI.Services;

public class ImageCacheService
{
    private static readonly ConcurrentDictionary<string, BitmapImage> _memoryCache = new();
    private static readonly string _cacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aether", "Cache", "Images");
    private readonly ILogger<ImageCacheService> _logger;

    public ImageCacheService(ILogger<ImageCacheService> logger)
    {
        _logger = logger;
        Directory.CreateDirectory(_cacheFolder);
        _logger.LogDebug("ImageCacheService initialized. Cache folder: {Folder}", _cacheFolder);
    }

    public async Task<BitmapImage?> GetImageAsync(string? url)
    {
        _logger.LogTrace("GetImageAsync url={Url}", url);
        if (string.IsNullOrEmpty(url)) return null;

        // Check memory cache
        if (_memoryCache.TryGetValue(url, out var cachedImage))
        {
            _logger.LogTrace("Image cache hit (memory) for {Url}", url);
            return cachedImage;
        }

        // HACK: for local protocol (file://), return directly
        if (url.StartsWith("file://") || File.Exists(url)) 
        {
             var uri = new Uri(url);
             var bitmap = new BitmapImage(uri);
             _memoryCache.TryAdd(url, bitmap);
             _logger.LogTrace("Image cache hit (local) for {Url}", url);
             return bitmap;
        }

        // Check disk cache
        var filename = GetSafeFilename(url);
        var filepath = Path.Combine(_cacheFolder, filename);

        if (File.Exists(filepath))
        {
            var bitmap = new BitmapImage(new Uri(filepath));
            _memoryCache.TryAdd(url, bitmap);
            _logger.LogTrace("Image cache hit (disk) for {Url}", url);
            return bitmap;
        }

        // Download
        try
        {
            using var client = new HttpClient();
            var data = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(filepath, data);
            
            var bitmap = new BitmapImage(new Uri(filepath));
            _memoryCache.TryAdd(url, bitmap);
            _logger.LogTrace("Image downloaded and cached for {Url}", url);
            return bitmap;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download image {Url}", url);
            return null;
        }
    }

    private string GetSafeFilename(string url)
    {
        _logger.LogTrace("GetSafeFilename url={Url}", url);
        var invalid = Path.GetInvalidFileNameChars();
        foreach (var c in invalid)
        {
            url = url.Replace(c, '_');
        }
        return url + ".jpg"; // simplified
    }
}
