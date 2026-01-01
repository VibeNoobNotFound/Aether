import SwiftUI
import Textual

/// Update prompt view with liquid glass aesthetic
struct UpdateView: View {
    @ObservedObject var updateManager = UpdateManager.shared
    @Environment(\.dismiss) private var dismiss

    var body: some View {
        ZStack {
            // Dark background with blur
            Color.black.opacity(0.85)
                .ignoresSafeArea()

            // Subtle gradient blobs
            GeometryReader { proxy in
                Circle()
                    .fill(Color.blue.opacity(0.15))
                    .frame(width: 300, height: 300)
                    .blur(radius: 80)
                    .position(x: 50, y: 50)

                Circle()
                    .fill(Color.purple.opacity(0.12))
                    .frame(width: 250, height: 250)
                    .blur(radius: 70)
                    .position(x: proxy.size.width - 50, y: proxy.size.height - 50)
            }
            .ignoresSafeArea()

            VStack(spacing: 24) {
                // Icon
                ZStack {
                    Circle()
                        .fill(
                            LinearGradient(
                                colors: [.blue, .purple],
                                startPoint: .topLeading,
                                endPoint: .bottomTrailing
                            )
                        )
                        .frame(width: 80, height: 80)

                    Image(systemName: "arrow.down.circle.fill")
                        .font(.system(size: 40))
                        .foregroundStyle(.white)
                }
                .shadow(color: .blue.opacity(0.4), radius: 20)

                // Title
                VStack(spacing: 8) {
                    Text("Update Available")
                        .font(.system(size: 28, weight: .bold, design: .rounded))
                        .foregroundStyle(.white)

                    if let info = updateManager.updateInfo {
                        Text("Version \(info.version)")
                            .font(.headline)
                            .foregroundStyle(.secondary)

                        if info.isPrerelease {
                            Text("Pre-release")
                                .font(.caption)
                                .fontWeight(.semibold)
                                .padding(.horizontal, 8)
                                .padding(.vertical, 4)
                                .background(.orange.opacity(0.2))
                                .foregroundStyle(.orange)
                                .clipShape(Capsule())
                        }
                    }
                }

                // Release notes
                if let info = updateManager.updateInfo, !info.releaseNotes.isEmpty {
                    GlassCard(padding: 0) {
                        ScrollView {
                            // Use Textual for robust Markdown rendering
                            // StructuredText(markdown:) creates a document from a markdown string
                            StructuredText(markdown: info.releaseNotes)
                                .textSelection(.enabled)
                                .padding(.horizontal, 4)
                        }
                        .frame(maxHeight: 300)
                        .padding()
                    }
                }

                // Auto-update toggle
                GlassCard {
                    HStack {
                        VStack(alignment: .leading, spacing: 2) {
                            Text("Check automatically")
                                .font(.body)
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
                                        forKey: "automaticallyCheckForUpdates")
                                        as? Bool ?? true
                                },
                                set: {
                                    UserDefaults.standard.set(
                                        $0, forKey: "automaticallyCheckForUpdates")
                                }
                            )
                        )
                        .toggleStyle(.switch)
                    }
                }

                // Progress indicator
                if updateManager.downloadStatus != .idle && updateManager.downloadStatus != .failed
                {
                    GlassCard {
                        VStack(spacing: 12) {
                            ProgressView(value: updateManager.downloadProgress)
                                .progressViewStyle(.linear)
                                .tint(.blue)

                            Text(statusText)
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        }
                    }
                }

                // Error message
                if let error = updateManager.errorMessage {
                    GlassCard(tint: .red) {
                        HStack {
                            Image(systemName: "exclamationmark.triangle.fill")
                                .foregroundStyle(.red)
                            Text(error)
                                .font(.caption)
                                .foregroundStyle(.red)
                        }
                    }
                }

                // Actions
                HStack(spacing: 16) {
                    GlassButton("Later", tint: .gray) {
                        updateManager.dismissUpdate()
                        dismiss()
                    }

                    switch updateManager.downloadStatus {
                    case .idle:
                        GlassButton("Update Now", tint: .blue) {
                            Task { await updateManager.downloadUpdate() }
                        }

                    case .readyToInstall(let path):
                        GlassButton("Install & Restart", tint: .green) {
                            Task { await updateManager.installUpdate(extractPath: path) }
                        }

                    case .failed:
                        GlassButton("Retry", tint: .orange) {
                            Task { await updateManager.downloadUpdate() }
                        }

                    default:
                        GlassButton("Downloading...", tint: .blue) {}
                            .disabled(true)
                    }
                }
            }
            .padding(40)
            .frame(width: 600)
        }
        .frame(width: 600, height: 700)
    }

    private var statusText: String {
        switch updateManager.downloadStatus {
        case .checking:
            return "Checking for updates..."
        case .downloading:
            return "Downloading... \(Int(updateManager.downloadProgress * 100))%"
        case .extracting:
            return "Extracting update..."
        case .readyToInstall:
            return "Ready to install!"
        case .failed:
            return "Download failed"
        case .idle:
            return ""
        }
    }
}

#Preview {
    UpdateView()
}
