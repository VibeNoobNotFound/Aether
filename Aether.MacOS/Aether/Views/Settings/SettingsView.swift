import SwiftUI

struct SettingsView: View {
    @EnvironmentObject var appState: AppState

    var body: some View {
        NavigationStack {
            List {
                Section {
                    ForEach(appState.plugins) { plugin in
                        NavigationLink(destination: PluginSetupView(plugin: plugin)) {
                            PluginRow(plugin: plugin)
                        }
                    }

                } header: {
                    Text("Installed Plugins")
                } footer: {
                    Text("Plugins extend Aether with new libraries and features.")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }
            .listStyle(.sidebar)  // or .insetGrouped for a different look
            .navigationTitle("Settings")
            .task {
                await appState.fetchPlugins()
            }
        }
    }
}

struct PluginRow: View {
    let plugin: PluginViewModel

    var body: some View {
        HStack(spacing: 12) {
            // Icon based on type
            ZStack {
                RoundedRectangle(cornerRadius: 8)
                    .fill(plugin.isImporter ? Color.blue.opacity(0.1) : Color.green.opacity(0.1))
                    .frame(width: 36, height: 36)

                Image(systemName: plugin.isImporter ? "arrow.down.circle.fill" : "puzzlepiece.fill")
                    .foregroundStyle(plugin.isImporter ? .blue : .green)
                    .font(.system(size: 18))
            }

            VStack(alignment: .leading, spacing: 2) {
                Text(plugin.name)
                    .font(.body)
                    .fontWeight(.medium)

                HStack(spacing: 6) {
                    Text("v\(plugin.version)")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .monospacedDigit()

                    Text("•")
                        .font(.caption)
                        .foregroundStyle(.secondary)

                    Text(plugin.author)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }

            Spacer()

            // Status Badge
            Text(plugin.isImporter ? "Importer" : "Plugin")
                .font(.caption)
                .padding(.horizontal, 8)
                .padding(.vertical, 4)
                .background(Color.secondary.opacity(0.1))
                .clipShape(Capsule())
                .foregroundStyle(.secondary)
        }
        .padding(.vertical, 4)
    }
}
