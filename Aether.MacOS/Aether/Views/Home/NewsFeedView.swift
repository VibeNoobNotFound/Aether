import AetherIPC
import SwiftProtobuf
import SwiftUI

struct NewsFeedView: View {
    let news: [NewsItem]
    var orientation: Axis = .horizontal
    var height: CGFloat? = 200
    @State private var scrollID: Int? = 0

    @State private var currentIndex = 0

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            // Section Header
            HStack {
                if orientation == .horizontal && news.count > 1 {
                    HStack(spacing: 8) {
                        ForEach(0..<min(news.count, 5), id: \.self) { index in
                            Circle()
                                .fill(
                                    currentIndex == index ? Color.white : Color.white.opacity(0.4)
                                )
                                .frame(width: 6, height: 6)
                                .onTapGesture {
                                    withAnimation { scrollID = index }
                                }
                        }
                    }
                }
            }
            .padding(.horizontal)

            if orientation == .horizontal {
                // Horizontal Multi-Card Scrollable Grid
                ScrollView(.horizontal, showsIndicators: false) {
                    LazyHStack(spacing: 16) {
                        ForEach(news) { item in
                            NewsCarouselCard(item: item, cardWidth: 320)
                                .frame(width: 320, height: height ?? 200)
                        }
                    }
                    .padding(.horizontal, 16)
                }
                .frame(height: height ?? 200)
            } else {
                // Vertical List Fallback
                ScrollView(.vertical, showsIndicators: false) {
                    VStack(spacing: 16) {
                        ForEach(news) { item in
                            NewsCarouselCard(item: item, cardWidth: .infinity)
                                .frame(height: 180)
                        }
                    }
                    .padding(.horizontal)
                }
                .frame(height: height)
            }
        }
    }

    // Carousel-style news card
    struct NewsCarouselCard: View {
        let item: NewsItem
        let cardWidth: CGFloat

        @Environment(\.openURL) var openURL
        @State private var isHovered = false

        var body: some View {
            Button {
                if let url = item.url ?? URL(string: "https://store.steampowered.com") {
                    openURL(url)
                }
            } label: {
                VStack() {
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
                                .background {
                                    GlassCard(padding: 0, cornerRadius: 100) {
                                        Color.clear
                                    }
                                }

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
                        }.zIndex(1)
                            .frame(
                                maxWidth: cardWidth == .infinity
                                    ? .infinity : max(0, cardWidth - 32),
                                alignment: .leading
                            )
                            .padding(16)
                    }
                    .frame(
                        width: cardWidth == .infinity ? nil : max(0, cardWidth),
                        height: nil
                    )
                    .clipShape(RoundedRectangle(cornerRadius: 16))
                }
            }
            .buttonStyle(.plain)
        }
    }
}
#Preview {
    
#if DEBUG
    ZStack {
        Color.black.ignoresSafeArea()
        NewsFeedView(
            news: [
                NewsItem(
                    from: Aether_NewsItem.with {
                        $0.title = "Steam Deck OLED Announced"
                        $0.author = "Valve"
                        $0.source = "Steam"
                        $0.dateUnix = Int64(Date().timeIntervalSince1970)
                    }),
                NewsItem(
                    from: Aether_NewsItem.with {
                        $0.title = "Epic Mega Sale"
                        $0.author = "Epic Games"
                        $0.source = "Epic"
                        $0.dateUnix = Int64(Date().timeIntervalSince1970)
                    }),
            ]
        )
        .padding()
    }
    #endif
}
