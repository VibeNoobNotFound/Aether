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

    public List<Widget> GetSetupWidgets()
    {
        return new List<Widget>
        {
            new Widget
            {
                PluginId = Name,
                Title = "Add Web Link / Steam ID",
                SortOrder = 2,
                LayoutJson = @"
                {
                    ""type"": ""Form"",
                    ""fields"": [
                        { ""id"": ""name"", ""type"": ""Text"", ""label"": ""Name"", ""required"": true },
                        { ""id"": ""url"", ""type"": ""Text"", ""label"": ""URL or Steam ID"", ""required"": true, ""placeholder"": ""https://... or 440"" },
                        { ""id"": ""imageUrl"", ""type"": ""Text"", ""label"": ""Image URL (Optional)"", ""required"": false }
                    ],
                    ""actions"": [
                        { ""id"": ""add_web_game"", ""label"": ""Add Link"", ""actionType"": ""submit"" }
                    ]
                }"
            }
        };
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
                data.TryGetValue("imageUrl", out var imageUrl);

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(inputUrl))
                    return Task.FromResult(WidgetActionResult.Fail("Name and URL are required"));

                // Convert Steam ID to Protocol URL
                string finalUrl = inputUrl;
                string platform = "Web";
                string externalId = inputUrl;

                if (int.TryParse(inputUrl, out _))
                {
                    // It's a Steam ID
                    finalUrl = $"steam://run/{inputUrl}";
                    platform = "Steam"; // Launch via Steam
                    externalId = inputUrl;
                }
                else if (!inputUrl.Contains("://"))
                {
                    // Default to https if no protocol
                    finalUrl = "https://" + inputUrl;
                }

                var game = new ImportedGame(
                    Title: name,
                    Platform: platform,
                    ExternalId: externalId,
                    InstallPath: "",
                    ExecutablePath: finalUrl
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
