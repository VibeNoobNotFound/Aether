import SwiftUI

struct GameGridCard: View {
    let game: GameViewModel

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            // Cover art
            AsyncImage(url: game.coverImageURL) { image in
                image
                    .resizable()
                    .aspectRatio(2 / 3, contentMode: .fill)
            } placeholder: {
                Rectangle()
                    .fill(.gray.opacity(0.3))
                    .overlay {
                        Image(systemName: "gamecontroller")
                            .font(.largeTitle)
                            .foregroundStyle(.secondary)
                    }
            }
            .frame(height: 240)
            .clipShape(RoundedRectangle(cornerRadius: 12))

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
            }
        }
        .contentShape(Rectangle())  // Make entire area clickable
    }

    func platformColor(_ platform: String) -> Color {
        switch platform.lowercased() {
        case "steam": return .blue
        case "epic games", "epic": return .purple
        case "app store": return .cyan
        default: return .gray
        }
    }
}
