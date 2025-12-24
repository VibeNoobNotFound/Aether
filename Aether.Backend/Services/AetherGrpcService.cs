using Aether.Protos;
using Aether.Backend.Plugins;
using Aether.Backend.Data;
using Grpc.Core;
using Microsoft.Extensions.Logging;

using Aether.PluginSDK;

namespace Aether.Backend.Services;

public partial class AetherGrpcService : AetherOrchestrator.AetherOrchestratorBase
{
    private readonly ILogger<AetherGrpcService> _logger;
    private readonly PluginManager _pluginManager;
    private readonly LibraryDatabase _database;

    public AetherGrpcService(
        ILogger<AetherGrpcService> logger,
        PluginManager pluginManager,
        LibraryDatabase database)
    {
        _logger = logger;
        _pluginManager = pluginManager;
        _database = database;
    }

    public override Task<PluginList> GetPlugins(Empty request, ServerCallContext context)
    {
        var response = new PluginList();

        // Add Library Importers
        foreach (var importer in _pluginManager.GetLibraryImporters())
        {
            response.Plugins.Add(new PluginInfo
            {
                Name = importer.Name,
                Version = importer.Version,
                Author = "Unknown",
                IsImporter = true,
                IsMetadataProvider = false
            });
        }

        // Add pure plugins if any (that aren't importers)
        foreach (var plugin in _pluginManager.GetPlugins())
        {
            // Simple de-duplication based on name
            var exists = response.Plugins.Any(p => p.Name == plugin.Name);
            if (!exists)
            {
                response.Plugins.Add(new PluginInfo
                {
                    Name = plugin.Name,
                    Version = "1.0.0",
                    Author = "Unknown",
                    IsImporter = false, // defaults
                    IsMetadataProvider = false
                });
            }
        }

        return Task.FromResult(response);
    }

    public override Task<WidgetList> GetSetupWidgets(PluginName request, ServerCallContext context)
    {
        var response = new WidgetList();

        // Search importers
        var importer = _pluginManager.GetLibraryImporters().FirstOrDefault(p => p.Name == request.Name);
        if (importer != null)
        {
            foreach (var widget in importer.GetSetupWidgets())
            {
                response.Widgets.Add(MapWidget(widget));
            }
            return Task.FromResult(response);
        }

        // Search plugins
        var plugin = _pluginManager.GetPlugins().FirstOrDefault(p => p.Name == request.Name);
        if (plugin != null)
        {
            foreach (var widget in plugin.GetSetupWidgets())
            {
                response.Widgets.Add(MapWidget(widget));
            }
            return Task.FromResult(response);
        }

        return Task.FromResult(response);
    }

    private static PluginWidget MapWidget(Aether.PluginSDK.UI.Widget widget)
    {
        return new PluginWidget
        {
            PluginId = widget.PluginId,
            Title = widget.Title,
            LayoutJson = widget.LayoutJson,
            SortOrder = widget.SortOrder
        };
    }


