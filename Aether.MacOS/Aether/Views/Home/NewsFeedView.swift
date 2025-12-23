import SwiftUI

struct NewsFeedView: View {
    let news: [NewsItem]
    @State private var currentIndex = 0

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            // Section Header
            HStack {
                Text("Latest News")
                    .font(.title2)
                    .fontWeight(.bold)
                    .foregroundStyle(.white)
                    .shadow(radius: 5)

                Spacer()

                if news.count > 1 {
                    HStack(spacing: 8) {
                        ForEach(0..<min(news.count, 5), id: \.self) { index in
                            Circle()
                                .fill(
                                    currentIndex == index ? Color.white : Color.white.opacity(0.4)
                                )
                                .frame(width: 6, height: 6)
                        }
                    }
                }
            }
            .padding(.horizontal)

            // Carousel-style horizontal scroll
            GeometryReader { geometry in
                ScrollView(.horizontal, showsIndicators: false) {
                    HStack(spacing: 16) {
                        ForEach(Array(news.indices), id: \.self) { index in
                            NewsCarouselCard(
                                item: news[index], cardWidth: min(360, geometry.size.width * 0.8)
                            )
                            .id(index)
                        }
                    }
                    .scrollTargetLayout()
                    .padding(.horizontal)
                }
                .scrollTargetBehavior(.viewAligned)
                .scrollPosition(
                    id: Binding(
                        get: { currentIndex },
                        set: { newValue in
                            if let val = newValue {
                                withAnimation {
                                    currentIndex = val
                                }
                            }
                        }
                    ))
            }
            .frame(height: 200)
        }
    }
}

// Carousel-style news card
struct NewsCarouselCard: View {
    let item: NewsItem
    let cardWidth: CGFloat
    @State private var isHovered = false

    var body: some View {
        Link(destination: item.url ?? URL(string: "https://store.steampowered.com")!) {
            ZStack(alignment: .bottomLeading) {
                // Background Image
                if let imageUrl = item.imageUrl {
                    CachedAsyncImage(url: imageUrl) { image in
                        image
                            .resizable()
                            .aspectRatio(contentMode: .fill)
                    } placeholder: {
                        Rectangle()
                            .fill(
                                LinearGradient(
                                    colors: [.blue.opacity(0.4), .purple.opacity(0.4)],
                                    startPoint: .topLeading,
                                    endPoint: .bottomTrailing
                                )
                            )
                    }
                } else {
                    Rectangle()
                        .fill(
                            LinearGradient(
                                colors: [.blue.opacity(0.4), .purple.opacity(0.4)],
                                startPoint: .topLeading,
                                endPoint: .bottomTrailing
                            )
                        )
                }

                // Bottom Gradient
                LinearGradient(
                    colors: [.clear, .black.opacity(0.8)],
                    startPoint: .center,
                    endPoint: .bottom
                )

                // Content
                VStack(alignment: .leading, spacing: 6) {
                    // Source Badge
                    Text(item.source)
                        .font(.caption2)
                        .fontWeight(.bold)
                        .padding(.horizontal, 8)
                        .padding(.vertical, 4)
                        .background(.ultraThinMaterial)
                        .clipShape(Capsule())

                    // Title
                    Text(item.title)
                        .font(.headline)
                        .fontWeight(.bold)
                        .lineLimit(2)
                        .foregroundStyle(.white)
                        .shadow(color: .black.opacity(0.5), radius: 5)

                    // Author & Date
                    HStack {
                        Text(item.author)
                            .lineLimit(1)
                        Spacer()
                        Text(item.date, style: .date)
                    }
                    .font(.caption)
                    .foregroundStyle(.white.opacity(0.7))
                }
                .frame(maxWidth: cardWidth - 32, alignment: .leading)
                .padding(16)
            }
            .frame(width: cardWidth, height: 180)
            .clipShape(RoundedRectangle(cornerRadius: 16))
            .overlay(
                RoundedRectangle(cornerRadius: 16)
                    .stroke(.white.opacity(isHovered ? 0.4 : 0.1), lineWidth: 1)
            )
            .shadow(color: .black.opacity(0.3), radius: isHovered ? 15 : 8, y: isHovered ? 8 : 4)
            .scaleEffect(isHovered ? 1.02 : 1.0)
            .animation(.spring(response: 0.3), value: isHovered)
            .onHover { hover in
                isHovered = hover
            }
        }
        .buttonStyle(.plain)
    }
}
