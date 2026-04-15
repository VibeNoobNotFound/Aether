using Aether.Protos;
using Aether.Backend.Plugins;
using Aether.PluginSDK;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Aether.Backend.Services;

public partial class AetherGrpcService
{
    public override async Task<MetadataSearchResponse> SearchMetadataProviders(MetadataSearchRequest request, ServerCallContext context)
    {
        var response = new MetadataSearchResponse();

        try
        {
            var providers = _pluginManager.GetMetadataProviders().ToList();

            // Filter by provider name if specified
            if (!string.IsNullOrEmpty(request.Provider))
            {
                providers = providers.Where(p => p.Name.Equals(request.Provider, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            foreach (var provider in providers)
            {
                try
                {
                    var metadatas = await provider.SearchAsync(request.Query);
                    if (metadatas != null)
                    {
                        foreach (var metadata in metadatas)
                        {
                            var result = new MetadataSearchResult
                            {
                                Provider = provider.Name,
                                ExternalId = metadata.ExternalId ?? "",
                                Title = metadata.Title ?? request.Query,
                                Developer = metadata.Developer ?? "",
                                Publisher = metadata.Publisher ?? "",
                                Description = metadata.Description ?? "",
                                CoverImageUrl = metadata.CoverImageUrl ?? "",
                                BackgroundImageUrl = metadata.BackgroundImageUrl ?? "",
                                LogoImageUrl = metadata.LogoImageUrl ?? "",
                                ReleaseYear = metadata.ReleaseDate?.Year ?? 0,
                                MetacriticScore = (int)(metadata.MetacriticScore ?? 0)
                            };

                            // Add arrays
                            if (metadata.Videos != null) result.Videos.AddRange(metadata.Videos);
                            if (metadata.Screenshots != null) result.Screenshots.AddRange(metadata.Screenshots);
                            if (metadata.Genres != null) result.Genres.AddRange(metadata.Genres);

                            response.Results.Add(result);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Search failed on provider {Provider}: {Error}", provider.Name, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching metadata providers");
        }

        return response;
    }

    public override Task<MetadataSettings> GetMetadataSettings(Empty request, ServerCallContext context)
    {
        var providers = _pluginManager.GetMetadataProviders().Select(p => p.Name).ToList();
        var config = _database.GetMetadataConfig();

        var settings = new MetadataSettings();
        settings.AvailableProviders.AddRange(providers);

        if (config.ProviderPriority != null && config.ProviderPriority.Count > 0)
        {
            settings.ProviderPriority.AddRange(config.ProviderPriority);
        }
        else
        {
            // Default Priority: Steam > IGDB > Others
            var defaults = new List<string> { "Steam", "IGDB" };
            // Add remaining that are not already in defaults
            defaults.AddRange(providers.Where(p => p != "Steam" && p != "IGDB"));

            settings.ProviderPriority.AddRange(defaults);
        }

        return Task.FromResult(settings);
    }

    public override Task<OperationStatus> SetMetadataSettings(MetadataSettings request, ServerCallContext context)
    {
        try
        {
            var config = _database.GetMetadataConfig();
            config.ProviderPriority = request.ProviderPriority.ToList();
            _database.SetMetadataConfig(config);

            return Task.FromResult(new OperationStatus { Success = true, Message = "Metadata settings updated." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving metadata settings");
            return Task.FromResult(new OperationStatus { Success = false, Message = ex.Message });
        }
    }
}
