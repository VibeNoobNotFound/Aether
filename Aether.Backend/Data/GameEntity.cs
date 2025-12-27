using LiteDB;
using Aether.PluginSDK.Library;

namespace Aether.Backend.Data;

/// <summary>
/// Persistent game entity with comprehensive metadata
/// </summary>
public class GameEntity
{
    [BsonId]
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Platform { get; set; } = "";
    public string ExternalId { get; set; } = "";
    public string LaunchArguments { get; set; } = "";

    // Paths
    public string InstallPath { get; set; } = string.Empty;
    public string? ExecutablePath { get; set; }

    // Images & Media
    public string? CoverImageUrl { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public string? LogoImageUrl { get; set; }
    public List<string>? Screenshots { get; set; }
    public List<string>? Videos { get; set; }

    // Description & Classification
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public List<string>? Genres { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Categories { get; set; }

    // People & Dates
    public string? Developer { get; set; }
    public string? Publisher { get; set; }
    public DateTime? ReleaseDate { get; set; }

    // Ratings & Reviews
    public decimal? MetacriticScore { get; set; }
    public decimal? UserScore { get; set; }
    public int? ReviewCount { get; set; }

    // Features
    public bool HasAchievements { get; set; }
    public int? AchievementCount { get; set; }
    public bool HasMultiplayer { get; set; }
    public bool HasSinglePlayer { get; set; }
    public bool HasCloudSaves { get; set; }

    // System Requirements
    public string? MinimumRequirements { get; set; }
    public string? RecommendedRequirements { get; set; }
    public List<string>? SupportedLanguages { get; set; }

    // User Stats
    public DateTime? LastPlayed { get; set; }
    public TimeSpan? TotalPlaytime { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsInstalled { get; set; }

    // Timestamps
    public DateTime ImportedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Cross-Platform News: Steam App ID for fetching news regardless of platform
    public string? SteamId { get; set; }

    public static GameEntity FromImportedGame(ImportedGame game, GameMetadata? metadata = null)
    {
        return new GameEntity
        {
            Title = game.Title,
            Platform = game.Platform,
            ExternalId = game.ExternalId,
            InstallPath = game.InstallPath,
            ExecutablePath = game.ExecutablePath,
            IsInstalled = Directory.Exists(game.InstallPath),

            // Metadata
            CoverImageUrl = metadata?.CoverImageUrl,
            BackgroundImageUrl = metadata?.BackgroundImageUrl,
            LogoImageUrl = metadata?.LogoImageUrl,
            Description = metadata?.Description,
            ShortDescription = metadata?.ShortDescription,
            Genres = metadata?.Genres?.ToList(),
            Tags = metadata?.Tags?.ToList(),
            Categories = metadata?.Categories?.ToList(),
            Developer = metadata?.Developer,
            Publisher = metadata?.Publisher,
            ReleaseDate = metadata?.ReleaseDate,
            Screenshots = metadata?.Screenshots?.ToList(),
            Videos = metadata?.Videos?.ToList(),
            MetacriticScore = metadata?.MetacriticScore,
            UserScore = metadata?.UserScore,
            ReviewCount = metadata?.ReviewCount,
            HasAchievements = metadata?.HasAchievements ?? false,
            AchievementCount = metadata?.AchievementCount,
            HasMultiplayer = metadata?.HasMultiplayer ?? false,
            HasSinglePlayer = metadata?.HasSinglePlayer ?? false,
            HasCloudSaves = metadata?.HasCloudSaves ?? false,
            MinimumRequirements = metadata?.MinimumRequirements,
            RecommendedRequirements = metadata?.RecommendedRequirements,
            SupportedLanguages = metadata?.SupportedLanguages?.ToList(),

            LastPlayed = game.LastPlayed,
            TotalPlaytime = game.SecondsPlayed.HasValue ? TimeSpan.FromSeconds(game.SecondsPlayed.Value) : null,

            // Auto-populate SteamId for Steam games
            SteamId = game.Platform == "Steam" ? game.ExternalId : null,

            ImportedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
