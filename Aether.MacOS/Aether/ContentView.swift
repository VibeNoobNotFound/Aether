//
//  ContentView.swift
//  Aether
//
//  Created by Chaniru Rajapakse on 2025-12-11.
//

import SwiftUI

struct ContentView: View {
    @AppStorage("hasCompletedOnboarding") var hasCompletedOnboarding: Bool = false
    @EnvironmentObject var appState: AppState

    var body: some View {
        Group {
            if hasCompletedOnboarding {
                MainWindowView()
                    .transition(.opacity.animation(.easeInOut))
            } else {
                OnboardingView()
                    .transition(.opacity.animation(.easeInOut))
            }
        }
    }
}

#Preview {
    #if DEBUG
        ContentView()
            .environmentObject(MockData.appState)
    #endif
}
