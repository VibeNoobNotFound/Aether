using Aether.Protos;
using Grpc.Core;

namespace Aether.Backend.Services;

public partial class AetherGrpcService
{
    public override Task<WidgetList> GetGameDetailWidgets(GameId request, ServerCallContext context)
    {
        return Task.FromResult(new WidgetList());
    }

    public override Task<OperationStatus> TriggerPluginAction(PluginAction request, ServerCallContext context)
    {
        return Task.FromResult(new OperationStatus { Success = true });
    }
}
