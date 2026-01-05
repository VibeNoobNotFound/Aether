import SwiftUI

struct CollectionRowView: View {
    let collection: CollectionViewModel
    @EnvironmentObject var appState: AppState

    var body: some View {
        let games = appState.getGames(for: collection)

        if !games.isEmpty {
            VStack(alignment: .leading, spacing: 16) {
                // Header
                HStack(alignment: .center, spacing: 10) {
                    Image(systemName: collection.iconName)
                        .font(.title2)
                        .foregroundStyle(.white)
                        .frame(width: 40, height: 40)
                        .background(
                            Circle()
                                .fill(.ultraThinMaterial)
                                .shadow(color: .black.opacity(0.2), radius: 4, x: 0, y: 2)
                        )

                    Text(collection.name)
                        .font(.title2)
                        .fontWeight(.bold)
                        .foregroundStyle(.white)
                        .shadow(color: .black.opacity(0.3), radius: 2, x: 0, y: 1)

                    if collection.gameCount > games.count {
                        // Badge showing total if filtered (e.g. system collections might have more)
                        // Actually getGames returns all, so games.count is accurate for now
                    }

                    Spacer()

                    // "See All" button? Future work
                }
                .padding(.horizontal)

                // Horizontal Scroll
                ScrollView(.horizontal, showsIndicators: false) {
                    LazyHStack(spacing: 20) {
                        ForEach(games) { game in
                            NavigationLink(destination: GameDetailView(game: game)) {
                                GameGridCard(game: game)
                                    .frame(width: 200)  // Fixed width cards for horizontal row
                            }
                            .buttonStyle(.plain)
                        }
                    }
                    .padding(.horizontal)
                    .padding(.bottom, 20)  // Space for shadows
                }
                .frame(height: 320)  // Adjust based on card aspect ratio
            }
            .padding(.top, 10)
        }
    }
}
