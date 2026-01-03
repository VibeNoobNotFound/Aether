using System.Text.Json;
using Aether.PluginSDK;
using Aether.PluginSDK.Library;
using Aether.PluginSDK.UI;

namespace Aether.Importers.IGDB;

/// <summary>
/// IGDB metadata provider using Twitch OAuth
/// </summary>
public class IGDBPlugin : IPlugin, IMetadataProvider
{
    public string Name => "IGDB";
    public string Author => "VibeNoobNotFound";
    public string Version => "1.0.0";

    public IEnumerable<string> SupportedPlatforms => Enumerable.Empty<string>(); // All platforms

    private readonly HttpClient _httpClient = new HttpClient();
    private string? _clientId;
    private string? _clientSecret;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    // IMetadataProvider Implementation
    public async Task<GameMetadata?> SearchAsync(string gameName, string? platform = null)
    {
        if (!await EnsureAuthenticated())
            return null;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/games");
            request.Headers.Add("Client-ID", _clientId);
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");

            // IGDB uses a custom query language
            var query = $"search \"{gameName}\"; fields name,summary,cover.url,first_release_date,involved_companies.company.name,genres.name,screenshots.url,videos.video_id; limit 1;";
            request.Content = new StringContent(query);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(content);
            var games = doc.RootElement;

            if (games.GetArrayLength() > 0)
            {
                var game = games[0];
                return ParseGameMetadata(game);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"IGDB search failed: {ex.Message}");
        }

        return null;
    }

    public async Task<GameMetadata?> GetByIdAsync(string gameId)
    {
        if (!await EnsureAuthenticated())
            return null;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/games");
            request.Headers.Add("Client-ID", _clientId);
            request.Headers.Add("Authorization", $"Bearer {_accessToken}");

            var query = $"where id = {gameId}; fields name,summary,cover.url,first_release_date,involved_companies.company.name,genres.name,screenshots.url,storyline,videos.video_id; limit 1;";
            request.Content = new StringContent(query);

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(content);
            var games = doc.RootElement;

            if (games.GetArrayLength() > 0)
            {
                return ParseGameMetadata(games[0]);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"IGDB fetch failed: {ex.Message}");
        }

        return null;
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
                    WidgetBuilder.TextInput("twitch_client_id", "Client ID", placeholder: "Enter your Twitch Client ID"),
                    WidgetBuilder.TextInput("twitch_client_secret", "Client Secret", placeholder: "Enter your Twitch Client Secret"),
                    WidgetBuilder.Row(
                        WidgetBuilder.Button("Test Connection", "test_twitch_auth"),
                        WidgetBuilder.PrimaryButton("Save Credentials", "save_twitch_credentials")
                    )
                )
            };
        }
        return new List<Widget>();
    }

    public List<Widget> GetWidgets(Game game) => new List<Widget>();

    public async Task<WidgetActionResult> OnWidgetAction(string actionId, string payload)
    {
        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(payload);

            if (actionId == "save_twitch_credentials")
            {
                if (data != null)
                {
                    _clientId = data.GetValueOrDefault("twitch_client_id");
                    _clientSecret = data.GetValueOrDefault("twitch_client_secret");

                    // TODO: Persist to secure storage
                    Console.WriteLine("IGDB credentials saved.");
                }
            }
            else if (actionId == "test_twitch_auth")
            {
                if (data != null)
                {
                    _clientId = data.GetValueOrDefault("twitch_client_id");
                    _clientSecret = data.GetValueOrDefault("twitch_client_secret");

                    if (await GetAccessToken())
                    {
                        Console.WriteLine("IGDB authentication successful!");
                        return WidgetActionResult.Ok("Authentication successful!");
                    }
                    else
                    {
                        Console.WriteLine("IGDB authentication failed.");
                        return WidgetActionResult.Fail("Authentication failed.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Widget action failed: {ex.Message}");
            return WidgetActionResult.Fail(ex.Message);
        }

        return WidgetActionResult.Ok();
    }

    public Task OnLibraryScan(LibraryContext context) => Task.CompletedTask;
    public Task OnGameLaunched(Game game) => Task.CompletedTask;
    public Task OnGameStopped(Game game, TimeSpan sessionDuration) => Task.CompletedTask;

    // Helper Methods
    private async Task<bool> EnsureAuthenticated()
    {
        if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            return false;

        if (DateTime.UtcNow < _tokenExpiry && !string.IsNullOrEmpty(_accessToken))
            return true;

        return await GetAccessToken();
    }

    private async Task<bool> GetAccessToken()
    {
        try
        {
            var url = $"https://id.twitch.tv/oauth2/token?client_id={_clientId}&client_secret={_clientSecret}&grant_type=client_credentials";
            var response = await _httpClient.PostAsync(url, null);
            var content = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("access_token", out var token))
            {
                _accessToken = token.GetString();

                if (doc.RootElement.TryGetProperty("expires_in", out var expiresIn))
                {
                    _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn.GetInt32() - 60); // Refresh 1 min early
                }

                return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to get Twitch token: {ex.Message}");
        }

        return false;
    }

    private GameMetadata? ParseGameMetadata(JsonElement game)
    {
        try
        {
            // Cover URL needs prefix
            string? coverUrl = null;
            if (game.TryGetProperty("cover", out var cover) && cover.TryGetProperty("url", out var coverUrlProp))
            {
                coverUrl = "https:" + coverUrlProp.GetString()?.Replace("t_thumb", "t_cover_big");
            }

            // Parse genres
            string[]? genres = null;
            if (game.TryGetProperty("genres", out var genresArray))
            {
                genres = genresArray.EnumerateArray()
                    .Select(g => g.TryGetProperty("name", out var n) ? n.GetString() : null)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Cast<string>()
                    .ToArray();
            }

            // Parse screenshots
            string[]? screenshots = null;
            if (game.TryGetProperty("screenshots", out var screenshotsArray))
            {
                screenshots = screenshotsArray.EnumerateArray()
                    .Select(s => s.TryGetProperty("url", out var u) ? "https:" + u.GetString()?.Replace("t_thumb", "t_screenshot_big") : null)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Cast<string>()
                    .ToArray();
            }

            // Parse developer
            string? developer = null;
            if (game.TryGetProperty("involved_companies", out var companies))
            {
                foreach (var company in companies.EnumerateArray())
                {
                    if (company.TryGetProperty("company", out var comp) && comp.TryGetProperty("name", out var name))
                    {
                        developer = name.GetString();
                        break;
                    }
                }
            }

            // Parse release date
            DateTime? releaseDate = null;
            if (game.TryGetProperty("first_release_date", out var releaseProp))
            {
                releaseDate = DateTimeOffset.FromUnixTimeSeconds(releaseProp.GetInt64()).DateTime;
            }

            // Parse videos
            string[]? videos = null;
            if (game.TryGetProperty("videos", out var videosArray))
            {
                videos = videosArray.EnumerateArray()
                    .Select(v => v.TryGetProperty("video_id", out var id) ? "https://www.youtube.com/watch?v=" + id.GetString() : null)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Cast<string>()
                    .ToArray();
            }

            return new GameMetadata
            {
                Description = game.TryGetProperty("summary", out var summary) ? summary.GetString() : null,
                ShortDescription = game.TryGetProperty("storyline", out var storyline) ? storyline.GetString() : null,
                CoverImageUrl = coverUrl,
                Genres = genres,
                Screenshots = screenshots,
                Videos = videos,
                Developer = developer,
                ReleaseDate = releaseDate
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse IGDB metadata: {ex.Message}");
            return null;
        }
    }
}
