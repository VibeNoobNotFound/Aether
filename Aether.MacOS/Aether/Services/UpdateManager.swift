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
    func installUpdate(extractPath: String) async {
        Logger.shared.log("Starting local update installation...")
        Logger.shared.log("Extract path: \(extractPath)")

        // 1. Prepare the update script
        do {
            try performLocalUpdate(extractPath: extractPath)
        } catch {
            Logger.shared.log("Failed to prepare local update: \(error)", type: .error)
            await MainActor.run {
                errorMessage = "Update failed: \(error.localizedDescription)"
                downloadStatus = .failed
            }
        }
    }

    private func performLocalUpdate(extractPath: String) throws {
        // 1. Identify paths
        let bundlePath = Bundle.main.bundlePath
        let pid = ProcessInfo.processInfo.processIdentifier

        // Find the new app inside the extract path (handling nested folders)
        let fileManager = FileManager.default
        let contents = try fileManager.contentsOfDirectory(atPath: extractPath)
        var sourceAppPath: String?

        // Simple search for .app at root or one level deep
        if let appName = contents.first(where: { $0.hasSuffix(".app") }) {
            sourceAppPath = (extractPath as NSString).appendingPathComponent(appName)
        } else {
            // Search subdirectories
            for item in contents {
                let subPath = (extractPath as NSString).appendingPathComponent(item)
                var isDir: ObjCBool = false
                if fileManager.fileExists(atPath: subPath, isDirectory: &isDir), isDir.boolValue {
                    if let subContents = try? fileManager.contentsOfDirectory(atPath: subPath),
                        let subApp = subContents.first(where: { $0.hasSuffix(".app") })
                    {
                        sourceAppPath = (subPath as NSString).appendingPathComponent(subApp)
                        break
                    }
                }
            }
        }

        guard let validSourcePath = sourceAppPath else {
            throw NSError(
                domain: "UpdateManager", code: 1,
                userInfo: [NSLocalizedDescriptionKey: "Could not find .app bundle in update"])
        }

        Logger.shared.log("Found source app: \(validSourcePath)")
        Logger.shared.log("Target app: \(bundlePath)")

        // 2. Generate Script
        let scriptContent = """
            #!/bin/bash
            # Aether Frontend Updater

            LOG_FILE="/tmp/aether_updater.log"
            exec > >(tee -a "$LOG_FILE") 2>&1
            echo "--- Update Started: $(date) ---"

            PID="\(pid)"
            NEW_APP="\(validSourcePath)"
            TARGET_APP="\(bundlePath)"

            echo "Waiting for PID $PID to exit..."
            while kill -0 "$PID" 2>/dev/null; do
                sleep 0.5
            done
            echo "App exited."

            # Backup
            if [ -d "$TARGET_APP" ]; then
                echo "Backing up to ${TARGET_APP}.old"
                rm -rf "${TARGET_APP}.old"
                mv "$TARGET_APP" "${TARGET_APP}.old"
            fi

            # Move New
            echo "Moving new app to target..."
            mv "$NEW_APP" "$TARGET_APP"

            if [ $? -ne 0 ]; then
                echo "ERROR: Move failed. Restoring..."
                mv "${TARGET_APP}.old" "$TARGET_APP"
                exit 1
            fi

            # Cleanup
            echo "Cleaning up..."
            rm -rf "\(extractPath)"
            rm -rf "${TARGET_APP}.old"

            echo "Relaunching..."
            open -n "$TARGET_APP"
            echo "--- Done ---"
            """

        // 3. Write Script
        let scriptPath = "/tmp/aether_updater.sh"
        try scriptContent.write(toFile: scriptPath, atomically: true, encoding: .utf8)
        try fileManager.setAttributes([.posixPermissions: 0o755], ofItemAtPath: scriptPath)

        Logger.shared.log("Update script written to \(scriptPath)")

        // 4. Force Shutdown & Execute
        Task { @MainActor in
            // Dismiss UI
            self.downloadStatus = .idle
            self.updateAvailable = false

            // Stop Backend
            Logger.shared.log("Stopping backend...")
            BackendManager.shared.stop()

            // Execute Script detached
            Logger.shared.log("Launching updater script and exiting...")
            let task = Process()
            task.executableURL = URL(fileURLWithPath: "/bin/bash")
            task.arguments = ["-c", "nohup \"\(scriptPath)\" > /dev/null 2>&1 &"]
            try? task.run()

            // Wait briefly for backend stop then exit
            try? await Task.sleep(for: .seconds(0.5))
            exit(0)
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
