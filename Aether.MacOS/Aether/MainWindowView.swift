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
                case .store, .settings:
                    Text("Coming Soon")
                        .font(.largeTitle)
                        .foregroundStyle(.secondary)
                }
            }
        }
        .frame(minWidth: 800, minHeight: 600)
    }
}
