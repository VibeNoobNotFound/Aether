import SwiftUI

struct HomeView: View {
    @EnvironmentObject var appState: AppState
    @State private var news: [NewsItem] = []
    @State private var carouselIndex = 0
    @State private var showCollectionEditor = false
    @State private var showCarouselEditor = false

    // Pick game for background based on Carousel index
    var backgroundGame: GameViewModel? {
        let games = appState.carouselGames
        if games.indices.contains(carouselIndex) {
            return games[carouselIndex]
        }
        return games.randomElement()
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
                    .blur(radius: 100)
                    .opacity(0.5)
                    .ignoresSafeArea()
                    .id(game.id)  // Force transition when game changes
                    .transition(.opacity.animation(.easeInOut(duration: 1.0)))
                }
            }

            // Content
            ScrollView {
                VStack(alignment: .leading, spacing: 30) {
                    // Wide Layout (Side by Side)
                    HStack(alignment: .top, spacing: 12) {
                        HeroCarousel(currentIndex: $carouselIndex, games: appState.carouselGames)
                            .frame(height: 380)
                            .clipped()

                        if !news.isEmpty {
                            NewsFeedView(news: news, orientation: .vertical, height: 380)
                                .frame(width: 350)
                                .fixedSize(horizontal: true, vertical: false)
                                .transition(.opacity)
                                .zIndex(1)
                        }
                    }
                    .padding(.top, 20)

                    // Dynamic Collections
                    ForEach(appState.visibleCollections) { collection in
                        CollectionRowView(collection: collection)
                            .transition(.opacity)
                    }
                }
                .padding(.vertical)
                .padding(16)
            }
        }
        .toolbar {
            ToolbarItem {
                Menu {
                    Button(action: {
                        showCarouselEditor = true
                    }) {
                        Label("Edit Carousel", systemImage: "photo.on.rectangle")
                    }

                    Button(action: {
                        showCollectionEditor = true
                    }) {
                        Label("Edit Collections", systemImage: "square.grid.3x3")
                    }

                    Divider()

                    Button(action: {
                        Task {
                            await appState.scanLibrary()
                        }
                    }) {
                        Label("Scan Library", systemImage: "arrow.clockwise")
                    }
                } label: {
                    Label("Options", systemImage: "ellipsis.circle")
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
        .sheet(isPresented: $showCollectionEditor) {
            CollectionEditorSheet()
        }
        .sheet(isPresented: $showCarouselEditor) {
            CarouselEditorSheet()
        }
    }
}
