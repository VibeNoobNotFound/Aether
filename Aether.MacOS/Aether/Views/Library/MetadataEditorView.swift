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
    @State private var logoImageUrl: String = ""
    @State private var genres: String = ""
    @State private var videos: [String] = []
    @State private var screenshots: [String] = []
    @State private var steamId: String = ""
    @State private var newVideoUrl: String = ""
    @State private var newScreenshotUrl: String = ""
    @State private var launchArguments: String = ""

    @State private var isSaving = false
    @State private var errorMessage: String?

    @State private var showingSearchSheet = false

    var body: some View {
        NavigationStack {
            ZStack {
                // Background
                Color.black.ignoresSafeArea()

                GeometryReader { proxy in
                    Circle()
                        .fill(Color.blue.opacity(0.1))
                        .frame(width: 400, height: 400)
                        .blur(radius: 100)
                        .position(x: 0, y: 0)

                    Circle()
                        .fill(Color.purple.opacity(0.1))
                        .frame(width: 300, height: 300)
                        .blur(radius: 80)
                        .position(x: proxy.size.width, y: proxy.size.height)
                }
                .ignoresSafeArea()

                ScrollView {
                    VStack(spacing: 24) {
                        // Error Banner
                        if let error = errorMessage {
                            Text(error)
                                .foregroundStyle(.white)
                                .padding()
                                .frame(maxWidth: .infinity)
                                .background(Color.red.opacity(0.8))
                                .clipShape(RoundedRectangle(cornerRadius: 12))
                        }

                        // Auto-Fill Section
                        GlassSection(title: "Auto-Fill") {
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
                                .padding()
                                .background(Color.blue.opacity(0.2))
                                .clipShape(RoundedRectangle(cornerRadius: 8))
                            }
                            .buttonStyle(.plain)

                            Text(
                                "Search Steam, IGDB and other providers to automatically fill metadata"
                            )
                            .font(.caption)
                            .foregroundStyle(.secondary)
                            .padding(.top, 4)
                        }

                        // Basic Info
                        GlassSection(title: "Basic Info") {
                            GlassTextField(title: "Title", text: $title)
                            GlassTextField(title: "Developer", text: $developer)
                            GlassTextField(title: "Publisher", text: $publisher)
                            GlassTextField(title: "Launch Arguments", text: $launchArguments)
                            GlassTextField(title: "Genres (comma separated)", text: $genres)
                        }

                        // Images
                        GlassSection(title: "Images") {
                            GlassTextField(title: "Cover Image URL", text: $coverImageUrl)

                            if let url = URL(string: coverImageUrl), !coverImageUrl.isEmpty {
                                AsyncImage(url: url) { image in
                                    image
                                        .resizable()
                                        .aspectRatio(contentMode: .fit)
                                        .frame(height: 150)
                                        .clipShape(RoundedRectangle(cornerRadius: 12))
                                } placeholder: {
                                    ProgressView()
                                        .frame(height: 150)
                                }
                            }

                            GlassTextField(title: "Background Image URL", text: $backgroundImageUrl)
                            GlassTextField(title: "Logo Image URL", text: $logoImageUrl)
                        }

                        // Cross-Platform News
                        GlassSection(title: "Cross-Platform News (Steam ID)") {
                            GlassTextField(title: "Steam App ID", text: $steamId)
                            Text("Enter a Steam App ID to fetch news for non-Steam games")
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        }

                        // Videos
                        GlassSection(title: "Videos") {
                            ForEach(videos, id: \.self) { video in
                                HStack {
                                    Text(video)
                                        .lineLimit(1)
                                        .truncationMode(.middle)
                                        .foregroundStyle(.secondary)
                                    Spacer()
                                    Button {
                                        if let index = videos.firstIndex(of: video) {
                                            videos.remove(at: index)
                                        }
                                    } label: {
                                        Image(systemName: "trash")
                                            .foregroundStyle(.red)
                                    }
                                    .buttonStyle(.plain)
                                }
                                .padding()
                                .background(.black.opacity(0.2))
                                .clipShape(RoundedRectangle(cornerRadius: 8))
                            }

                            HStack {
                                GlassTextField(title: "Add Video URL", text: $newVideoUrl)
                                GlassButton("Add", systemImage: "plus") {
                                    if !newVideoUrl.isEmpty {
                                        videos.append(newVideoUrl)
                                        newVideoUrl = ""
                                    }
                                }
                                .opacity(newVideoUrl.isEmpty ? 0.5 : 1.0)
                                .disabled(newVideoUrl.isEmpty)
                            }
                        }

                        // Screenshots
                        GlassSection(title: "Screenshots") {
                            ForEach(screenshots, id: \.self) { screenshot in
                                HStack {
                                    Text(screenshot)
                                        .lineLimit(1)
                                        .truncationMode(.middle)
                                        .foregroundStyle(.secondary)
                                    Spacer()
                                    Button {
                                        if let index = screenshots.firstIndex(of: screenshot) {
                                            screenshots.remove(at: index)
                                        }
                                    } label: {
                                        Image(systemName: "trash")
                                            .foregroundStyle(.red)
                                    }
                                    .buttonStyle(.plain)
                                }
                                .padding()
                                .background(Color.black.opacity(0.2))
                                .clipShape(RoundedRectangle(cornerRadius: 8))

                                // Image Preview if valid URL
                                if let url = URL(string: screenshot), !screenshot.isEmpty {
                                    AsyncImage(url: url) { image in
                                        image
                                            .resizable()
                                            .aspectRatio(contentMode: .fit)
                                            .frame(height: 100)
                                            .clipShape(RoundedRectangle(cornerRadius: 8))
                                    } placeholder: {
                                        EmptyView()
                                    }
                                }
                            }

                            HStack {
                                GlassTextField(title: "Add Screenshot URL", text: $newScreenshotUrl)
                                GlassButton("Add", systemImage: "plus") {
                                    if !newScreenshotUrl.isEmpty {
                                        screenshots.append(newScreenshotUrl)
                                        newScreenshotUrl = ""
                                    }
                                }
                                .opacity(newScreenshotUrl.isEmpty ? 0.5 : 1.0)
                                .disabled(newScreenshotUrl.isEmpty)
                            }
                        }

                        // Description
                        GlassSection(title: "Description") {
                            TextEditor(text: $description)
                                .scrollContentBackground(.hidden)
                                .background(Color.white.opacity(0.05))
                                .frame(minHeight: 100)
                                .clipShape(RoundedRectangle(cornerRadius: 8))
                                .overlay(
                                    RoundedRectangle(cornerRadius: 8)
                                        .stroke(Color.white.opacity(0.1), lineWidth: 1)
                                )
                        }
                    }
                    .padding()
                }
            }
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
            .toolbarBackground(.automatic, for: .windowToolbar)
            .sheet(isPresented: $showingSearchSheet) {
                MetadataSearchSheet(
                    initialQuery: title,
                    onSelect: { result in
                        // Apply ALL available metadata from the search result
                        title = result.title
                        developer = result.developer
                        publisher = result.publisher
                        description = result.description
                        coverImageUrl = result.coverImageUrl
                        if !result.logoImageUrl.isEmpty { logoImageUrl = result.logoImageUrl }
                        if !result.videos.isEmpty { videos = result.videos }
                        if !result.screenshots.isEmpty { screenshots = result.screenshots }
                        if !result.genres.isEmpty { genres = result.genres.joined(separator: ", ") }

                        // Auto-set SteamId if the result comes from Steam provider
                        if result.provider == "Steam" && !result.externalId.isEmpty {
                            steamId = result.externalId
                        }
                    }
                )
                .presentationBackground(.clear)
            }
        }
        .frame(minWidth: 600, minHeight: 700)
        .onAppear {
            // Initialize with current values
            title = game.title
            developer = game.developer ?? ""
            publisher = game.publisher ?? ""
            description = game.description
            coverImageUrl = game.coverImageURL?.absoluteString ?? ""
            backgroundImageUrl = game.backgroundImageURL?.absoluteString ?? ""
            logoImageUrl = game.logoImageURL?.absoluteString ?? ""
            genres = game.genres.joined(separator: ", ")
            videos = game.videos.map { $0.absoluteString }
            screenshots = game.screenshots.map { $0.absoluteString }
            steamId = game.steamId ?? ""
            launchArguments = game.launchArguments ?? ""
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
                    logoImageUrl: logoImageUrl,
                    genres: genresList,
                    videos: videos,
                    screenshots: screenshots,
                    steamId: steamId,
                    launchArguments: launchArguments
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

// MARK: - Components
// GlassSection and GlassTextField are now in Views/Components/GlassComponents.swift

struct SearchResultCard: View {
    let result: MetadataSearchResult
    let action: () -> Void

    var body: some View {
        Button(action: action) {
            GlassCard(isHoverable: true) {
                HStack(spacing: 16) {
                    AsyncImage(url: URL(string: result.coverImageUrl)) { image in
                        image
                            .resizable()
                            .aspectRatio(contentMode: .fit)
                    } placeholder: {
                        Rectangle()
                            .fill(Color.gray.opacity(0.3))
                    }
                    .frame(width: 60, height: 90)
                    .clipShape(RoundedRectangle(cornerRadius: 4))

                    VStack(alignment: .leading, spacing: 4) {
                        Text(result.title)
                            .font(.headline)
                            .foregroundStyle(.white)
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
                        .background {
                            GlassCard(padding: 0, cornerRadius: 100) { Color.clear }
                        }
                        .foregroundStyle(.blue)
                }
            }
        }
        .buttonStyle(.plain)
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
            ZStack {
                Color.black.ignoresSafeArea()

                ScrollView {
                    VStack(spacing: 20) {
                        // Search Bar
                        HStack(spacing: 12) {
                            GlassTextField(title: "Search Term", text: $searchQuery)
                                .onSubmit { search() }

                            Picker("Provider", selection: $selectedProvider) {
                                Text("All Providers").tag("")
                                Text("Steam").tag("Steam")
                                Text("IGDB").tag("IGDB")
                            }
                            .pickerStyle(.menu)
                            .frame(width: 120)

                            Button {
                                search()
                            } label: {
                                Image(systemName: "magnifyingglass")
                                    .padding()
                                    .background(Color.blue)
                                    .foregroundStyle(.white)
                                    .clipShape(RoundedRectangle(cornerRadius: 8))
                            }
                            .buttonStyle(.plain)
                            .disabled(isSearching || searchQuery.isEmpty)
                        }
                        .background {
                            GlassCard(padding: 0, cornerRadius: 12) { Color.clear }
                        }

                        // Results
                        if isSearching {
                            ProgressView("Searching...")
                                .padding(.top, 40)
                        } else if results.isEmpty {
                            Text("No results found")
                                .foregroundStyle(.secondary)
                                .padding(.top, 40)
                        } else {
                            LazyVStack(spacing: 12) {
                                ForEach(results, id: \.externalId) { result in
                                    SearchResultCard(result: result) {
                                        onSelect(result)
                                        dismiss()
                                    }

                                }
                            }
                        }
                    }
                    .padding()
                }
            }
            .navigationTitle("Search Metadata")
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
            }
            .toolbarBackground(.automatic, for: .windowToolbar)
        }
        .frame(minWidth: 600, minHeight: 500)
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

#Preview {
    MetadataEditorView(game: MockData.games[0])
        .environmentObject(MockData.appState)
}
