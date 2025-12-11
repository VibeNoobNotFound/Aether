namespace Aether.PluginSDK.Library;

/// <summary>
/// Metadata provider interface for enriching games with additional information
/// </summary>
public interface IMetadataProvider
{
    string Name { get; } // "Steam", "IGDB", "Custom", etc.
    
    /// <summary>
    /// Search for game metadata by name
    /// </summary>
    Task<GameMetadata?> SearchAsync(string gameName, string? platform = null);
    
    /// <summary>
    /// Get metadata by external ID (e.g., Steam AppID)
    /// </summary>
    Task<GameMetadata?> GetByIdAsync(string gameId);
    
    /// <summary>
    /// Get additional media for a game
    /// </summary>
    Task<List<string>> GetScreenshotsAsync(string gameId);
    Task<List<Achievement>> GetAchievementsAsync(string gameId);
    Task<string?> GetBackgroundImageAsync(string gameId);
    Task<string?> GetLogoImageAsync(string gameId);
}
