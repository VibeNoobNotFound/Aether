import SwiftUI
internal import UniformTypeIdentifiers

struct SettingsView: View {
    @EnvironmentObject var appState: AppState
    @State private var isImportingPlugin = false

    var body: some View {
        ZStack {
            // Ambient Background
            Color.black.ignoresSafeArea()

            // Subtle gradient blobs
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

            ScrollView {
                VStack(alignment: .leading, spacing: 30) {

                    // Header
                    VStack(alignment: .leading, spacing: 8) {
                        Text("Settings")
                            .font(.system(size: 32, weight: .bold, design: .rounded))
                            .foregroundStyle(.white)

                        Text("Manage your library and plugins")
                            .font(.subheadline)
                            .foregroundStyle(.secondary)
                    }
                    .padding(.top, 40)
                    .padding(.horizontal)

                    // Appearance Section
                    VStack(alignment: .leading, spacing: 16) {
                        Text("APPEARANCE")
                            .font(.caption)
                            .fontWeight(.bold)
                            .foregroundStyle(.secondary)
                            .padding(.horizontal)

                        HStack(spacing: 16) {
                            ZStack {
                                Circle()
                                    .fill(Color.orange.opacity(0.2))
                                    .frame(width: 40, height: 40)

                                Image(systemName: "sidebar.left")
                                    .font(.system(size: 20))
                                    .foregroundStyle(.orange)
                            }

                            VStack(alignment: .leading, spacing: 2) {
                                Text("Navigation Style")
                                    .font(.body)
                                    .fontWeight(.medium)
                                    .foregroundStyle(.white)

                                Text("Choose between sidebar or top navigation")
                                    .font(.caption)
                                    .foregroundStyle(.secondary)
                            }

                            Spacer()

                            Picker(
                                "",
                                selection: Binding(
                                    get: { UserDefaults.standard.bool(forKey: "useTopNavigation") },
                                    set: {
                                        UserDefaults.standard.set($0, forKey: "useTopNavigation")
                                    }
                                )
                            ) {
                                Text("Sidebar").tag(false)
                                Text("Top").tag(true)
                            }
                            .pickerStyle(.segmented)
                            .frame(width: 150)
                        }
                        .padding()
                        .background(.ultraThinMaterial)
                        .clipShape(RoundedRectangle(cornerRadius: 12))
                        .padding(.horizontal)
                    }

                    // Plugins Section
                    VStack(alignment: .leading, spacing: 16) {
                        HStack {
                            Text("INSTALLED PLUGINS")
                                .font(.caption)
                                .fontWeight(.bold)
                                .foregroundStyle(.secondary)

                            Spacer()

                            Button {
                                isImportingPlugin = true
                            } label: {
                                Label("Add Plugin", systemImage: "plus.circle")
                                    .font(.caption)
                                    .fontWeight(.medium)
                                    .foregroundStyle(.blue)
                            }
                            .buttonStyle(.plain)
                        }
                        .padding(.horizontal)

                        LazyVGrid(
                            columns: [GridItem(.adaptive(minimum: 300), spacing: 16)], spacing: 16
                        ) {
                            ForEach(appState.plugins) { plugin in
                                NavigationLink(destination: PluginSetupView(plugin: plugin)) {
                                    PluginCard(plugin: plugin)
                                }
                                .buttonStyle(.plain)
                                .contextMenu {
                                    Button(role: .destructive) {
                                        Task {
                                            try? await appState.uninstallPlugin(name: plugin.name)
                                        }
                                    } label: {
                                        Label("Uninstall Plugin", systemImage: "trash")
                                    }
                                }
                            }
                        }
                        .padding(.horizontal)
                    }

                    // Library Management
                    VStack(alignment: .leading, spacing: 16) {
                        Text("LIBRARY")
                            .font(.caption)
                            .fontWeight(.bold)
                            .foregroundStyle(.secondary)
                            .padding(.horizontal)

                        Button {
                            Task { await appState.scanLibrary() }
                        } label: {
                            SettingsActionCard(
                                icon: "arrow.triangle.2.circlepath",
                                color: .blue,
                                title: "Rescan Library",
                                description: "Scan all sources for new games"
                            )
                        }
                        .buttonStyle(.plain)

                        Button {
                            Task { await appState.clearLibrary() }
                        } label: {
                            SettingsActionCard(
                                icon: "trash",
                                color: .red,
                                title: "Clear Library",
                                description: "Remove all games from the database"
                            )
                        }
                        .buttonStyle(.plain)
                    }
                }
                .padding(.bottom, 50)
            }
        }
        .task {
            await appState.fetchPlugins()
        }
        .fileImporter(
            isPresented: $isImportingPlugin,
            allowedContentTypes: [.item],  // Ideally .dll or generic data
            allowsMultipleSelection: false
        ) { result in
            switch result {
            case .success(let urls):
                guard let url = urls.first else { return }
                guard url.startAccessingSecurityScopedResource() else { return }
                defer { url.stopAccessingSecurityScopedResource() }

                Task {
                    do {
                        try await appState.installPlugin(fileURL: url)
                    } catch {
                        print("Failed to install plugin: \(error)")
                    }
                }
            case .failure(let error):
                print("Import failed: \(error)")
            }
        }
    }
}

