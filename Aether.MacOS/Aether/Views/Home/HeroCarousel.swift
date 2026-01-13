import SwiftUI

struct HeroCarousel: View {
    @EnvironmentObject var appState: AppState
    @Binding var currentIndex: Int
    let games: [GameViewModel]
    @State private var scrollID: Int? = 0

    // Fallback if no games
    let defaultGame = "Scanning Library..."

    var body: some View {
        ZStack {
            // ScrollView-based Carousel (interactive content)
            GeometryReader { geometry in
                ScrollView(.horizontal, showsIndicators: false) {
                    LazyHStack(spacing: 0) {
                        if games.isEmpty {
                            gameCard(
                                title: defaultGame, id: "", imageUrl: nil,
                                size: geometry.size)
                        } else {
                            ForEach(Array(games.enumerated()), id: \.element.id) {
                                index, game in
                                NavigationLink(value: game) {
                                    gameCard(
                                        title: game.title,
                                        id: game.id,
                                        imageUrl: nil,
                                        size: geometry.size
                                    )
                                }
                                .buttonStyle(.plain)
                                .id(index)
                            }
                        }
                    }
                    .scrollTargetLayout()
                }
                .scrollTargetBehavior(.viewAligned)
                .scrollPosition(id: $scrollID)
                .onChange(of: scrollID) { oldValue, newValue in
                    if let val = newValue {
                        withAnimation(.easeInOut(duration: 0.5)) {
                            currentIndex = val
                        }
                    }
                }
            }
            // Removed fixed height frame to allow dynamic sizing from parent

            // Custom Page Indicators (non-blocking)
            VStack {
                Spacer()
                if !games.isEmpty && games.count > 1 {
                    HStack(spacing: 8) {
                        ForEach(0..<games.count, id: \.self) { index in
                            Circle()
                                .fill(
                                    currentIndex == index ? Color.white : Color.white.opacity(0.5)
                                )
                                .frame(width: 8, height: 8)
                                .onTapGesture {
                                    withAnimation(.spring(response: 0.3, dampingFraction: 0.7)) {
                                        scrollID = index
                                    }
                                }
                        }
                    }
                    .padding(.bottom, 20)
                }
            }
            .allowsHitTesting(true)  // Allow dot clicks

            // Navigation Arrows (overlaid, only capture clicks on arrows)
            if !games.isEmpty && games.count > 1 {
                HStack {
                    // Previous Button
                    Button(action: {
                        withAnimation(.spring(response: 0.3, dampingFraction: 0.7)) {
                            scrollID = max(0, currentIndex - 1)
                        }
                    }) {
                        Image(systemName: "chevron.left.circle.fill")
                            .font(.system(size: 40))
                            .foregroundStyle(.white.opacity(0.7))
                            .shadow(radius: 10)
                    }
                    .buttonStyle(.plain)
                    .opacity(currentIndex > 0 ? 1.0 : 0.3)
                    .disabled(currentIndex == 0)

                    Spacer()

                    // Next Button
                    Button(action: {
                        withAnimation(.spring(response: 0.3, dampingFraction: 0.7)) {
                            scrollID = min(games.count - 1, currentIndex + 1)
                        }
                    }) {
                        Image(systemName: "chevron.right.circle.fill")
                            .font(.system(size: 40))
                            .foregroundStyle(.white.opacity(0.7))
                            .shadow(radius: 10)
                    }
                    .buttonStyle(.plain)
                    .opacity(currentIndex < games.count - 1 ? 1.0 : 0.3)
                    .disabled(currentIndex >= games.count - 1)
                }
                .padding(.horizontal, 40)
                .allowsHitTesting(true)
            }
        }
        .clipped()
    }

