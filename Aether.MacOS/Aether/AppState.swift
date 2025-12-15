import AetherIPC
import Combine
import GRPCCore
import SwiftProtobuf
import SwiftUI
import os

struct GameViewModel: Identifiable {
    let id: String
    let title: String
    let platform: String
    let externalID: String

    // Paths
    let installPath: String
    let executablePath: String

    // Images
    let coverImageURL: URL?
    let backgroundImageURL: URL?
    let logoImageURL: URL?
    let screenshots: [URL]
    let videos: [URL]

    // Metadata
    let description: String
    let shortDescription: String
    let genres: [String]
    let tags: [String]
    let categories: [String]
    let developer: String?
    let publisher: String?
    let releaseDate: Date?

    // Ratings
    let metacriticScore: Double?
    let userScore: Double?
    let reviewCount: Int

    // Features
    let hasAchievements: Bool
    let achievementCount: Int
    let hasMultiplayer: Bool
    let hasSinglePlayer: Bool
    let hasCloudSaves: Bool

    // System Requirements
    let minimumRequirements: String?
    let recommendedRequirements: String?
    let supportedLanguages: [String]

    // User Stats
    let totalPlaytime: TimeInterval
    let lastPlayed: Date?
    var isFavorite: Bool
    let isInstalled: Bool

    // Computed Properties
    var formattedPlaytime: String {
        let hours = Int(totalPlaytime) / 3600
        let minutes = (Int(totalPlaytime) % 3600) / 60
        if hours > 0 {
            return "\(hours)h \(minutes)m"
        } else if minutes > 0 {
            return "\(minutes)m"
        }
        return "Never played"
    }

    var formattedReleaseDate: String {
        guard let date = releaseDate else { return "Unknown" }
        let formatter = DateFormatter()
        formatter.dateStyle = .medium
        return formatter.string(from: date)
    }

    var ratingText: String {
        if let score = metacriticScore, score > 0 {
            return "\(Int(score))/100"
        }
        return "No rating"
    }

    var genreText: String {
        if genres.isEmpty {
            return "Unknown"
        }
        return genres.prefix(3).joined(separator: ", ")
    }

    // Initializer from proto
    init(from proto: Aether_Game) {
        self.id = proto.id
        self.title = proto.title
        self.platform = proto.platform
        self.externalID = proto.externalID
        self.installPath = proto.installPath
        self.executablePath = proto.executablePath

        // Images (with validation)
        self.coverImageURL = URL(string: proto.coverImageURL)
        self.backgroundImageURL = URL(string: proto.backgroundImageURL)
        self.logoImageURL = URL(string: proto.logoImageURL)
        self.screenshots = proto.screenshots.compactMap { URL(string: $0) }
        self.videos = proto.videos.compactMap { URL(string: $0) }

        // Metadata
        self.description = proto.description_p
        self.shortDescription = proto.shortDescription
        self.genres = Array(proto.genres)
        self.tags = Array(proto.tags)
        self.categories = Array(proto.categories)
        self.developer = proto.developer.isEmpty ? nil : proto.developer
        self.publisher = proto.publisher.isEmpty ? nil : proto.publisher

        // Convert Unix timestamps
        self.releaseDate =
            proto.releaseDateUnix > 0
            ? Date(timeIntervalSince1970: TimeInterval(proto.releaseDateUnix))
            : nil
        self.lastPlayed =
            proto.lastPlayedUnix > 0
            ? Date(timeIntervalSince1970: TimeInterval(proto.lastPlayedUnix))
            : nil

        // Ratings
        self.metacriticScore = proto.metacriticScore > 0 ? proto.metacriticScore : nil
        self.userScore = proto.userScore > 0 ? proto.userScore : nil
        self.reviewCount = Int(proto.reviewCount)

        // Features
        self.hasAchievements = proto.hasAchievements_p
        self.achievementCount = Int(proto.achievementCount)
        self.hasMultiplayer = proto.hasMultiplayer_p
        self.hasSinglePlayer = proto.hasSinglePlayer_p
        self.hasCloudSaves = proto.hasCloudSaves_p

        // System Requirements
        self.minimumRequirements =
            proto.minimumRequirements.isEmpty ? nil : proto.minimumRequirements
        self.recommendedRequirements =
            proto.recommendedRequirements.isEmpty ? nil : proto.recommendedRequirements
        self.supportedLanguages = Array(proto.supportedLanguages)

        // User Stats
        self.totalPlaytime = TimeInterval(proto.totalPlaytimeSeconds)
        self.isFavorite = proto.isFavorite
        self.isInstalled = proto.isInstalled
    }
}

