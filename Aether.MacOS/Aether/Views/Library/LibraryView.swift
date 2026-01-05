import SwiftUI

struct LibraryView: View {
    @EnvironmentObject var appState: AppState
    @State private var selectedGame: GameViewModel?
    @State private var selectedPlugin: PluginViewModel?
    @State private var showCollectionEditor = false

    let columns = [
        GridItem(.adaptive(minimum: 180, maximum: 220), spacing: 20)
    ]

    var body: some View {
        ZStack {
            // Ambient Background
            Color.black.ignoresSafeArea()

            // Subtle gradient blobs (Matching SettingsView)
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
        // Removed manual top padding
        .toolbar {
            ToolbarItemGroup(placement: .primaryAction) {
                // Add Game Menu
                Menu {
                    ForEach(appState.plugins.filter { $0.supportsManualAddition }) { plugin in
                        Button(action: {
                            selectedPlugin = plugin
                        }) {
                            Label(plugin.name, systemImage: "plus")
                        }
                    }
                } label: {
                    Label("Add Game", systemImage: "plus")
                }

                // Management Group
                ControlGroup {
                    Button(action: {
                        showCollectionEditor = true
                    }) {
                        Label("Collections", systemImage: "square.grid.3x3")
                    }

                    Button(action: {
                        Task { await appState.scanLibrary() }
                    }) {
                        Label("Scan", systemImage: "arrow.clockwise")
                    }
                }

                // Destructive/Advanced Actions Menu
                Menu {
                    Button(role: .destructive) {
                        Task { await appState.clearLibrary() }
                    } label: {
                        Label("Clear Library", systemImage: "trash")
                    }
                } label: {
                    Label("More", systemImage: "ellipsis.circle")
                }
            }
        }
        .sheet(item: $selectedPlugin) { plugin in
            LibraryAddMenuView(pluginName: plugin.name)
                .frame(minWidth: 500, minHeight: 400)
        }
        .sheet(isPresented: $showCollectionEditor) {
            CollectionEditorSheet()
        }
    }
}
