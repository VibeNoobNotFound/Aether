import SwiftUI

struct HomeView: View {
    @EnvironmentObject var appState: AppState
    @State private var news: [NewsItem] = []
    @State private var carouselIndex = 0

    // Pick game for background based on Carousel index
    var backgroundGame: GameViewModel? {
        let games = Array(appState.games.prefix(5))  // Logic must match HeroCarousel
        if games.indices.contains(carouselIndex) {
            return games[carouselIndex]
        }
        return appState.games.randomElement()
    }

    var body: some View {
        ZStack {
            // Liquid Background
            Color.black.ignoresSafeArea()

            if let game = backgroundGame, let url = game.backgroundImageURL ?? game.coverImageURL {
                GeometryReader { proxy in
                    CachedAsyncImage(url: url) { image in
                        image.resizable().aspectRatio(contentMode: .fill)
                    } placeholder: {
                        // Smooth transition placeholder
                        Color.black
                    }
                    .frame(width: proxy.size.width, height: proxy.size.height)
                    .blur(radius: 60)
                    .opacity(0.4)
                    .ignoresSafeArea()
                    .id(game.id)  // Force transition when game changes
                    .transition(.opacity.animation(.easeInOut(duration: 1.0)))
                }
            }

            // Content
            ScrollView {
                VStack(alignment: .leading, spacing: 30) {
                    HeroCarousel(currentIndex: $carouselIndex)
                        .padding(.top, 20)

                    if !news.isEmpty {
                        NewsFeedView(news: news)
                            .transition(.opacity.combined(with: .move(edge: .trailing)))
                    }

                    if !appState.games.filter({ $0.isFavorite }).isEmpty {
                        VStack(alignment: .leading, spacing: 10) {
                            Text("Favorites")
                                .font(.title2)
                                .fontWeight(.bold)
                                .padding(.horizontal)
                                .shadow(radius: 5)

                            favoritesList
                        }
                    }

                    VStack(alignment: .leading, spacing: 10) {
                        Text("All Games")
                            .font(.title2)
                            .fontWeight(.bold)
                            .padding(.horizontal)
                            .shadow(radius: 5)

                        recentGamesList
                    }
                }
                .padding(.vertical)
            }
        }
        .toolbar {
            ToolbarItem {
                Button(action: {
                    Task {
                        await appState.scanLibrary()
                    }
                }) {
                    Label("Scan Library", systemImage: "arrow.clockwise")
                }
            }
        }
        .task {
            // Fetch aggregated news on load - deduplicate by id
            let allNews = await appState.fetchGeneralNews()
            var seen = Set<String>()
            self.news = allNews.filter { item in
                if seen.contains(item.id) {
                    return false
                }
                seen.insert(item.id)
                return true
            }
        }
    }

    var favoritesList: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(alignment: .top, spacing: 20) {
                ForEach(appState.games.filter { $0.isFavorite }) { game in
                    NavigationLink(value: game) {
                        GameGridCard(game: game)
                            .frame(width: 160)
                            .contextMenu {
                                Button {
                                    appState.launchGame(game)
                                } label: {
                                    Label("Play", systemImage: "play.fill")
                                }

                                Button {
                                    Task { await appState.toggleFavorite(game: game) }
                                } label: {
                                    Label("Unfavorite", systemImage: "heart.slash")
                                }

                                Button {
                                    Task { await appState.openGameLocation(game: game) }
                                } label: {
                                    Label("Show in Finder", systemImage: "folder")
                                }
                            }
                    }
                    .buttonStyle(.plain)
                }
            }
            .padding(.horizontal)
        }
    }

    var recentGamesList: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(alignment: .top, spacing: 20) {
                ForEach(appState.games) { game in
                    NavigationLink(value: game) {
                        GameGridCard(game: game)
                            .frame(width: 160)
                            .contextMenu {
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
                    .buttonStyle(.plain)
                }
            }
            .padding(.horizontal)
        }
    }
}
