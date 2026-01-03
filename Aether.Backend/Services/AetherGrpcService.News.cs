using Aether.Protos;
using Aether.Backend.Plugins;
using Aether.PluginSDK;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Aether.Backend.Services;

public partial class AetherGrpcService
{
    public override async Task<NewsList> GetGameNews(GameId request, ServerCallContext context)
    {
        var response = new NewsList();
        try
        {
            if (!int.TryParse(request.Id, out int dbId))
            {
                _logger.LogWarning("GetGameNews: Invalid game ID format: {Id}", request.Id);
                return response;
            }

            var game = _database.GetGameById(dbId);
            if (game == null)
            {
                _logger.LogWarning("GetGameNews: Game not found with ID: {Id}", dbId);
                return response;
            }

            _logger.LogInformation("GetGameNews: Fetching news for {Title} (Platform: {Platform}, ExternalId: {ExternalId}, SteamId: {SteamId})",
                game.Title, game.Platform, game.ExternalId, game.SteamId);

            // Find valid news providers - collect from both sources and deduplicate
            var providers = new List<INewsProvider>();
            providers.AddRange(_pluginManager.GetPlugins().OfType<INewsProvider>());
            providers.AddRange(_pluginManager.GetLibraryImporters().OfType<INewsProvider>());
            providers = providers.Distinct().ToList();

            _logger.LogInformation("GetGameNews: Found {Count} news providers", providers.Count);

            INewsProvider? provider = null;
            string? newsId = null;

            // PRIORITY 1: SteamId override - Always use Steam provider if SteamId is set
            if (!string.IsNullOrEmpty(game.SteamId))
            {
                provider = providers.FirstOrDefault(p => (p as IPlugin)?.Name == "Steam");
                newsId = game.SteamId;
                if (provider != null)
                {
                    _logger.LogInformation("GetGameNews: Using SteamId override for news: {SteamId}", game.SteamId);
                }
            }

            // PRIORITY 2: Match by platform
            if (provider == null)
            {
                foreach (var p in providers)
                {
                    var pluginName = (p as IPlugin)?.Name;
                    if (pluginName == game.Platform)
                    {
                        provider = p;
                        newsId = game.ExternalId;
                        break;
                    }
                }
            }

            // PRIORITY 3: If no platform match but we have a numeric ExternalId, try Steam provider
            if (provider == null && !string.IsNullOrEmpty(game.ExternalId) && int.TryParse(game.ExternalId, out _))
            {
                provider = providers.FirstOrDefault(p => (p as IPlugin)?.Name == "Steam");
                newsId = game.ExternalId;
                if (provider != null)
                {
                    _logger.LogInformation("GetGameNews: Using Steam provider for numeric ExternalId");
                }
            }

            if (provider != null && !string.IsNullOrEmpty(newsId))
            {
                _logger.LogInformation("GetGameNews: Using provider {Name} to fetch news for ID: {NewsId}",
                    (provider as IPlugin)?.Name, newsId);

                var news = await provider.GetNewsAsync(newsId);
                _logger.LogInformation("GetGameNews: Received {Count} news items", news?.Count ?? 0);

                if (news != null)
                {
                    foreach (var n in news)
                    {
                        response.News.Add(new Aether.Protos.NewsItem
                        {
                            Id = n.Id ?? "",
                            Title = n.Title ?? "",
                            Url = n.Url ?? "",
                            ContentHtml = n.ContentHtml ?? "",
                            Author = n.Author ?? "",
                            DateUnix = n.DateUnix,
                            ImageUrl = n.ImageUrl ?? "",
                            Source = n.Source ?? ""
                        });
                    }
                }
            }
            else
            {
                _logger.LogWarning("GetGameNews: No news provider found for platform: {Platform}", game.Platform);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching game news");
        }
        return response;
    }


    public override async Task<NewsList> GetGeneralNews(Empty request, ServerCallContext context)
    {
        var response = new NewsList();
        try
        {
            var providers = _pluginManager.GetPlugins().OfType<INewsProvider>().ToList();
            providers.AddRange(_pluginManager.GetLibraryImporters().OfType<INewsProvider>());

            var allNews = new List<Aether.Protos.NewsItem>();

            foreach (var provider in providers)
            {
                try
                {
                    var news = await provider.GetGeneralNewsAsync();
                    foreach (var n in news)
                    {
                        allNews.Add(new Aether.Protos.NewsItem
                        {
                            Id = n.Id,
                            Title = n.Title,
                            Url = n.Url,
                            ContentHtml = n.ContentHtml,
                            Author = n.Author,
                            DateUnix = n.DateUnix,
                            ImageUrl = n.ImageUrl,
                            Source = n.Source
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch general news from provider");
                }
            }

            // Sort by date descending and take top 20
            var sortedNews = allNews.OrderByDescending(n => n.DateUnix).Take(20);
            response.News.AddRange(sortedNews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching general news");
        }
        return response;
    }
}
