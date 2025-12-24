import AVKit
import SwiftUI

struct GameDetailView: View {
    @Environment(\.dismiss) var dismiss
    @EnvironmentObject var appState: AppState
    let game: GameViewModel

    @State private var showingMetadataEditor = false
    @State private var selectedMedia: MediaItem?
    @State private var isDescriptionExpanded = false
    @State private var news: [NewsItem] = []
    @State private var canLaunchInfo: (canLaunch: Bool, reason: String?, launchMethod: String?) = (
        false, nil, nil
    )
    @State private var isCheckingLaunch = true

    var body: some View {
        GeometryReader { geo in
            ZStack {
                // 1. LIQUID GLASS BACKGROUND
                // Full screen blurred art that sets the mood
                if let bgURL = game.backgroundImageURL ?? game.coverImageURL {
                    CachedAsyncImage(url: bgURL) { image in
                        image.resizable()
                            .aspectRatio(contentMode: .fill)
                            .frame(width: geo.size.width, height: geo.size.height)
                            .blur(radius: 60)  // Heavy blur for "liquid" feel
                            .overlay(Color.black.opacity(0.4))  // Darken for text readability
                    } placeholder: {
                        Color.black
                    }
                    .ignoresSafeArea()
                } else {
                    Color.black.ignoresSafeArea()
                }

                ScrollView {
                    VStack(spacing: 0) {
                        // 2. IMMERSIVE PARALLAX HEADER
                        HeroHeaderView(game: game)
                            .frame(height: 500)  // Taller header

                        // 3. CONTENT CONTENT (Glass Sheet)
                        VStack(alignment: .leading, spacing: 32) {

                            // Title & Actions
                            headerContent

                            Divider().background(Color.white.opacity(0.2))

                            // Media Carousel (Autoplay)
                            mediaCarousel

                            // Info Grid
                            InfoGridView(game: game)

                            // Latest News
                            if !news.isEmpty {
                                VStack(alignment: .leading, spacing: 16) {
                                    Label("Latest News", systemImage: "newspaper.fill")
                                        .font(.title3)
                                        .fontWeight(.bold)
                                        .foregroundStyle(.white.opacity(0.9))

                                    NewsFeedView(news: news)
                                }
                            }

                            // About Section
                            aboutSection

                            // Tags
                            tagsSection
                        }
                        .padding(32)
                        .background(.ultraThinMaterial)  // The "Glass" Sheet
                        .clipShape(RoundedRectangle(cornerRadius: 24, style: .continuous))
                        .padding(.horizontal, 20)
                        .offset(y: -50)  // Overlap the header slightly
                    }
                }
                .edgesIgnoringSafeArea(.top)
                .task {
                    self.news = await appState.fetchGameNews(gameId: game.id)
                    self.canLaunchInfo = await appState.canLaunchGame(game.id)
                    self.isCheckingLaunch = false
                }

                // Lightbox Overlay
                if let selected = selectedMedia {
                    MediaLightbox(
                        selectedMedia: $selectedMedia, allMedia: mediaItems, initialMedia: selected)
                }
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

    // MARK: - Sub-Views

    var headerContent: some View {
        HStack(alignment: .bottom, spacing: 24) {
            // Actions
            if isCheckingLaunch {
                ProgressView()
                    .padding(.horizontal, 32)
                    .padding(.vertical, 16)
                    .background(.ultraThinMaterial)
                    .clipShape(Capsule())
            } else if canLaunchInfo.canLaunch {
                Button(action: { appState.launchGame(game) }) {
                    HStack {
                        Image(systemName: "play.fill")
                        Text(playButtonText)
                            .fontWeight(.bold)
                    }
                    .padding(.horizontal, 32)
                    .padding(.vertical, 16)
                    .background(Color.blue.gradient)
                    .foregroundStyle(.white)
                    .clipShape(Capsule())
                    .shadow(color: .blue.opacity(0.5), radius: 20, x: 0, y: 10)
                }
                .buttonStyle(.plain)
            } else {
                Text(canLaunchInfo.reason ?? "Cannot launch")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .padding(.horizontal, 24)
                    .padding(.vertical, 12)
                    .background(.ultraThinMaterial)
                    .clipShape(Capsule())
            }

            Button(action: { toggleFavorite() }) {
                Image(systemName: game.isFavorite ? "heart.fill" : "heart")
                    .font(.title2)
                    .foregroundStyle(game.isFavorite ? .red : .white.opacity(0.7))
                    .padding(14)
                    .background(.ultraThinMaterial)
                    .clipShape(Circle())
            }
            .buttonStyle(.plain)

            Spacer()

            // Metacritic Badge
            if let score = game.metacriticScore {
                HStack(spacing: 6) {
                    Text("\(Int(score))")
                        .font(.title2)
                        .fontWeight(.heavy)
                        .foregroundStyle(score >= 75 ? .green : (score >= 50 ? .yellow : .red))
                    Text("METASCORE")
                        .font(.caption)
                        .fontWeight(.bold)
                        .foregroundStyle(.secondary)
                }
                .padding(.horizontal, 16)
                .padding(.vertical, 10)
                .background(.thinMaterial)
                .clipShape(RoundedRectangle(cornerRadius: 12))
            }
        }
    }

    var playButtonText: String {
        guard let method = canLaunchInfo.launchMethod else {
            return "Play Now"
        }
        switch method.lowercased() {
        case "steam": return "Play on Steam"
        case "epic_games": return "Play on Epic"
        case "app_store": return "Play"
        case "direct": return "Launch"
        default: return "Play Now"
        }
    }

    var mediaCarousel: some View {
        VStack(alignment: .leading, spacing: 16) {
            Label("Gallery", systemImage: "play.rectangle.on.rectangle")
                .font(.title3)
                .fontWeight(.bold)
                .foregroundStyle(.white.opacity(0.9))

            ScrollView(.horizontal, showsIndicators: false) {
                HStack(spacing: 16) {
                    ForEach(mediaItems) { item in
                        MediaThumbnailView(item: item)
                            .onTapGesture {
                                selectedMedia = item
                            }
                    }
                }
            }
            .padding(.horizontal)  // Fixed invalid .visible padding
        }
    }
    var aboutSection: some View {
        VStack(alignment: .leading, spacing: 12) {
            Label("About", systemImage: "info.circle")
                .font(.title3)
                .fontWeight(.bold)
                .foregroundStyle(.white.opacity(0.9))

            ZStack(alignment: .bottom) {
                HTMLText(html: game.description)
                    .frame(
                        maxHeight: isDescriptionExpanded ? .infinity : 200,
                        alignment: .top
                    )
                    .mask(
                        LinearGradient(
                            colors: [.black, .black, isDescriptionExpanded ? .black : .clear],
                            startPoint: .top,
                            endPoint: .bottom
                        )
                    )

                if !isDescriptionExpanded {
                    Button {
                        withAnimation(.spring()) { isDescriptionExpanded = true }
                    } label: {
                        Text("Read More")
                            .font(.subheadline)
                            .fontWeight(.medium)
                            .foregroundStyle(.white)
                            .padding(.horizontal, 24)
                            .padding(.vertical, 8)
                            .background(.ultraThinMaterial)
                            .clipShape(Capsule())
                    }
                    .buttonStyle(.plain)
                    .padding(.bottom)
                }
            }
        }
    }

    var tagsSection: some View {
        FlowLayout(spacing: 8) {
            ForEach(game.genres, id: \.self) { genre in
                Text(genre)
                    .font(.caption)
                    .fontWeight(.semibold)
                    .padding(.horizontal, 12)
                    .padding(.vertical, 6)
                    .background(Color.white.opacity(0.1))
                    .clipShape(Capsule())
                    .overlay(
                        Capsule().stroke(Color.white.opacity(0.2), lineWidth: 1)
                    )
            }
        }
        .padding(.top, 16)
    }

    var mediaItems: [MediaItem] {
        var items: [MediaItem] = []
        for video in game.videos {
            items.append(MediaItem(id: video.absoluteString, url: video, type: .video))
        }
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

// MARK: - Components

struct HeroHeaderView: View {
    let game: GameViewModel

    var body: some View {
        GeometryReader { geo in
            let minY = geo.frame(in: .global).minY

            ZStack(alignment: .bottomLeading) {
                if let bgURL = game.backgroundImageURL ?? game.coverImageURL {
                    CachedAsyncImage(url: bgURL) { image in
                        image.resizable()
                            .aspectRatio(contentMode: .fill)
                            .frame(
                                width: geo.size.width,
                                height: geo.size.height + (minY > 0 ? minY : 0)
                            )
                            .offset(y: minY > 0 ? -minY : 0)
                            // Fade into the glass sheet below
                            .mask(
                                LinearGradient(
                                    colors: [.black, .black, .clear],
                                    startPoint: .top,
                                    endPoint: .bottom
                                )
                            )
                    } placeholder: {
                        Color.black
                    }
                }

                // Bottom Gradient for text readability
                LinearGradient(
                    colors: [.clear, .black.opacity(0.6), .black.opacity(0.85)],
                    startPoint: .center,
                    endPoint: .bottom
                )
                // Logo or Title Overlay
                VStack(alignment: .leading, spacing: 10) {
                    if let logoURL = game.logoImageURL {
                        CachedAsyncImage(url: logoURL) { image in
                            image.resizable()
                                .aspectRatio(contentMode: .fit)
                            
                            .shadow(color: .black.opacity(0.5), radius: 8, x: 0, y: 3)
                            .minimumScaleFactor(0.8)
                        } placeholder: {
                            Color.clear
                        }
                        .frame(height: 140)
                        .shadow(color: .black.opacity(0.5), radius: 60, x: 0, y: 10)
                    } else {
                        Text(game.title)
                            .font(.system(size: 56, weight: .black, design: .rounded))
                            .foregroundStyle(.white)
                            .shadow(color: .black.opacity(0.5), radius: 15, x: 0, y: 5)
                            .lineLimit(2)
                    }

                    if let developer = game.developer {
                        Text(developer.uppercased())
                            .font(.headline)
                            .fontWeight(.bold)
                            .foregroundStyle(.secondary)
                            .padding(.horizontal, 10)
                            .padding(.vertical, 4)
                            .background(.ultraThinMaterial)
                            .clipShape(RoundedRectangle(cornerRadius: 6))
                    }
                }
                .padding(.horizontal, 32)
                .padding(.bottom, 60)  // Lift up to clear the glass sheet overlap
                .offset(y: minY > 0 ? -minY * 0.5 : 0)  // Slower parallax for content
            }
        }
    }
}

struct MediaThumbnailView: View {
    let item: MediaItem

    var body: some View {
        ZStack {
            if item.type == .video {
                // Autoplay Video!
                AutoplayVideoPlayer(url: item.url)
            } else {
                CachedAsyncImage(url: item.url) { image in
                    image.resizable().aspectRatio(contentMode: .fill)
                } placeholder: {
                    Color.white.opacity(0.1)
                }
            }
        }
        .frame(width: 300, height: 169)  // 16:9
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(Color.white.opacity(0.2), lineWidth: 1)
        )
        .shadow(color: .black.opacity(0.3), radius: 8, x: 0, y: 4)
    }
}

struct InfoGridView: View {
    let game: GameViewModel

    var body: some View {
        LazyVGrid(
            columns: [GridItem(.flexible()), GridItem(.flexible()), GridItem(.flexible())],
            spacing: 20
        ) {
            InfoItem(label: "Released", value: game.formattedReleaseDate)
            InfoItem(label: "Playtime", value: game.formattedPlaytime)
            InfoItem(label: "Last Played", value: game.formattedLastPlayed)
        }
        .padding(20)
        .background(Color.black.opacity(0.2))
        .clipShape(RoundedRectangle(cornerRadius: 16))
    }
}

struct InfoItem: View {
    let label: String
    let value: String

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(label.uppercased())
                .font(.caption2)
                .fontWeight(.bold)
                .foregroundStyle(.white.opacity(0.6))
            Text(value)
                .font(.callout)
                .fontWeight(.medium)
                .foregroundStyle(.white)
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
                    CachedAsyncImage(url: currentMedia.url) { image in
                        image.resizable().aspectRatio(contentMode: .fit)
                    } placeholder: {
                        ProgressView()
                    }
                }
            }
            .id(currentMedia)  // Force transition
            .transition(.opacity)

            // Navigation Overlay
            VStack {
                // Top Bar
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

                // Bottom Bar (Arrows)
                HStack {
                    Button {
                        previousMedia()
                    } label: {
                        Image(systemName: "chevron.left.circle.fill")
                            .font(.system(size: 40))
                            .foregroundStyle(.white.opacity(0.6))
                    }
                    .buttonStyle(.plain)
                    .keyboardShortcut(.leftArrow, modifiers: [])

                    Spacer()

                    Text("\(currentIndex + 1) / \(allMedia.count)")
                        .foregroundStyle(.white.opacity(0.6))
                        .font(.headline)

                    Spacer()

                    Button {
                        nextMedia()
                    } label: {
                        Image(systemName: "chevron.right.circle.fill")
                            .font(.system(size: 40))
                            .foregroundStyle(.white.opacity(0.6))
                    }
                    .buttonStyle(.plain)
                    .keyboardShortcut(.rightArrow, modifiers: [])
                }
                .padding(.horizontal, 40)
                .padding(.bottom, 40)
            }
        }
        .zIndex(100)
    }

    var currentIndex: Int {
        allMedia.firstIndex(of: currentMedia) ?? 0
    }

    func nextMedia() {
        if let idx = allMedia.firstIndex(of: currentMedia), idx < allMedia.count - 1 {
            withAnimation {
                currentMedia = allMedia[idx + 1]
            }
        }
    }

    func previousMedia() {
        if let idx = allMedia.firstIndex(of: currentMedia), idx > 0 {
            withAnimation {
                currentMedia = allMedia[idx - 1]
            }
        }
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
