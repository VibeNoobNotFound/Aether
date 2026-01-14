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
        // Initialize logger first
        _ = AetherLogger.shared

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
        .commands {
            // Override "About" menu
            CommandGroup(replacing: .appInfo) {
                Button("About Aether") {
                    NotificationCenter.default.post(name: .openSettings, object: nil)
                }
            }

            // Standard Sidebar commands
            SidebarCommands()

            CommandGroup(replacing: .newItem) {}

            // Custom Library Menu
            CommandGroup(after: .newItem) {
                Divider()
                Button("Scan Library") {
                    Task { await appState.scanLibrary() }
                }
                .keyboardShortcut("R", modifiers: [.command])

                Button("Manage Collections") {
                    NotificationCenter.default.post(name: .openCollectionEditor, object: nil)
                }
                .keyboardShortcut("C", modifiers: [.command, .shift])
            }

            CommandGroup(replacing: .help) {}
        }
    }
}

// Notification extension for menu commands
extension Notification.Name {
    static let openCollectionEditor = Notification.Name("openCollectionEditor")
    static let openSettings = Notification.Name("openSettings")
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
