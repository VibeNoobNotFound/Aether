import Charts
import SwiftUI

struct InsightsView: View {
    @EnvironmentObject var appState: AppState
    @Environment(\.dismiss) var dismiss
    @State private var selectedTab = 0
    @State private var stats: LibraryStats? = nil

    var body: some View {
        ZStack {
            if let stats = stats {
                content(stats: stats)
            } else {
                ProgressView()
                    .onAppear {
                        // Calculate stats off the main thread to prevent UI locking, then animate in
                        Task {
                            let calculated = calculateStats(games: appState.games)
                            await MainActor.run {
                                withAnimation {
                                    self.stats = calculated
                                }
                            }
                        }
                    }
            }
        }
        .frame(width: 800, height: 600)
    }

    func content(stats: LibraryStats) -> some View {
        ZStack {
            // Dark Atmospheric Background
            ZStack {
                Color.black.ignoresSafeArea()

                // Dynamic background gradient based on page
                LinearGradient(
                    colors: [
                        pageColor(for: selectedTab).opacity(0.3),
                        .black,
                    ],
                    startPoint: .topLeading,
                    endPoint: .bottomTrailing
                )
                .ignoresSafeArea()
                .animation(.easeInOut(duration: 0.5), value: selectedTab)

                // Animated Bubbles
                StatsBackgroundEffect(color: pageColor(for: selectedTab))
            }

            VStack {
                // Toolbar
                HStack {
                    Button {
                        dismiss()
                    } label: {
                        Image(systemName: "xmark.circle.fill")
                            .font(.title)
                            .foregroundStyle(.white.opacity(0.6))
                    }
                    .buttonStyle(.plain)

                    Spacer()

                    // Page Indicator
                    HStack(spacing: 6) {
                        ForEach(0..<5) { index in
                            Capsule()
                                .fill(selectedTab == index ? Color.white : Color.white.opacity(0.3))
                                .frame(width: selectedTab == index ? 20 : 6, height: 6)
                                .animation(.spring(), value: selectedTab)
                        }
                    }
                }
                .padding()

                // Slides
                TabView(selection: $selectedTab) {
                    IntroSlide(stats: stats)
                        .tag(0)

                    TimeSlide(stats: stats)
                        .tag(1)

                    TopGamesSlide(stats: stats)
                        .tag(2)

                    GenreSlide(stats: stats)
                        .tag(3)

                    SummarySlide(stats: stats)
                        .tag(4)
                }
                // .tabViewStyle(.page(indexDisplayMode: .never)) // iOS only, using default logic for macOS which works well with gestures or buttons managed above
                .tabViewStyle(.automatic)
            }
        }
        .frame(width: 800, height: 600)
    }

    func pageColor(for index: Int) -> Color {
        switch index {
        case 0: return .blue
        case 1: return .orange
        case 2: return .purple
        case 3: return .green
        case 4: return .pink
        default: return .blue
        }
    }
}

// MARK: - Logic

struct LibraryStats {
    let totalGames: Int
    let totalHours: Int
    let totalSessions: Int
    let topGames: [GameViewModel]
    let topGenres: [(String, Int)]
    let activeDayCount: Int  // Just a mock for "days active"
}

func calculateStats(games: [GameViewModel]) -> LibraryStats {
    let totalHours = Int(games.reduce(0) { $0 + $1.totalPlaytime }) / 3600
    let totalSessions = games.reduce(0) { $0 + $1.playCount }

    let sortedByTime = games.sorted { $0.totalPlaytime > $1.totalPlaytime }
    let topGames = Array(sortedByTime.prefix(3))

    var genreCounts: [String: Int] = [:]
    for game in games {
        for genre in game.genres {
            genreCounts[genre, default: 0] += 1
        }
    }
    let topGenres = genreCounts.sorted { $0.value > $1.value }.prefix(5).map { ($0.key, $0.value) }

    return LibraryStats(
        totalGames: games.count,
        totalHours: totalHours,
        totalSessions: totalSessions,
        topGames: topGames,
        topGenres: topGenres,
        activeDayCount: max(1, totalSessions / 2)  // Mock logic
    )
}

// MARK: - Components

struct StatsBackgroundEffect: View {
    let color: Color
    @State private var animate = false

    var body: some View {
        ZStack {
            ForEach(0..<3) { i in
                Circle()
                    .fill(color.opacity(0.1))
                    .frame(width: CGFloat.random(in: 200...400))
                    .offset(
                        x: animate ? CGFloat.random(in: -300...300) : 0,
                        y: animate ? CGFloat.random(in: -200...200) : 0
                    )
                    .blur(radius: 60)
                    .animation(
                        .easeInOut(duration: Double.random(in: 10...20))
                            .repeatForever(autoreverses: true)
                            .delay(Double(i) * 2),
                        value: animate
                    )
            }
        }
        .onAppear { animate = true }
    }
}

// MARK: - Slide 1: Intro

struct IntroSlide: View {
    let stats: LibraryStats
    @State private var show = false

