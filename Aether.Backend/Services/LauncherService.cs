using Aether.Protos;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Aether.Backend.Services;

public partial class AetherGrpcService
{
    public override Task<LaunchResponse> LaunchGame(LaunchRequest request, ServerCallContext context)
    {
        _logger.LogInformation($"Launching game {request.GameId}");
        return Task.FromResult(new LaunchResponse { Success = true, ProcessId = 1234 });
    }

    public override Task<OperationStatus> StopGame(GameId request, ServerCallContext context)
    {
        return Task.FromResult(new OperationStatus { Success = true, Message = "Stopped" });
    }

    public override async Task SubscribeToGameState(Empty request, IServerStreamWriter<GameStateUpdate> responseStream, ServerCallContext context)
    {
        await Task.Delay(100);
    }
}
