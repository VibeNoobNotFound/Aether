using global::Aether.Protos;

namespace Aether.WinUI.Models;

public record NewsItemType(string Id, string Title, string Description, string ImageUrl, string Link, long PublishedAt)
{
    public static NewsItemType FromProto(NewsItem proto)
    {
        return new NewsItemType(
            proto.Id,
            proto.Title,
            proto.ContentHtml, // Description -> ContentHtml
            proto.ImageUrl,
            proto.Url, // Link -> Url
            proto.DateUnix // PublishedAt -> DateUnix
        );
    }
}
