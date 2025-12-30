import SwiftUI

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
                    ScrollView {
                        Text(info.releaseNotes)
                            .font(.body)
                            .foregroundStyle(.secondary)
                            .multilineTextAlignment(.leading)
                            .frame(maxWidth: .infinity, alignment: .leading)
                    }
                    .frame(maxHeight: 200)
                    .padding()
                    .background(.ultraThinMaterial)
                    .clipShape(RoundedRectangle(cornerRadius: 12))
                }

                // Progress indicator
                if updateManager.downloadStatus != .idle && updateManager.downloadStatus != .failed
                {
                    VStack(spacing: 12) {
                        ProgressView(value: updateManager.downloadProgress)
                            .progressViewStyle(.linear)
                            .tint(.blue)

                        Text(statusText)
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                    .padding()
                    .background(.ultraThinMaterial)
                    .clipShape(RoundedRectangle(cornerRadius: 12))
                }

                // Error message
                if let error = updateManager.errorMessage {
                    HStack {
                        Image(systemName: "exclamationmark.triangle.fill")
                            .foregroundStyle(.red)
                        Text(error)
                            .font(.caption)
                            .foregroundStyle(.red)
                    }
                    .padding()
                    .background(.red.opacity(0.1))
                    .clipShape(RoundedRectangle(cornerRadius: 8))
                }

                // Actions
                HStack(spacing: 16) {
                    Button("Later") {
                        updateManager.dismissUpdate()
                        dismiss()
                    }
                    .buttonStyle(GlassButtonStyle(color: .gray))

                    switch updateManager.downloadStatus {
                    case .idle:
                        Button("Update Now") {
                            Task { await updateManager.downloadUpdate() }
                        }
                        .buttonStyle(GlassButtonStyle(color: .blue))

                    case .readyToInstall(let path):
                        Button("Install & Restart") {
                            Task { await updateManager.installUpdate(extractPath: path) }
                        }
                        .buttonStyle(GlassButtonStyle(color: .green))

                    case .failed:
                        Button("Retry") {
                            Task { await updateManager.downloadUpdate() }
                        }
                        .buttonStyle(GlassButtonStyle(color: .orange))

                    default:
                        Button("Downloading...") {}
                            .buttonStyle(GlassButtonStyle(color: .blue))
                            .disabled(true)
                    }
                }
            }
            .padding(40)
            .frame(width: 450)
        }
        .frame(width: 450, height: 500)
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

/// Glass-style button for update UI
struct GlassButtonStyle: ButtonStyle {
    let color: Color

    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.headline)
            .foregroundStyle(.white)
            .padding(.horizontal, 24)
            .padding(.vertical, 12)
            .background(
                RoundedRectangle(cornerRadius: 12)
                    .fill(color.opacity(configuration.isPressed ? 0.5 : 0.3))
                    .overlay(
                        RoundedRectangle(cornerRadius: 12)
                            .stroke(color.opacity(0.5), lineWidth: 1)
                    )
            )
            .scaleEffect(configuration.isPressed ? 0.97 : 1)
            .animation(.easeInOut(duration: 0.1), value: configuration.isPressed)
    }
}

#Preview {
    UpdateView()
}