extension GameViewModel: Hashable {
    static func == (lhs: GameViewModel, rhs: GameViewModel) -> Bool {
        lhs.id == rhs.id
    }

    func hash(into hasher: inout Hasher) {
        hasher.combine(id)
    }
}

struct PluginViewModel: Identifiable {
    let id = UUID()
    let name: String
    let version: String
    let author: String
    let isImporter: Bool

    init(from proto: Aether_PluginInfo) {
        self.name = proto.name
        self.version = proto.version
        self.author = proto.author
        self.isImporter = proto.isImporter
    }
}

@MainActor
class AppState: ObservableObject {
    @Published var games: [GameViewModel] = []
    @Published var plugins: [PluginViewModel] = []
    @Published var currentScreen: AppScreen = .home

    private let grpcClient = GrpcClient()

    init() {
        Logger.shared.log("AppState initialized")
    }

    func refreshLibrary() async {
        Logger.shared.log("Refreshing library...")

        do {
            // Use gRPC Swift v2 closure pattern
            try await grpcClient.client.getLibrary(Aether_Empty()) { response in
                var games: [Aether_Game] = []
                for try await game in response.messages {
                    Logger.shared.log("Received game: \(game.title)")
                    games.append(game)
                }

                // Update state on MainActor
                let fetchedGames = games.map { GameViewModel(from: $0) }
                await MainActor.run {
                    self.games = fetchedGames
                    Logger.shared.log("Library refreshed. Total games: \(self.games.count)")
                }
            }

            // Also refresh plugins
            await fetchPlugins()

        } catch {
            Logger.shared.log("Failed to fetch library: \(error)", type: .error)
        }
    }

    func scanLibrary() async {
        Logger.shared.log("Starting library scan...")

        do {
            let request = Aether_ScanRequest.with { $0.forceRefresh = false }

            try await grpcClient.client.scanLibrary(request) { response in
                for try await progress in response.messages {
                    // Log progress
                    if !progress.currentStatus.isEmpty {
                        Logger.shared.log("Scan Status: \(progress.currentStatus)")
                    } else {
                        Logger.shared.log(
                            "Scan: \(progress.currentPlatform) - \(progress.currentGame) (\(Int(progress.progressPercentage))%)"
                        )
                    }

                    // Handle found game
                    if progress.hasFoundGame {
                        // Create view model locally (off-main-actor safety depends on Sendable, assuming GameViewModel is value type and Sendable)
                        let newGame = GameViewModel(from: progress.foundGame)

                        // Update state on MainActor
                        await MainActor.run {
                            if let index = self.games.firstIndex(where: { $0.id == newGame.id }) {
                                self.games[index] = newGame
                            } else {
                                self.games.append(newGame)
                            }
                        }
                    }
                }
            }

            Logger.shared.log("Scan complete!")
        } catch {
            Logger.shared.log("Scan failed: \(error)", type: .error)
        }
    }

    func fetchSetupWidgets(for pluginName: String) async -> [Aether_PluginWidget] {
        do {
            var request = Aether_PluginName()
            request.name = pluginName
            let response = try await grpcClient.client.getSetupWidgets(request)
            return response.widgets
        } catch {
            Logger.shared.log("Failed to fetch setup widgets: \(error)", type: .error)
            return []
        }
    }

    func triggerPluginAction(pluginName: String, actionId: String, payload: String) async throws
        -> Aether_OperationStatus
    {
        var request = Aether_PluginAction()
        request.pluginName = pluginName
        request.actionID = actionId
        request.payloadJson = payload

        return try await grpcClient.client.triggerPluginAction(request)
    }

