import AetherIPC
import Combine
import SwiftUI
import os

@MainActor
class AppState: ObservableObject {
    @Published var games: [GameViewModel] = []
    @Published var isScanning = false
    @Published var currentScreen: AppScreen = .home

    private let grpc = GrpcClient.shared

    init() {
        Logger.shared.log("AppState initializing...")
        grpc.connect()
        Task {
            Logger.shared.log("Starting library refresh task...")
            await refreshLibrary()
        }
    }

    func refreshLibrary() async {
        Logger.shared.log("refreshLibrary() called, setting isScanning=true")
        isScanning = true
        do {
            Logger.shared.log("Calling grpc.getGames()...")
            let protos: [Aether_Game] = try await grpc.getGames()
            Logger.shared.log("Received \(protos.count) games, mapping to ViewModels")
            self.games = protos.map { GameViewModel(proto: $0) }
            Logger.shared.log("Games successfully loaded into AppState")
        } catch {
            Logger.shared.log("Failed to fetch library: \(error)", type: .error)
        }
        isScanning = false
        Logger.shared.log("refreshLibrary() completed, isScanning=false")
    }

    func launch(gameId: String) {
        Logger.shared.log("Launching game: \(gameId)")
        Task {
            do {
                try await grpc.launchGame(id: gameId)
                Logger.shared.log("Game \(gameId) launched successfully")
            } catch {
                Logger.shared.log("Failed to launch game \(gameId): \(error)", type: .error)
            }
        }
    }
}

// Easier to use model for View
struct GameViewModel: Identifiable {
    let id: String
    let title: String
    let platform: String
    // let image: URL?

    init(proto: Aether_Game) {
        self.id = proto.id
        self.title = proto.title
        self.platform = "STEAM"
    }
}
