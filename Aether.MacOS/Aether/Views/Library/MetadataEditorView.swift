import SwiftUI

struct MetadataEditorView: View {
    @Environment(\.dismiss) var dismiss
    @EnvironmentObject var appState: AppState

    let game: GameViewModel

    @State private var title: String = ""
    @State private var developer: String = ""
    @State private var publisher: String = ""
    @State private var description: String = ""
    @State private var coverImageUrl: String = ""
    @State private var backgroundImageUrl: String = ""
    @State private var genres: String = ""

    @State private var isSaving = false
    @State private var errorMessage: String?
    @State private var showingSearchSheet = false

    var body: some View {
        NavigationStack {
            Form {
                Section("Basic Info") {
                    TextField("Title", text: $title)
                    TextField("Developer", text: $developer)
                    TextField("Publisher", text: $publisher)
                    TextField("Genres (comma separated)", text: $genres)
                }

                Section("Images") {
                    TextField("Cover Image URL", text: $coverImageUrl)

                    if let url = URL(string: coverImageUrl), !coverImageUrl.isEmpty {
                        AsyncImage(url: url) { image in
                            image
                                .resizable()
                                .aspectRatio(contentMode: .fit)
                                .frame(height: 150)
                        } placeholder: {
                            ProgressView()
                        }
                    }

                    TextField("Background Image URL", text: $backgroundImageUrl)
                }

                Section("Description") {
                    TextEditor(text: $description)
                        .frame(minHeight: 100)
                }

                // Search Providers - prominent button in form
                Section {
                    Button {
                        showingSearchSheet = true
                    } label: {
                        HStack {
                            Image(systemName: "magnifyingglass")
                            Text("Search Metadata Providers")
                            Spacer()
                            Image(systemName: "chevron.right")
                                .foregroundStyle(.secondary)
                        }
                    }
                    .buttonStyle(.plain)
                } header: {
                    Text("Auto-Fill")
                } footer: {
                    Text("Search Steam, IGDB and other providers to automatically fill metadata")
                }

                if let error = errorMessage {
                    Section {
                        Text(error)
                            .foregroundStyle(.red)
                    }
                }
            }
            .formStyle(.grouped)
            .navigationTitle("Edit Metadata")
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }

                ToolbarItem(placement: .confirmationAction) {
                    Button("Save") {
                        saveMetadata()
                    }
                    .disabled(isSaving)
                }
            }
            .sheet(isPresented: $showingSearchSheet) {
                MetadataSearchSheet(
                    initialQuery: title,
                    onSelect: { result in
                        // Apply selected metadata
                        title = result.title
                        developer = result.developer
                        coverImageUrl = result.coverImageUrl
                    }
                )
            }
        }
        .frame(minWidth: 500, minHeight: 600)
        .onAppear {
            // Initialize with current values
            title = game.title
            developer = game.developer ?? ""
            publisher = game.publisher ?? ""
            description = game.description
            coverImageUrl = game.coverImageURL?.absoluteString ?? ""
            backgroundImageUrl = game.backgroundImageURL?.absoluteString ?? ""
            genres = game.genres.joined(separator: ", ")
        }
    }

    private func saveMetadata() {
        isSaving = true
        errorMessage = nil

        Task {
            do {
                let genresList = genres.split(separator: ",").map {
                    String($0.trimmingCharacters(in: .whitespaces))
                }

                try await appState.updateGameMetadata(
                    gameId: game.id,
                    title: title,
                    developer: developer,
                    publisher: publisher,
                    description: description,
                    coverImageUrl: coverImageUrl,
                    backgroundImageUrl: backgroundImageUrl,
                    genres: genresList
                )

                await MainActor.run {
                    dismiss()
                }
            } catch {
                await MainActor.run {
                    errorMessage = error.localizedDescription
                    isSaving = false
                }
            }
        }
    }
}

struct MetadataSearchSheet: View {
    @Environment(\.dismiss) var dismiss
    @EnvironmentObject var appState: AppState

    let initialQuery: String
    let onSelect: (MetadataSearchResult) -> Void

    @State private var searchQuery: String = ""
    @State private var selectedProvider: String = ""
    @State private var results: [MetadataSearchResult] = []
    @State private var isSearching = false

    var body: some View {
        NavigationStack {
            VStack(spacing: 0) {
                // Search bar
                HStack {
                    TextField("Search for game...", text: $searchQuery)
                        .textFieldStyle(.roundedBorder)
                        .onSubmit { search() }

                    Picker("Provider", selection: $selectedProvider) {
                        Text("All Providers").tag("")
                        Text("Steam").tag("Steam")
                        Text("IGDB").tag("IGDB")
                    }
                    .pickerStyle(.menu)
                    .frame(width: 150)

                    Button("Search") { search() }
                        .disabled(isSearching || searchQuery.isEmpty)
                }
                .padding()

                Divider()

                // Results
                if isSearching {
                    Spacer()
                    ProgressView("Searching...")
                    Spacer()
                } else if results.isEmpty {
                    Spacer()
                    Text("No results")
                        .foregroundStyle(.secondary)
                    Spacer()
                } else {
                    List(results, id: \.externalId) { result in
                        HStack(spacing: 12) {
                            AsyncImage(url: URL(string: result.coverImageUrl)) { image in
                                image
                                    .resizable()
                                    .aspectRatio(contentMode: .fit)
                            } placeholder: {
                                Rectangle()
                                    .fill(.gray.opacity(0.3))
                            }
                            .frame(width: 60, height: 90)
                            .clipShape(RoundedRectangle(cornerRadius: 4))

                            VStack(alignment: .leading, spacing: 4) {
                                Text(result.title)
                                    .font(.headline)
                                Text(result.developer)
                                    .font(.subheadline)
                                    .foregroundStyle(.secondary)
                                if result.releaseYear > 0 {
                                    Text(String(result.releaseYear))
                                        .font(.caption)
                                        .foregroundStyle(.secondary)
                                }
                            }

                            Spacer()

                            Text(result.provider)
                                .font(.caption)
                                .padding(.horizontal, 8)
                                .padding(.vertical, 4)
                                .background(.blue.opacity(0.2))
                                .foregroundStyle(.blue)
                                .clipShape(Capsule())
                        }
                        .contentShape(Rectangle())
                        .onTapGesture {
                            onSelect(result)
                            dismiss()
                        }
                    }
                }
            }
            .navigationTitle("Search Metadata")
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
            }
        }
        .frame(minWidth: 500, minHeight: 400)
        .onAppear {
            searchQuery = initialQuery
            if !initialQuery.isEmpty {
                search()
            }
        }
    }

    private func search() {
        isSearching = true
        results = []

        Task {
            do {
                let searchResults = try await appState.searchMetadataProviders(
                    query: searchQuery,
                    provider: selectedProvider
                )
                await MainActor.run {
                    results = searchResults
                    isSearching = false
                }
            } catch {
                await MainActor.run {
                    isSearching = false
                }
            }
        }
    }
}

// Model for search results
struct MetadataSearchResult: Identifiable {
    var id: String { externalId.isEmpty ? title : externalId }
    let provider: String
    let externalId: String
    let title: String
    let developer: String
    let coverImageUrl: String
    let releaseYear: Int
}
