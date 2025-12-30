import AetherIPC
import Combine
import Foundation
import GRPCCore
import SwiftProtobuf
import SwiftUI
import os

/// Manages app updates via gRPC backend
class UpdateManager: ObservableObject {
    static let shared = UpdateManager()

    @Published var updateAvailable = false
    @Published var updateInfo: UpdateInfo?
    @Published var downloadProgress: Double = 0
    @Published var downloadStatus: DownloadStatus = .idle
    @Published var errorMessage: String?

    // Use shared file logger
    // private let logger = os.Logger(subsystem: "Aether", category: "UpdateManager")

    enum DownloadStatus: Equatable {
        case idle
        case checking
        case downloading
        case extracting
        case readyToInstall(extractPath: String)
        case failed
    }

    struct UpdateInfo: Sendable {
        let version: String
        let releaseNotes: String
        let htmlUrl: String
        let sizeBytes: Int64
        let isPrerelease: Bool
    }

    private init() {}

    /// Check for updates on app launch
    func checkForUpdates() async {
        await MainActor.run {
            downloadStatus = .checking
            errorMessage = nil
        }

        let client = GrpcClient.shared.client

        let currentVersion =
            Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "0.0.0"
        let includeBeta = UserDefaults.standard.bool(forKey: "includeBetaUpdates")

        do {
            let request = Aether_CheckUpdateRequest.with {
                $0.currentVersion = currentVersion
                $0.includePrerelease = includeBeta
            }

            let response = try await client.checkForUpdates(request)

            await MainActor.run {
                if response.updateAvailable {
                    updateAvailable = true
                    updateInfo = UpdateInfo(
                        version: response.version,
                        releaseNotes: response.releaseNotes,
                        htmlUrl: response.htmlURL,
                        sizeBytes: response.sizeBytes,
                        isPrerelease: response.isPrerelease
                    )
                    Logger.shared.log("Update available: \(response.version)")
                } else {
                    updateAvailable = false
                    updateInfo = nil
                    Logger.shared.log("No updates available")
                }
                downloadStatus = .idle
            }
        } catch {
            Logger.shared.log("Failed to check for updates: \(error)", type: .error)
            await MainActor.run { downloadStatus = .idle }
        }
    }

    /// Download and extract the update
    func downloadUpdate() async {
        let version: String? = await MainActor.run { return updateInfo?.version }
        guard let version else { return }

        await MainActor.run {
            downloadStatus = .downloading
            downloadProgress = 0
            errorMessage = nil
        }

        let client = GrpcClient.shared.client

        let request = Aether_DownloadUpdateRequest.with {
            $0.version = version
        }

        do {
            try await client.downloadUpdate(request) { response in
                for try await progress in response.messages {
                    await self.handleDownloadProgress(progress)
                }
            }
        } catch {
            await MainActor.run {
                downloadStatus = .failed
                errorMessage = error.localizedDescription
                Logger.shared.log("Download error: \(error)", type: .error)
            }
        }
    }

    /// Handle download progress on MainActor
    @MainActor
    private func handleDownloadProgress(_ progress: Aether_DownloadProgress) {
        switch progress.status {
        case .downloading:
            downloadStatus = .downloading
            downloadProgress = Double(progress.percent) / 100.0
        case .extracting:
            downloadStatus = .extracting
            downloadProgress = 1.0
        case .complete:
            downloadStatus = .readyToInstall(extractPath: progress.extractPath)
            downloadProgress = 1.0
            Logger.shared.log("Download complete, ready to install")
        case .failed:
            downloadStatus = .failed
            errorMessage = progress.errorMessage
            Logger.shared.log("Download failed: \(progress.errorMessage)", type: .error)
        case .UNRECOGNIZED:
            break
        }
    }

    /// Install the update (will quit the app)
    /// Install the update (will quit the app)
    func installUpdate(extractPath: String) async {
        Logger.shared.log("Requesting install update with path: \(extractPath)")
        let client = GrpcClient.shared.client

        let request = Aether_InstallUpdateRequest.with {
            $0.extractPath = extractPath
        }

        do {
            Logger.shared.log("Sending installAppUpdate RPC...")
            let response = try await client.installAppUpdate(request)
            Logger.shared.log(
                "Received installAppUpdate response: Success=\(response.success), Message=\(response.message)"
            )

            if response.success {
                Logger.shared.log(
                    "Update helper launched successfully. Initiating force shutdown sequence...")

                await MainActor.run {
                    // 1. Dismiss UI immediately
                    self.updateAvailable = false
                    self.downloadStatus = .idle

                    // 2. Stop the backend explicitly (crucial for update helper to proceed)
                    Logger.shared.log("Stopping BackendManager...")
                    BackendManager.shared.stop()

                    // 3. Force exit after a bref delay to allow backend to receive signal
                    DispatchQueue.main.asyncAfter(deadline: .now() + 1.0) {
                        Logger.shared.log("Calling exit(0) to force quit...")
                        exit(0)
                    }
                }
            } else {
                Logger.shared.log("Backend returned failure: \(response.message)", type: .error)
                await MainActor.run {
                    errorMessage = response.message
                    downloadStatus = .failed
                }
            }
        } catch {
            Logger.shared.log("Failed to install update: \(error)", type: .error)
            await MainActor.run {
                errorMessage = error.localizedDescription
                downloadStatus = .failed
            }
        }
    }

    func dismissUpdate() {
        // Since this is called from UI, it might be on main thread already, but safe to force dispatch if async
        Task { @MainActor in
            updateAvailable = false
            updateInfo = nil
            downloadStatus = .idle
        }
    }
}