    var body: some View {
        VStack(spacing: 20) {
            Image(systemName: "sparkles")
                .font(.system(size: 80))
                .foregroundStyle(.blue)
                .shadow(color: .blue, radius: 20)
                .scaleEffect(show ? 1 : 0.5)
                .opacity(show ? 1 : 0)

            Text("Your Library Insights")
                .font(.system(size: 40, weight: .bold, design: .rounded))
                .foregroundStyle(.white)
                .offset(y: show ? 0 : 20)
                .opacity(show ? 1 : 0)

            Text("You've built quite a collection.")
                .font(.title2)
                .foregroundStyle(.white.opacity(0.7))
                .offset(y: show ? 0 : 20)
                .opacity(show ? 1 : 0)

            // Big Number
            VStack {
                Text("\(stats.totalGames)")
                    .font(.system(size: 120, weight: .heavy, design: .rounded))
                    .foregroundStyle(
                        LinearGradient(
                            colors: [.blue, .purple], startPoint: .top, endPoint: .bottom)
                    )
                    .shadow(color: .blue.opacity(0.5), radius: 30)

                Text("GAMES COLLECTED")
                    .font(.headline)
                    .fontWeight(.bold)
                    .foregroundStyle(.white.opacity(0.5))
                    .tracking(2)
            }
            .scaleEffect(show ? 1 : 0.8)
            .opacity(show ? 1 : 0)
            .padding(.top, 40)
        }
        .onAppear {
            withAnimation(.spring(duration: 1.0)) { show = true }
        }
    }
}

// MARK: - Slide 2: Time

struct TimeSlide: View {
    let stats: LibraryStats
    @State private var progress: CGFloat = 0.0

    var body: some View {
        HStack(spacing: 60) {
            // Circular Progress
            ZStack {
                Circle()
                    .stroke(Color.white.opacity(0.1), lineWidth: 30)

                Circle()
                    .trim(from: 0, to: progress)
                    .stroke(
                        AngularGradient(colors: [.orange, .pink, .orange], center: .center),
                        style: StrokeStyle(lineWidth: 30, lineCap: .round)
                    )
                    .rotationEffect(.degrees(-90))
                    .shadow(color: .orange.opacity(0.5), radius: 20)

                VStack {
                    Text("\(stats.totalHours)")
                        .font(.system(size: 80, weight: .bold, design: .rounded))
                        .foregroundStyle(.white)
                    Text("HOURS")
                        .font(.title3)
                        .fontWeight(.bold)
                        .foregroundStyle(.white.opacity(0.6))
                }
            }
            .frame(width: 300, height: 300)

            // Side Stats
            VStack(alignment: .leading, spacing: 40) {
                StatRow(
                    icon: "play.circle.fill", value: "\(stats.totalSessions)",
                    label: "Total Sessions", color: .green)
                StatRow(
                    icon: "calendar", value: "\(stats.activeDayCount)", label: "Active Days",
                    color: .purple)
                StatRow(
                    icon: "clock.badge.checkmark",
                    value: "\(stats.totalHours / max(1, stats.totalGames))h", label: "Avg per Game",
                    color: .blue)
            }
        }
        .onAppear {
            withAnimation(.easeInOut(duration: 1.5)) {
                progress = 0.75  // Mock fill, normally calculate based on goal
            }
        }
    }
}

struct StatRow: View {
    let icon: String
    let value: String
    let label: String
    let color: Color

    var body: some View {
        HStack(spacing: 20) {
            Image(systemName: icon)
                .font(.system(size: 40))
                .foregroundStyle(color)
                .frame(width: 50)

            VStack(alignment: .leading) {
                Text(value)
                    .font(.system(size: 32, weight: .bold, design: .rounded))
                    .foregroundStyle(.white)
                Text(label)
                    .font(.headline)
                    .foregroundStyle(.white.opacity(0.5))
            }
        }
    }
}

// MARK: - Slide 3: Top Games

struct TopGamesSlide: View {
    let stats: LibraryStats
    @State private var show = false

    var body: some View {
        VStack(spacing: 40) {
            Text("Your Favorites")
                .font(.system(size: 40, weight: .bold))
                .foregroundStyle(.white)

            HStack(alignment: .bottom, spacing: 20) {
                // #2
                if stats.topGames.count > 1 {
                    PodiumColumn(
                        game: stats.topGames[1], rank: 2, height: 250, delay: 0.2, show: show)
                }

                // #1
                if let game = stats.topGames.first {
                    PodiumColumn(game: game, rank: 1, height: 320, delay: 0.0, show: show)
                }

                // #3
                if stats.topGames.count > 2 {
                    PodiumColumn(
                        game: stats.topGames[2], rank: 3, height: 200, delay: 0.4, show: show)
                }
            }
        }
        .onAppear { show = true }
    }
}

struct PodiumColumn: View {
    let game: GameViewModel
    let rank: Int
    let height: CGFloat
    let delay: Double
    let show: Bool

