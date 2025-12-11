import SwiftUI

struct SidebarView: View {
    @EnvironmentObject var appState: AppState

    var body: some View {
        List(selection: $appState.currentScreen) {
            Section(header: Text("Explore")) {
                NavigationLink(value: AppScreen.home) {
                    Label("Home", systemImage: AppScreen.home.icon)
                }
                NavigationLink(value: AppScreen.store) {
                    Label("Store", systemImage: AppScreen.store.icon)
                }
            }

            Section(header: Text("Library")) {
                NavigationLink(value: AppScreen.library) {
                    Label("All Games", systemImage: AppScreen.library.icon)
                }
            }

            Section(header: Text("System")) {
                NavigationLink(value: AppScreen.settings) {
                    Label("Settings", systemImage: AppScreen.settings.icon)
                }
            }
        }
        .listStyle(.sidebar)
        .navigationTitle("Aether")
    }
}
