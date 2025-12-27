using Aether.PluginSDK;
using Aether.PluginSDK.Library;
using Aether.PluginSDK.UI;
using System.Diagnostics;
namespace Aether.Importers.Steam;

/// <summary>
/// Steam library importer and metadata provider
/// </summary>
public class SteamPlugin : IPlugin, ILibraryImporter, IMetadataProvider, INewsProvider, IGameLauncher
{
    public string Name => "Steam";
    public string Author => "VibeNoobNotFound";
    public string Version => "1.0.0";

    public IEnumerable<string> SupportedPlatforms => Enumerable.Empty<string>(); // All platforms
    public bool SupportsManualAddition => false;

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

    private readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

    public async Task<GameMetadata?> SearchAsync(string gameName, string? platform = null)
    {
        try
        {
            var term = System.Web.HttpUtility.UrlEncode(gameName);
            var url = $"https://store.steampowered.com/api/storesearch/?term={term}&l=english&cc=US";
            var response = await _httpClient.GetStringAsync(url);

            using var doc = System.Text.Json.JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("items", out var items) && items.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var idElem))
                    {
                        var id = idElem.ToString();
                        // Put "id" back into standard flow to get full richness
                        return await GetByIdAsync(id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Steam search failed for {gameName}: {ex.Message}");
        }
        return null;
    }

