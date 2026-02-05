using global::Aether.Protos;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using System;

namespace Aether.WinUI.Services;

public class GrpcClientService : IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly ILogger<GrpcClientService> _logger;
    public AetherOrchestrator.AetherOrchestratorClient Client { get; }

    public GrpcClientService(ILogger<GrpcClientService> logger)
    {
        _logger = logger;
        _logger.LogDebug("GrpcClientService initializing");
        // Backend runs on localhost:55551 with HTTP/2 plaintext
        // WinUI 3 uses HttpClient which supports HTTP/2
        _channel = GrpcChannel.ForAddress("http://127.0.0.1:55551");
        Client = new AetherOrchestrator.AetherOrchestratorClient(_channel);
        _logger.LogInformation("GrpcClientService initialized");
    }

    public void Dispose()
    {
        _logger.LogInformation("GrpcClientService disposing");
        _channel?.Dispose();
        GC.SuppressFinalize(this);
    }
}
