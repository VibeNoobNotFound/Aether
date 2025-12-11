import SwiftUI

struct HeroCarousel: View {
    @EnvironmentObject var appState: AppState
    @State private var scrollID: Int? = 0

    // Fallback if no games
    let defaultGame = "Scanning Library..."

    var displayGames: [GameViewModel] {
        Array(appState.games.prefix(5))
    }

    var currentIndex: Int {
        scrollID ?? 0
    }

    var body: some View {
        ZStack {
            // ScrollView-based Carousel
            GeometryReader { geometry in
                ScrollView(.horizontal, showsIndicators: false) {
                    LazyHStack(spacing: 0) {
                        if appState.games.isEmpty {
                            gameCard(
                                title: defaultGame, id: "", imageUrl: nil,
                                width: geometry.size.width)
                        } else {
                            ForEach(Array(displayGames.enumerated()), id: \.element.id) {
                                index, game in
                                gameCard(
                                    title: game.title,
                                    id: game.id,
                                    imageUrl: nil,
                                    width: geometry.size.width
                                )
                                .id(index)
                            }
                        }
                    }
                    .scrollTargetLayout()
                }
                .scrollTargetBehavior(.viewAligned)
                .scrollPosition(id: $scrollID)
            }
            .frame(height: 400)

            // Custom Page Indicators (rounded dots)
            VStack {
                Spacer()
                if !appState.games.isEmpty && displayGames.count > 1 {
                    HStack(spacing: 8) {
                        ForEach(0..<displayGames.count, id: \.self) { index in
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

            // Navigation Arrows (overlaid)
            if !appState.games.isEmpty && displayGames.count > 1 {
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
                            scrollID = min(displayGames.count - 1, currentIndex + 1)
                        }
                    }) {
                        Image(systemName: "chevron.right.circle.fill")
                            .font(.system(size: 40))
                            .foregroundStyle(.white.opacity(0.7))
                            .shadow(radius: 10)
                    }
                    .buttonStyle(.plain)
                    .opacity(currentIndex < displayGames.count - 1 ? 1.0 : 0.3)
                    .disabled(currentIndex >= displayGames.count - 1)
                }
                .padding(.horizontal, 40)
            }
        }
    }

    @ViewBuilder
    func gameCard(title: String, id: String, imageUrl: String?, width: CGFloat) -> some View {
        ZStack(alignment: .bottomLeading) {
            // Background Image (when available) or Gradient Placeholder
            if let imageUrl = imageUrl {
                // Future: Load AsyncImage from URL
                AsyncImage(url: URL(string: imageUrl)) { phase in
                    switch phase {
                    case .success(let image):
                        image
                            .resizable()
                            .aspectRatio(contentMode: .fill)
                            .frame(width: width - 40)
                            .clipped()
                    case .failure(_), .empty:
                        gradientPlaceholder(width: width - 40)
                    @unknown default:
                        gradientPlaceholder(width: width - 40)
                    }
                }
            } else {
                gradientPlaceholder(width: width - 40)
            }

            // Content Overlay with Glass Effect
            VStack(alignment: .leading, spacing: 10) {
                Text(title)
                    .font(.system(size: 48, weight: .bold, design: .rounded))
                    .foregroundStyle(.white)
                    .shadow(color: .black.opacity(0.3), radius: 10, x: 0, y: 5)

                if !id.isEmpty {
                    HStack {
                        Button(action: {
                            appState.launch(gameId: id)
                        }) {
                            Label("Play Now", systemImage: "play.fill")
                                .padding(.horizontal, 20)
                                .padding(.vertical, 10)
                                .font(.headline)
                        }
                        .buttonStyle(.plain)
                        .background(Capsule().fill(.ultraThinMaterial))
                        .overlay(Capsule().stroke(Color.white.opacity(0.5), lineWidth: 1))
                        .shadow(radius: 5)

                        Button(action: {}) {
                            Image(systemName: "plus.circle.fill")
                                .font(.title2)
                                .foregroundStyle(.white)
                        }
                        .buttonStyle(.plain)
                    }
                }
            }
            .padding(40)
        }
        .frame(width: width - 40)
        .clipShape(RoundedRectangle(cornerRadius: 20))
        .padding(.horizontal, 20)
        .containerRelativeFrame(.horizontal)
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
        .frame(width: width)
    }
}
