using Aether.PluginSDK;
using Aether.PluginSDK.Library;

namespace Aether.Importers.Custom;

/// <summary>
/// Custom game importer for manually added games
/// Allows users to add any game and optionally fetch metadata from Steam
/// </summary>
public class CustomPlugin : ILibraryImporter, IMetadataProvider
{
    public string Name => "Custom";
    public string Version => "1.0.0";

    // In-memory storage for custom games
    // In production, this would be persisted separately or use a service
    private static readonly List<CustomGameEntry> _customGames = new();

    public async Task<bool> CanImportAsync()
    {
        // Custom importer is always available
        return true;
    }

    public async IAsyncEnumerable<ImportedGame> ScanLibraryAsync(IProgress<ScanProgress>? progress = null)
    {
        int processed = 0;

        foreach (var customGame in _customGames)
        {
            processed++;
            progress?.Report(new ScanProgress(
                "Custom",
                _customGames.Count,
                processed,
                customGame.Title,
                (double)processed / _customGames.Count * 100
            ));

            yield return new ImportedGame(
                customGame.Title,
                "Custom",
                customGame.SteamAppId ?? customGame.Title, // Use Steam ID if available
                customGame.InstallPath,
                customGame.ExecutablePath
            );
        }
    }

    // IMetadataProvider - Fetches from Steam if AppID is provided
    public async Task<GameMetadata?> SearchAsync(string gameName, string? platform = null)
    {
        // TODO: Implement Steam Web API search by game name
        // This would require Steam Web API key and search endpoint
        return null;
    }

    public async Task<GameMetadata?> GetByIdAsync(string gameId)
    {
        // If gameId looks like a Steam AppID (numeric), fetch from Steam
        if (int.TryParse(gameId, out var steamAppId))
        {
            return await FetchSteamMetadataAsync(steamAppId.ToString());
        }

        return null;
    }

    public async Task<List<string>> GetScreenshotsAsync(string gameId)
    {
        // TODO: Fetch from Steam API if gameId is Steam AppID
        return new List<string>();
    }

    public async Task<List<Achievement>> GetAchievementsAsync(string gameId)
    {
        // TODO: Fetch from Steam API if gameId is Steam AppID
        return new List<Achievement>();
    }

    public async Task<string?> GetBackgroundImageAsync(string gameId)
    {
        if (int.TryParse(gameId, out _))
        {
            return $"https://steamcdn-a.akamaihd.net/steam/apps/{gameId}/library_hero.jpg";
        }
        return null;
    }

    public async Task<string?> GetLogoImageAsync(string gameId)
    {
        if (int.TryParse(gameId, out _))
        {
            return $"https://steamcdn-a.akamaihd.net/steam/apps/{gameId}/logo.png";
        }
        return null;
    }

    /// <summary>
    /// Add a custom game to the library
    /// </summary>
    public static void AddCustomGame(string title, string installPath, string? executablePath = null, string? steamAppId = null)
    {
        _customGames.Add(new CustomGameEntry
        {
            Title = title,
            InstallPath = installPath,
            ExecutablePath = executablePath,
            SteamAppId = steamAppId
        });
    }

    /// <summary>
    /// Remove a custom game
    /// </summary>
    public static bool RemoveCustomGame(string title)
    {
        var game = _customGames.FirstOrDefault(g => g.Title == title);
        if (game != null)
        {
            _customGames.Remove(game);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Get all custom games
    /// </summary>
    public static IReadOnlyList<CustomGameEntry> GetCustomGames() => _customGames.AsReadOnly();

    private async Task<GameMetadata?> FetchSteamMetadataAsync(string steamAppId)
    {
        // For now, just return CDN URLs
        // TODO: Implement full Steam Web API integration (requires API key)
        // API endpoint: https://store.steampowered.com/api/appdetails?appids={appId}

        return new GameMetadata
        {
            CoverImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{steamAppId}/library_600x900_2x.jpg",
            BackgroundImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{steamAppId}/library_hero.jpg",
            LogoImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{steamAppId}/logo.png"
        };
    }
}

/// <summary>
/// Represents a manually added custom game entry
/// </summary>
public class CustomGameEntry
{
    public string Title { get; set; } = string.Empty;
    public string InstallPath { get; set; } = string.Empty;
    public string? ExecutablePath { get; set; }
    public string? SteamAppId { get; set; } // Optional: Link to Steam for metadata
}
