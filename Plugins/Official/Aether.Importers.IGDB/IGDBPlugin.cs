using System.Text.Json;
using Aether.PluginSDK;
using Aether.PluginSDK.Library;
using Aether.PluginSDK.Storage;
using Aether.PluginSDK.UI;
using IGDB;
using IGDBGame = IGDB.Models.Game;
using IGDBImageSize = IGDB.ImageSize;

namespace Aether.Importers.IGDB;

/// <summary>
/// IGDB metadata provider using the official igdb-dotnet library.
/// Implements IStorageAware for credential persistence.
/// </summary>
public class IGDBPlugin : IPlugin, IMetadataProvider, IStorageAware, Aether.PluginSDK.Logging.ILoggingAware
{
    public string Name => "IGDB";
    public string Author => "VibeNoobNotFound";
    public string Version => "2.0.0";

    // Logging
    private Serilog.ILogger? _logger;

    public void SetLogger(Serilog.ILogger logger)
    {
        _logger = logger;
        _logger.Information("IGDBPlugin initialized");
    }

    public static class Constants
    {
        public const string ClientId = "twitch_client_id";
        public const string ClientSecret = "twitch_client_secret";
        public const string ActionTestAuth = "test_twitch_auth";
        public const string ActionSaveCredentials = "save_twitch_credentials";
    }

    public IEnumerable<string> SupportedPlatforms => Enumerable.Empty<string>(); // All platforms

    private IPluginStorage? _storage;
    private IGDBClient? _client;

    // IStorageAware implementation
    public void SetStorage(IPluginStorage storage)
    {
        _storage = storage;
        _ = InitializeClientAsync(); // Fire and forget
    }

    private async Task InitializeClientAsync()
    {
        if (_storage == null) return;

        try
        {
            var creds = await _storage.LoadAsync<TwitchCredentials>("credentials");
            if (creds != null && !string.IsNullOrEmpty(creds.ClientId) && !string.IsNullOrEmpty(creds.ClientSecret))
            {
                _client = new IGDBClient(
                    creds.ClientId,
                    creds.ClientSecret,
                    new PluginTokenStore(_storage)
                );
                _logger?.Information("IGDB client initialized from stored credentials.");
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Failed to initialize IGDB client");
        }
    }

    // IMetadataProvider Implementation
    // IMetadataProvider Implementation
    public async Task<List<GameMetadata>> SearchAsync(string gameName, string? platform = null)
    {
        var results = new List<GameMetadata>();
        if (_client == null)
            return results;

        _logger?.Debug("Searching IGDB for: {Name}", gameName);
        try
        {
            var games = await _client.QueryAsync<IGDBGame>(
                IGDBClient.Endpoints.Games,
                $"search \"{EscapeQuery(gameName)}\"; fields name,summary,cover.*,first_release_date,involved_companies.company.name,involved_companies.developer,involved_companies.publisher,genres.name,screenshots.*,videos.*; limit 10;"
            );

            if (games != null)
            {
                _logger?.Debug("Found {Count} results from IGDB", games.Length);
                foreach (var game in games)
                {
                    results.Add(MapToMetadata(game));
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "IGDB search failed for {Name}", gameName);
        }
        return results;
    }

    public async Task<GameMetadata?> GetByIdAsync(string gameId)
    {
        if (_client == null || !int.TryParse(gameId, out var id))
            return null;

        try
        {
            var games = await _client.QueryAsync<IGDBGame>(
                IGDBClient.Endpoints.Games,
                $"where id = {id}; fields name,summary,storyline,cover.*,first_release_date,involved_companies.company.name,genres.name,screenshots.*,videos.*; limit 1;"
            );

            var game = games.FirstOrDefault();
            return game != null ? MapToMetadata(game) : null;
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "IGDB fetch failed for {Id}", gameId);
            return null;
        }
    }

    public Task<List<string>> GetScreenshotsAsync(string gameId) => Task.FromResult(new List<string>());
    public Task<List<Achievement>> GetAchievementsAsync(string gameId) => Task.FromResult(new List<Achievement>());
    public Task<string?> GetBackgroundImageAsync(string gameId) => Task.FromResult<string?>(null);
    public Task<string?> GetLogoImageAsync(string gameId) => Task.FromResult<string?>(null);

    // IPlugin Implementation
    public List<Widget> GetPluginWidgets(WidgetLocation location)
    {
        if (location == WidgetLocation.Settings)
        {
            return new List<Widget>
            {
                WidgetBuilder.Section("Twitch API Credentials", "Required for IGDB metadata. Get credentials at dev.twitch.tv",
                    WidgetBuilder.TextInput(Constants.ClientId, "Client ID", placeholder: "Enter your Twitch Client ID"),
                    WidgetBuilder.TextInput(Constants.ClientSecret, "Client Secret", placeholder: "Enter your Twitch Client Secret", secure: true),
                    WidgetBuilder.Row(
                        WidgetBuilder.Button("Test Connection", Constants.ActionTestAuth),
                        WidgetBuilder.PrimaryButton("Save Credentials", Constants.ActionSaveCredentials)
                    )
                )
            };
        }
        return new List<Widget>();
    }

    public List<Widget> GetWidgets(Aether.PluginSDK.Game game) => new List<Widget>();

    public async Task<WidgetActionResult> OnWidgetAction(string actionId, string payload)
    {
        if (_storage == null)
            return WidgetActionResult.Fail("Storage not initialized");

        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(payload);
            if (data == null)
                return WidgetActionResult.Fail("Invalid payload");

            var clientId = data.GetValueOrDefault(Constants.ClientId);
            var clientSecret = data.GetValueOrDefault(Constants.ClientSecret);

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
                return WidgetActionResult.Fail("Client ID and Secret are required");

            if (actionId == Constants.ActionSaveCredentials)
            {
                await _storage.SaveAsync("credentials", new TwitchCredentials
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                });

                // Reinitialize client with new credentials
                _client = new IGDBClient(
                    clientId,
                    clientSecret,
                    new PluginTokenStore(_storage)
                );

                Console.WriteLine("IGDB credentials saved.");
                return WidgetActionResult.Ok("Credentials saved successfully!");
            }
            else if (actionId == Constants.ActionTestAuth)
            {
                _logger?.Information("Testing IGDB credentials...");
                // Create temp client to test
                var testClient = new IGDBClient(
                    clientId,
                    clientSecret,
                    new PluginTokenStore(_storage)
                );

                // Try a simple query
                var result = await testClient.QueryAsync<IGDBGame>(
                    IGDBClient.Endpoints.Games,
                    "fields name; limit 1;"
                );

                if (result != null)
                {
                    _logger?.Information("IGDB authentication successful!");
                    return WidgetActionResult.Ok("Authentication successful!");
                }
                else
                {
                    _logger?.Warning("IGDB authentication failed - no response");
                    return WidgetActionResult.Fail("Authentication failed - no response");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex, "Widget action failed: {Action}", actionId);
            return WidgetActionResult.Fail(ex.Message);
        }

        return WidgetActionResult.Ok();
    }

