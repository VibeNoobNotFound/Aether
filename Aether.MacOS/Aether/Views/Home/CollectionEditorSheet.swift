import AetherIPC
import SwiftUI

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

// Extracted Subview with Layout Preview
struct CollectionRowItem: View {
    let collection: CollectionViewModel
    let onToggleVisibility: () -> Void
    let onEdit: () -> Void
    let onDelete: () -> Void

    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            // Drag Handle
            Image(systemName: "line.3.horizontal")
                .foregroundStyle(.secondary)
                .frame(maxHeight: .infinity)  // Center vertically relative to row
                .padding(.leading, 4)

            // Layout Preview (Header + Content)
            HStack(spacing: 0) {
                // Header Area
                ZStack {
                    Rectangle()
                        .fill(Color.blue.opacity(0.1))

                    VStack(spacing: 4) {
                        Image(systemName: collection.iconName)
                            .font(.system(size: 14))
                    }
                }
                .frame(width: 40)

                // Content Area (Mock cards)
                HStack(spacing: 4) {
                    ForEach(0..<3) { _ in
                        RoundedRectangle(cornerRadius: 4)
                            .fill(Color.secondary.opacity(0.1))
                            .aspectRatio(0.7, contentMode: .fit)
                    }
                    Spacer()
                }
                .padding(4)
            }
            .frame(height: 50)
            .background(RoundedRectangle(cornerRadius: 6).stroke(Color.secondary.opacity(0.2)))

            // Title and Info
            VStack(alignment: .leading, spacing: 4) {
                Text(collection.name)
                    .font(.headline)
                    .lineLimit(1)

                if collection.isSystem {
                    Text("System")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }
            .frame(maxWidth: .infinity, alignment: .topLeading)  // Align top-left
            .padding(.top, 0)  // Remove top padding to align with box top

            // Actions
            HStack(spacing: 12) {
                // Visibility Toggle
                Button(action: onToggleVisibility) {
                    Image(systemName: collection.isVisible ? "eye" : "eye.slash")
                        .foregroundStyle(collection.isVisible ? .primary : .secondary)
                }
                .buttonStyle(.plain)

                // Edit
                Button(action: onEdit) {
                    Image(systemName: "pencil")
                }
                .buttonStyle(.plain)

                // Delete
                if !collection.isSystem {
                    Button(action: onDelete) {
                        Image(systemName: "trash")
                            .foregroundStyle(.red)
                    }
                    .buttonStyle(.plain)
                }
            }
            .padding(.top, 4)  // Align with top elements
            .padding(.trailing, 8)
        }
        .padding(.vertical, 4)
    }
}

struct CollectionDetailEditor: View {
    let collection: CollectionViewModel
    @EnvironmentObject var appState: AppState
    @Environment(\.dismiss) var dismiss

    @State private var name: String
    @State private var iconName: String
    @State private var selectedGames: Set<String>
    @State private var showIconPicker = false

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
                    if collection.isSystem {
                        // System collections: Name is read-only
                        LabeledContent("Name", value: name)
                    } else {
                        TextField("Name", text: $name)
                    }

                    HStack {
                        Text("Icon")
                        Spacer()
                        Button(action: { showIconPicker = true }) {
                            HStack {
                                Image(systemName: iconName)
                                    .font(.title2)
                                    .frame(width: 32, height: 32)
                                    .background(Color.secondary.opacity(0.1))
                                    .clipShape(RoundedRectangle(cornerRadius: 6))
                            }
                        }
                        .buttonStyle(.plain)
                    }
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
            .navigationTitle(collection.isSystem ? "Edit Icon" : "Edit Collection")
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
            .sheet(isPresented: $showIconPicker) {
                SFIconPicker(selectedIcon: $iconName)
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
                    await appState.addGameToCollection(
                        collectionId: collection.id, gameId: id)
                }

                for id in toRemove {
                    await appState.removeGameFromCollection(
                        collectionId: collection.id, gameId: id)
                }
            }
            dismiss()
        }
    }
}

struct SFIconPicker: View {
    @Binding var selectedIcon: String
    @Environment(\.dismiss) var dismiss
    @State private var searchText = ""

    let icons = [
        "folder.fill", "gamecontroller.fill", "heart.fill", "star.fill", "clock.fill",
        "flame.fill", "bolt.fill", "desktopcomputer", "laptopcomputer", "display",
        "keyboard", "mouse", "logo.playstation", "logo.xbox", "apple.logo",
        "globe", "cloud.fill", "wifi", "person.fill", "person.2.fill",
        "house.fill", "building.2.fill", "cart.fill", "bag.fill", "creditcard.fill",
        "wand.and.stars", "sparkles", "crown.fill", "rosette", "trophy.fill",
        "flag.fill", "location.fill", "tag.fill", "bookmark.fill", "book.closed.fill",
    ]

    var filteredIcons: [String] {
        if searchText.isEmpty {
            return icons
        } else {
            return icons.filter { $0.localizedCaseInsensitiveContains(searchText) }
        }
    }

    let columns = [GridItem(.adaptive(minimum: 50))]

    var body: some View {
        NavigationStack {
            ScrollView {
                LazyVGrid(columns: columns, spacing: 20) {
                    ForEach(filteredIcons, id: \.self) { icon in
                        Button(action: {
                            selectedIcon = icon
                            dismiss()
                        }) {
                            VStack {
                                Image(systemName: icon)
                                    .font(.title)
                                    .frame(width: 40, height: 40)
                                    .foregroundStyle(selectedIcon == icon ? .blue : .primary)
                            }
                            .padding(8)
                            .background(
                                RoundedRectangle(cornerRadius: 8)
                                    .fill(
                                        selectedIcon == icon ? Color.blue.opacity(0.1) : Color.clear
                                    )
                            )
                        }
                        .buttonStyle(.plain)
                    }
                }
                .padding()
            }
            .searchable(text: $searchText, prompt: "Search icons")
            .navigationTitle("Choose Icon")
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
            }
        }
        .frame(width: 400, height: 500)
    }
}
