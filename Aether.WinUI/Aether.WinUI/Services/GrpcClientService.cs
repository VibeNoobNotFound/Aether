using global::Aether.Protos;
using Grpc.Net.Client;
using System;

namespace Aether.WinUI.Services;

public class GrpcClientService : IDisposable
{
    private readonly GrpcChannel _channel;
    public AetherOrchestrator.AetherOrchestratorClient Client { get; }

    public GrpcClientService()
    {
        // Backend runs on localhost:55551 with HTTP/2 plaintext
        // WinUI 3 uses HttpClient which supports HTTP/2
        _channel = GrpcChannel.ForAddress("http://127.0.0.1:55551");
        Client = new AetherOrchestrator.AetherOrchestratorClient(_channel);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        GC.SuppressFinalize(this);
    }
}
