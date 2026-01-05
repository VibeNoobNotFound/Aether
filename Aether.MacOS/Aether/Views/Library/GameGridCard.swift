import SwiftUI

struct GameGridCard: View {
    let game: GameViewModel
    @State private var isHovered = false
    @State private var hasAppeared = false

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            // Cover art with proper 2:3 aspect ratio for game covers
            RoundedRectangle(cornerRadius: 16)
                .fill(Color.gray.opacity(0.3))
                .aspectRatio(2 / 3, contentMode: .fit)
                .overlay {
                    if let coverURL = game.coverImageURL {
                        CachedAsyncImage(url: coverURL) { image in
                            image
                                .resizable()
                                .aspectRatio(contentMode: .fill)
                        } placeholder: {
                            Image(systemName: "photo")
                                .foregroundStyle(.white.opacity(0.5))
                        }
                    } else {
                        Image(systemName: "gamecontroller")
                            .font(.largeTitle)
                            .foregroundStyle(.secondary)
                    }
                }
                .clipShape(RoundedRectangle(cornerRadius: 16))
                .frame(
                    maxWidth: .infinity, alignment: .init(horizontal: .leading, vertical: .bottom))

            // Title
            Text(game.title)
                .font(.headline)
                .lineLimit(2)

            // Platform badge
            Text(game.platform)
                .font(.caption)
                .padding(.horizontal, 8)
                .padding(.vertical, 4)
                .background(platformColor(game.platform).opacity(0.2))
                .foregroundStyle(platformColor(game.platform))
                .clipShape(Capsule())

            // Genres
            if !game.genres.isEmpty {
                Text(game.genres.prefix(2).joined(separator: ", "))
                    .font(.caption)
                    .foregroundStyle(.secondary)
                    .lineLimit(1)
            } else {
                Spacer()
            }
        }
        .contentShape(Rectangle())  // Make entire area clickable
        .scaleEffect(isHovered ? 1.03 : 1.0)
        .shadow(
            color: .black.opacity(isHovered ? 0.3 : 0.1), radius: isHovered ? 12 : 4,
            y: isHovered ? 6 : 2
        )
        .animation(.spring(response: 0.3, dampingFraction: 0.7), value: isHovered)
        .onHover { hover in
            isHovered = hover
        }
        .opacity(hasAppeared ? 1 : 0)
        .offset(y: hasAppeared ? 0 : 20)
        .onAppear {
            withAnimation(.easeOut(duration: 0.4).delay(Double.random(in: 0...0.2))) {
                hasAppeared = true
            }
        }
    }

    func platformColor(_ platform: String) -> Color {
        switch platform.lowercased() {
        case "steam": return .blue
        case "epic games", "epic": return .purple
        case "app store": return .cyan
        case "crossover": return .yellow
        case "gog": return .red
        default: return .gray
        }
    }

    @ViewBuilder
    private func placeholderView(icon: String) -> some View {
        Rectangle()
            .fill(.gray.opacity(0.3))
            .overlay {
                Image(systemName: icon)
                    .font(.largeTitle)
                    .foregroundStyle(.secondary)
            }
    }
}

#Preview {
    ZStack {
        Color.black
        GameGridCard(game: MockData.games[0])
            .frame(width: 200, height: 300)
    }
}

#Preview {
    ZStack {
        Color.black
        GameGridCard(game: MockData.games[0])
            .frame(width: 200, height: 300)
    }
}
