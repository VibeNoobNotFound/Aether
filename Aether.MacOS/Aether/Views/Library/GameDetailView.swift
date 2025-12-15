import AVKit
import SwiftUI

struct GameDetailView: View {
    @Environment(\.dismiss) var dismiss
    @EnvironmentObject var appState: AppState
    let game: GameViewModel

    @State private var showingMetadataEditor = false
    @State private var selectedMedia: MediaItem?
    @State private var isDescriptionExpanded = false

    var body: some View {
        ZStack {
            ScrollView {
                VStack(spacing: 0) {
                    // Immersive Hero Header
                    HeroHeaderView(game: game)
                        .frame(height: 400)

                    VStack(alignment: .leading, spacing: 32) {
                        // Title & Primary Actions
                        VStack(alignment: .leading, spacing: 16) {
                            Text(game.title)
                                .font(.system(size: 40, weight: .bold, design: .default))
                                .foregroundStyle(.white)

                            HStack(spacing: 16) {
                                Button(action: { appState.launchGame(game) }) {
                                    HStack {
                                        Image(systemName: "play.fill")
                                        Text("Play")
                                    }
                                    .font(.headline)
                                    .padding(.horizontal, 32)
                                    .padding(.vertical, 12)
                                    .background(Color.blue)
                                    .foregroundStyle(.white)
                                    .clipShape(Capsule())
                                }
                                .buttonStyle(.plain)

                                Button(action: { toggleFavorite() }) {
                                    Image(systemName: game.isFavorite ? "heart.fill" : "heart")
                                        .font(.title2)
                                        .foregroundStyle(game.isFavorite ? .red : .gray)
                                        .padding(12)
                                        .background(.ultraThinMaterial)
                                        .clipShape(Circle())
                                }
                                .buttonStyle(.plain)

                                Spacer()

                                // Age Rating / simple badges
                                if let score = game.metacriticScore {
                                    HStack(spacing: 4) {
                                        Text("\(Int(score))")
                                            .fontWeight(.bold)
                                        Text("Metacritic")
                                            .font(.caption)
                                            .foregroundStyle(.secondary)
                                    }
                                    .padding(.horizontal, 12)
                                    .padding(.vertical, 8)
                                    .background(.ultraThinMaterial)
                                    .clipShape(RoundedRectangle(cornerRadius: 8))
                                }
                            }
                        }
                        .padding(.horizontal)

                        Divider()
                            .padding(.horizontal)

                        // Media Carousel (Videos & Screenshots)
                        if !game.videos.isEmpty || !game.screenshots.isEmpty {
                            VStack(alignment: .leading, spacing: 16) {
                                Text("Preview")
                                    .font(.title2)
                                    .fontWeight(.semibold)
                                    .padding(.horizontal)

                                ScrollView(.horizontal, showsIndicators: false) {
                                    HStack(spacing: 16) {
                                        ForEach(mediaItems) { item in
                                            MediaThumbnailView(item: item)
                                                .onTapGesture {
                                                    selectedMedia = item
                                                }
                                        }
                                    }
                                    .padding(.horizontal)
                                }
                            }
                        }

                        // Info Grid
                        InfoGridView(game: game)
                            .padding(.horizontal)

                        // Description
                        VStack(alignment: .leading, spacing: 12) {
                            Text("About")
                                .font(.title2)
                                .fontWeight(.semibold)

                            ZStack(alignment: .bottom) {
                                HTMLText(html: game.description)
                                    .frame(
                                        maxHeight: isDescriptionExpanded ? .infinity : 200,
                                        alignment: .top
                                    )
                                    .mask(
                                        LinearGradient(
                                            colors: [
                                                .black, .black,
                                                isDescriptionExpanded ? .black : .clear,
                                            ],
                                            startPoint: .top,
                                            endPoint: .bottom
                                        )
                                    )

                                if !isDescriptionExpanded {
                                    Button("Read More") {
                                        withAnimation { isDescriptionExpanded = true }
                                    }
                                    .buttonStyle(.bordered)
                                    .padding(.bottom)
                                }
                            }
                        }
                        .padding(.horizontal)

                        // Tags
                        if !game.genres.isEmpty {
                            FlowLayout(spacing: 8) {
                                ForEach(game.genres, id: \.self) { genre in
                                    Text(genre)
                                        .font(.subheadline)
                                        .padding(.horizontal, 12)
                                        .padding(.vertical, 6)
                                        .background(Color.white.opacity(0.1))
                                        .clipShape(Capsule())
                                }
                            }
                            .padding(.horizontal)
                            .padding(.bottom, 50)
                        }
                    }
                    .padding(.top, 24)
                }
            }
            .ignoresSafeArea(edges: .top)

            // Lightbox Overlay
            if let selected = selectedMedia {
                MediaLightbox(
                    selectedMedia: $selectedMedia, allMedia: mediaItems, initialMedia: selected)
            }
        }
        .toolbar {
            ToolbarItem(placement: .primaryAction) {
                Button {
                    showingMetadataEditor = true
                } label: {
                    Label("Edit", systemImage: "pencil")
                }
            }
        }
        .sheet(isPresented: $showingMetadataEditor) {
            MetadataEditorView(game: game)
                .environmentObject(appState)
        }
    }

