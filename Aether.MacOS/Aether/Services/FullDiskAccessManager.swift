import AppKit
import Combine
import Foundation

/// Manages filesystem permissions by requesting user-selected access to specific folders.
/// This bypasses sandbox/hardened runtime restrictions by leveraging the user's intent to grant access via NSOpenPanel.
class PermissionManager: ObservableObject {
    static let shared = PermissionManager()

    @Published var hasFullDiskAccess: Bool = false

    // Core check: We use ~/Library/Safari as a proxy for Full Disk Access
    // because it requires FDA to be readable.
    func checkPermissions() {
        let home = FileManager.default.homeDirectoryForCurrentUser
        let safariPath = home.appendingPathComponent("Library/Safari")

        let hasAccess = FileManager.default.isReadableFile(atPath: safariPath.path)

        DispatchQueue.main.async {
            self.hasFullDiskAccess = hasAccess
            print("🛡️ Full Disk Access Status: \(hasAccess ? "Granted" : "Denied")")
        }
    }

    func checkAndPromptIfNeeded() {
        checkPermissions()
        // Auto-prompt disabled per user request.
        // We now rely on backend errors to trigger this.
    }

    func promptForPermissions() {
        DispatchQueue.main.async {
            let alert = NSAlert()
            alert.messageText = "Full Disk Access Required"
            alert.informativeText =
                "Aether's backend process requires Full Disk Access to scan your games.\n\nPermissions granted via 'Open File' dialogs are NOT inherited by the scanner.\n\nPlease grant Full Disk Access to Aether in System Settings."

            alert.addButton(withTitle: "Open System Settings")
            alert.addButton(withTitle: "Ignore")

            let response = alert.runModal()
            if response == .alertFirstButtonReturn {
                self.openFullDiskAccessSettings()

                // Show follow-up to confirm and restart backend
                let confirmAlert = NSAlert()
                confirmAlert.messageText = "Restart Backend Service"
                confirmAlert.informativeText =
                    "Once you have granted Full Disk Access, click 'Restart' to reload the backend service with the new permissions."
                confirmAlert.addButton(withTitle: "Restart Service")

                let confirmResponse = confirmAlert.runModal()
                if confirmResponse == .alertFirstButtonReturn {
                    BackendManager.shared.restart()
                }
            }
        }
    }

    // MARK: - Helpers

    /// Requests access to a specific folder using NSOpenPanel (For UI path selection only)
    func requestCustomFolderAccess(completion: @escaping (URL?) -> Void) {
        DispatchQueue.main.async {
            let openPanel = NSOpenPanel()
            openPanel.message = "Select Custom Library Folder"
            openPanel.prompt = "Select"
            openPanel.canChooseFiles = false
            openPanel.canChooseDirectories = true
            openPanel.allowsMultipleSelection = false

            openPanel.begin { response in
                if response == .OK, let url = openPanel.url {
                    completion(url)
                } else {
                    completion(nil)
                }
            }
        }
    }

    /// Open System Settings to the Full Disk Access pane
    func openFullDiskAccessSettings() {
        let urlString = "x-apple.systempreferences:com.apple.preference.security?Privacy_AllFiles"
        if let url = URL(string: urlString) {
            NSWorkspace.shared.open(url)
        }
    }
}
