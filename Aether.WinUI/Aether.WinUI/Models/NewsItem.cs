using global::Aether.Protos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace Aether.WinUI.Models;

public sealed class NewsItemViewModel
{
    private static ILogger<NewsItemViewModel> Logger =>
        (Ioc.Default.GetService<ILogger<NewsItemViewModel>>()) ?? NullLogger<NewsItemViewModel>.Instance;
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string Url { get; init; } = "";
    public string ContentHtml { get; init; } = "";
    public string Author { get; init; } = "";
    public long DateUnix { get; init; }
    public string ImageUrl { get; init; } = "";
    public string Source { get; init; } = "";

    public DateTimeOffset PublishedDate => DateUnix > 0
        ? DateTimeOffset.FromUnixTimeSeconds(DateUnix)
        : DateTimeOffset.MinValue;

    public string PublishedDateText => PublishedDate == DateTimeOffset.MinValue
        ? "Unknown"
        : PublishedDate.ToString("MMM dd, yyyy");

    public static NewsItemViewModel FromProto(NewsItem proto)
    {
        Logger.LogDebug("NewsItemViewModel.FromProto: {NewsId}", proto.Id);
        return new NewsItemViewModel
        {
            Id = proto.Id,
            Title = proto.Title,
            Url = proto.Url,
            ContentHtml = proto.ContentHtml,
            Author = proto.Author,
            DateUnix = proto.DateUnix,
            ImageUrl = proto.ImageUrl,
            Source = proto.Source
        };
    }
}
