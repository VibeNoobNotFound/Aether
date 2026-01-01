import SwiftUI

struct MainWindowView: View {
    @EnvironmentObject var appState: AppState
    @AppStorage("useTopNavigation") private var useTopNavigation = false

    var body: some View {
        Group {
            if useTopNavigation {
                // Top Navigation Layout - Nav in title bar using toolbar
                NavigationStack {
                    ZStack {
                        Color.black.ignoresSafeArea()

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
                    .toolbar {
                        ToolbarItem(placement: .principal) {
                            TopNavigationBar()
                        }
                    }
                    .navigationDestination(for: GameViewModel.self) { game in
                        GameDetailView(game: game)
                    }
                    .toolbarBackground(.automatic, for: .windowToolbar)
                }
                .overlay(alignment: .bottom) {
                    ConnectionStatusBar()
                        .padding(.bottom, 16)
                }
            } else {
                // Sidebar Navigation Layout
                NavigationSplitView {
                    SidebarView()
                        .frame(minWidth: 200)
                } detail: {
                    NavigationStack {
                        ZStack {
                            Color.black.edgesIgnoringSafeArea(.all)

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
                        .toolbarBackground(.automatic, for: .windowToolbar)
                    }
                }
                .overlay(alignment: .bottom) {
                    ConnectionStatusBar()
                        .padding(.bottom, 16)
                }
            }
        }
        .frame(minWidth: 800, minHeight: 600)
        .task {
            await appState.refreshLibrary()
        }
    }
}

// Top Navigation Bar Component - Clean style (uses macOS toolbar glass)
struct TopNavigationBar: View {
    @EnvironmentObject var appState: AppState

    private let screens: [AppScreen] = [.home, .library, .store, .settings]

    var body: some View {
        HStack(spacing: 2) {
            ForEach(screens, id: \.self) { screen in
                Button {
                    withAnimation(.spring(response: 0.3, dampingFraction: 0.8)) {
                        appState.currentScreen = screen
                    }
                } label: {
                    Text(screen.title)
                        .font(
                            .system(
                                size: 13,
                                weight: appState.currentScreen == screen ? .semibold : .medium)
                        )
                        .foregroundStyle(
                            appState.currentScreen == screen ? .black : .white.opacity(0.9)
                        )
                        .padding(.horizontal, 14)
                        .padding(.vertical, 6)
                        .background(
                            Group {
                                if appState.currentScreen == screen {
                                    Capsule()
                                        .fill(Color.white)
                                }
                            }
                        )
                }
                .buttonStyle(.plain)
            }
        }
    }
}