    public override async Task<OperationStatus> TriggerPluginAction(PluginAction request, ServerCallContext context)
    {
        try
        {
            // Resolve plugin
            IPlugin? plugin = _pluginManager.GetPlugins().FirstOrDefault(p => p.Name == request.PluginName);
            if (plugin == null)
            {
                // Try importers
                plugin = _pluginManager.GetLibraryImporters().FirstOrDefault(i => i.Name == request.PluginName);
            }

            if (plugin == null)
            {
                return new OperationStatus { Success = false, Message = $"Plugin '{request.PluginName}' not found." };
            }

            await plugin.OnWidgetAction(request.ActionId, request.PayloadJson);
            return new OperationStatus { Success = true, Message = "Action executed." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing plugin action");
            return new OperationStatus { Success = false, Message = ex.Message };
        }
    }



    public override Task<OperationStatus> ClearLibrary(Empty request, ServerCallContext context)
    {
        try
        {
            var count = _database.ClearAllGames();
            _logger.LogInformation("Library cleared. Removed {Count} games", count);
            return Task.FromResult(new OperationStatus { Success = true, Message = $"Cleared {count} games." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing library");
            return Task.FromResult(new OperationStatus { Success = false, Message = ex.Message });
        }
    }

    public override Task<OperationStatus> RemoveGame(GameId request, ServerCallContext context)
    {
        try
        {
            // Note: DB needs DeleteGame method
            var success = _database.DeleteGame(request.Id);
            return Task.FromResult(new OperationStatus
            {
                Success = success,
                Message = success ? "Game removed." : "Game not found."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing game");
            return Task.FromResult(new OperationStatus { Success = false, Message = ex.Message });
        }
    }

    public override Task<OperationStatus> ToggleFavorite(GameId request, ServerCallContext context)
    {
        try
        {
            // DB already has ToggleFavorite taking int, but ID is string?
            // Wait, Game entity uses int ID in LiteDB but string ID in Proto?
            // Need to verify ID mapping. Assuming proto ID matches what we stored.
            // Actually, proto ID is string. LiteDB internal ID is int. 
            // We should use ExternalId or GUID for string ID.
            // Let's assume for now we parse it if it's int, or use string ID if we migrated.
            // Upon checking LibraryDatabase, GetGameById takes 'int id'.
            // But Aether uses string IDs in proto.
            // We need to resolve this.

            if (int.TryParse(request.Id, out int dbId))
            {
                _database.ToggleFavorite(dbId);
                return Task.FromResult(new OperationStatus { Success = true, Message = "Favorite toggled." });
            }
            return Task.FromResult(new OperationStatus { Success = false, Message = "Invalid ID format." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling favorite");
            return Task.FromResult(new OperationStatus { Success = false, Message = ex.Message });
        }
    }

    public override Task<OperationStatus> OpenGameLocation(GameId request, ServerCallContext context)
    {
        try
        {
            if (int.TryParse(request.Id, out int dbId))
            {
                var game = _database.GetGameById(dbId);
                if (game != null && !string.IsNullOrEmpty(game.InstallPath))
                {
                    if (OperatingSystem.IsMacOS())
                    {
                        // Use -R to reveal in Finder instead of launching
                        var startInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "open",
                            Arguments = $"-R \"{game.InstallPath}\"",
                            UseShellExecute = false
                        };
                        System.Diagnostics.Process.Start(startInfo);
                    }
                    else if (OperatingSystem.IsWindows())
                    {
                        // explorer /select,path
                        System.Diagnostics.Process.Start("explorer", $"/select,\"{game.InstallPath}\"");
                    }
                    return Task.FromResult(new OperationStatus { Success = true, Message = "Location opened." });
                }
            }
            return Task.FromResult(new OperationStatus { Success = false, Message = "Game or path not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening location");
            return Task.FromResult(new OperationStatus { Success = false, Message = ex.Message });
        }
    }

    public override async Task<OperationStatus> UpdateGameMetadata(GameMetadataUpdate request, ServerCallContext context)
    {
        try
        {
            if (!int.TryParse(request.GameId, out int dbId))
            {
                return new OperationStatus { Success = false, Message = "Invalid game ID" };
            }

            var game = _database.GetGameById(dbId);
            if (game == null)
            {
                return new OperationStatus { Success = false, Message = "Game not found" };
            }

            // Update fields if provided
            if (request.HasTitle) game.Title = request.Title;
            if (request.HasDeveloper) game.Developer = request.Developer;
            if (request.HasPublisher) game.Publisher = request.Publisher;
            if (request.HasDescription) game.Description = request.Description;
            if (request.HasCoverImageUrl) game.CoverImageUrl = request.CoverImageUrl;
            if (request.HasBackgroundImageUrl) game.BackgroundImageUrl = request.BackgroundImageUrl;
            if (request.HasLogoImageUrl) game.LogoImageUrl = request.LogoImageUrl;
            if (request.Genres.Count > 0) game.Genres = request.Genres.ToList();
            if (request.Screenshots.Count > 0) game.Screenshots = request.Screenshots.ToList();
            if (request.Videos.Count > 0) game.Videos = request.Videos.ToList();
            if (request.HasReleaseDateUnix) game.ReleaseDate = DateTimeOffset.FromUnixTimeSeconds(request.ReleaseDateUnix).DateTime;
            if (request.HasSteamId) game.SteamId = request.SteamId;

            game.UpdatedAt = DateTime.UtcNow;
            _database.UpsertGame(game);

            _logger.LogInformation("Updated metadata for game: {Title}", game.Title);
            return new OperationStatus { Success = true, Message = "Metadata updated successfully" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating metadata");
            return new OperationStatus { Success = false, Message = ex.Message };
        }
    }

    public override async Task<MetadataSearchResponse> SearchMetadataProviders(MetadataSearchRequest request, ServerCallContext context)
    {
        var response = new MetadataSearchResponse();

        try
        {
            var providers = _pluginManager.GetMetadataProviders().ToList();

            // Filter by provider name if specified
            if (!string.IsNullOrEmpty(request.Provider))
            {
                providers = providers.Where(p => p.Name.Equals(request.Provider, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            foreach (var provider in providers)
            {
                try
                {
                    var metadata = await provider.SearchAsync(request.Query);
                    if (metadata != null)
                    {
                        var result = new MetadataSearchResult
                        {
                            Provider = provider.Name,
                            ExternalId = metadata.ExternalId ?? "",
                            Title = metadata.Title ?? request.Query,
                            Developer = metadata.Developer ?? "",
                            Publisher = metadata.Publisher ?? "",
                            Description = metadata.Description ?? "",
                            CoverImageUrl = metadata.CoverImageUrl ?? "",
                            LogoImageUrl = metadata.LogoImageUrl ?? "",
                            ReleaseYear = metadata.ReleaseDate?.Year ?? 0
                        };

                        // Add arrays
                        if (metadata.Videos != null) result.Videos.AddRange(metadata.Videos);
                        if (metadata.Screenshots != null) result.Screenshots.AddRange(metadata.Screenshots);
                        if (metadata.Genres != null) result.Genres.AddRange(metadata.Genres);

                        response.Results.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Search failed on provider {Provider}: {Error}", provider.Name, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching metadata providers");
        }

        return response;
    }

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
