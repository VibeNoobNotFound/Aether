import SwiftUI

struct HomeView: View {
    @EnvironmentObject var appState: AppState

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 20) {
                HeroCarousel()

                Text("Jump Back In")
                    .font(.title2)
                    .fontWeight(.bold)
                    .padding(.horizontal)

                ScrollView(.horizontal, showsIndicators: false) {
                    HStack(spacing: 15) {
                        ForEach(appState.games) { game in
                            Button(action: {
                                appState.launch(gameId: game.id)
                            }) {
                                RoundedRectangle(cornerRadius: 12)
                                    .fill(Color.white.opacity(0.1))
                                    .frame(width: 200, height: 120)
                                    .overlay(
                                        VStack {
                                            Text(game.title)
                                                .font(.headline)
                                                .foregroundStyle(.white)
                                            Text("Play")
                                                .font(.caption)
                                                .foregroundStyle(.secondary)
                                        }
                                    )
                            }
                            .buttonStyle(PlainButtonStyle())
                        }
                    }
                    .padding(.horizontal)
                }
            }
            .padding(.vertical)
        }
    }
}
