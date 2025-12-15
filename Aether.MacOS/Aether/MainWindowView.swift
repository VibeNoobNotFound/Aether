import SwiftUI

struct MainWindowView: View {
    @EnvironmentObject var appState: AppState

    var body: some View {
        NavigationSplitView {
            SidebarView()
                .frame(minWidth: 200)
        } detail: {
            NavigationStack {
                ZStack {
                    // Global Background
                    Color.black.edgesIgnoringSafeArea(.all)  // Dark mode base

                    switch appState.currentScreen {
                    case .home:
                        HomeView()
                    case .library:
                        LibraryView()
                    case .store:
                        Text("Coming Soon")
                            .font(.largeTitle)
                            .foregroundStyle(.secondary)
                    case .settings:
                        SettingsView()
                    }

                }
                .navigationDestination(for: GameViewModel.self) { game in
                    GameDetailView(game: game)
                }
            }
        }
        .frame(minWidth: 800, minHeight: 600)
        .task {
            // Load library on start
            await appState.refreshLibrary()
        }
    }
}
