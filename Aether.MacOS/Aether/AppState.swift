import AetherIPC
import Combine
import GRPCCore
import SwiftProtobuf
import SwiftUI
import os

struct GameViewModel: Identifiable, Hashable {
    let id: String
    let title: String
    let platform: String
    let externalID: String

    // Paths
    let installPath: String
    let executablePath: String
    let launchArguments: String?

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

    // Cross-Platform News
    let steamId: String?

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

    var formattedLastPlayed: String {
        guard let date = lastPlayed else { return "Never" }
        let formatter = RelativeDateTimeFormatter()
        formatter.unitsStyle = .full
        return formatter.localizedString(for: date, relativeTo: Date())
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
        self.launchArguments = proto.launchArguments.isEmpty ? nil : proto.launchArguments

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

        // Cross-Platform News
        self.steamId = proto.steamID.isEmpty ? nil : proto.steamID
    }
}

struct PluginViewModel: Identifiable {
    let id = UUID()
    let name: String
    let version: String
    let author: String
    let isImporter: Bool
    let supportsManualAddition: Bool
    let supportedPlatforms: [String]

    init(from proto: Aether_PluginInfo) {
        self.name = proto.name
        self.version = proto.version
        self.author = proto.author
        self.isImporter = proto.isImporter
        self.supportsManualAddition = proto.supportsManualAddition
        self.supportedPlatforms = Array(proto.supportedPlatforms)
    }
}

// News Model
// News Model
struct NewsItem: Identifiable, Hashable {
    let id: String
    let title: String
    let url: URL?
    let contentHtml: String
    let author: String
    let date: Date
    let imageUrl: URL?
    let source: String

    init(from proto: Aether_NewsItem) {
        self.id = proto.id
        self.title = proto.title
        self.url = URL(string: proto.url)
        self.contentHtml = proto.contentHtml
        self.author = proto.author
        self.date = Date(timeIntervalSince1970: TimeInterval(proto.dateUnix))
        self.imageUrl = URL(string: proto.imageURL)
        self.source = proto.source
    }
}

struct MetadataSearchResult: Identifiable, Hashable {
    // Computed ID to avoid hash issues if creating multiple instances
    var id: String { externalId.isEmpty ? title : externalId }
    let provider: String
    let externalId: String
    let title: String
    let developer: String
    let publisher: String
    let description: String
    let coverImageUrl: String
    let logoImageUrl: String
    let releaseYear: Int
    let videos: [String]
    let screenshots: [String]
    let genres: [String]
}

@MainActor
class AppState: ObservableObject {
    @Published var games: [GameViewModel] = []
    @Published var plugins: [PluginViewModel] = []
    @Published var collections: [CollectionViewModel] = []
    @Published var carouselConfig: CarouselConfig?
    @Published var currentScreen: AppScreen = .home

    // Computed Properties

    var visibleCollections: [CollectionViewModel] {
        collections.filter { $0.isVisible }
            .sorted { $0.sortOrder < $1.sortOrder }
    }

    var carouselGames: [GameViewModel] {
        guard let config = carouselConfig else {
            return Array(games.prefix(5))
        }

        var result: [GameViewModel] = []

        if let colId = config.collectionId {
            // Priority 1: From Collection
            if let col = collections.first(where: { $0.id == colId }) {
                result = getGames(for: col)
            }
        } else if !config.gameIds.isEmpty {
            // Priority 2: Manual Game IDs
            result = config.gameIds.compactMap { id in
                games.first { $0.id == id }
            }
        } else {
            // Priority 3: Default (Favorites + Recent mixed)
            let favorites = games.filter { $0.isFavorite }
            let recent = games.filter { $0.lastPlayed != nil }
                .sorted { ($0.lastPlayed ?? .distantPast) > ($1.lastPlayed ?? .distantPast) }

            var combined = favorites
            for game in recent {
                if !combined.contains(where: { $0.id == game.id }) {
                    combined.append(game)
                }
            }
            result = combined
        }

        return Array(result.prefix(config.maxGames))
    }

    func getGames(for collection: CollectionViewModel) -> [GameViewModel] {
        if let filter = collection.platformFilter?.lowercased() {
            return games.filter {
                if filter == "favorites" { return $0.isFavorite }
                return $0.platform.lowercased().contains(filter)
            }
        } else if collection.type == .collectionFavorites {
            return games.filter { $0.isFavorite }
        } else if collection.type == .collectionRecentlyPlayed {
            return games.filter { $0.lastPlayed != nil }
                .sorted { ($0.lastPlayed ?? .distantPast) > ($1.lastPlayed ?? .distantPast) }
        } else {
            // Custom collection with game IDs
            let ids = Set(collection.gameIds)
            return games.filter { ids.contains(Int32($0.id) ?? 0) }
        }
    }

