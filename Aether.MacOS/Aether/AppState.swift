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
    let isFavorite: Bool
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

@MainActor
class AppState: ObservableObject {
    @Published var games: [GameViewModel] = []
    @Published var currentScreen: AppScreen = .home
    private let grpcClient = GrpcClient()

    init() {
        Logger.shared.log("AppState initialized")
    }

    func refreshLibrary() async {
        Logger.shared.log("Refreshing library...")

        do {
            // Use gRPC Swift v2 closure pattern
            let protos = try await grpcClient.client.getLibrary(Aether_Empty()) { response in
                var games: [Aether_Game] = []
                for try await game in response.messages {
                    Logger.shared.log("Received game: \(game.title)")
                    games.append(game)
                }
                return games
            }

            self.games = protos.map { GameViewModel(from: $0) }
            Logger.shared.log("Library refreshed. Total games: \(self.games.count)")
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
                    Logger.shared.log(
                        "Scan: \(progress.currentPlatform) - \(progress.currentGame) (\(Int(progress.progressPercentage))%)"
                    )
                }
            }

            Logger.shared.log("Scan complete! Refreshing library...")
            await refreshLibrary()
        } catch {
            Logger.shared.log("Scan failed: \(error)", type: .error)
        }
    }

    func launchGame(id: String) {
        Logger.shared.log("Launching game with ID: \(id)")

        Task {
            do {
                let request = Aether_LaunchRequest.with {
                    $0.gameID = id
                }

                let response = try await grpcClient.client.launchGame(request)

                if response.success {
                    Logger.shared.log("Game launched successfully. PID: \(response.processID)")
                } else {
                    Logger.shared.log("Launch failed: \(response.errorMessage)", type: .error)
                }
            } catch {
                Logger.shared.log("Launch error: \(error)", type: .error)
            }
        }
    }
}
