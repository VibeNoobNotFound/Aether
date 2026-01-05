import AetherIPC
import SwiftUI

struct SearchResultsView: View {
    @ObservedObject var viewModel: SearchViewModel

    let columns = [
        GridItem(.adaptive(minimum: 160, maximum: 200), spacing: 20)
    ]

    var body: some View {
        VStack(spacing: 0) {
            // Filter Bar
            ScrollView(.horizontal, showsIndicators: false) {
                HStack(spacing: 12) {
                    // Dynamic Importer Filters from AppState
                    ForEach(viewModel.availableImporters) { plugin in
                        FilterChip(
                            title: plugin.name,
                            icon: "gamecontroller",
                            isSelected: viewModel.filterPlatform == plugin.name
                        ) {
                            if viewModel.filterPlatform == plugin.name {
                                viewModel.filterPlatform = nil
                            } else {
                                viewModel.filterPlatform = plugin.name
                            }
                        }
                    }

                    // Sort Options
                    Menu {
                        Picker("Sort By", selection: $viewModel.sortBy) {
                            Text("Relevance").tag(Aether_LibrarySearchRequest.SortOption.relevance)
                            Text("Name").tag(Aether_LibrarySearchRequest.SortOption.name)
                            Text("Date Added").tag(
                                Aether_LibrarySearchRequest.SortOption.releaseDate)
                            Text("Playtime").tag(Aether_LibrarySearchRequest.SortOption.playtime)
                        }
                    } label: {
                        HStack(spacing: 4) {
                            Image(systemName: "arrow.up.arrow.down")
                            Text("Sort")
                        }
                        .font(.system(size: 13, weight: .medium))
                        .padding(.horizontal, 10)
                        .padding(.vertical, 5)
                        .background(.regularMaterial)  // Standard macOS material
                        .cornerRadius(8)
                        .overlay(
                            RoundedRectangle(cornerRadius: 8)
                                .stroke(Color.white.opacity(0.1), lineWidth: 1)
                        )
                    }
                    .menuStyle(.borderlessButton)
                    .fixedSize()
                }
                .padding(.horizontal, 24)
                .padding(.vertical, 12)
            }
            .background(.regularMaterial)  // Use material instead of opacity black for better glass effect

            // Results Grid
            ScrollView {
                if viewModel.isSearching && viewModel.results.isEmpty {
                    ProgressView()
                        .padding(.top, 40)
                } else if viewModel.results.isEmpty && !viewModel.query.isEmpty {
                    VStack(spacing: 16) {
                        Image(systemName: "magnifyingglass")
                            .font(.system(size: 40))
                            .foregroundColor(.secondary)
                        Text("No games found")
                            .font(.headline)
                            .foregroundColor(.secondary)
                    }
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
                    .padding(.top, 60)
                } else {
                    LazyVGrid(columns: columns, spacing: 20) {
                        ForEach(viewModel.results) { game in
                            GameGridCard(game: game)
                        }
                    }
                    .padding(24)
                    .padding(.bottom, 60)
                }
            }
        }
        .background(
            ZStack {
                Color.black.ignoresSafeArea()

                // Subtle gradient blobs to match LibraryView/SettingsView
                GeometryReader { proxy in
                    Circle()
                        .fill(Color.blue.opacity(0.1))
                        .frame(width: 400, height: 400)
                        .blur(radius: 100)
                        .position(x: 0, y: 0)

                    Circle()
                        .fill(Color.purple.opacity(0.1))
                        .frame(width: 300, height: 300)
                        .blur(radius: 80)
                        .position(x: proxy.size.width, y: proxy.size.height)
                }
                .ignoresSafeArea()
            }
        )
    }
}

struct FilterChip: View {
    let title: String
    let icon: String?
    let isSelected: Bool
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            HStack(spacing: 6) {
                if let icon = icon {
                    Image(systemName: icon)
                }
                Text(title)
            }
            .font(.system(size: 13, weight: .medium))
            .padding(.horizontal, 12)
            .padding(.vertical, 6)
            .foregroundColor(isSelected ? .white : .primary)
            .cornerRadius(12)
            .overlay(
                RoundedRectangle(cornerRadius: 12)
                    .stroke(
                        isSelected ? Color.clear : Color.white.opacity(0.1),
                        lineWidth: 1
                    )
            )
        }
        .buttonStyle(.plain)
    }
}