    public async Task<GameMetadata?> GetByIdAsync(string gameId)
    {
        try
        {
            var url = $"https://store.steampowered.com/api/appdetails?appids={gameId}";
            var response = await _httpClient.GetStringAsync(url);

            // Parse JSON using System.Text.Json
            using var doc = System.Text.Json.JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty(gameId, out var appData))
            {
                if (appData.TryGetProperty("success", out var success) && success.GetBoolean())
                {
                    if (appData.TryGetProperty("data", out var data))
                    {
                        // Prepare Genre list
                        string[]? genresList = null;
                        if (data.TryGetProperty("genres", out var genres))
                        {
                            genresList = genres.EnumerateArray()
                                .Select(g => GetString(g, "description"))
                                .Where(s => !string.IsNullOrEmpty(s))
                                .Cast<string>()
                                .ToArray();
                        }

                        // Prepare Metacritic score
                        decimal? metacriticScore = null;
                        if (data.TryGetProperty("metacritic", out var metacritic))
                        {
                            if (metacritic.TryGetProperty("score", out var score))
                                metacriticScore = (decimal)score.GetInt32();
                        }

                        // Prepare Screenshots list
                        string[]? screenshotsList = null;
                        if (data.TryGetProperty("screenshots", out var screenshots))
                        {
                            screenshotsList = screenshots.EnumerateArray()
                                .Select(s => GetString(s, "path_full"))
                                .Where(s => !string.IsNullOrEmpty(s))
                                .Cast<string>()
                                .ToArray();
                        }

                        // Prepare Videos list
                        string[]? videosList = null;
                        if (data.TryGetProperty("movies", out var movies))
                        {
                            videosList = movies.EnumerateArray()
                                .Select(m =>
                                {
                                    // Try MP4 max quality first
                                    var url = GetString(m, "mp4", "max");
                                    // Fallback to HLS
                                    if (string.IsNullOrEmpty(url))
                                        url = GetString(m, "hls_h264");
                                    return url;
                                })
                                .Where(s => !string.IsNullOrEmpty(s))
                                .Cast<string>()
                                .ToArray();
                        }

                        var metadata = new GameMetadata
                        {
                            ExternalId = gameId,
                            Description = GetString(data, "detailed_description"),
                            ShortDescription = GetString(data, "short_description"),
                            Developer = GetFirstString(data, "developers"),
                            Publisher = GetFirstString(data, "publishers"),
                            ReleaseDate = ParseDate(GetString(data, "release_date", "date")),

                            // Images - Use portrait cover art (600x900), not wide header
                            // Steam library_600x900 is the proper vertical box art
                            CoverImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{gameId}/library_600x900_2x.jpg",
                            // Header image is wide and works well for backgrounds
                            BackgroundImageUrl = GetString(data, "header_image") ?? GetString(data, "background") ?? $"https://steamcdn-a.akamaihd.net/steam/apps/{gameId}/library_hero.jpg",
                            LogoImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{gameId}/logo.png",

                            // Assign pre-calculated values
                            Genres = genresList,
                            MetacriticScore = metacriticScore,
                            Screenshots = screenshotsList,
                            Videos = videosList
                        };

                        return metadata;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching Steam metadata for {gameId}: {ex.Message}");
        }

        // Fallback
        return new GameMetadata
        {
            CoverImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{gameId}/library_600x900_2x.jpg",
            BackgroundImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{gameId}/library_hero.jpg",
            LogoImageUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{gameId}/logo.png"
        };
    }

    private string? GetString(System.Text.Json.JsonElement element, string property, string? subProperty = null)
    {
        if (element.TryGetProperty(property, out var prop))
        {
            if (subProperty != null)
            {
                if (prop.TryGetProperty(subProperty, out var subProp))
                    return subProp.GetString();
            }
            else
            {
                if (prop.ValueKind == System.Text.Json.JsonValueKind.String)
                    return prop.GetString();
            }
        }
        return null;
    }

    private string? GetFirstString(System.Text.Json.JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var array) && array.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in array.EnumerateArray())
            {
                return item.GetString();
            }
        }
        return null;
    }

    private DateTime? ParseDate(string? dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return null;
        if (DateTime.TryParse(dateString, out var date)) return date;
        return null;
    }

    public async Task<List<string>> GetScreenshotsAsync(string gameId)
    {
        // TODO: Extract screens from Store API if needed
        return new List<string>();
    }

    public async Task<List<Achievement>> GetAchievementsAsync(string gameId)
    {
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

    public async Task<WidgetActionResult> OnWidgetAction(string actionId, string payload)
    {
        // Handle widget actions
        return WidgetActionResult.Ok();
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

    // IGameLauncher Implementation
    public bool CanLaunch(LaunchContext context)
    {
        // Can launch if it's a Steam game or has a valid Steam App ID
        return context.Platform == "Steam" ||
               (!string.IsNullOrEmpty(context.ExternalId) && int.TryParse(context.ExternalId, out _));
    }

    public Task<LaunchResult> LaunchAsync(LaunchContext context)
    {
        var uri = GetLaunchUri(context.ExternalId);
        if (string.IsNullOrEmpty(uri))
            return Task.FromResult(LaunchResult.Failed("Invalid Steam App ID"));

        if (!string.IsNullOrEmpty(context.LaunchArguments))
        {
            // Steam protocol: steam://run/<id>//<args>
            uri += $"//{context.LaunchArguments}";
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            };

            if (OperatingSystem.IsMacOS())
            {
                startInfo = new ProcessStartInfo("open", uri) { UseShellExecute = true };
            }
            else if (OperatingSystem.IsLinux())
            {
                startInfo = new ProcessStartInfo("xdg-open", uri) { UseShellExecute = true };
            }

            Process.Start(startInfo);
            return Task.FromResult(LaunchResult.Succeeded(processId: 0, method: "steam_protocol"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(LaunchResult.Failed(ex.Message));
        }
    }

    public string? GetLaunchUri(string externalId)
    {
        if (string.IsNullOrEmpty(externalId) || !int.TryParse(externalId, out _))
            return null;
        return $"steam://rungameid/{externalId}";
    }

    // INewsProvider Implementation
    public async Task<List<NewsItem>> GetNewsAsync(string gameId)
    {
        try
        {
            var url = $"https://api.steampowered.com/ISteamNews/GetNewsForApp/v0002/?appid={gameId}&count=5&format=json";
            var response = await _httpClient.GetStringAsync(url);

            using var doc = System.Text.Json.JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("appnews", out var appNews) &&
                appNews.TryGetProperty("newsitems", out var newsItems) &&
                newsItems.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var newsList = new List<NewsItem>();
                foreach (var item in newsItems.EnumerateArray())
                {
                    var newsItem = new NewsItem
                    {
                        Id = GetString(item, "gid") ?? Guid.NewGuid().ToString(),
                        Title = GetString(item, "title") ?? "News",
                        Url = GetString(item, "url") ?? "",
                        Author = GetString(item, "author") ?? GetString(item, "feedlabel") ?? "Steam",
                        ContentHtml = GetString(item, "contents") ?? "",
                        Source = "Steam"
                    };

                    if (item.TryGetProperty("date", out var dateElem))
                    {
                        newsItem.DateUnix = dateElem.GetInt64();
                    }

                    // Attempt to extract image from content
                    newsItem.ImageUrl = ExtractImageFromHtml(newsItem.ContentHtml);

                    newsList.Add(newsItem);
                }
                return newsList;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching news for {gameId}: {ex.Message}");
        }
        return new List<NewsItem>();
    }

    public async Task<List<NewsItem>> GetGeneralNewsAsync()
    {
        // Fetch news for Steam Client (753) as "General" news
        return await GetNewsAsync("753");
    }

    private string ExtractImageFromHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";

        // Simple regex to find src="..."
        var match = System.Text.RegularExpressions.Regex.Match(html, "src=\"(http[^\"]+)\"");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        // Fallback: look for [img] tags if Steam uses BBCode
        var bbMatch = System.Text.RegularExpressions.Regex.Match(html, @"\[img\](.*?)\[/img\]");
        if (bbMatch.Success)
        {
            return bbMatch.Groups[1].Value;
        }

        return "";
    }

    // Helper Methods
    private static List<string> GetPossibleSteamPaths()
    {
        var paths = new List<string>();

        if (OperatingSystem.IsMacOS())
        {
            // UserProfile may be empty when running in a sandboxed context
            // Use HOME environment variable as fallback
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home))
            {
                home = Environment.GetEnvironmentVariable("HOME") ?? "";
            }
            if (!string.IsNullOrEmpty(home))
            {
                paths.Add(Path.Combine(home, "Library", "Application Support", "Steam"));
            }
        }
        else if (OperatingSystem.IsWindows())
        {
            paths.Add(@"C:\Program Files (x86)\Steam");
            paths.Add(@"C:\Program Files\Steam");
        }
        else if (OperatingSystem.IsLinux())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home))
            {
                home = Environment.GetEnvironmentVariable("HOME") ?? "";
            }
            if (!string.IsNullOrEmpty(home))
            {
                paths.Add(Path.Combine(home, ".local", "share", "Steam"));
                paths.Add(Path.Combine(home, ".steam", "steam"));
            }
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

            DateTime? lastPlayed = null;

            foreach (var line in lines)
            {
                if (line.Contains("\"appid\""))
                    appId = ExtractValue(line);
                else if (line.Contains("\"name\""))
                    name = ExtractValue(line);
                else if (line.Contains("\"installdir\""))
                    installDir = ExtractValue(line);
                else if (line.Contains("\"LastPlayed\"")) // Note: Case sensitive check on key
                {
                    if (long.TryParse(ExtractValue(line), out long unixTime) && unixTime > 0)
                    {
                        lastPlayed = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;
                    }
                }
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
                null, // We'll detect executable later
                lastPlayed
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