    var mediaItems: [MediaItem] {
        var items: [MediaItem] = []
        // Add videos first
        for video in game.videos {
            items.append(MediaItem(id: video.absoluteString, url: video, type: .video))
        }
        // Add screenshots
        for screenshot in game.screenshots {
            items.append(MediaItem(id: screenshot.absoluteString, url: screenshot, type: .image))
        }
        return items
    }

    func toggleFavorite() {
        Task {
            await appState.toggleFavorite(game: game)
        }
    }
}

// MARK: - Subviews

struct HeroHeaderView: View {
    let game: GameViewModel

    var body: some View {
        GeometryReader { geo in
            let minY = geo.frame(in: .global).minY

            ZStack(alignment: .bottomLeading) {
                if let bgURL = game.backgroundImageURL {
                    AsyncImage(url: bgURL) { image in
                        image.resizable()
                            .aspectRatio(contentMode: .fill)
                            .frame(
                                width: geo.size.width,
                                height: geo.size.height + (minY > 0 ? minY : 0)
                            )
                            .offset(y: minY > 0 ? -minY : 0)
                            // Removed blur and material overlay for better visibility
                            .overlay(
                                LinearGradient(
                                    colors: [.clear, .black.opacity(0.8)],
                                    startPoint: .center,
                                    endPoint: .bottom
                                )
                            )
                    } placeholder: {
                        Rectangle().fill(Color.black)
                    }
                }

                // Content Overlay (Logo / Title)
                VStack(alignment: .leading, spacing: 16) {
                    if let logoURL = game.logoImageURL {
                        AsyncImage(url: logoURL) { image in
                            image.resizable()
                                .aspectRatio(contentMode: .fit)
                        } placeholder: {
                            // If logo loads slowly, don't show anything or show title
                            Color.clear
                        }
                        .frame(height: 120)  // Limit logo height
                        .shadow(radius: 10)
                    } else {
                        // Fallback Title if no logo
                        Text(game.title)
                            .font(.system(size: 48, weight: .heavy, design: .rounded))
                            .foregroundStyle(.white)
                            .shadow(color: .black.opacity(0.5), radius: 10, x: 0, y: 5)
                    }
                }
                .padding(.horizontal, 32)
                .padding(.bottom, 32)
                .offset(y: minY > 0 ? -minY : 0)  // Parallax the logo too? Maybe slightly less?
            }
        }
    }
}

struct MediaThumbnailView: View {
    let item: MediaItem

    var body: some View {
        ZStack {
            if item.type == .video {
                // Video thumbnail (placeholder logic or try to fetch thumbnail?)
                // For simplicity, use a generic video placeholder
                Rectangle()
                    .fill(Color.black)
                    .overlay(
                        Image(systemName: "play.circle.fill")
                            .font(.system(size: 40))
                            .foregroundStyle(.white)
                    )
            } else {
                AsyncImage(url: item.url) { image in
                    image.resizable().aspectRatio(contentMode: .fill)
                } placeholder: {
                    Color.gray.opacity(0.3)
                }
            }
        }
        .frame(width: 280, height: 160)
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(Color.white.opacity(0.1), lineWidth: 1)
        )
    }
}

