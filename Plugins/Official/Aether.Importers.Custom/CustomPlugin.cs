using System.Text.Json;
using Aether.PluginSDK;
using Aether.PluginSDK.Library;

using Aether.PluginSDK.UI;

namespace Aether.Importers.Custom;

// ...




/// <summary>
/// Custom game importer for manually added games
/// Allows users to add any game and optionally fetch metadata from Steam
/// </summary>
public class CustomPlugin : ILibraryImporter, IMetadataProvider
{
    public string Name => "Custom";
    public string Author => "VibeNoobNotFound";
    public string Version => "1.0.4";

    public static class Constants
    {
        public const string FormId = "add_custom_game_form";
        public const string ActionAddGame = "add_game";

        public const string Title = "title";
        public const string InstallPath = "installPath";
        public const string ExecutablePath = "executablePath";
        public const string LaunchArguments = "launchArguments";
        public const string SteamId = "steamId";
    }

    // Custom importer works on all platforms
    public IEnumerable<string> SupportedPlatforms => Enumerable.Empty<string>();

    // Shows up in "Add Game" menu
    public bool SupportsManualAddition => true;

    // Logging
    private Serilog.ILogger? _logger;

    public void SetLogger(Serilog.ILogger logger)
    {
        _logger = logger;
        _logger.Information("CustomPlugin initialized");
    }

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
        _logger?.Information("Scanning Custom Games list ({Count} items)", _customGames.Count);
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

            _logger?.Debug("Yielding custom game: {Title}", customGame.Title);
            yield return new ImportedGame(
                customGame.Title,
                "Custom",
                customGame.SteamAppId ?? customGame.Title, // Use Steam ID if available
                customGame.InstallPath,
                customGame.ExecutablePath,
                LaunchArguments: customGame.LaunchArguments
            );
        }
    }

    // IMetadataProvider - Fetches from Steam if AppID is provided
    public async Task<List<GameMetadata>> SearchAsync(string gameName, string? platform = null)
    {
        // TODO: Implement Steam Web API search by game name
        // This would require Steam Web API key and search endpoint
        return new List<GameMetadata>();
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
    public static void AddCustomGame(string title, string installPath, string? executablePath = null, string? steamAppId = null, string? launchArguments = null)
    {
        _customGames.Add(new CustomGameEntry
        {
            Title = title,
            InstallPath = installPath,
            ExecutablePath = executablePath,
            SteamAppId = steamAppId,
            LaunchArguments = launchArguments
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
        return new GameMetadata
        {
            CoverImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{steamAppId}/library_600x900_2x.jpg",
            BackgroundImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{steamAppId}/library_hero.jpg",
            LogoImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{steamAppId}/logo.png"
        };
    }

    public List<Widget> GetPluginWidgets(WidgetLocation location)
    {
        if (location == WidgetLocation.LibraryAddMenu)
        {
            return new List<Widget>
            {
                WidgetBuilder.Form(Constants.FormId, "Add Game", Constants.ActionAddGame,
                    WidgetBuilder.TextInput(Constants.Title, "Game Title", required: true),
                    WidgetBuilder.FolderPicker(Constants.InstallPath, "Install Path", required: true),
                    WidgetBuilder.FilePicker(Constants.ExecutablePath, "Executable Path"),
                    WidgetBuilder.TextInput(Constants.LaunchArguments, "Launch Arguments"),
                    WidgetBuilder.TextInput(Constants.SteamId, "Steam App ID (Optional)", placeholder: "For metadata fetch")
                )
            };
        }

        return new List<Widget>(); // No settings for Custom plugin yet
    }

    // IPlugin Implementation stubs
    public List<Widget> GetWidgets(Game game) => new List<Widget>();

    public Task<WidgetActionResult> OnWidgetAction(string actionId, string payload)
    {
        if (actionId == Constants.ActionAddGame)
        {
            try
            {
                if (string.IsNullOrEmpty(payload))
                {
                    return Task.FromResult(WidgetActionResult.Fail("No payload provided"));
                }

                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(payload);
                if (data == null)
                {
                    return Task.FromResult(WidgetActionResult.Fail("Invalid payload"));
                }

                // Extract fields safely
                data.TryGetValue(Constants.Title, out var title);
                data.TryGetValue(Constants.InstallPath, out var installPath);
                data.TryGetValue(Constants.ExecutablePath, out var executablePath);
                data.TryGetValue(Constants.SteamId, out var steamId);
                data.TryGetValue(Constants.LaunchArguments, out var launchArguments);

                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(installPath))
                {
                    return Task.FromResult(WidgetActionResult.Fail("Title and Install Path are required"));
                }

                // Create the game to be added to the library
                var importedGame = new ImportedGame(
                    Title: title,
                    Platform: "Custom",
                    ExternalId: steamId ?? title, // Use Steam ID if provided, else title
                    InstallPath: installPath,
                    ExecutablePath: executablePath,
                    LaunchArguments: launchArguments
                );

                // Optionally fetch metadata if Steam ID is provided
                Dictionary<string, GameMetadata>? metadata = null;
                if (!string.IsNullOrEmpty(steamId))
                {
                    metadata = new Dictionary<string, GameMetadata>
                    {
                        [importedGame.ExternalId] = new GameMetadata
                        {
                            ExternalId = steamId,
                            CoverImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{steamId}/library_600x900_2x.jpg",
                            BackgroundImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{steamId}/library_hero.jpg",
                            LogoImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{steamId}/logo.png"
                        }
                    };
                }

                return Task.FromResult(WidgetActionResult.AddGames(
                    new List<ImportedGame> { importedGame },
                    metadata
                ));
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Error processing AddGame action");
                return Task.FromResult(WidgetActionResult.Fail($"Error: {ex.Message}"));
            }
        }

        return Task.FromResult(WidgetActionResult.Ok());
    }

    public Task OnLibraryScan(LibraryContext context) => Task.CompletedTask;
    public Task OnGameLaunched(Game game) => Task.CompletedTask;
    public Task OnGameStopped(Game game, TimeSpan sessionDuration) => Task.CompletedTask;
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
    public string? LaunchArguments { get; set; }
}