    var body: some View {
        VStack {
            // Game Art
            AsyncImage(url: game.coverImageURL) { text in
                text.resizable().aspectRatio(contentMode: .fill)
            } placeholder: {
                Color.gray
            }
            .frame(width: 100, height: 140)
            .clipShape(RoundedRectangle(cornerRadius: 12))
            .shadow(color: .black.opacity(0.5), radius: 10, y: 5)
            .offset(y: show ? 0 : 50)
            .opacity(show ? 1 : 0)

            // Bar
            VStack {
                Text("#\(rank)")
                    .font(.largeTitle)
                    .fontWeight(.black)
                    .foregroundStyle(.white.opacity(0.3))
                    .padding(.top, 20)

                Spacer()

                Text(game.formattedPlaytime)
                    .font(.headline)
                    .foregroundStyle(.white)
                    .padding(.bottom, 20)
            }
            .frame(width: 120, height: show ? height : 0)
            .background(
                LinearGradient(
                    colors: [
                        rank == 1 ? .yellow : (rank == 2 ? .gray : .brown), .black.opacity(0.5),
                    ],
                    startPoint: .top, endPoint: .bottom
                )
            )
            .clipShape(RoundedRectangle(cornerRadius: 12))

            Text(game.title)
                .font(.caption)
                .fontWeight(.bold)
                .foregroundStyle(.white)
                .lineLimit(1)
                .frame(width: 120)
        }
        .animation(.spring(response: 0.6, dampingFraction: 0.7).delay(delay), value: show)
    }
}

// MARK: - Slide 4: Genres

struct GenreSlide: View {
    let stats: LibraryStats
    @State private var show = false

    var body: some View {
        VStack(spacing: 30) {
            Text("What You Play")
                .font(.system(size: 40, weight: .bold))
                .foregroundStyle(.white)

            VStack(spacing: 16) {
                ForEach(Array(stats.topGenres.enumerated()), id: \.offset) { index, item in
                    HStack {
                        Text(item.0)
                            .font(.title3)
                            .fontWeight(.bold)
                            .foregroundStyle(.white)
                            .frame(width: 150, alignment: .leading)

                        GeometryReader { g in
                            RoundedRectangle(cornerRadius: 8)
                                .fill(
                                    Color(
                                        hue: Double(index) * 0.1,
                                        saturation: 0.8,
                                        brightness: 1.0
                                    )
                                )
                                .frame(
                                    width: show
                                        ? g.size.width
                                            * (CGFloat(item.1)
                                                / CGFloat(max(1, stats.topGenres.first?.1 ?? 1)))
                                        : 0)
                        }
                        .frame(height: 24)

                        Text("\(item.1)")
                            .font(.headline)
                            .foregroundStyle(.white.opacity(0.7))
                    }
                    .padding(.horizontal, 40)
                }
            }
            .padding(.top, 40)
        }
        .onAppear {
            withAnimation(.spring(duration: 1.0)) { show = true }
        }
    }
}

// MARK: - Slide 5: Summary

struct SummarySlide: View {
    let stats: LibraryStats
    @State private var show = false

    var body: some View {
        VStack(spacing: 40) {
            Text("In Summary")
                .font(.system(size: 40, weight: .bold))
                .foregroundStyle(.white)

            VStack(spacing: 30) {
                HStack(spacing: 40) {
                    SummaryCard(title: "Playtime", value: "\(stats.totalHours)h", color: .orange)
                    SummaryCard(title: "Games", value: "\(stats.totalGames)", color: .blue)
                }
                HStack(spacing: 40) {
                    SummaryCard(
                        title: "Top Genre", value: stats.topGenres.first?.0 ?? "N/A", color: .purple
                    )
                    SummaryCard(title: "Sessions", value: "\(stats.totalSessions)", color: .green)
                }
            }
            .scaleEffect(show ? 1 : 0.9)
            .opacity(show ? 1 : 0)

            Button {
                // Simple placeholder for sharing
            } label: {
                HStack {
                    Image(systemName: "square.and.arrow.up")
                    Text("Share Insights")
                }
                .padding()
                .padding(.horizontal, 20)
                .background(.white.opacity(0.2))
                .clipShape(Capsule())
            }
            .buttonStyle(.plain)
            .padding(.top, 40)
        }
        .onAppear {
            withAnimation(.spring(duration: 0.8)) { show = true }
        }
    }
}

struct SummaryCard: View {
    let title: String
    let value: String
    let color: Color

    var body: some View {
        VStack {
            Text(value)
                .font(.system(size: 36, weight: .black, design: .rounded))
                .foregroundStyle(color)
            Text(title.uppercased())
                .font(.caption)
                .fontWeight(.bold)
                .foregroundStyle(.white.opacity(0.5))
        }
        .frame(width: 160, height: 120)
        .background(.ultraThinMaterial)
        .clipShape(RoundedRectangle(cornerRadius: 20))
        .overlay(
            RoundedRectangle(cornerRadius: 20).stroke(.white.opacity(0.1), lineWidth: 1)
        )
    }
}
