import SwiftUI

struct MainWindowView: View {
    @EnvironmentObject var appState: AppState

    var body: some View {
        NavigationSplitView {
            SidebarView()
                .frame(minWidth: 200)
        } detail: {
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
        }
        .frame(minWidth: 800, minHeight: 600)
    }
}
