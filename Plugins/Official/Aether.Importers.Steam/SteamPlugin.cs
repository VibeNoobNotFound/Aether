using Aether.PluginSDK;
using Aether.PluginSDK.Library;

namespace Aether.Importers.Steam;

/// <summary>
/// Steam library importer and metadata provider
/// </summary>
public class SteamPlugin : IPlugin, ILibraryImporter, IMetadataProvider
{
    public string Name => "Steam";
    public string Version => "1.0.0";

    // ILibraryImporter Implementation
    public async Task<bool> CanImportAsync()
    {
        var steamPaths = GetPossibleSteamPaths();
        return steamPaths.Any(Directory.Exists);
    }

    public async IAsyncEnumerable<ImportedGame> ScanLibraryAsync(IProgress<ScanProgress>? progress = null)
    {
        var steamPaths = GetPossibleSteamPaths();
        var foundPath = steamPaths.FirstOrDefault(Directory.Exists);

        if (foundPath == null)
            yield break;

        var libraryFoldersPath = Path.Combine(foundPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(libraryFoldersPath))
            yield break;

        var libraryFolders = ParseLibraryFolders(libraryFoldersPath);
        int totalProcessed = 0;

        foreach (var folder in libraryFolders)
        {
            var manifestsPath = Path.Combine(folder, "steamapps");
            if (!Directory.Exists(manifestsPath))
                continue;

            var manifestFiles = Directory.GetFiles(manifestsPath, "appmanifest_*.acf");

            foreach (var manifestFile in manifestFiles)
            {
                var game = ParseAppManifest(manifestFile);
                if (game != null)
                {
                    totalProcessed++;
                    progress?.Report(new ScanProgress(
                        "Steam",
                        totalProcessed,
                        totalProcessed,
                        game.Title,
                        0
                    ));

                    yield return game;
                }
            }
        }
    }

    // IMetadataProvider Implementation
    public async Task<GameMetadata?> SearchAsync(string gameName, string? platform = null)
    {
        // For now, return minimal metadata
        // TODO: Implement SteamKit2 API calls
        return null;
    }

    public async Task<GameMetadata?> GetByIdAsync(string gameId)
    {
        // TODO: Fetch from Steam API using SteamKit2
        var coverUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{gameId}/library_600x900_2x.jpg";

        return new GameMetadata
        {
            CoverImageUrl = coverUrl,
            BackgroundImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{gameId}/library_hero.jpg",
            LogoImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{gameId}/logo.png"
        };
    }

    public async Task<List<string>> GetScreenshotsAsync(string gameId)
    {
        // TODO: Implement via SteamKit2
        return new List<string>();
    }

    public async Task<List<Achievement>> GetAchievementsAsync(string gameId)
    {
        // TODO: Implement via SteamKit2 ISteamUserStats
        return new List<Achievement>();
    }

    public async Task<string?> GetBackgroundImageAsync(string gameId)
    {
        return $"https://steamcdn-a.akamaihd.net/steam/apps/{gameId}/library_hero.jpg";
    }

    public async Task<string?> GetLogoImageAsync(string gameId)
    {
        return $"https://steamcdn-a.akamaihd.net/steam/apps/{gameId}/logo.png";
    }

    // IPlugin Hooks
    public async Task OnLibraryScan(LibraryContext context)
    {
        // Hook for additional scan logic
    }

    public List<Aether.PluginSDK.UI.Widget> GetWidgets(Aether.PluginSDK.Game game)
    {
        // Return empty for now - could add Steam-specific widgets later
        return new List<Aether.PluginSDK.UI.Widget>();
    }

    public async Task OnWidgetAction(string actionId, string payload)
    {
        // Handle widget actions
    }

    public async Task OnGameLaunched(Aether.PluginSDK.Game game)
    {
        // Track launch via Steam API if needed
    }

    public async Task OnGameStopped(Aether.PluginSDK.Game game, TimeSpan sessionDuration)
    {
        // Track playtime
    }

    public List<Aether.PluginSDK.UI.Widget> GetSetupWidgets()
    {
        return new List<Aether.PluginSDK.UI.Widget>();
    }

    // Helper Methods
    private static List<string> GetPossibleSteamPaths()
    {
        var paths = new List<string>();

        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            paths.Add(Path.Combine(home, "Library", "Application Support", "Steam"));
        }
        else if (OperatingSystem.IsWindows())
        {
            paths.Add(@"C:\Program Files (x86)\Steam");
            paths.Add(@"C:\Program Files\Steam");
        }
        else if (OperatingSystem.IsLinux())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            paths.Add(Path.Combine(home, ".local", "share", "Steam"));
            paths.Add(Path.Combine(home, ".steam", "steam"));
        }

        return paths;
    }

    private static List<string> ParseLibraryFolders(string vdfPath)
    {
        var folders = new List<string>();

        try
        {
            var lines = File.ReadAllLines(vdfPath);
            foreach (var line in lines)
            {
                if (line.Contains("\"path\""))
                {
                    // Extract path from: "path"		"/path/to/steam"
                    var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var path = parts[1].Trim('"');
                        if (Directory.Exists(path))
                        {
                            folders.Add(path);
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return folders;
    }

    private static ImportedGame? ParseAppManifest(string manifestPath)
    {
        try
        {
            var content = File.ReadAllText(manifestPath);
            var lines = content.Split('\n');

            string? appId = null;
            string? name = null;
            string? installDir = null;

            foreach (var line in lines)
            {
                if (line.Contains("\"appid\""))
                    appId = ExtractValue(line);
                else if (line.Contains("\"name\""))
                    name = ExtractValue(line);
                else if (line.Contains("\"installdir\""))
                    installDir = ExtractValue(line);
            }

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(name))
                return null;

            var manifestDir = Path.GetDirectoryName(manifestPath)!;
            var libraryRoot = Path.GetDirectoryName(manifestDir)!;
            var fullInstallPath = Path.Combine(libraryRoot, "steamapps", "common", installDir ?? name);

            return new ImportedGame(
                name,
                "Steam",
                appId,
                fullInstallPath,
                null // We'll detect executable later
            );
        }
        catch
        {
            return null;
        }
    }

    private static string ExtractValue(string line)
    {
        var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return parts[1].Trim('"');
        }
        return string.Empty;
    }
}
