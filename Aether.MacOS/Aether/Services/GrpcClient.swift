import AetherIPC
import Foundation
import GRPCCore
import GRPCNIOTransportHTTP2
import os

class GrpcClient {
    static let shared = GrpcClient()

    // Exposed client for direct RPC calls
    // We forcedly unwrap connection in init for simplicity as we expect local server
    var client: Aether_AetherOrchestrator.Client<HTTP2ClientTransport.Posix>

    private var grpcClient: GRPCClient<HTTP2ClientTransport.Posix>
    private var transport: HTTP2ClientTransport.Posix

    init() {
        AetherLogger.shared.info("Initializing gRPC Client for 127.0.0.1:55551")

        // Setup Transport for Localhost TCP
        // Using try! is acceptable here as failure means the app is fundamentally broken
        let transport = try! HTTP2ClientTransport.Posix(
            target: .ipv4(address: "127.0.0.1", port: 55551),
            transportSecurity: .plaintext
        )

        self.transport = transport
        self.grpcClient = GRPCClient(transport: transport)
        self.client = Aether_AetherOrchestrator.Client(wrapping: grpcClient)

        // Start transport in background
        Task {
            do {
                AetherLogger.shared.info("Starting transport execution...")
                try await withThrowingTaskGroup(of: Void.self) { group in
                    group.addTask {
                        try await transport.connect()
                    }
                    try await group.waitForAll()
                }
            } catch {
                AetherLogger.shared.error("Transport execution error: \(error)")
            }
        }
    }
}
