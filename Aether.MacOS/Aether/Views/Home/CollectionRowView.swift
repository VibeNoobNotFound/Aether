import SwiftUI

struct CollectionRowView: View {
    let collection: CollectionViewModel
    @EnvironmentObject var appState: AppState

    var body: some View {
        let games = appState.getGames(for: collection)

        if !games.isEmpty {
            VStack(alignment: .leading, spacing: 12) {
                // Header
                HStack(alignment: .center, spacing: 12) {
                    Image(systemName: collection.iconName)
                        .font(.title2)
                        .foregroundStyle(.white)
                        .frame(width: 32, height: 32)
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

                    Spacer()
                }
                .padding(.horizontal)

                // Horizontal Scroll
                ScrollView(.horizontal, showsIndicators: false) {
                    LazyHStack(alignment: .top, spacing: 20) {
                        ForEach(games) { game in
                            NavigationLink(destination: GameDetailView(game: game)) {
                                GameGridCard(game: game)
                            }
                            .buttonStyle(.plain)
                        }
                    }
                    .padding(.horizontal)
                    .padding(.bottom, 20)
                    .padding(.top, 10)
                }
                .frame(minHeight: 350)
            }
            .padding(.top, 10)
        }
    }
}

#Preview {
    CollectionRowView(collection: MockData.collections[0])
        .environmentObject(MockData.appState)
        .frame(width: 800, height: 400)
}
