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

    // Explicitly use os.Logger
    private let logger = os.Logger(subsystem: "Aether", category: "UpdateManager")

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
                    logger.info("Update available: \(response.version)")
                } else {
                    updateAvailable = false
                    updateInfo = nil
                    logger.info("No updates available")
                }
                downloadStatus = .idle
            }
        } catch {
            logger.error("Failed to check for updates: \(error)")
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
                logger.error("Download error: \(error)")
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
            logger.info("Download complete, ready to install")
        case .failed:
            downloadStatus = .failed
            errorMessage = progress.errorMessage
            logger.error("Download failed: \(progress.errorMessage)")
        case .UNRECOGNIZED:
            break
        }
    }

    /// Install the update (will quit the app)
    /// Install the update (will quit the app)
    func installUpdate(extractPath: String) async {
        logger.info("Requesting install update with path: \(extractPath)")
        let client = GrpcClient.shared.client

        let request = Aether_InstallUpdateRequest.with {
            $0.extractPath = extractPath
        }

        do {
            logger.info("Sending installAppUpdate RPC...")
            let response = try await client.installAppUpdate(request)
            logger.info(
                "Received installAppUpdate response: Success=\(response.success), Message=\(response.message)"
            )

            if response.success {
                logger.info("Update helper launched successfully. Preparing to quit...")
                // Quit the app so the helper can replace files
                await MainActor.run {
                    // Slight delay to allow UI to update if needed, but mostly just to get on main thread for termination
                    logger.info("Scheduling app termination in 0.5s")
                    DispatchQueue.main.asyncAfter(deadline: .now() + 0.5) {
                        self.logger.info("Calling NSApplication terminate")
                        NSApplication.shared.terminate(nil)
                    }
                }
            } else {
                logger.error("Backend returned failure: \(response.message)")
                await MainActor.run {
                    errorMessage = response.message
                    downloadStatus = .failed
                }
            }
        } catch {
            logger.error("Failed to install update: \(error)")
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