struct InfoGridView: View {
    let game: GameViewModel

    var body: some View {
        LazyVGrid(
            columns: [GridItem(.flexible()), GridItem(.flexible()), GridItem(.flexible())],
            spacing: 20
        ) {
            InfoItem(label: "Developer", value: game.developer ?? "-")
            InfoItem(label: "Release", value: game.formattedReleaseDate)
            InfoItem(label: "Publisher", value: game.publisher ?? "-")
            InfoItem(label: "Playtime", value: game.formattedPlaytime)
            InfoItem(label: "Last Played", value: game.lastPlayed != nil ? "Recently" : "Never")
        }
        .padding(16)
        .background(Color.white.opacity(0.05))
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }
}

struct InfoItem: View {
    let label: String
    let value: String

    var body: some View {
        VStack(alignment: .leading) {
            Text(label.uppercased())
                .font(.caption2)
                .fontWeight(.bold)
                .foregroundStyle(.secondary)
            Text(value)
                .font(.subheadline)
                .lineLimit(1)
        }
    }
}

// MARK: - Lightbox

struct MediaLightbox: View {
    @Binding var selectedMedia: MediaItem?
    let allMedia: [MediaItem]
    @State var currentMedia: MediaItem

    init(selectedMedia: Binding<MediaItem?>, allMedia: [MediaItem], initialMedia: MediaItem) {
        self._selectedMedia = selectedMedia
        self.allMedia = allMedia
        self._currentMedia = State(initialValue: initialMedia)
    }

    var body: some View {
        ZStack {
            Color.black.ignoresSafeArea()

            // Content
            ZStack {
                if currentMedia.type == .video {
                    VideoPlayer(player: AVPlayer(url: currentMedia.url))
                } else {
                    AsyncImage(url: currentMedia.url) { image in
                        image.resizable().aspectRatio(contentMode: .fit)
                    } placeholder: {
                        ProgressView()
                    }
                }
            }
            .id(currentMedia.id)  // Force redraw on change

            // Navigation Overlay
            HStack {
                Button {
                    navigate(-1)
                } label: {
                    Image(systemName: "chevron.left")
                        .font(.system(size: 40))
                        .foregroundStyle(.white.opacity(0.8))
                        .padding()
                        .background(Color.black.opacity(0.3))
                        .clipShape(Circle())
                }
                .buttonStyle(.plain)
                .opacity(canNavigate(-1) ? 1 : 0)

                Spacer()

                Button {
                    navigate(1)
                } label: {
                    Image(systemName: "chevron.right")
                        .font(.system(size: 40))
                        .foregroundStyle(.white.opacity(0.8))
                        .padding()
                        .background(Color.black.opacity(0.3))
                        .clipShape(Circle())
                }
                .buttonStyle(.plain)
                .opacity(canNavigate(1) ? 1 : 0)
            }
            .padding(.horizontal, 40)

            // Close button
            VStack {
                HStack {
                    Spacer()
                    Button {
                        selectedMedia = nil
                    } label: {
                        Image(systemName: "xmark.circle.fill")
                            .font(.system(size: 30))
                            .foregroundStyle(.white.opacity(0.8))
                            .padding()
                    }
                    .buttonStyle(.plain)
                    .keyboardShortcut(.cancelAction)
                }
                Spacer()
            }
        }
        .transition(.opacity)
        .zIndex(100)
    }

    private func navigate(_ direction: Int) {
        if let index = allMedia.firstIndex(of: currentMedia) {
            let newIndex = index + direction
            if newIndex >= 0 && newIndex < allMedia.count {
                currentMedia = allMedia[newIndex]
            }
        }
    }

    private func canNavigate(_ direction: Int) -> Bool {
        if let index = allMedia.firstIndex(of: currentMedia) {
            let newIndex = index + direction
            return newIndex >= 0 && newIndex < allMedia.count
        }
        return false
    }
}

struct MediaItem: Identifiable, Hashable {
    let id: String
    let url: URL
    let type: MediaType
}

enum MediaType {
    case image
    case video
}
