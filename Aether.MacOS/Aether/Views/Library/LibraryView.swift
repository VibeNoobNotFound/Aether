import SwiftUI

struct LibraryView: View {
    @EnvironmentObject var appState: AppState
    @State private var selectedGame: GameViewModel?

    let columns = [
        GridItem(.adaptive(minimum: 180, maximum: 220), spacing: 20)
    ]

    var body: some View {
        ScrollView {
            LazyVGrid(columns: columns, spacing: 20) {
                ForEach(appState.games) { game in
                    GameGridCard(game: game)
                        .onTapGesture {
                            selectedGame = game
                        }
                }
            }
            .padding()
        }
        .background(Color.clear)
        .sheet(item: $selectedGame) { game in
            GameDetailView(game: game)
        }
    }
}

struct GameCard: View {
    let game: GameViewModel

    var body: some View {
        VStack(alignment: .leading) {
            // Placeholder Box Art
            Rectangle()
                .fill(Color.gray.opacity(0.3))
                .aspectRatio(2 / 3, contentMode: .fit)
                .cornerRadius(12)
                .overlay(
                    Image(systemName: "gamecontroller")
                        .font(.largeTitle)
                        .foregroundStyle(.white.opacity(0.5))
                )

            Text(game.title)
                .font(.headline)
                .lineLimit(1)

            Text(game.platform)
                .font(.caption)
                .foregroundStyle(.secondary)
        }
        .padding(10)
        .background(Material.regular)  // Fallback or use glassEffect if compiler supported
        .clipShape(RoundedRectangle(cornerRadius: 16))
        .onTapGesture {
            // Open Detail
        }
        // Attempting to use the new "Liquid Glass" style if available
        // .glassEffect()
    }
}
