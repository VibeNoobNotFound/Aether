//
//  AetherApp.swift
//  Aether
//
//  Created by Chaniru Rajapakse on 2025-12-11.
//

import Combine
import SwiftUI

@main
struct AetherApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) var appDelegate
    @StateObject private var appState = AppState()
    @ObservedObject private var updateManager = UpdateManager.shared

    init() {
        // Check permissions state silently on launch
        PermissionManager.shared.checkPermissions()
    }

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(appState)
                .environmentObject(updateManager)
                .sheet(isPresented: $updateManager.updateAvailable) {
                    UpdateView()
                }
                .task {
                    // Start backend when UI appears
                    // Ensure we don't start multiple times if views re-appear
                    if !BackendManager.shared.isRunning {
                        BackendManager.shared.start()
                    }

                    // Check for updates after backend starts (if enabled)
                    try? await Task.sleep(for: .seconds(3))

                    // Default to true if not set
                    let autoCheck =
                        UserDefaults.standard.object(forKey: "automaticallyCheckForUpdates")
                        as? Bool ?? true
                    if autoCheck {
                        await updateManager.checkForUpdates()
                    }
                }
        }
        .windowToolbarStyle(.unified(showsTitle: false))
        .windowResizability(.contentSize)
    }
}

class AppDelegate: NSObject, NSApplicationDelegate {
    func applicationShouldTerminateAfterLastWindowClosed(_ sender: NSApplication) -> Bool {
        return true
    }

    func applicationWillTerminate(_ notification: Notification) {
        print("📱 App Terminating - Stopping Backend...")
        BackendManager.shared.stop()
    }
}
