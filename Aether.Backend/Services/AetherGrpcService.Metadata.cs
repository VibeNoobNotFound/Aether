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
                    var metadata = await provider.SearchAsync(request.Query);
                    if (metadata != null)
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
                            LogoImageUrl = metadata.LogoImageUrl ?? "",
                            ReleaseYear = metadata.ReleaseDate?.Year ?? 0
                        };

                        // Add arrays
                        if (metadata.Videos != null) result.Videos.AddRange(metadata.Videos);
                        if (metadata.Screenshots != null) result.Screenshots.AddRange(metadata.Screenshots);
                        if (metadata.Genres != null) result.Genres.AddRange(metadata.Genres);

                        response.Results.Add(result);
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
}
