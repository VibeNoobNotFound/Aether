using Aether.Protos;
using Aether.Backend.Plugins;
using Aether.Backend.Data;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Aether.Backend.Services;

public partial class AetherGrpcService : AetherOrchestrator.AetherOrchestratorBase
{
    private readonly ILogger<AetherGrpcService> _logger;
    private readonly PluginManager _pluginManager;
    private readonly LibraryDatabase _database;

    public AetherGrpcService(
        ILogger<AetherGrpcService> logger,
        PluginManager pluginManager,
        LibraryDatabase database)
    {
        _logger = logger;
        _pluginManager = pluginManager;
        _database = database;
    }
}
