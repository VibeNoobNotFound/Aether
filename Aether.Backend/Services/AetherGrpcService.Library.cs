using Aether.Protos;
using Aether.Backend.Plugins;
using Aether.Backend.Data;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Aether.Backend.Services;

public partial class AetherGrpcService
{
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

    public override Task<OperationStatus> ResetSystem(Empty request, ServerCallContext context)
    {
        try
        {
            _logger.LogWarning("Performing FACTORY RESET via gRPC");
            _database.FactoryReset();
            return Task.FromResult(new OperationStatus { Success = true, Message = "Factory reset complete." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing factory reset");
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
            if (request.HasLaunchArguments) game.LaunchArguments = request.LaunchArguments;
            if (request.HasMetacriticScore) game.MetacriticScore = request.MetacriticScore;

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

    public override async Task ScanLibrary(ScanRequest request, IServerStreamWriter<ScanProgress> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("Starting library scan (force_refresh={ForceRefresh})", request.ForceRefresh);

        var importers = _pluginManager.GetLibraryImporters().ToList();
        // Fetch metadata priority
        var metadataConfig = _database.GetMetadataConfig();
        var priorityList = metadataConfig?.ProviderPriority ?? new List<string>();

        // Default if empty
        if (priorityList.Count == 0)
        {
            priorityList = new List<string> { "Steam", "IGDB" };
        }

        var metadataProviders = _pluginManager.GetMetadataProviders()
            .OrderBy(p =>
            {
                var index = priorityList.IndexOf(p.Name);
                return index == -1 ? int.MaxValue : index;
            })
            .ToList();
        int totalGamesFound = 0;

        foreach (var importer in importers)
        {
            try
            {
                if (!await importer.CanImportAsync())
                {
                    _logger.LogInformation("Skipping {ImporterName} - not available", importer.Name);
                    continue;
                }

                _logger.LogInformation("Scanning {ImporterName}...", importer.Name);

                var progress = new Progress<PluginSDK.Library.ScanProgress>(p =>
                {
                // This callback is for generic progress (scanning stages)
                // Actual game discovery is handled in the loop below
                var protoProgress = new ScanProgress
                    {
                        CurrentPlatform = p.CurrentPlatform,
                        GamesFound = p.GamesFound,
                        GamesProcessed = p.GamesProcessed,
                        CurrentGame = p.CurrentGame ?? "",
                        ProgressPercentage = (float)p.ProgressPercentage
                    };

                    try { responseStream.WriteAsync(protoProgress).Wait(); } catch { /* Ignore write errors during scan */ }
                });

                await foreach (var importedGame in importer.ScanLibraryAsync(progress))
                {
                    try
                    {
                        // Try to get metadata
                        PluginSDK.Library.GameMetadata? metadata = null;
                        string? metadataSourceProvider = null;  // Track which provider gave us metadata
                        string? metadataExternalId = null;      // Track the external ID from that provider

                        var metadataProvider = metadataProviders.FirstOrDefault(p => p.Name == importer.Name);
                        if (metadataProvider != null && !string.IsNullOrEmpty(importedGame.ExternalId))
                        {
                            try
                            {
                                metadata = await metadataProvider.GetByIdAsync(importedGame.ExternalId);
                                if (metadata != null)
                                {
                                    metadataSourceProvider = metadataProvider.Name;
                                    metadataExternalId = importedGame.ExternalId;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to fetch metadata for {GameTitle} from {Provider}", importedGame.Title, importer.Name);
                            }
                        }

                        // Fallback: If metadata is missing/incomplete, try searching other providers
                        if (metadata == null)
                        {
                            foreach (var provider in metadataProviders)
                            {
                                if (provider == metadataProvider) continue; // Already tried by ID

                                try
                                {
                                    var searchResults = await provider.SearchAsync(importedGame.Title);
                                    var searchResult = searchResults.FirstOrDefault();

                                    if (searchResult != null)
                                    {
                                        metadata = searchResult;
                                        metadataSourceProvider = provider.Name;
                                        // ExternalId from search result (if metadata has it)
                                        metadataExternalId = searchResult.ExternalId;
                                        _logger.LogInformation("Found metadata for {GameTitle} via {Provider} search",
                                            importedGame.Title, provider.Name);
                                        break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning("Fallback search failed on {Provider}: {Message}", provider.Name, ex.Message);
                                }
                            }
                        }

                        // Create entity and update database
                        var entity = GameEntity.FromImportedGame(importedGame, metadata);

                        // Set SteamId based on source: if Steam was the provider (either direct or fallback), use that ID
                        if (metadataSourceProvider == "Steam" && !string.IsNullOrEmpty(metadataExternalId))
                        {
                            entity.SteamId = metadataExternalId;
                            _logger.LogInformation("Set SteamId={SteamId} for {GameTitle} from {Provider}",
                                metadataExternalId, entity.Title, metadataSourceProvider);
                        }

                        _database.UpsertGame(entity);

                        // Stream the found game to the client immediately
                        var foundGameProto = MapToProto(entity);
                        await responseStream.WriteAsync(new ScanProgress
                        {
                            CurrentPlatform = importer.Name,
                            GamesFound = totalGamesFound + 1,
                            GamesProcessed = totalGamesFound + 1,
                            CurrentGame = entity.Title,
                            ProgressPercentage = (float)((double)totalGamesFound / 100 * 100),
                            FoundGame = foundGameProto
                        });

                        totalGamesFound++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing game {Title} from {Importer}", importedGame.Title, importer.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CRITICAL: Importer {ImporterName} failed to scan", importer.Name);

                // Inform client of error but don't crash
                await responseStream.WriteAsync(new ScanProgress
                {
                    CurrentPlatform = importer.Name,
                    CurrentStatus = $"Error: {ex.Message}"
                });
            }
        }

        _logger.LogInformation("Library scan complete. Found {Count} games", totalGamesFound);

        // Send final status
        await responseStream.WriteAsync(new ScanProgress
        {
            CurrentStatus = "Complete",
            PercentComplete = 100
        });
    }

    public override async Task GetLibrary(Empty request, IServerStreamWriter<Game> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("Received GetLibrary request from {Peer}", context.Peer);

        var games = _database.GetAllGames().ToList();

        foreach (var entity in games)
        {
            var protoGame = MapToProto(entity);
            await responseStream.WriteAsync(protoGame);
            _logger.LogDebug("Streamed game: {GameTitle}", entity.Title);
        }

        _logger.LogInformation("Finished streaming {Count} games", games.Count);
    }

    public override Task<LibrarySearchResponse> SearchLibrary(LibrarySearchRequest request, ServerCallContext context)
    {
        try
        {
            var (games, totalMatches) = _database.SearchLibrary(
                request.Query,
                request.FilterPlatforms.ToList(),
                request.FilterGenres.ToList(),
                (LibraryDatabase.SortOption)request.SortBy, // Map Proto enum to Database Enum
                request.SortAscending,
                request.Limit > 0 ? request.Limit : 50
            );

            var response = new LibrarySearchResponse
            {
                TotalMatches = totalMatches
            };
            response.Games.AddRange(games.Select(MapToProto));

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching library");
            // Return empty result on error
            return Task.FromResult(new LibrarySearchResponse());
        }
    }

    private static Game MapToProto(GameEntity entity)
    {
        var protoGame = new Game
        {
            Id = entity.Id.ToString(),
            Title = entity.Title,
            Platform = entity.Platform,
            ExternalId = entity.ExternalId ?? "",
            InstallPath = entity.InstallPath ?? "",
            ExecutablePath = entity.ExecutablePath ?? "",

            // Images
            CoverImageUrl = entity.CoverImageUrl ?? "",
            BackgroundImageUrl = entity.BackgroundImageUrl ?? "",
            LogoImageUrl = entity.LogoImageUrl ?? "",

            // Description
            Description = entity.Description ?? "",
            ShortDescription = entity.ShortDescription ?? "",
            Developer = entity.Developer ?? "",
            Publisher = entity.Publisher ?? "",

            // Features
            HasAchievements = entity.HasAchievements,
            AchievementCount = entity.AchievementCount ?? 0,
            HasMultiplayer = entity.HasMultiplayer,
            HasSinglePlayer = entity.HasSinglePlayer,
            HasCloudSaves = entity.HasCloudSaves,

            // Launch Args
            LaunchArguments = entity.LaunchArguments ?? "",

            // User stats
            IsFavorite = entity.IsFavorite,
            IsInstalled = entity.IsInstalled,
            TotalPlaytimeSeconds = (long)(entity.TotalPlaytime?.TotalSeconds ?? 0),
            MetacriticScore = (int)(entity.MetacriticScore ?? 0),

            // Timestamps
            ReleaseDateUnix = entity.ReleaseDate.HasValue ? new DateTimeOffset(entity.ReleaseDate.Value).ToUnixTimeSeconds() : 0,
            LastPlayedUnix = entity.LastPlayed.HasValue ? new DateTimeOffset(entity.LastPlayed.Value).ToUnixTimeSeconds() : 0,

            // Requirements
            MinimumRequirements = entity.MinimumRequirements ?? "",
            RecommendedRequirements = entity.RecommendedRequirements ?? "",

            // Cross-Platform News
            SteamId = entity.SteamId ?? ""
        };

        // Add arrays
        if (entity.Screenshots != null)
            protoGame.Screenshots.AddRange(entity.Screenshots);
        if (entity.Videos != null)
            protoGame.Videos.AddRange(entity.Videos);
        if (entity.Genres != null)
            protoGame.Genres.AddRange(entity.Genres);
        if (entity.Tags != null)
            protoGame.Tags.AddRange(entity.Tags);
        if (entity.Categories != null)
            protoGame.Categories.AddRange(entity.Categories);
        if (entity.SupportedLanguages != null)
            protoGame.SupportedLanguages.AddRange(entity.SupportedLanguages);

        return protoGame;
    }
}