    @ViewBuilder
    func gameCard(title: String, id: String, imageUrl: String?, size: CGSize) -> some View {
        // Find the game object if possible to get more metadata
        let game =
            games.first(where: { $0.id == id }) ?? appState.games.first(where: { $0.id == id })

        ZStack(alignment: .leading) {
            // Background Image - fills and clips
            Group {
                if let game = game, let bgURL = game.backgroundImageURL ?? game.coverImageURL {
                    AsyncImage(url: bgURL) { image in
                        image
                            .resizable()
                            .aspectRatio(contentMode: .fill)
                    } placeholder: {
                        gradientPlaceholder(width: size.width - 40)
                    }
                } else {
                    gradientPlaceholder(width: size.width - 40)
                }
            }
            .frame(width: max(0, size.width - 40))
            .frame(maxHeight: .infinity)
            .clipped()

            // Leading Gradient for text readability
            LinearGradient(
                colors: [.black.opacity(0.8), .black.opacity(0.4), .clear],
                startPoint: .leading,
                endPoint: .trailing
            )

            // Content Overlay Group
            ZStack(alignment: .leading) {
                // 1. Logo and Play Button (Vertically Centered)
                VStack(alignment: .leading, spacing: 24) {
                    // Logo or Title
                    if let game = game, let logoURL = game.logoImageURL {
                        CachedAsyncImage(url: logoURL) { image in
                            image
                                .resizable()
                                .scaledToFit()
                        } placeholder: {
                            Text(title)
                                .font(.system(size: 28, weight: .bold, design: .rounded))
                                .foregroundStyle(.white)
                                .shadow(color: .black.opacity(0.5), radius: 8, x: 0, y: 3)
                        }
                        .frame(height: size.height * 0.35)  // Slightly smaller logo to fit play button
                        .frame(maxWidth: size.width * 0.5, alignment: .leading)
                    } else {
                        Text(title)
                            .font(.system(size: 40, weight: .bold, design: .rounded))
                            .foregroundStyle(.white)
                            .shadow(color: .black.opacity(0.5), radius: 8, x: 0, y: 3)
                            .lineLimit(2)
                            .minimumScaleFactor(0.8)
                    }

                    // Big Play Button
                    if let game = game {
                        Button {
                            appState.launchGame(game)
                        } label: {
                            HStack(spacing: 8) {
                                Image(systemName: "play.fill")
                                Text("Play Now")
                            }
                            .font(.title2)
                            .fontWeight(.bold)
                            .foregroundStyle(.white)
                            .padding(.horizontal, 32)  // Bigger padding
                            .padding(.vertical, 16)  // Bigger vertical padding
                            .glassEffect()  // Ensure glassEffect is available or use appropriate modifier
                        }
                        .buttonStyle(.plain)
                    }
                }
                .frame(maxWidth: size.width * 0.6, alignment: .leading)
                .frame(maxHeight: .infinity, alignment: .leading)  // Center vertically, align leading

                // 2. Metadata (Bottom Left Corner)
                VStack {
                    Spacer()
                    HStack(spacing: 12) {
                        if let game = game {
                            if !game.genres.isEmpty {
                                Text(game.genres.prefix(3).joined(separator: " • "))
                                    .font(.subheadline)
                                    .fontWeight(.medium)
                                    .foregroundStyle(.white.opacity(0.9))
                                    .padding(.horizontal, 12)
                                    .padding(.vertical, 6)
                                    .background {
                                        GlassCard(padding: 0, cornerRadius: 100) {
                                            Color.clear
                                        }
                                    }
                            }

                            if let score = game.metacriticScore {
                                HStack(spacing: 4) {
                                    Image(systemName: "star.fill")
                                        .foregroundStyle(.white)
                                    Text("\(Int(score))")
                                        .foregroundStyle(.white)
                                }
                                .font(.subheadline)
                                .fontWeight(.bold)
                                .padding(.horizontal, 12)
                                .padding(.vertical, 6)
                                .background {
                                    GlassCard(padding: 0, cornerRadius: 100) {
                                        Color.clear
                                    }
                                }
                            }
                        }
                    }
                }
                .frame(maxWidth: .infinity, alignment: .leading)
                .padding(.bottom, 40)
            }
            .padding(.horizontal, 40)
        }
        .frame(width: max(0, size.width - 40))
        .frame(maxHeight: .infinity)
        .clipShape(RoundedRectangle(cornerRadius: 20))
        .contextMenu {
            if let game = game {
                Button {
                    appState.launchGame(game)
                } label: {
                    Label("Play", systemImage: "play.fill")
                }

                Button {
                    Task { await appState.toggleFavorite(game: game) }
                } label: {
                    Label(
                        game.isFavorite ? "Unfavorite" : "Favorite",
                        systemImage: game.isFavorite ? "heart.slash" : "heart")
                }

                Button {
                    Task { await appState.openGameLocation(game: game) }
                } label: {
                    Label("Show in Finder", systemImage: "folder")
                }
            }
        }
        .padding(.horizontal, 20)
    }

    @ViewBuilder
    func gradientPlaceholder(width: CGFloat) -> some View {
        LinearGradient(
            gradient: Gradient(colors: [
                .blue.opacity(0.6),
                .purple.opacity(0.8),
                .pink.opacity(0.6),
            ]),
            startPoint: .topLeading,
            endPoint: .bottomTrailing
        )
        .frame(width: max(0, width))
    }
}

#Preview {

    #if DEBUG
        HeroCarousel(
            currentIndex: .constant(0),
            games: MockData.games
        )
        .environmentObject(MockData.appState)
        .frame(width: 800, height: 500)
    #endif
}