struct PluginCard: View {
    let plugin: PluginViewModel
    @State private var isHovered = false

    var body: some View {
        HStack(spacing: 16) {
            // Icon
            ZStack {
                Circle()
                    .fill(plugin.isImporter ? Color.blue.opacity(0.2) : Color.green.opacity(0.2))
                    .frame(width: 48, height: 48)

                Image(systemName: plugin.isImporter ? "arrow.down.circle.fill" : "puzzlepiece.fill")
                    .font(.system(size: 24))
                    .foregroundStyle(plugin.isImporter ? .blue : .green)
            }

            VStack(alignment: .leading, spacing: 4) {
                Text(plugin.name)
                    .font(.headline)
                    .foregroundStyle(.white)

                HStack(spacing: 6) {
                    Text("v\(plugin.version)")
                        .padding(.horizontal, 6)
                        .padding(.vertical, 2)
                        .background(.ultraThinMaterial)
                        .clipShape(Capsule())

                    Text("by \(plugin.author)")
                        .foregroundStyle(.secondary)
                }
                .font(.caption)
            }

            Spacer()

            Image(systemName: "chevron.right")
                .foregroundStyle(.secondary)
                .opacity(isHovered ? 1 : 0.5)
        }
        .padding()
        .background(.ultraThinMaterial)
        .clipShape(RoundedRectangle(cornerRadius: 16))
        .overlay(
            RoundedRectangle(cornerRadius: 16)
                .stroke(.white.opacity(isHovered ? 0.2 : 0.05), lineWidth: 1)
        )
        .scaleEffect(isHovered ? 1.02 : 1.0)
        .animation(.spring(response: 0.3), value: isHovered)
        .onHover { isHovered = $0 }
    }
}

struct SettingsActionCard: View {
    let icon: String
    let color: Color
    let title: String
    let description: String
    @State private var isHovered = false

    var body: some View {
        HStack(spacing: 16) {
            ZStack {
                Circle()
                    .fill(color.opacity(0.2))
                    .frame(width: 40, height: 40)

                Image(systemName: icon)
                    .font(.system(size: 20))
                    .foregroundStyle(color)
            }

            VStack(alignment: .leading, spacing: 2) {
                Text(title)
                    .font(.body)
                    .fontWeight(.medium)
                    .foregroundStyle(.white)

                Text(description)
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Spacer()
        }
        .padding()
        .background(.ultraThinMaterial)
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .padding(.horizontal)
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(isHovered ? color.opacity(0.5) : Color.clear, lineWidth: 1)
                .padding(.horizontal)
        )
        .scaleEffect(isHovered ? 1.01 : 1.0)
        .animation(.snappy, value: isHovered)
        .onHover { isHovered = $0 }
    }
}