    private let grpcClient = GrpcClient()

    init() {
        Logger.shared.log("AppState initialized")
        startAutoRefresh()
    }

    private func startAutoRefresh() {
        Task {
            while true {
                try? await Task.sleep(nanoseconds: 30 * 1_000_000_000)  // 30 seconds
                await refreshLibrary()
            }
        }
    }

    private func waitForBackend() async {
        // Wait for BackendManager to report connected state
        var attempts = 0
        while !BackendManager.shared.connectionState.isReady {
            if attempts > 120 { break }  // 60s timeout (120 * 0.5s)
            try? await Task.sleep(nanoseconds: 500_000_000)  // 0.5s
            attempts += 1
        }
    }

    func refreshLibrary() async {
        await waitForBackend()
        Logger.shared.log("Refreshing library...")

        do {
            // Use gRPC Swift v2 closure pattern
            try await grpcClient.client.getLibrary(Aether_Empty()) { response in
                var games: [Aether_Game] = []
                for try await game in response.messages {
                    // Logger.shared.log("Received game: \(game.title)") // Reduced logging to avoid excessive awaits in loop
                    games.append(game)
                }

                // Update state on MainActor
                // Construct ViewModels on MainActor to avoid isolation issues
                let gamesToMap = games
                await MainActor.run {
                    self.games = gamesToMap.map { GameViewModel(from: $0) }
                    Task {
                        Logger.shared.log(
                            "Library refreshed. Total games: \(self.games.count)")
                    }
                }
            }

            // Also refresh plugins and collections
            await fetchPlugins()
            await fetchCollections()
            await fetchCarouselConfig()

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
                        await Logger.shared.log("Scan Status: \(progress.currentStatus)")

                        // Check for backend permission errors
                        if progress.currentStatus.contains("Access")
                            && (progress.currentStatus.contains("denied")
                                || progress.currentStatus.contains("Unauthorized"))
                        {
                            await MainActor.run {
                                PermissionManager.shared.promptForPermissions()
                            }
                        }
                    } else {
                        await Logger.shared.log(
                            "Scan: \(progress.currentPlatform) - \(progress.currentGame) (\(Int(progress.progressPercentage))%)"
                        )
                    }

                    // Handle found game
                    if progress.hasFoundGame {
                        let gameProto = progress.foundGame

                        // Update state on MainActor
                        await MainActor.run {
                            let newGame = GameViewModel(from: gameProto)
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

    func fetchWidgets(for pluginName: String, location: Aether_WidgetLocation) async
        -> [Aether_UIWidget]
    {
        do {
            var request = Aether_WidgetRequest()
            request.pluginName = pluginName
            request.location = location
            let response = try await grpcClient.client.getWidgets(request)
            return response.widgets
        } catch {
            Logger.shared.log("Failed to fetch widgets: \(error)", type: .error)
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
        Task {
            Logger.shared.log("Launching game with ID: \(game.id)")

            do {
                let request = Aether_LaunchRequest.with {
                    $0.gameID = game.id
                }

                let response = try await grpcClient.client.launchGame(request)

                if response.success {
                    Logger.shared.log(
                        "Game launched successfully. PID: \(response.processID)")

                    // Trigger refresh to update "Last Played" status immediately
                    await refreshLibrary()
                } else {
                    Logger.shared.log("Launch failed: \(response.message)", type: .error)
                }
            } catch {
                Logger.shared.log("Launch error: \(error)", type: .error)
            }
        }
    }

    func canLaunchGame(_ gameId: String) async -> (
        canLaunch: Bool, reason: String?, launchMethod: String?
    ) {
        do {
            let request = Aether_GameId.with {
                $0.id = gameId
            }
            let response = try await grpcClient.client.canLaunchGame(request)
            return (response.canLaunch, response.reason, response.launchMethod)
        } catch {
            Logger.shared.log("Error checking canLaunch: \(error)", type: .error)
            return (false, "Error checking launch capability", nil)
        }
    }

    func fetchPlugins() async {
        Logger.shared.log("Fetching plugins...")
        do {
            let pluginsList = try await grpcClient.client.getPlugins(Aether_Empty())
            let protos = pluginsList.plugins
            // Map on MainActor
            await MainActor.run {
                self.plugins = protos.map { PluginViewModel(from: $0) }
            }
            Logger.shared.log("Plugins fetched: \(self.plugins.count)")
        } catch {
            Logger.shared.log("Failed to fetch plugins: \(error)", type: .error)
        }
    }

    // MARK: - Collections & Carousel

    func fetchCollections() async {
        do {
            let response = try await grpcClient.client.getCollections(Aether_Empty())
            let protos = response.collections
            await MainActor.run {
                self.collections = protos.map { CollectionViewModel(from: $0) }
            }
        } catch {
            Logger.shared.log("Failed to fetch collections: \(error)", type: .error)
        }
    }

    func createCollection(name: String, iconName: String) async {
        do {
            var request = Aether_CreateCollectionRequest()
            request.name = name
            request.iconName = iconName

            _ = try await grpcClient.client.createCollection(request)
            await fetchCollections()
        } catch {
            Logger.shared.log("Failed to create collection: \(error)", type: .error)
        }
    }

    func updateCollection(
        id: Int32, name: String? = nil, iconName: String? = nil, sortOrder: Int32? = nil,
        isVisible: Bool? = nil
    ) async {
        do {
            var request = Aether_UpdateCollectionRequest()
            request.id = id
            if let n = name { request.name = n }
            if let i = iconName { request.iconName = i }
            if let s = sortOrder { request.sortOrder = s }
            if let v = isVisible { request.isVisible = v }

            _ = try await grpcClient.client.updateCollection(request)
            await fetchCollections()
        } catch {
            Logger.shared.log("Failed to update collection: \(error)", type: .error)
        }
    }

    func deleteCollection(id: Int32) async {
        do {
            var request = Aether_CollectionId()
            request.id = id
            _ = try await grpcClient.client.deleteCollection(request)
            await fetchCollections()
        } catch {
            Logger.shared.log("Failed to delete collection: \(error)", type: .error)
        }
    }

    func addGameToCollection(collectionId: Int32, gameId: String) async {
        do {
            var request = Aether_CollectionGameAction()
            request.collectionID = collectionId
            request.gameID = gameId
            _ = try await grpcClient.client.addGameToCollection(request)
            await fetchCollections()
        } catch {
            Logger.shared.log("Failed to add game to collection: \(error)", type: .error)
        }
    }

    func removeGameFromCollection(collectionId: Int32, gameId: String) async {
        do {
            var request = Aether_CollectionGameAction()
            request.collectionID = collectionId
            request.gameID = gameId
            _ = try await grpcClient.client.removeGameFromCollection(request)
            await fetchCollections()
        } catch {
            Logger.shared.log("Failed to remove game from collection: \(error)", type: .error)
        }
    }

    func reorderCollections(ids: [Int32]) async {
        do {
            var request = Aether_ReorderCollectionsRequest()
            request.collectionIds = ids
            _ = try await grpcClient.client.reorderCollections(request)
            // Optimistic update done in UI usually, but confirm with fetch
            await fetchCollections()
        } catch {
            Logger.shared.log("Failed to reorder collections: \(error)", type: .error)
        }
    }

    func fetchCarouselConfig() async {
        do {
            let configProto = try await grpcClient.client.getCarouselConfig(Aether_Empty())
            await MainActor.run {
                self.carouselConfig = CarouselConfig(from: configProto)
            }
        } catch {
            Logger.shared.log("Failed to fetch carousel config: \(error)", type: .error)
        }
    }

    func updateCarouselConfig(collectionId: Int32?, gameIds: [String]?, maxGames: Int = 5) async {
        do {
            var request = Aether_CarouselConfig()
            if let c = collectionId {
                request.collectionID = c
            }
            if let g = gameIds {
                request.gameIds = g
            }
            request.maxGames = Int32(maxGames)

            _ = try await grpcClient.client.setCarouselConfig(request)
            await fetchCarouselConfig()
        } catch {
            Logger.shared.log("Failed to update carousel config: \(error)", type: .error)
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
        title: String? = nil,
        developer: String? = nil,
        publisher: String? = nil,
        description: String? = nil,
        coverImageUrl: String? = nil,
        backgroundImageUrl: String? = nil,
        logoImageUrl: String? = nil,
        genres: [String]? = nil,
        videos: [String]? = nil,
        screenshots: [String]? = nil,
        steamId: String? = nil,
        launchArguments: String? = nil
    ) async throws {
        var request = Aether_GameMetadataUpdate()
        request.gameID = gameId

        if let t = title { request.title = t }
        if let d = developer { request.developer = d }
        if let p = publisher { request.publisher = p }
        if let desc = description { request.description_p = desc }
        if let c = coverImageUrl { request.coverImageURL = c }
        if let b = backgroundImageUrl { request.backgroundImageURL = b }
        if let l = logoImageUrl { request.logoImageURL = l }
        if let g = genres { request.genres = g }
        if let v = videos { request.videos = v }
        if let s = screenshots { request.screenshots = s }
        if let sid = steamId { request.steamID = sid }
        if let la = launchArguments { request.launchArguments = la }

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
                publisher: result.publisher,
                description: result.description_p,
                coverImageUrl: result.coverImageURL,
                logoImageUrl: result.logoImageURL,
                releaseYear: Int(result.releaseYear),
                videos: Array(result.videos),
                screenshots: Array(result.screenshots),
                genres: Array(result.genres)
            )
        }
    }

    // MARK: - News

    func fetchGameNews(gameId: String) async -> [NewsItem] {
        do {
            var request = Aether_GameId()
            request.id = gameId
            let response = try await grpcClient.client.getGameNews(request)
            return response.news.map { NewsItem(from: $0) }
        } catch {
            Logger.shared.log("Failed to fetch game news: \(error)", type: .error)
            return []
        }
    }

    func fetchGeneralNews() async -> [NewsItem] {
        await waitForBackend()
        do {
            let response = try await grpcClient.client.getGeneralNews(Aether_Empty())
            return response.news.map { NewsItem(from: $0) }
        } catch {
            Logger.shared.log("Failed to fetch general news: \(error)", type: .error)
            return []
        }
    }

    // MARK: - Plugin Management

    func installPlugin(fileURL: URL) async throws {
        // Read file data
        let content = try Data(contentsOf: fileURL)
        let filename = fileURL.lastPathComponent

        var request = Aether_PluginFile()
        request.filename = filename
        request.data = content

        let response = try await grpcClient.client.installPlugin(request)

        if !response.success {
            throw NSError(
                domain: "PluginError", code: 1,
                userInfo: [NSLocalizedDescriptionKey: response.message])
        }

        Logger.shared.log("Plugin installed: \(filename)")
        // Allow some time for backend reload
        try? await Task.sleep(nanoseconds: 1 * 1_000_000_000)
        await fetchPlugins()
    }

    func uninstallPlugin(name: String) async throws {
        var request = Aether_PluginName()
        request.name = name

        let response = try await grpcClient.client.uninstallPlugin(request)

        if !response.success {
            throw NSError(
                domain: "PluginError", code: 1,
                userInfo: [NSLocalizedDescriptionKey: response.message])
        }

        Logger.shared.log("Plugin uninstalled: \(name)")
        try? await Task.sleep(nanoseconds: 1 * 1_000_000_000)
        await fetchPlugins()
    }

    // MARK: - Update Management

    func checkForUpdates(currentVersion: String) async throws -> Aether_UpdateInfo {
        var request = Aether_CheckUpdateRequest()
        request.currentVersion = currentVersion
        request.includePrerelease = false  // TODO: Add setting for this

        return try await grpcClient.client.checkForUpdates(request)
    }

    func downloadUpdate(version: String, progressHandler: @escaping (Double) -> Void) async throws {
        var request = Aether_DownloadUpdateRequest()
        request.version = version

        try await grpcClient.client.downloadUpdate(request) { response in
            for try await progress in response.messages {
                if progress.status == .downloading {
                    let percent = Double(progress.percent) / 100.0
                    progressHandler(percent)
                } else if progress.status == .failed {
                    throw NSError(
                        domain: "UpdateError", code: 1,
                        userInfo: [NSLocalizedDescriptionKey: progress.errorMessage])
                }
            }
        }
    }

    func installUpdate(extractPath: String) async throws {
        var request = Aether_InstallUpdateRequest()
        request.extractPath = extractPath
        let response = try await grpcClient.client.installAppUpdate(request)

        if !response.success {
            throw NSError(
                domain: "UpdateError", code: 2,
                userInfo: [NSLocalizedDescriptionKey: response.message])
        }
    }
}
