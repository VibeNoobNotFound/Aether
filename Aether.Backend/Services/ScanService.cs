using Aether.Protos;
using Aether.Backend.Plugins;
using Aether.Backend.Data;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Aether.Backend.Services;

public partial class AetherGrpcService
{
    public override async Task ScanLibrary(ScanRequest request, IServerStreamWriter<ScanProgress> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("Starting library scan (force_refresh={ForceRefresh})", request.ForceRefresh);

        var importers = _pluginManager.GetLibraryImporters().ToList();
        var metadataProviders = _pluginManager.GetMetadataProviders().ToList();
        int totalGamesFound = 0;

        foreach (var importer in importers)
        {
            if (!await importer.CanImportAsync())
            {
                _logger.LogInformation("Skipping {ImporterName} - not available", importer.Name);
                continue;
            }

            _logger.LogInformation("Scanning {ImporterName}...", importer.Name);

            var progress = new Progress<PluginSDK.Library.ScanProgress>(p =>
            {
                // Convert plugin ScanProgress to proto ScanProgress
                var protoProgress = new ScanProgress
                {
                    CurrentPlatform = p.CurrentPlatform,
                    GamesFound = p.GamesFound,
                    GamesProcessed = p.GamesProcessed,
                    CurrentGame = p.CurrentGame ?? "",
                    ProgressPercentage = p.ProgressPercentage
                };

                responseStream.WriteAsync(protoProgress).Wait();
            });

            await foreach (var importedGame in importer.ScanLibraryAsync(progress))
            {
                // Try to get metadata
                PluginSDK.Library.GameMetadata? metadata = null;
                var metadataProvider = metadataProviders.FirstOrDefault(p => p.Name == importer.Name);
                if (metadataProvider != null && !string.IsNullOrEmpty(importedGame.ExternalId))
                {
                    try
                    {
                        metadata = await metadataProvider.GetByIdAsync(importedGame.ExternalId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch metadata for {GameTitle}", importedGame.Title);
                    }
                }

                // Create entity and save to database
                var entity = GameEntity.FromImportedGame(importedGame, metadata);
                _database.UpsertGame(entity);

                totalGamesFound++;
            }
        }

        _logger.LogInformation("Library scan complete. Found {Count} games", totalGamesFound);

        // Send final status
        await responseStream.WriteAsync(new ScanProgress
        {
            CurrentPlatform = "Complete",
            GamesFound = totalGamesFound,
            GamesProcessed = totalGamesFound,
            CurrentGame = "",
            ProgressPercentage = 100
        });
    }

    public override async Task GetLibrary(Empty request, IServerStreamWriter<Game> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("Received GetLibrary request from {Peer}", context.Peer);

        var games = _database.GetAllGames().ToList();

        foreach (var entity in games)
        {
            var protoGame = new Game
            {
                Id = entity.Id.ToString(),
                Title = entity.Title,
                Platform = entity.Platform,
                ExternalId = entity.ExternalId ?? "",
                InstallPath = entity.InstallPath,
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

                // User stats
                IsFavorite = entity.IsFavorite,
                IsInstalled = entity.IsInstalled,
                TotalPlaytimeSeconds = (long)(entity.TotalPlaytime?.TotalSeconds ?? 0),

                // Timestamps
                ReleaseDateUnix = entity.ReleaseDate.HasValue ? new DateTimeOffset(entity.ReleaseDate.Value).ToUnixTimeSeconds() : 0,
                LastPlayedUnix = entity.LastPlayed.HasValue ? new DateTimeOffset(entity.LastPlayed.Value).ToUnixTimeSeconds() : 0,
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

            await responseStream.WriteAsync(protoGame);
            _logger.LogDebug("Streamed game: {GameTitle}", entity.Title);
        }

        _logger.LogInformation("Finished streaming {Count} games", games.Count);
    }
}
