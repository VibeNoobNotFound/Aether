using Aether.Protos;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Aether.Backend.Services;

public partial class AetherGrpcService
{
    public override async Task<UpdateInfo> CheckForUpdates(CheckUpdateRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Checking for updates. Current version: {Version}, Include prerelease: {Prerelease}",
            request.CurrentVersion, request.IncludePrerelease);
        return await _updateService.CheckForUpdates(request.CurrentVersion, request.IncludePrerelease);
    }

    public override async Task DownloadUpdate(DownloadUpdateRequest request, IServerStreamWriter<DownloadProgress> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("Starting download for version: {Version}", request.Version);

        await foreach (var progress in _updateService.DownloadUpdate(request.Version))
        {
            await responseStream.WriteAsync(progress);
        }
    }

    public override Task<OperationStatus> InstallAppUpdate(InstallUpdateRequest request, ServerCallContext context)
    {
        _logger.LogWarning("InstallAppUpdate RPC is deprecated. Frontend should handle installation locally.");
        return Task.FromResult(new OperationStatus
        {
            Success = false,
            Message = "This RPC is deprecated. Client must handle installation locally."
        });
    }
}
