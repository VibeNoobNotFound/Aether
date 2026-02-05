using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aether.WinUI.Models;

public sealed class MediaItemViewModel
{
    private static ILogger<MediaItemViewModel> Logger =>
        (Ioc.Default.GetService<ILogger<MediaItemViewModel>>()) ?? NullLogger<MediaItemViewModel>.Instance;

    public string Url { get; }
    public bool IsVideo { get; }

    public MediaItemViewModel(string url, bool isVideo)
    {
        Url = url;
        IsVideo = isVideo;
        Logger.LogDebug("MediaItemViewModel created: {Url} IsVideo={IsVideo}", url, isVideo);
    }
}
