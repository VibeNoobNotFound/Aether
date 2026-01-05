import SwiftUI
import AetherIPC

struct CollectionEditorSheet: View {
    @EnvironmentObject var appState: AppState
    @Environment(\.dismiss) var dismiss

    @State private var editingCollection: CollectionViewModel?
    @State private var showNewCollection = false
    @State private var newCollectionName = ""

    // Local state for reordering updates
    @State private var localCollections: [CollectionViewModel] = []

    var body: some View {
        NavigationStack {
            VStack(spacing: 0) {
                List {
                    ForEach(localCollections) { collection in
                        CollectionRowItem(
                            collection: collection,
                            onToggleVisibility: { toggleVisibility(collection) },
                            onEdit: { editingCollection = collection },
                            onDelete: {
                                Task { await appState.deleteCollection(id: collection.id) }
                            }
                        )
                    }
                    .onMove(perform: moveCollection)
                }
                .listStyle(.inset)

                // Bottom Bar
                HStack {
                    Button(action: {
                        showNewCollection = true
                    }) {
                        Label("New Collection", systemImage: "plus")
                    }

                    Spacer()

                    Button("Done") {
                        dismiss()
                    }
                    .keyboardShortcut(.defaultAction)
                }
                .padding()
                .background(Color(nsColor: .windowBackgroundColor))
            }
            .navigationTitle("Manage Collections")
            // Initialize local state
            .onAppear {
                localCollections = appState.collections.sorted { $0.sortOrder < $1.sortOrder }
            }
            // Sync with global state
            .onChange(of: appState.collections) { _, newValue in
                localCollections = newValue.sorted { $0.sortOrder < $1.sortOrder }
            }
            .sheet(item: $editingCollection) { collection in
                CollectionDetailEditor(collection: collection)
            }
            .alert("New Collection", isPresented: $showNewCollection) {
                TextField("Name", text: $newCollectionName)
                Button("Cancel", role: .cancel) { newCollectionName = "" }
                Button("Create") {
                    Task {
                        await appState.createCollection(name: newCollectionName, iconName: "folder")
                        newCollectionName = ""
                    }
                }
            }
        }
        .frame(width: 500, height: 600)
    }

    func toggleVisibility(_ collection: CollectionViewModel) {
        Task {
            await appState.updateCollection(id: collection.id, isVisible: !collection.isVisible)
        }
    }

    func moveCollection(from source: IndexSet, to destination: Int) {
        var movedItems = localCollections
        movedItems.move(fromOffsets: source, toOffset: destination)
        localCollections = movedItems

        // Extract IDs in new order
        let ids = localCollections.map { $0.id }

        Task {
            // Optimistic update done, send to server
            await appState.reorderCollections(ids: ids)
        }
    }
}

// Extracted Subview to fix type-check performance issue
struct CollectionRowItem: View {
    let collection: CollectionViewModel
    let onToggleVisibility: () -> Void
    let onEdit: () -> Void
    let onDelete: () -> Void

    var body: some View {
        HStack {
            Image(systemName: collection.iconName)
                .frame(width: 24)

            Text(collection.name)
                .font(.headline)

            if collection.isSystem {
                Text("System")
                    .font(.caption)
                    .padding(.horizontal, 6)
                    .padding(.vertical, 2)
                    .background(.white.opacity(0.1))
                    .clipShape(Capsule())
            }

            Spacer()

            // Visibility Toggle
            Button(action: onToggleVisibility) {
                Image(systemName: collection.isVisible ? "eye" : "eye.slash")
                    .foregroundStyle(collection.isVisible ? .primary : .secondary)
            }
            .buttonStyle(.plain)
            .padding(.trailing, 8)

            // Edit Button
            Button(action: onEdit) {
                Image(systemName: "pencil")
            }
            .buttonStyle(.plain)
            .disabled(collection.isSystem && collection.type != .collectionCustom)

            // Delete Button
            if !collection.isSystem {
                Button(action: onDelete) {
                    Image(systemName: "trash")
                        .foregroundStyle(.red)
                }
                .buttonStyle(.plain)
                .padding(.leading, 8)
            }
        }
        .padding(.vertical, 4)
    }
}

// Placeholder for detail editor
struct CollectionDetailEditor: View {
    let collection: CollectionViewModel
    @EnvironmentObject var appState: AppState
    @Environment(\.dismiss) var dismiss

    @State private var name: String
    @State private var iconName: String
    @State private var selectedGames: Set<String>

    init(collection: CollectionViewModel) {
        self.collection = collection
        _name = State(initialValue: collection.name)
        _iconName = State(initialValue: collection.iconName)
        _selectedGames = State(initialValue: Set(collection.gameIds.map { String($0) }))
    }

    var body: some View {
        NavigationStack {
            Form {
                Section("Details") {
                    TextField("Name", text: $name)
                    TextField("Icon (SF Symbol)", text: $iconName)
                }

                if collection.type == .collectionCustom {
                    Section("Games") {
                        List(appState.games, id: \.id) { game in
                            HStack {
                                Toggle(
                                    isOn: Binding(
                                        get: { selectedGames.contains(game.id) },
                                        set: { isSelected in
                                            if isSelected {
                                                selectedGames.insert(game.id)
                                            } else {
                                                selectedGames.remove(game.id)
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
            }
            .formStyle(.grouped)
            .navigationTitle("Edit Collection")
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
        }
        .frame(width: 400, height: 500)
    }

    func save() {
        Task {
            // Update details
            await appState.updateCollection(id: collection.id, name: name, iconName: iconName)

            // Update games (diffing)
            if collection.type == .collectionCustom {
                let currentIds = Set(collection.gameIds.map { String($0) })

                let toAdd = selectedGames.subtracting(currentIds)
                let toRemove = currentIds.subtracting(selectedGames)

                for id in toAdd {
                    await appState.addGameToCollection(collectionId: collection.id, gameId: id)
                }

                for id in toRemove {
                    await appState.removeGameFromCollection(collectionId: collection.id, gameId: id)
                }
            }
            dismiss()
        }
    }
}
