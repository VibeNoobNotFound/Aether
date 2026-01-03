using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Aether.PluginSDK;
using Aether.PluginSDK.Library;
using Aether.PluginSDK.UI;

namespace Aether.Importers.Web;

/// <summary>
/// Web Importer to add URL shortcuts or Steam Protocols
/// </summary>
public class WebPlugin : ILibraryImporter, IGameLauncher
{
    public string Name => "Web";
    public string Author => "VibeNoobNotFound";
    public string Version => "1.0.0";

    public IEnumerable<string> SupportedPlatforms => Enumerable.Empty<string>(); // All
    public bool SupportsManualAddition => true;

    // Persist added web games
    private static readonly List<ImportedGame> _webGames = new();

    public async Task<bool> CanImportAsync()
    {
        return true;
    }

    public async IAsyncEnumerable<ImportedGame> ScanLibraryAsync(IProgress<ScanProgress>? progress = null)
    {
        int processed = 0;
        foreach (var game in _webGames)
        {
            processed++;
            progress?.Report(new ScanProgress("Web", _webGames.Count, processed, game.Title, 0));
            yield return game;
        }
    }

    public List<Widget> GetPluginWidgets(WidgetLocation location)
    {
        if (location == WidgetLocation.LibraryAddMenu)
        {
            return new List<Widget>
            {
                WidgetBuilder.Form("add_web_game_form", "Add Link", "add_web_game",
                    WidgetBuilder.TextInput("name", "Name", required: true),
                    WidgetBuilder.TextInput("url", "URL", required: true, placeholder: "https://..."),
                    WidgetBuilder.TextInput("steam_id", "Steam ID (Optional - for News/Data)", placeholder: "440"),
                    WidgetBuilder.TextInput("imageUrl", "Image URL (Optional)")
                )
            };
        }

        return new List<Widget>();
    }

    public Task<WidgetActionResult> OnWidgetAction(string actionId, string payload)
    {
        if (actionId == "add_web_game")
        {
            try
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(payload);
                if (data == null) return Task.FromResult(WidgetActionResult.Fail("Invalid data"));

                data.TryGetValue("name", out var name);
                data.TryGetValue("url", out var inputUrl);
                data.TryGetValue("steam_id", out var steamId);
                data.TryGetValue("imageUrl", out var imageUrl);

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(inputUrl))
                    return Task.FromResult(WidgetActionResult.Fail("Name and URL are required"));

                // Logic:
                // Url is always the executable path for launching.
                // SteamId (if present) becomes the ExternalId for metadata/news lookups.
                // Platform is "Web" so we handle launching via URL, but we need to ensure news fetching works.

                string finalUrl = inputUrl;
                if (!inputUrl.Contains("://") && !inputUrl.StartsWith("http"))
                {
                    finalUrl = "https://" + inputUrl;
                }

                string externalId = !string.IsNullOrEmpty(steamId) ? steamId : finalUrl;

                var game = new ImportedGame(
                    Title: name,
                    Platform: "Web",
                    ExternalId: externalId,
                    InstallPath: "",
                    ExecutablePath: finalUrl,
                    LaunchArguments: steamId // Store SteamID in args as backup or for context
                );

                _webGames.Add(game);

                // Return immediate addition result
                // We can construct metadata manually if ImageURL was provided
                Dictionary<string, GameMetadata>? metadata = null;
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    metadata = new Dictionary<string, GameMetadata>
                    {
                        [externalId] = new GameMetadata
                        {
                            CoverImageUrl = imageUrl,
                            BackgroundImageUrl = imageUrl
                        }
                    };
                }

                return Task.FromResult(WidgetActionResult.AddGames(new List<ImportedGame> { game }, metadata));
            }
            catch (Exception ex)
            {
                return Task.FromResult(WidgetActionResult.Fail(ex.Message));
            }
        }
        return Task.FromResult(WidgetActionResult.Ok());
    }

    // IGameLauncher
    public bool CanLaunch(LaunchContext context)
    {
        // We handle games with platform "Web" or if executable path is a URL
        return context.Platform == "Web" ||
               (!string.IsNullOrEmpty(context.ExecutablePath) &&
               (context.ExecutablePath.StartsWith("http") || context.ExecutablePath.StartsWith("steam://")));
    }

    public async Task<LaunchResult> LaunchAsync(LaunchContext context)
    {
        try
        {
            var url = context.ExecutablePath;
            if (string.IsNullOrEmpty(url)) return LaunchResult.Failed("No URL provided");

            var startInfo = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true // Required to open URLs on Windows/Shell
            };

            // On macOS/Linux, UseShellExecute might behave differently for URLs in newer .NET
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                startInfo = new ProcessStartInfo("open", url) { UseShellExecute = true };
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                startInfo = new ProcessStartInfo("xdg-open", url) { UseShellExecute = true };
            }

            Process.Start(startInfo);
            return LaunchResult.Succeeded(processId: 0, method: "url");
        }
        catch (Exception ex)
        {
            return LaunchResult.Failed($"Error opening URL: {ex.Message}");
        }
    }

    // IPlugin Stubs
    public List<Widget> GetWidgets(Game game) => new List<Widget>();
    public Task OnLibraryScan(LibraryContext context) => Task.CompletedTask;
    public Task OnGameLaunched(Game game) => Task.CompletedTask;
    public Task OnGameStopped(Game game, TimeSpan sessionDuration) => Task.CompletedTask;
    public string? GetLaunchUri(string externalId)
    {
        // For web games, external ID matches the URL/SteamID
        if (externalId.StartsWith("http") || externalId.StartsWith("steam://"))
            return externalId;
        return null;
    }
}
