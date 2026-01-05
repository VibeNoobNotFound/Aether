import SwiftUI

struct CarouselEditorSheet: View {
    @EnvironmentObject var appState: AppState
    @Environment(\.dismiss) var dismiss

    // Config state
    @State private var sourceType: Int = 0  // 0: Auto, 1: Collection, 2: Manual
    @State private var selectedCollectionId: Int32?
    @State private var manualGameIds: Set<String> = []
    @State private var maxGames: Int = 5

    var body: some View {
        NavigationStack {
            Form {
                Section("Source") {
                    Picker("Content Source", selection: $sourceType) {
                        Text("Auto (Favorites + Recent)").tag(0)
                        Text("From Collection").tag(1)
                        Text("Manual Selection").tag(2)
                    }
                    .pickerStyle(.segmented)
                }

                if sourceType == 1 {
                    Section("Select Collection") {
                        Picker("Collection", selection: $selectedCollectionId) {
                            Text("Select a collection").tag(nil as Int32?)
                            ForEach(appState.collections) { col in
                                Text(col.name).tag(col.id as Int32?)
                            }
                        }
                    }
                }

                if sourceType == 2 {
                    Section("Select Games") {
                        List(appState.games, id: \.id) { game in
                            HStack {
                                Toggle(
                                    isOn: Binding(
                                        get: { manualGameIds.contains(game.id) },
                                        set: { isSelected in
                                            if isSelected {
                                                manualGameIds.insert(game.id)
                                            } else {
                                                manualGameIds.remove(game.id)
                                            }
                                        }
                                    )
                                ) {
                                    Text(game.title)
                                }
                            }
                        }
                        .frame(height: 300)
                    }
                }

                Section("Settings") {
                    Stepper("Max Games: \(maxGames)", value: $maxGames, in: 3...20)
                }
            }
            .formStyle(.grouped)
            .navigationTitle("Configure Carousel")
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Save") {
                        save()
                    }
                }
            }
            .onAppear {
                loadConfig()
            }
        }
        .frame(width: 600, height: 550)
    }

    func loadConfig() {
        if let config = appState.carouselConfig {
            maxGames = config.maxGames
            if let colId = config.collectionId {
                sourceType = 1
                selectedCollectionId = colId
            } else if !config.gameIds.isEmpty {
                sourceType = 2
                manualGameIds = Set(config.gameIds)
            } else {
                sourceType = 0
            }
        }
    }

    func save() {
        Task {
            var colId: Int32? = nil
            var gameIds: [String]? = nil

            if sourceType == 1 {
                colId = selectedCollectionId
                gameIds = []  // clear manual
            } else if sourceType == 2 {
                gameIds = Array(manualGameIds)
                colId = nil  // clear collection
            } else {
                // Auto: clear both
                colId = nil
                gameIds = []
            }

            // Sending nil/empty to clear logic resides in backend/appState wrapper
            // AppState.updateCarouselConfig expects update logic
            // Assuming passing nil/empty works as "unset" or "empty"

            await appState.updateCarouselConfig(
                collectionId: colId, gameIds: gameIds, maxGames: maxGames)
            dismiss()
        }
    }
}
