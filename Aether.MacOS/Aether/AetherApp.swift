//
//  AetherApp.swift
//  Aether
//
//  Created by Chaniru Rajapakse on 2025-12-11.
//

import SwiftUI

@main
struct AetherApp: App {
    @StateObject private var appState = AppState()

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(appState)
        }
        .windowToolbarStyle(.unified(showsTitle: false))
        .windowResizability(.contentSize)
    }
}
