import AetherIPC
import Foundation
import GRPCCore
import GRPCNIOTransportHTTP2
import os

@MainActor
class GrpcClient {
    static let shared = GrpcClient()

    // Explicit Generic Types
    private var client: Aether_AetherOrchestrator.Client<HTTP2ClientTransport.Posix>?
    private var grpcClient: GRPCClient<HTTP2ClientTransport.Posix>?
    private var transport: HTTP2ClientTransport.Posix?

    private let udsPath = FileManager.default.homeDirectoryForCurrentUser
        .appendingPathComponent("Library/Application Support/Aether/aether.sock").path

    func connect() {
        Task {
            do {
                // Setup Transport for Localhost TCP
                let transport = try HTTP2ClientTransport.Posix(
                    target: .ipv4(address: "127.0.0.1", port: 50051),
                    transportSecurity: .plaintext
                )

                self.transport = transport

                // Create Client
                self.grpcClient = GRPCClient(transport: transport)

                if let grpcClient = self.grpcClient {
                    self.client = Aether_AetherOrchestrator.Client(wrapping: grpcClient)
                    await Logger.shared.log("gRPC Client initialized for 127.0.0.1:50051")

                    // CRITICAL: Run transport in background
                    Task {
                        do {
                            await Logger.shared.log("Starting transport execution...")
                            try await withThrowingTaskGroup(of: Void.self) { group in
                                group.addTask {
                                    try await transport.connect()
                                }
                                try await group.waitForAll()
                            }
                            await Logger.shared.log("Transport execution completed")
                        } catch {
                            await Logger.shared.log(
                                "Transport execution error: \(error)", type: .error)
                        }
                    }

                    // Give transport time to initialize
                    try await Task.sleep(for: .milliseconds(500))
                    await Logger.shared.log("Transport should be ready")
                }

            } catch {
                await Logger.shared.log("Failed to initialize gRPC: \(error)", type: .error)
            }
        }
    }

    func getGames() async throws -> [Aether_Game] {
        guard let client = client else {
            Logger.shared.log("Attempted to get games but client is nil", type: .error)
            return []
        }

        let request = Aether_Empty()
        Logger.shared.log("Requesting library...", type: .debug)

        do {
            // Add timeout to detect connection issues
            Logger.shared.log("About to call client.getLibrary with 10s timeout...", type: .debug)

            return try await withThrowingTaskGroup(of: [Aether_Game].self) { group in
                // Task 1: Actual gRPC call
                group.addTask {
                    try await client.getLibrary(request) { response in
                        await Logger.shared.log("Inside getLibrary response handler", type: .debug)
                        var games: [Aether_Game] = []

                        do {
                            for try await game in response.messages {
                                await Logger.shared.log(
                                    "Received game: \(game.title)", type: .debug)
                                games.append(game)
                            }
                            await Logger.shared.log(
                                "Finished iterating games, count: \(games.count)", type: .info)
                        } catch {
                            await Logger.shared.log(
                                "Error iterating messages: \(error)", type: .error)
                            throw error
                        }

                        return games
                    }
                }

                // Task 2: Timeout
                group.addTask {
                    try await Task.sleep(for: .seconds(10))
                    await Logger.shared.log("gRPC call timed out after 10 seconds!", type: .error)
                    throw GrpcError.timeout
                }

                // Return first completed task, cancel the other
                let result = try await group.next()!
                group.cancelAll()
                Logger.shared.log(
                    "client.getLibrary completed, received \(result.count) games", type: .info)
                return result
            }
        } catch {
            Logger.shared.log("Error in getGames: \(error)", type: .error)
            throw error
        }
    }

    enum GrpcError: Error {
        case timeout
    }

    func launchGame(id: String) async throws {
        guard let client = client else { return }

        var request = Aether_LaunchRequest()
        request.gameID = id
        request.runAsAdmin = false

        // Unary calls have a default onResponse that returns the message
        let response = try await client.launchGame(request)
        if !response.success {
            print("Launch Error: \(response.errorMessage)")
        }
    }
}
