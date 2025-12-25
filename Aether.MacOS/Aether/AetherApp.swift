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
    @StateObject private var appState = AppState()

    init() {

        // Check permissions state silently on launch
        PermissionManager.shared.checkPermissions()

        // Register to stop backend when app terminates
        NotificationCenter.default.addObserver(
            forName: NSApplication.willTerminateNotification,
            object: nil,
            queue: .main
        ) { _ in
            BackendManager.shared.stop()
        }
    }

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(appState)
                .task {
                    // Start backend when UI appears (active context for Admin Prompt)
                    BackendManager.shared.start()
                }
        }
        .windowToolbarStyle(.unified(showsTitle: false))
        .windowResizability(.contentSize)
    }
}
