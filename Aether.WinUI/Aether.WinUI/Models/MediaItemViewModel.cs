namespace Aether.WinUI.Models;

public sealed class MediaItemViewModel
{
    public string Url { get; }
    public bool IsVideo { get; }

    public MediaItemViewModel(string url, bool isVideo)
    {
        Url = url;
        IsVideo = isVideo;
    }
}
