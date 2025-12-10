using Aether.Protos;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Aether.Backend.Services;

public partial class AetherGrpcService
{
    public override async Task ScanLibrary(ScanRequest request, IServerStreamWriter<ScanProgress> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("Scanning library...");
        await responseStream.WriteAsync(new ScanProgress { CurrentStatus = "Starting scan...", PercentComplete = 0 });
        await Task.Delay(100);
        await responseStream.WriteAsync(new ScanProgress { CurrentStatus = "Scanning Steam...", PercentComplete = 50 });
        await Task.Delay(100);
        await responseStream.WriteAsync(new ScanProgress { CurrentStatus = "Complete", PercentComplete = 100 });
    }

    public override async Task GetLibrary(Empty request, IServerStreamWriter<Game> responseStream, ServerCallContext context)
    {
        // Mock data
        await responseStream.WriteAsync(new Game { Id = "1", Title = "Mock Game 1", Platform = Platform.Steam });
    }
}
