using Aether.PluginSDK.Storage;
using IGDB;

namespace Aether.Importers.IGDB;

/// <summary>
/// Custom ITokenStore implementation that persists OAuth tokens using IPluginStorage.
/// </summary>
public class PluginTokenStore : ITokenStore
{
    private readonly IPluginStorage _storage;
    private const string TokenKey = "twitch_oauth_token";

    public PluginTokenStore(IPluginStorage storage)
    {
        _storage = storage;
    }

    public async Task<TwitchAccessToken?> GetTokenAsync()
    {
        return await _storage.LoadAsync<TwitchAccessToken>(TokenKey);
    }

    public async Task<TwitchAccessToken> StoreTokenAsync(TwitchAccessToken token)
    {
        await _storage.SaveAsync(TokenKey, token);
        return token;
    }
}

/// <summary>
/// Model for storing Twitch API credentials
/// </summary>
public class TwitchCredentials
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}