    public Task OnLibraryScan(LibraryContext context) => Task.CompletedTask;
    public Task OnGameLaunched(Aether.PluginSDK.Game game) => Task.CompletedTask;
    public Task OnGameStopped(Aether.PluginSDK.Game game, TimeSpan sessionDuration) => Task.CompletedTask;

    // Helper Methods
    private static string EscapeQuery(string input)
    {
        return input.Replace("\"", "\\\"");
    }

    private GameMetadata MapToMetadata(IGDBGame game)
    {
        // Cover URL
        string? coverUrl = null;
        if (game.Cover?.Value?.ImageId != null)
        {
            coverUrl = ImageHelper.GetImageUrl(
                imageId: game.Cover.Value.ImageId,
                size: IGDBImageSize.CoverBig,
                retina: false
            );
        }

        // Screenshots
        string[]? screenshots = null;
        if (game.Screenshots?.Values != null)
        {
            screenshots = game.Screenshots.Values
                .Where(s => s.ImageId != null)
                .Select(s => ImageHelper.GetImageUrl(
                    imageId: s.ImageId!,
                    size: IGDBImageSize.ScreenshotBig,
                    retina: false
                ))
                .ToArray();
        }

        // Genres
        string[]? genres = null;
        if (game.Genres?.Values != null)
        {
            genres = game.Genres.Values
                .Where(g => !string.IsNullOrEmpty(g.Name))
                .Select(g => g.Name!)
                .ToArray();
        }

        // Developer & Publisher
        string? developer = null;
        string? publisher = null;
        if (game.InvolvedCompanies?.Values != null)
        {
            var devCompany = game.InvolvedCompanies.Values.FirstOrDefault(c => c.Developer == true);
            var pubCompany = game.InvolvedCompanies.Values.FirstOrDefault(c => c.Publisher == true);

            developer = devCompany?.Company?.Value?.Name;
            publisher = pubCompany?.Company?.Value?.Name;
        }

        // Videos
        string[]? videos = null;
        if (game.Videos?.Values != null)
        {
            videos = game.Videos.Values
                .Where(v => !string.IsNullOrEmpty(v.VideoId))
                .Select(v => $"https://www.youtube.com/watch?v={v.VideoId}")
                .ToArray();
        }

        // Release date
        DateTime? releaseDate = game.FirstReleaseDate?.DateTime;

        return new GameMetadata
        {
            ExternalId = game.Id.ToString(),
            Title = game.Name ?? "Unknown",
            Description = game.Summary,
            ShortDescription = game.Storyline,
            CoverImageUrl = coverUrl,
            BackgroundImageUrl = screenshots?.FirstOrDefault(), // Use first screenshot as background
            Genres = genres,
            Screenshots = screenshots,
            Videos = videos,
            Developer = developer,
            Publisher = publisher,
            ReleaseDate = releaseDate
        };
    }
}

