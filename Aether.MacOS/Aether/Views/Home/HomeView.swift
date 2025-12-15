import SwiftUI

struct HomeView: View {
    @EnvironmentObject var appState: AppState

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 20) {
                HeroCarousel()

                if !appState.games.filter({ $0.isFavorite }).isEmpty {
                    Text("Favorites")
                        .font(.title2)
                        .fontWeight(.bold)
                        .padding(.horizontal)

                    favoritesList
                }

                Text("Jump Back In")
                    .font(.title2)
                    .fontWeight(.bold)
                    .padding(.horizontal)

                recentGamesList
            }
            .padding(.vertical)
        }
        .toolbar {
            ToolbarItem {
                Button(action: {
                    Task {
                        await appState.scanLibrary()
                    }
                }) {
                    Label("Scan Library", systemImage: "arrow.clockwise")
                }
            }
        }
    }

    var favoritesList: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 15) {
                ForEach(appState.games.filter { $0.isFavorite }) { game in
                    NavigationLink(value: game) {
                        GameGridCard(game: game)
                            .frame(width: 200)
                            .contextMenu {
                                Button {
                                    appState.launchGame(game)
                                } label: {
                                    Label("Play", systemImage: "play.fill")
                                }

                                Button {
                                    Task { await appState.toggleFavorite(game: game) }
                                } label: {
                                    Label("Unfavorite", systemImage: "heart.slash")
                                }

                                Button {
                                    Task { await appState.openGameLocation(game: game) }
                                } label: {
                                    Label("Show in Finder", systemImage: "folder")
                                }
                            }
                    }
                    .buttonStyle(.plain)
                }
            }
            .padding(.horizontal)
        }
    }

    var recentGamesList: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 15) {
                ForEach(appState.games) { game in
                    NavigationLink(value: game) {
                        GameGridCard(game: game)
                            .frame(width: 200)
                            .contextMenu {
                                Button {
                                    appState.launchGame(game)
                                } label: {
                                    Label("Play", systemImage: "play.fill")
                                }

                                Button {
                                    Task { await appState.toggleFavorite(game: game) }
                                } label: {
                                    Label(
                                        game.isFavorite ? "Unfavorite" : "Favorite",
                                        systemImage: game.isFavorite ? "heart.slash" : "heart")
                                }

                                Button {
                                    Task { await appState.openGameLocation(game: game) }
                                } label: {
                                    Label("Show in Finder", systemImage: "folder")
                                }
                            }
                    }
                    .buttonStyle(.plain)
                }
            }
            .padding(.horizontal)
        }
    }
}
