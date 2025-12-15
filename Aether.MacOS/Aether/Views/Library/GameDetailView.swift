import SwiftUI

struct GameDetailView: View {
    @Environment(\.dismiss) var dismiss
    @EnvironmentObject var appState: AppState
    let game: GameViewModel

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 0) {
                // Header with background
                ZStack(alignment: .bottomLeading) {
                    if let bgURL = game.backgroundImageURL {
                        AsyncImage(url: bgURL) { image in
                            image.resizable().aspectRatio(contentMode: .fill)
                        } placeholder: {
                            Rectangle().fill(.gray.gradient)
                        }
                        .frame(height: 300)
                        .clipped()
                    } else if let coverURL = game.coverImageURL {
                        // Fallback to cover image if no background
                        AsyncImage(url: coverURL) { image in
                            image.resizable().aspectRatio(contentMode: .fill)
                        } placeholder: {
                            Rectangle().fill(.gray.gradient)
                        }
                        .frame(height: 300)
                        .clipped()
                        .overlay(Material.ultraThin)  // Blur it contentMode
                    } else {
                        Rectangle()
                            .fill(
                                LinearGradient(
                                    colors: [.blue.opacity(0.3), .purple.opacity(0.3)],
                                    startPoint: .topLeading, endPoint: .bottomTrailing)
                            )
                            .frame(height: 300)
                    }

                    LinearGradient(
                        colors: [.clear, .black.opacity(0.8)],
                        startPoint: .top,
                        endPoint: .bottom
                    )
                    .frame(height: 300)

                    // Title overlay
                    VStack(alignment: .leading, spacing: 8) {
                        Text(game.title)
                            .font(.largeTitle)
                            .fontWeight(.bold)
                            .shadow(radius: 4)

                        HStack {
                            if let dev = game.developer {
                                Text(dev)
                                    .font(.subheadline)
                            }
                            if game.developer != nil && game.releaseDate != nil {
                                Text("•")
                            }
                            Text(game.formattedReleaseDate)
                                .font(.subheadline)
                        }
                        .foregroundStyle(.white.opacity(0.9))
                        .shadow(radius: 2)
                    }
                    .padding()
                }

                // Content
                VStack(alignment: .leading, spacing: 24) {
                    // Action buttons
                    HStack(spacing: 16) {
                        Button(action: { appState.launchGame(game) }) {
                            Label("Play", systemImage: "play.fill")
                                .font(.headline)
                                .frame(maxWidth: .infinity)
                                .frame(height: 48)
                        }
                        .buttonStyle(.borderedProminent)
                        .controlSize(.large)

                        Button(action: { toggleFavorite() }) {
                            Image(systemName: game.isFavorite ? "heart.fill" : "heart")
                                .frame(height: 48)
                        }
                        .buttonStyle(.bordered)
                        .controlSize(.large)
                    }

                    // Stats
                    HStack(spacing: 32) {
                        if let score = game.metacriticScore {
                            StatBadge(label: "Metacritic", value: "\(Int(score))")
                        }
                        if game.hasAchievements {
                            StatBadge(label: "Achievements", value: "\(game.achievementCount)")
                        }
                        StatBadge(label: "Playtime", value: game.formattedPlaytime)
                    }

                    // Genres
                    if !game.genres.isEmpty {
                        VStack(alignment: .leading, spacing: 8) {
                            Text("Genres")
                                .font(.headline)

                            FlowLayout(spacing: 8) {
                                ForEach(game.genres, id: \.self) { genre in
                                    Text(genre)
                                        .font(.caption)
                                        .padding(.horizontal, 12)
                                        .padding(.vertical, 6)
                                        .background(.blue.opacity(0.2))
                                        .foregroundStyle(.blue)
                                        .clipShape(Capsule())
                                }
                            }
                        }
                    }

                    // Description
                    if !game.description.isEmpty {
                        VStack(alignment: .leading, spacing: 8) {
                            Text("About")
                                .font(.headline)
                            HTMLText(html: game.description)
                        }
                    }

                    // Screenshots
                    if !game.screenshots.isEmpty {
                        VStack(alignment: .leading, spacing: 8) {
                            Text("Screenshots")
                                .font(.headline)

                            ScrollView(.horizontal, showsIndicators: false) {
                                HStack(spacing: 12) {
                                    ForEach(game.screenshots, id: \.self) { url in
                                        AsyncImage(url: url) { image in
                                            image
                                                .resizable()
                                                .aspectRatio(contentMode: .fill)
                                                .frame(width: 300, height: 170)
                                                .clipShape(RoundedRectangle(cornerRadius: 8))
                                        } placeholder: {
                                            Rectangle()
                                                .fill(.gray.opacity(0.3))
                                                .frame(width: 300, height: 170)
                                                .clipShape(RoundedRectangle(cornerRadius: 8))
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                .padding()
            }
        }
        .toolbar {
            ToolbarItem(placement: .primaryAction) {
                Button {
                    showingMetadataEditor = true
                } label: {
                    Label("Edit Metadata", systemImage: "pencil")
                }
            }
        }
        .sheet(isPresented: $showingMetadataEditor) {
            MetadataEditorView(game: game)
                .environmentObject(appState)
        }
    }

    @State private var showingMetadataEditor = false

    func toggleFavorite() {
        Task {
            await appState.toggleFavorite(game: game)
        }
    }
}

struct StatBadge: View {
    let label: String
    let value: String

    var body: some View {
        VStack(spacing: 4) {
            Text(value)
                .font(.title3)
                .fontWeight(.semibold)
            Text(label)
                .font(.caption)
                .foregroundStyle(.secondary)
        }
    }
}