    func launchGame(_ game: GameViewModel) {
        Logger.shared.log("Launching game with ID: \(game.id)")

        Task {
            do {
                let request = Aether_LaunchRequest.with {
                    $0.gameID = game.id
                }

                let response = try await grpcClient.client.launchGame(request)

                if response.success {
                    Logger.shared.log("Game launched successfully. PID: \(response.processID)")
                } else {
                    Logger.shared.log("Launch failed: \(response.message)", type: .error)
                }
            } catch {
                Logger.shared.log("Launch error: \(error)", type: .error)
            }
        }
    }

    func fetchPlugins() async {
        Logger.shared.log("Fetching plugins...")
        do {
            let pluginsList = try await grpcClient.client.getPlugins(Aether_Empty())
            self.plugins = pluginsList.plugins.map { PluginViewModel(from: $0) }
            Logger.shared.log("Plugins fetched: \(self.plugins.count)")
        } catch {
            Logger.shared.log("Failed to fetch plugins: \(error)", type: .error)
        }
    }

    // MARK: - QoL Actions

    func clearLibrary() async {
        Logger.shared.log("Clearing library...")
        do {
            _ = try await grpcClient.client.clearLibrary(Aether_Empty())
            self.games = []  // Clear locally
            Logger.shared.log("Library cleared.")
        } catch {
            Logger.shared.log("Failed to clear library: \(error)", type: .error)
        }
    }

    func removeGame(id: String) async {
        do {
            var request = Aether_GameId()
            request.id = id
            let response = try await grpcClient.client.removeGame(request)
            if response.success {
                self.games.removeAll { $0.id == id }
                Logger.shared.log("Game removed: \(id)")
            }
        } catch {
            Logger.shared.log("Failed to remove game: \(error)", type: .error)
        }
    }

    func toggleFavorite(game: GameViewModel) async {
        // Optimistic update
        if let index = games.firstIndex(where: { $0.id == game.id }) {
            games[index].isFavorite.toggle()
        }

        do {
            var request = Aether_GameId()
            request.id = game.id
            let response = try await grpcClient.client.toggleFavorite(request)

            if !response.success {
                // Revert on failure
                if let index = games.firstIndex(where: { $0.id == game.id }) {
                    games[index].isFavorite.toggle()
                }
                Logger.shared.log(
                    "Failed to toggle favorite on server: \(response.message)", type: .error)
            }
        } catch {
            // Revert on failure
            if let index = games.firstIndex(where: { $0.id == game.id }) {
                games[index].isFavorite.toggle()
            }
            Logger.shared.log("Failed to toggle favorite: \(error)", type: .error)
        }
    }

    func openGameLocation(game: GameViewModel) async {
        do {
            var request = Aether_GameId()
            request.id = game.id
            _ = try await grpcClient.client.openGameLocation(request)
        } catch {
            Logger.shared.log("Failed to open location: \(error)", type: .error)
        }
    }

    func updateGameMetadata(
        gameId: String,
        title: String,
        developer: String,
        publisher: String,
        description: String,
        coverImageUrl: String,
        backgroundImageUrl: String,
        genres: [String],
        videos: [String]
    ) async throws {
        var request = Aether_GameMetadataUpdate()
        request.gameID = gameId
        request.title = title
        request.developer = developer
        request.publisher = publisher
        request.description_p = description
        request.coverImageURL = coverImageUrl
        request.backgroundImageURL = backgroundImageUrl
        request.genres = genres
        request.videos = videos

        let response = try await grpcClient.client.updateGameMetadata(request)

        if !response.success {
            throw NSError(
                domain: "MetadataError", code: 1,
                userInfo: [NSLocalizedDescriptionKey: response.message])
        }

        // Refresh library to get updated data
        await refreshLibrary()
    }

    func searchMetadataProviders(query: String, provider: String) async throws
        -> [MetadataSearchResult]
    {
        var request = Aether_MetadataSearchRequest()
        request.query = query
        request.provider = provider

        let response = try await grpcClient.client.searchMetadataProviders(request)

        return response.results.map { result in
            MetadataSearchResult(
                provider: result.provider,
                externalId: result.externalID,
                title: result.title,
                developer: result.developer,
                coverImageUrl: result.coverImageURL,
                releaseYear: Int(result.releaseYear),
                videos: result.videos
            )
        }
    }
}
