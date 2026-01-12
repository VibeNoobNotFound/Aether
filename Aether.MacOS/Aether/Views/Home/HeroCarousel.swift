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
                                width: geometry.size.width)
                        } else {
                            ForEach(Array(games.enumerated()), id: \.element.id) {
                                index, game in
                                NavigationLink(value: game) {
                                    gameCard(
                                        title: game.title,
                                        id: game.id,
                                        imageUrl: nil,
                                        width: geometry.size.width
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
            .frame(height: 380)

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
    func gameCard(title: String, id: String, imageUrl: String?, width: CGFloat) -> some View {
        // Find the game object if possible to get more metadata
        let game =
            games.first(where: { $0.id == id }) ?? appState.games.first(where: { $0.id == id })

        ZStack(alignment: .bottomLeading) {
            // Background Image - fills and clips
            Group {
                if let game = game, let bgURL = game.backgroundImageURL ?? game.coverImageURL {
                    AsyncImage(url: bgURL) { image in
                        image
                            .resizable()
                            .aspectRatio(contentMode: .fill)
                    } placeholder: {
                        gradientPlaceholder(width: width - 40)
                    }
                } else {
                    gradientPlaceholder(width: width - 40)
                }
            }
            .frame(width: max(0, width - 40), height: 380)
            .clipped()

            // Bottom Gradient for text readability
            LinearGradient(
                colors: [.clear, .black.opacity(0.6), .black.opacity(0.85)],
                startPoint: .center,
                endPoint: .bottom
            )

            // Content Overlay - FIXED at bottom left
            VStack(alignment: .leading, spacing: 8) {
                Text(title)
                    .font(.system(size: 28, weight: .bold, design: .rounded))
                    .foregroundStyle(.white)
                    .shadow(color: .black.opacity(0.5), radius: 8, x: 0, y: 3)
                    .lineLimit(2)
                    .minimumScaleFactor(0.8)

                if let game = game {
                    HStack(spacing: 8) {
                        if !game.genres.isEmpty {
                            Text(game.genres.prefix(2).joined(separator: " • "))
                                .font(.caption)
                                .fontWeight(.medium)
                                .foregroundStyle(.white.opacity(0.9))
                                .padding(.horizontal, 8)
                                .padding(.vertical, 4)
                                .background {
                                    GlassCard(padding: 0, cornerRadius: 100) {
                                        Color.clear
                                    }
                                }
                        }

                        if let score = game.metacriticScore {
                            HStack(spacing: 3) {
                                Image(systemName: "star.fill")
                                    .foregroundStyle(.yellow)
                                    .foregroundStyle(.white)
                                Text("\(Int(score))")
                                    .foregroundStyle(.white)
                            }
                            .font(.caption)
                            .fontWeight(.bold)
                            .padding(.horizontal, 8)
                            .padding(.vertical, 4)
                            .background {
                                GlassCard(padding: 0, cornerRadius: 100) {
                                    Color.clear
                                }
                            }
                        }
                    }
                }
            }
            .frame(maxWidth: max(0, width - 100), alignment: .leading)
            .padding(.horizontal, 20)
            .padding(.bottom, 20)
        }
        .frame(width: max(0, width - 40), height: 380)
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
    .frame(width: 800, height: 400)
    #endif
}
