import SwiftUI

struct LibraryView: View {
    @EnvironmentObject var appState: AppState
    @State private var selectedGame: GameViewModel?
    @State private var selectedPlugin: PluginViewModel?

    let columns = [
        GridItem(.adaptive(minimum: 180, maximum: 220), spacing: 20)
    ]

    var body: some View {
        Group {
            if appState.games.isEmpty {
                VStack(spacing: 20) {
                    Image(systemName: "gamecontroller.fill")
                        .font(.system(size: 60))
                        .foregroundStyle(.secondary)

                    Text("No Games Found")
                        .font(.title2)
                        .fontWeight(.bold)

                    Text("Scan your library to find games from Steam, Epic, and more.")
                        .font(.body)
                        .foregroundStyle(.secondary)
                        .multilineTextAlignment(.center)
                        .padding(.horizontal, 40)

                    Button {
                        Task {
                            await appState.scanLibrary()
                        }
                    } label: {
                        Label("Scan Library", systemImage: "arrow.clockwise")
                            .padding(.horizontal, 10)
                            .padding(.vertical, 5)
                    }
                    .buttonStyle(.borderedProminent)
                    .controlSize(.large)
                }
                .frame(maxWidth: .infinity, maxHeight: .infinity)
            } else {
                ScrollView {
                    LazyVGrid(columns: columns, spacing: 20) {
                        ForEach(appState.games) { game in
                            NavigationLink(value: game) {
                                GameGridCard(game: game)
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
                                                systemImage: game.isFavorite
                                                    ? "heart.slash" : "heart")
                                        }

                                        Button {
                                            Task { await appState.openGameLocation(game: game) }
                                        } label: {
                                            Label("Show in Finder", systemImage: "folder")
                                        }

                                        Divider()

                                        Button(role: .destructive) {
                                            Task { await appState.removeGame(id: game.id) }
                                        } label: {
                                            Label("Remove from Library", systemImage: "trash")
                                        }
                                    }
                            }
                            .buttonStyle(.plain)
                        }
                    }
                    .padding()
                }
            }
        }
        .background(Color.clear)
        .toolbar {
            ToolbarItem(placement: .primaryAction) {
                HStack {
                    Button(action: {
                        if let customPlugin = appState.plugins.first(where: { $0.name == "Custom" })
                        {
                            selectedPlugin = customPlugin
                        }
                    }) {
                        Label("Add Game", systemImage: "plus")
                    }

                    Menu {
                        Button {
                            Task { await appState.scanLibrary() }
                        } label: {
                            Label("Scan Library", systemImage: "arrow.clockwise")
                        }

                        Button(role: .destructive) {
                            Task { await appState.clearLibrary() }
                        } label: {
                            Label("Clear Library", systemImage: "trash")
                        }
                    } label: {
                        Label("Manage", systemImage: "ellipsis.circle")
                    }
                }
            }
        }
        .sheet(item: $selectedPlugin) { plugin in
            PluginSetupView(plugin: plugin)
                .frame(minWidth: 500, minHeight: 400)
        }
    }

}
