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
    private readonly UpdateService _updateService;
    private readonly GameSessionManager _sessionManager;

    public AetherGrpcService(
        ILogger<AetherGrpcService> logger,
        PluginManager pluginManager,
        LibraryDatabase database,
        UpdateService updateService,
        GameSessionManager sessionManager)
    {
        _logger = logger;
        _pluginManager = pluginManager;
        _database = database;
        _updateService = updateService;
        _sessionManager = sessionManager;
    }

    // HEALTH CHECK
    public override Task<PingResponse> Ping(Empty request, ServerCallContext context)
    {
        return Task.FromResult(new PingResponse
        {
            Healthy = true,
            Version = "1.0.0" // TODO: Read from assembly
        });
    }
}
