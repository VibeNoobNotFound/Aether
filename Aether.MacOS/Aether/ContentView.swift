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
    @State private var showOnboarding = false

    var body: some View {
        MainWindowView()
            .onAppear {
                // Show onboarding sheet on first launch
                if !hasCompletedOnboarding {
                    showOnboarding = true
                }
            }
            .sheet(isPresented: $showOnboarding) {
                OnboardingView()
                    .interactiveDismissDisabled()
            }
    }
}

#Preview {
    #if DEBUG
        ContentView()
            .environmentObject(MockData.appState)
    #endif
}
