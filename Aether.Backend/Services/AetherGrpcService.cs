using Aether.Protos;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Aether.Backend.Services;

public partial class AetherGrpcService : AetherOrchestrator.AetherOrchestratorBase
{
    private readonly ILogger<AetherGrpcService> _logger;

    public AetherGrpcService(ILogger<AetherGrpcService> logger)
    {
        _logger = logger;
    }
}
