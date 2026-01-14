import SwiftUI
internal import UniformTypeIdentifiers

struct SettingsView: View {
    @EnvironmentObject var appState: AppState
    @State private var isImportingPlugin = false
    @State private var showMetadataSettings = false
    @State private var showFactoryResetAlert = false

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
                VStack(alignment: .leading, spacing: 32) {

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

                    // 1. ABOUT
                    SettingsTile {
                        HStack(spacing: 16) {
                            Image(nsImage: NSImage(named: "Aether") ?? NSImage())
                                .resizable()
                                .frame(width: 48, height: 48)
                                .clipShape(RoundedRectangle(cornerRadius: 10))

                            VStack(alignment: .leading, spacing: 4) {
                                Text(
                                    Bundle.main.infoDictionary?["CFBundleName"] as? String
                                        ?? "Aether"
                                )
                                .font(.title3)
                                .fontWeight(.bold)
                                .foregroundStyle(.white)

                                HStack(spacing: 6) {
                                    Text(
                                        "Version \(Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "0.0.0")"
                                    )
                                    .font(.subheadline)
                                    .foregroundStyle(.secondary)
                                    Text(
                                        "(\(Bundle.main.infoDictionary?["CFBundleVersion"] as? String ?? "0"))"
                                    )
                                    .font(.caption)
                                    .foregroundStyle(.secondary.opacity(0.7))
                                }
                            }
                            Spacer()
                        }
                    }

                    // 2. APPEARANCE
                    VStack(alignment: .leading, spacing: 16) {
                        SectionHeader(title: "APPEARANCE")

                        SettingsTile {
                            VStack(spacing: 0) {
                                HStack(spacing: 16) {
                                    SettingsIcon(icon: "sidebar.left", color: .orange)

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
                                            get: {
                                                UserDefaults.standard.bool(
                                                    forKey: "useTopNavigation")
                                            },
                                            set: {
                                                UserDefaults.standard.set(
                                                    $0, forKey: "useTopNavigation")
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

                                Divider().background(.white.opacity(0.1))

                                HStack(spacing: 16) {
                                    SettingsIcon(icon: "square.stack.3d.up", color: .blue)

                                    VStack(alignment: .leading, spacing: 2) {
                                        Text("Game Card Style")
                                            .font(.body)
                                            .fontWeight(.medium)
                                            .foregroundStyle(.white)
                                        Text("Use Liquid Glass effect on game cards")
                                            .font(.caption)
                                            .foregroundStyle(.secondary)
                                    }

                                    Spacer()

                                    Picker(
                                        "",
                                        selection: Binding(
                                            get: {
                                                UserDefaults.standard.bool(
                                                    forKey: "useLiquidGlassCards")
                                            },
                                            set: {
                                                UserDefaults.standard.set(
                                                    $0, forKey: "useLiquidGlassCards")
                                            }
                                        )
                                    ) {
                                        Text("Standard").tag(false)
                                        Text("Liquid Glass").tag(true)
                                    }
                                    .pickerStyle(.segmented)
                                    .frame(width: 180)
                                }
                                .padding()
                            }
                        }
                    }

                    // 3. UPDATES
                    VStack(alignment: .leading, spacing: 16) {
                        SectionHeader(title: "UPDATES")

                        SettingsTile {
                            VStack(spacing: 0) {
                                // Auto Check
                                HStack(spacing: 16) {
                                    SettingsIcon(
                                        icon: "arrow.triangle.2.circlepath.circle.fill",
                                        color: .green)

                                    VStack(alignment: .leading, spacing: 2) {
                                        Text("Automatic Updates")
                                            .font(.body)
                                            .fontWeight(.medium)
                                            .foregroundStyle(.white)
                                        Text("Check for updates on launch")
                                            .font(.caption)
                                            .foregroundStyle(.secondary)
                                    }

                                    Spacer()

                                    Toggle(
                                        "",
                                        isOn: Binding(
                                            get: {
                                                UserDefaults.standard.object(
                                                    forKey: "automaticallyCheckForUpdates") as? Bool
                                                    ?? true
                                            },
                                            set: {
                                                UserDefaults.standard.set(
                                                    $0, forKey: "automaticallyCheckForUpdates")
                                            }
                                        )
                                    )
                                    .toggleStyle(.switch)
                                }
                                .padding()

                                Divider().background(.white.opacity(0.1))

                                // Beta
                                HStack(spacing: 16) {
                                    SettingsIcon(icon: "testtube.2", color: .purple)

                                    VStack(alignment: .leading, spacing: 2) {
                                        Text("Include Beta Updates")
                                            .font(.body)
                                            .fontWeight(.medium)
                                            .foregroundStyle(.white)
                                        Text("Get pre-release versions")
                                            .font(.caption)
                                            .foregroundStyle(.secondary)
                                    }

                                    Spacer()

                                    Toggle(
                                        "",
                                        isOn: Binding(
                                            get: {
                                                UserDefaults.standard.bool(
                                                    forKey: "includeBetaUpdates")
                                            },
                                            set: {
                                                UserDefaults.standard.set(
                                                    $0, forKey: "includeBetaUpdates")
                                            }
                                        )
                                    )
                                    .toggleStyle(.switch)
                                }
                                .padding()
                            }
                        }

                        // Check Now Action Card
                        Button {
                            Task { await UpdateManager.shared.checkForUpdates() }
                        } label: {
                            SettingsActionCard(
                                icon: "arrow.triangle.2.circlepath",
                                color: .blue,
                                title: "Check for Updates",
                                description: "Scan for the latest version"
                            )
                        }
                        .buttonStyle(.plain)
                    }

                    // 4. LIBRARY
                    VStack(alignment: .leading, spacing: 16) {
                        SectionHeader(title: "LIBRARY")

                        Button {
                            showMetadataSettings = true
                        } label: {
                            SettingsActionCard(
                                icon: "list.number",
                                color: .purple,
                                title: "Metadata Sources",
                                description: "Configure provider priority"
                            )
                        }
                        .buttonStyle(.plain)
                        .sheet(isPresented: $showMetadataSettings) {
                            MetadataSettingsView()
                                .presentationDetents([.medium, .large])
                        }

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

                    // 5. PLUGINS
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

                    // 6. ADVANCED
                    VStack(alignment: .leading, spacing: 16) {
                        SectionHeader(title: "ADVANCED")

                        Button {
                            let appSupport = FileManager.default.urls(
                                for: .applicationSupportDirectory, in: .userDomainMask
                            ).first!
                            let logsDir = appSupport.appendingPathComponent("Aether")
                            NSWorkspace.shared.open(logsDir)
                        } label: {
                            SettingsActionCard(
                                icon: "doc.text.magnifyingglass",
                                color: .orange,
                                title: "Open Logs Folder",
                                description: "View application logs and data"
                            )
                        }
                        .buttonStyle(.plain)

                        Button {
                            showFactoryResetAlert = true
                        } label: {
                            SettingsActionCard(
                                icon: "exclamationmark.triangle.fill",
                                color: .red,
                                title: "Factory Reset",
                                description: "Clear library, reset settings, and restart onboarding"
                            )
                        }
                        .buttonStyle(.plain)
                        .alert("Factory Reset", isPresented: $showFactoryResetAlert) {
                            Button("Cancel", role: .cancel) {}
                            Button("Reset Everything", role: .destructive) {
                                Task { await performFactoryReset() }
                            }
                        } message: {
                            Text(
                                "Are you sure? This will delete your entire library database and reset all settings to default. This action cannot be undone."
                            )
                        }
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
            allowedContentTypes: [.item],
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

    func performFactoryReset() async {
        // 1. Call Backend Factory Reset (Clears DB collections)
        try? await BackendManager.shared.resetBackend()

        // 2. Clear UserDefaults (Settings)
        if let bundleID = Bundle.main.bundleIdentifier {
            UserDefaults.standard.removePersistentDomain(forName: bundleID)
        }

        // 3. Reset Onboarding State
        UserDefaults.standard.set(false, forKey: "hasCompletedOnboarding")

        // 4. Restart Application
        let url = URL(fileURLWithPath: Bundle.main.bundlePath)
        let config = NSWorkspace.OpenConfiguration()
        config.arguments = []
        config.createsNewApplicationInstance = true

        NSWorkspace.shared.openApplication(at: url, configuration: config) { _, _ in
            DispatchQueue.main.async {
                NSApplication.shared.terminate(nil)
            }
        }
    }
}

// MARK: - Components

struct SectionHeader: View {
    let title: String
    var body: some View {
        Text(title)
            .font(.caption)
            .fontWeight(.bold)
            .foregroundStyle(.secondary)
            .padding(.horizontal)
    }
}

struct SettingsTile<Content: View>: View {
    let content: Content

    init(@ViewBuilder content: () -> Content) {
        self.content = content()
    }

    var body: some View {
        Group {
            content
        }
        .padding()
        .background(.ultraThinMaterial)
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .padding(.horizontal)
    }
}

struct SettingsIcon: View {
    let icon: String
    let color: Color

    var body: some View {
        ZStack {
            Circle()
                .fill(color.opacity(0.2))
                .frame(width: 40, height: 40)

            Image(systemName: icon)
                .font(.system(size: 20))
                .foregroundStyle(color)
        }
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

#Preview {
    #if DEBUG
        SettingsView()
            .environmentObject(MockData.appState)
    #endif
}
