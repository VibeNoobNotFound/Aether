import AetherIPC
import Combine
import Foundation
import GRPCCore
import NIO

@MainActor
class SearchViewModel: ObservableObject {
    @Published var query: String = ""
    @Published var results: [GameViewModel] = []
    @Published var totalMatches: Int = 0
    @Published var isSearching: Bool = false
    @Published var errorMessage: String?

    // Filters
    // @Published var filterInstalled: Bool = false // Removed
    @Published var filterPlatform: String? = nil  // nil = all
    @Published var filterGenre: String? = nil

    // Sorting
    @Published var sortBy: Aether_LibrarySearchRequest.SortOption = .relevance
    @Published var sortAscending: Bool = false

    // Reference to AppState for plugins
    var appState: AppState?

    var availableImporters: [PluginViewModel] {
        appState?.plugins.filter { $0.isImporter } ?? []
    }

    private var cancellables = Set<AnyCancellable>()
    private var searchDebounceTask: Task<Void, Never>?

    init() {
        // Debounce search query
        $query
            .removeDuplicates()
            .debounce(for: .milliseconds(300), scheduler: RunLoop.main)
            .sink { [weak self] _ in
                self?.performSearch()
            }
            .store(in: &cancellables)

        // Trigger search on filter changes
        Publishers.CombineLatest3($filterPlatform, $filterGenre, $sortBy)
            .sink { [weak self] _ in
                self?.performSearch()
            }
            .store(in: &cancellables)

        $sortAscending
            .sink { [weak self] _ in
                self?.performSearch()
            }
            .store(in: &cancellables)
    }

    func performSearch() {
        searchDebounceTask?.cancel()

        // Don't search if empty query unless a filter is active
        if query.isEmpty && filterPlatform == nil && filterGenre == nil {
            self.results = []
            return
        }

        searchDebounceTask = Task {
            do {
                self.isSearching = true
                self.errorMessage = nil

                var request = Aether_LibrarySearchRequest()
                request.query = query
                // request.filterInstalled = filterInstalled // Removed
                if let platform = filterPlatform {
                    request.filterPlatforms = [platform]
                }
                if let genre = filterGenre {
                    request.filterGenres = [genre]
                }
                request.sortBy = sortBy
                request.sortAscending = sortAscending
                request.limit = 50

                let response = try await GrpcClient.shared.client.searchLibrary(request)

                // Check cancellation
                if Task.isCancelled { return }

                self.results = response.games.map { GameViewModel(from: $0) }
                self.totalMatches = Int(response.totalMatches)
                self.isSearching = false

            } catch {
                if !Task.isCancelled {
                    self.isSearching = false
                    self.errorMessage = error.localizedDescription
                    print("Search Error: \(error)")
                }
            }
        }
    }

    func clearFilters() {
        filterPlatform = nil
        filterGenre = nil
        query = ""
    }
}
