using global::Aether.Protos;
using System;

namespace Aether.WinUI.Models;

public sealed class NewsItemViewModel
{
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
