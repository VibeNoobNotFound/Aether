namespace Aether.PluginSDK.Library;

/// <summary>
/// Comprehensive game metadata matching Steam's data model
/// </summary>
public record GameMetadata
{
    // Identifier (from the metadata provider, e.g., Steam App ID)
    public string? ExternalId { get; init; }
    public string? Title { get; init; }

    // Basic Info
    public string? CoverImageUrl { get; init; }
    public string? BackgroundImageUrl { get; init; }
    public string? LogoImageUrl { get; init; }
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    
    // Classification
    public string[]? Genres { get; init; }
    public string[]? Tags { get; init; }
    public string[]? Categories { get; init; }
    
    // People & Dates
    public string? Developer { get; init; }
    public string? Publisher { get; init; }
    public DateTime? ReleaseDate { get; init; }
    
    // Media
    public string[]? Screenshots { get; init; }
    public string[]? Videos { get; init; }
    
    // Ratings & Stats
    public decimal? MetacriticScore { get; init; }
    public decimal? UserScore { get; init; }
    public int? ReviewCount { get; init; }
    
    // Features
    public bool HasAchievements { get; init; }
    public int? AchievementCount { get; init; }
    public bool HasMultiplayer { get; init; }
    public bool HasSinglePlayer { get; init; }
    public bool HasCloudSaves { get; init; }
    
    // System Requirements
    public string? MinimumRequirements { get; init; }
    public string? RecommendedRequirements { get; init; }
    public string[]? SupportedLanguages { get; init; }
}

public record Achievement(
    string Name,
    string Description,
    string IconUrl,
    string IconGrayUrl,
    bool IsHidden,
    decimal GlobalUnlockPercentage
);
