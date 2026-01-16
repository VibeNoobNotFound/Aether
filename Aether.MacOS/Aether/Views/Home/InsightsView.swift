import AppKit
import AetherIPC
import Charts
import SwiftUI

struct InsightsView: View {
    @EnvironmentObject var appState: AppState
    @Environment(\.dismiss) var dismiss
    @State private var selectedTab = 0
    @State private var stats: LibraryStats? = nil

    // Animation States
    @State private var direction: MoveTransitionDirection = .forward

    enum MoveTransitionDirection {
        case forward, backward
    }

    var body: some View {
        ZStack {
            // Background Layer
            AuroraBackground()
                .opacity(0.6)

            if let stats = stats {
                mainContent(stats: stats)
            } else {
                ProgressView()
                    .scaleEffect(1.5)
                    .tint(.white)
                    .onAppear {
                        loadStats()
                    }
            }
        }
        .background(Color.black)
        .frame(width: 950, height: 700)
    }

    func loadStats() {
        Task {
            // Wait for backend
            try? await Task.sleep(nanoseconds: 300_000_000)

            // 1. Fetch Aggregates from Backend (RPC)
            let serverStats = await appState.getLibraryStats()

            // 2. Local fallback / enrichment for "Top Games" (since they are in memory)
            let calculated = calculateStats(games: appState.games, serverStats: serverStats)

            await MainActor.run {
                withAnimation {
                    self.stats = calculated
                }
            }
        }
    }

    func mainContent(stats: LibraryStats) -> some View {
        VStack(spacing: 0) {
            // Header
            HStack {
                Button {
                    dismiss()
                } label: {
                    Image(systemName: "xmark.circle.fill")
                        .font(.system(size: 24))
                        .foregroundStyle(.white.opacity(0.6))
                        .contentShape(Circle())
                }
                .buttonStyle(.plain)

                Spacer()

                HStack(spacing: 8) {
                    ForEach(0..<6) { index in
                        Capsule()
                            .fill(selectedTab == index ? Color.white : Color.white.opacity(0.3))
                            .frame(width: selectedTab == index ? 24 : 8, height: 8)
                            .animation(.spring(response: 0.3), value: selectedTab)
                    }
                }
            }
            .padding(24)

            // Content Area
            ZStack {
                switch selectedTab {
                case 0:
                    IntroSlide(stats: stats).transition(
                        .asymmetric(
                            insertion: .move(edge: direction == .forward ? .trailing : .leading),
                            removal: .move(edge: direction == .forward ? .leading : .trailing)))
                case 1:
                    TimeSlide(stats: stats).transition(
                        .asymmetric(
                            insertion: .move(edge: direction == .forward ? .trailing : .leading),
                            removal: .move(edge: direction == .forward ? .leading : .trailing)))
                case 2:
                    TopGamesSlide(stats: stats).transition(
                        .asymmetric(
                            insertion: .move(edge: direction == .forward ? .trailing : .leading),
                            removal: .move(edge: direction == .forward ? .leading : .trailing)))
                case 3:
                    AllGamesSlide(stats: stats).transition(
                        .asymmetric(
                            insertion: .move(edge: direction == .forward ? .trailing : .leading),
                            removal: .move(edge: direction == .forward ? .leading : .trailing)))
                case 4:
                    GenreSlide(stats: stats).transition(
                        .asymmetric(
                            insertion: .move(edge: direction == .forward ? .trailing : .leading),
                            removal: .move(edge: direction == .forward ? .leading : .trailing)))
                case 5:
                    SummarySlide(stats: stats).transition(
                        .asymmetric(
                            insertion: .move(edge: direction == .forward ? .trailing : .leading),
                            removal: .move(edge: direction == .forward ? .leading : .trailing)))
                default: EmptyView()
                }
            }
            .frame(maxWidth: .infinity, maxHeight: .infinity)
            .id(selectedTab)

            // Bottom Bar
            HStack {
                Button {
                    if selectedTab > 0 {
                        direction = .backward
                        withAnimation(.spring(response: 0.5, dampingFraction: 0.8)) {
                            selectedTab -= 1
                        }
                    }
                } label: {
                    HStack {
                        Image(systemName: "arrow.left")
                        Text("Back")
                    }
                    .padding(.horizontal, 20)
                    .padding(.vertical, 10)
                    .background(.ultraThinMaterial)
                    .clipShape(Capsule())
                    .overlay(Capsule().stroke(.white.opacity(0.2), lineWidth: 1))
                }
                .buttonStyle(.plain)
                .opacity(selectedTab > 0 ? 1 : 0)
                .disabled(selectedTab == 0)

                Spacer()

                Button {
                    if selectedTab < 5 {
                        direction = .forward
                        withAnimation(.spring(response: 0.5, dampingFraction: 0.8)) {
                            selectedTab += 1
                        }
                    } else {
                        dismiss()
                    }
                } label: {
                    HStack {
                        Text(selectedTab < 5 ? "Next" : "Done")
                        if selectedTab < 5 {
                            Image(systemName: "arrow.right")
                        } else {
                            Image(systemName: "checkmark")
                        }
                    }
                    .padding(.horizontal, 24)
                    .padding(.vertical, 10)
                    .background(
                        LinearGradient(
                            colors: [.blue, .purple], startPoint: .leading, endPoint: .trailing)
                    )
                    .foregroundStyle(.white)
                    .clipShape(Capsule())
                    .shadow(color: .blue.opacity(0.4), radius: 10, x: 0, y: 5)
                }
                .buttonStyle(.plain)
            }
            .padding(30)
        }
    }
}

// MARK: - Slide 1: Intro
struct IntroSlide: View {
    let stats: LibraryStats
    @State private var show = false

    var body: some View {
        VStack(spacing: 30) {
            Spacer()
            Image(systemName: "chart.bar.doc.horizontal.fill")
                .font(.system(size: 80))
                .foregroundStyle(
                    LinearGradient(
                        colors: [.cyan, .blue], startPoint: .topLeading, endPoint: .bottomTrailing)
                )
                .shadow(color: .cyan.opacity(0.5), radius: 20)
                .scaleEffect(show ? 1 : 0.5)
                .opacity(show ? 1 : 0)
                .rotationEffect(.degrees(show ? 0 : -20))

            VStack(spacing: 12) {
                Text("Library Insights")
                    .font(.system(size: 48, weight: .heavy, design: .rounded))
                    .foregroundStyle(.white)
                Text("A quick look at your library stats")
                    .font(.title2)
                    .foregroundStyle(.white.opacity(0.7))
            }
            .offset(y: show ? 0 : 30)
            .opacity(show ? 1 : 0)

            Spacer()

            VStack(spacing: 8) {
                Text("\(stats.totalGames)")
                    .font(.system(size: 90, weight: .black, design: .rounded))
                    .foregroundStyle(.white)
                    .shadow(color: .white.opacity(0.3), radius: 10)
                Text("GAMES COLLECTED")
                    .font(.headline).tracking(4).foregroundStyle(.white.opacity(0.5))
            }
            .scaleEffect(show ? 1 : 0.8).opacity(show ? 1 : 0)

            Spacer()
        }
        .onAppear { withAnimation(.spring(response: 0.8, dampingFraction: 0.7)) { show = true } }
    }
}

// MARK: - Slide 2: Time
struct TimeSlide: View {
    let stats: LibraryStats
    @State private var show = false
    @State private var progress: CGFloat = 0.0

    var body: some View {
        HStack(spacing: 60) {
            ZStack {
                Circle().stroke(Color.white.opacity(0.1), lineWidth: 30)
                Circle().trim(from: 0, to: progress)
                    .stroke(
                        AngularGradient(colors: [.orange, .pink, .orange], center: .center),
                        style: StrokeStyle(lineWidth: 30, lineCap: .round)
                    )
                    .rotationEffect(.degrees(-90))
                    .shadow(color: .orange.opacity(0.4), radius: 15)
                VStack {
                    Text("\(stats.totalHours)")
                        .font(.system(size: 80, weight: .bold, design: .rounded))
                        .foregroundStyle(.white)
                        .contentTransition(.numericText())
                    Text("HOURS")
                        .font(.title3).fontWeight(.bold).foregroundStyle(.white.opacity(0.6))
                }
            }
            .frame(width: 300, height: 300)
            .scaleEffect(show ? 1 : 0.8).opacity(show ? 1 : 0)

            VStack(alignment: .leading, spacing: 40) {
                StatRowAnimated(
                    icon: "play.circle.fill", value: "\(stats.totalSessions)",
                    label: "Total Sessions", color: .green, delay: 0.2)
                StatRowAnimated(
                    icon: "calendar", value: "\(stats.activeDayCount)", label: "Active Days",
                    color: .purple, delay: 0.3)
                StatRowAnimated(
                    icon: "clock.badge.checkmark",
                    value: "\(stats.totalHours / max(1, stats.totalGames))h", label: "Avg per Game",
                    color: .blue, delay: 0.4)
            }
        }
        .onAppear {
            withAnimation(.spring(response: 0.8, dampingFraction: 0.7)) { show = true }
            withAnimation(.easeInOut(duration: 2.0).delay(0.2)) { progress = 0.75 }
        }
    }
}

struct StatRowAnimated: View {
    let icon: String
    let value: String
    let label: String
    let color: Color
    let delay: Double
    @State private var show = false
    var body: some View {
        HStack(spacing: 20) {
            Image(systemName: icon).font(.system(size: 40)).foregroundStyle(color).frame(width: 50)
                .symbolEffect(.bounce, value: show)
            VStack(alignment: .leading) {
                Text(value).font(.system(size: 32, weight: .bold, design: .rounded))
                    .foregroundStyle(.white)
                Text(label).font(.headline).foregroundStyle(.white.opacity(0.5))
            }
        }
        .offset(x: show ? 0 : 50).opacity(show ? 1 : 0)
        .onAppear {
            withAnimation(.spring(response: 0.6, dampingFraction: 0.7).delay(delay)) { show = true }
        }
    }
}

// MARK: - Slide 3: Top Games
struct TopGamesSlide: View {
    let stats: LibraryStats
    @State private var show = false

    var body: some View {
        VStack(spacing: 40) {
            Text("Top 3 Favorites").font(.system(size: 40, weight: .bold)).foregroundStyle(.white)
                .opacity(show ? 1 : 0).offset(y: show ? 0 : -20)
            HStack(alignment: .bottom, spacing: 20) {
                // Reduced heights for better fit: 200, 260, 160
                if stats.topGames.count > 1 {
                    PodiumColumn(
                        game: stats.topGames[1], rank: 2, height: 200, delay: 0.2, show: show)
                }
                if let game = stats.topGames.first {
                    PodiumColumn(game: game, rank: 1, height: 260, delay: 0.0, show: show)
                }
                if stats.topGames.count > 2 {
                    PodiumColumn(
                        game: stats.topGames[2], rank: 3, height: 160, delay: 0.4, show: show)
                }
            }
            .padding(.bottom, 20)
        }
        .onAppear { withAnimation(.spring()) { show = true } }
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
            AsyncImage(url: game.coverImageURL) { i in
                i.resizable().aspectRatio(contentMode: .fill)
            } placeholder: {
                Color.gray
            }
            .frame(width: 100, height: 130).clipShape(RoundedRectangle(cornerRadius: 12))
            .shadow(color: .black.opacity(0.5), radius: 10, y: 5)
            .offset(y: show ? 0 : 50).opacity(show ? 1 : 0)
            VStack {
                Text("#\(rank)").font(.largeTitle).fontWeight(.black).foregroundStyle(
                    .white.opacity(0.3)
                ).padding(.top, 10)
                Spacer()
                Text(game.formattedPlaytime).font(.headline).foregroundStyle(.white).padding(
                    .bottom, 10)
            }
            .frame(width: 120, height: show ? height : 0)
            .background(
                LinearGradient(
                    colors: [
                        rank == 1 ? .yellow : (rank == 2 ? .gray : .brown), .black.opacity(0.6),
                    ], startPoint: .top, endPoint: .bottom)
            )
            .clipShape(RoundedRectangle(cornerRadius: 12))

            Text(game.title).font(.headline).foregroundStyle(.white).lineLimit(1).frame(width: 120)
                .opacity(show ? 1 : 0)
        }
        .animation(.spring(response: 0.6, dampingFraction: 0.7).delay(delay), value: show)
    }
}

// MARK: - Slide 4: All Games
struct AllGamesSlide: View {
    let stats: LibraryStats
    @State private var show = false

    var body: some View {
        VStack(spacing: 20) {
            Text("Playtime Breakdown").font(.title).fontWeight(.bold).foregroundStyle(.white)
            ScrollView {
                VStack(spacing: 16) {
                    ForEach(Array(stats.allGamesSorted.enumerated()), id: \.1.id) { index, game in
                        HStack {
                            Text(game.title).font(.body).fontWeight(.medium).lineLimit(1).frame(
                                maxWidth: 200, alignment: .leading
                            ).foregroundStyle(.white)
                            GeometryReader { g in
                                RoundedRectangle(cornerRadius: 4).fill(
                                    LinearGradient(
                                        colors: [.blue, .purple], startPoint: .leading,
                                        endPoint: .trailing)
                                )
                                .frame(
                                    width: show
                                        ? max(
                                            0,
                                            g.size.width
                                                * (CGFloat(game.totalPlaytime)
                                                    / CGFloat(
                                                        max(
                                                            1.0,
                                                            stats.allGamesSorted.first?
                                                                .totalPlaytime ?? 1.0)))) : 0
                                )
                                .animation(
                                    .spring(response: 0.8).delay(Double(index) * 0.05), value: show)
                            }
                            .frame(height: 12)

                            Text(game.formattedPlaytime).font(.caption).monospacedDigit()
                                .foregroundStyle(.white.opacity(0.7)).frame(
                                    width: 80, alignment: .trailing)
                        }
                    }
                }
                .padding()
            }
            .background(Color.black.opacity(0.2)).clipShape(RoundedRectangle(cornerRadius: 12))
            // Removed maxWidth, fixed height
            .frame(maxHeight: 500)
        }.padding().onAppear { show = true }
    }
}

// MARK: - Slide 5: Genres
struct GenreSlide: View {
    let stats: LibraryStats
    @State private var show = false

    var body: some View {
        VStack(spacing: 30) {
            Text("Top Genres").font(.system(size: 40, weight: .bold)).foregroundStyle(.white)
            VStack(spacing: 20) {
                ForEach(Array(stats.topGenres.enumerated()), id: \.offset) { index, item in
                    HStack {
                        Text(item.0).font(.title3).fontWeight(.bold).foregroundStyle(.white).frame(
                            width: 150, alignment: .leading)
                        GeometryReader { g in
                            RoundedRectangle(cornerRadius: 8)
                                .fill(
                                    Color(
                                        hue: Double(index) * 0.15, saturation: 0.8, brightness: 1.0)
                                )
                                .frame(
                                    width: show
                                        ? g.size.width
                                            * (CGFloat(item.1)
                                                / CGFloat(max(1, stats.topGenres.first?.1 ?? 1)))
                                        : 0)
                        }.frame(height: 28)
                        Text("\(item.1)").font(.headline).foregroundStyle(.white.opacity(0.7))
                    }
                    .padding(.horizontal, 40).offset(x: show ? 0 : 100).opacity(show ? 1 : 0)
                    .animation(
                        .spring(response: 0.6, dampingFraction: 0.7).delay(Double(index) * 0.1),
                        value: show)
                }
            }
            .padding(.top, 20)
            Spacer()
        }.onAppear { show = true }
    }
}

// MARK: - Slide 6: Summary & Share
struct SummarySlide: View {
    let stats: LibraryStats
    @State private var show = false

    // COMPACT CARD (320x400)
    var shareCard: some View {
        ZStack {
            LinearGradient(
                colors: [
                    Color(hue: 0.6, saturation: 0.7, brightness: 0.5),
                    Color(hue: 0.05, saturation: 0.8, brightness: 0.6),
                ], startPoint: .topLeading, endPoint: .bottomTrailing)
            VStack(spacing: 15) {  // Compact spacing
                HStack {
                    VStack(alignment: .leading) {
                        Text("LIBRARY STATS").font(.caption).fontWeight(.black).foregroundStyle(
                            .white.opacity(0.6)
                        ).tracking(2)
                        Text("AETHER").font(.system(size: 24, weight: .heavy, design: .rounded))
                            .foregroundStyle(.white)
                    }
                    Spacer()
                    Image(systemName: "gamecontroller.fill").font(.title).foregroundStyle(.white)
                }
                Divider().background(.white.opacity(0.3))
                HStack(spacing: 15) {
                    SummaryStatBox(title: "TOTAL HOURS", value: "\(stats.totalHours)")
                    SummaryStatBox(title: "GAMES PLAYED", value: "\(stats.totalGames)")
                }
                VStack(spacing: 10) {
                    SummaryDetailRow(label: "Top Genre", value: stats.topGenres.first?.0 ?? "-")
                    SummaryDetailRow(
                        label: "Most Played", value: stats.topGames.first?.title ?? "-")
                    SummaryDetailRow(label: "Total Sessions", value: "\(stats.totalSessions)")
                }
                .padding().background(.ultraThinMaterial).clipShape(
                    RoundedRectangle(cornerRadius: 12))
            }.padding(20)
        }
        .frame(width: 320, height: 400).clipShape(RoundedRectangle(cornerRadius: 24)).overlay(
            RoundedRectangle(cornerRadius: 24).stroke(.white.opacity(0.2), lineWidth: 1)
        ).drawingGroup()
    }

    var body: some View {
        VStack(spacing: 20) {
            Text("Ready to Share?").font(.system(size: 32, weight: .bold)).foregroundStyle(.white)
                .opacity(show ? 1 : 0).offset(y: show ? 0 : -20)
            shareCard.scaleEffect(show ? 1 : 0.9).opacity(show ? 1 : 0).shadow(
                color: .black.opacity(0.5), radius: 30, y: 10)
            Button {
                saveImage()
            } label: {
                HStack {
                    Image(systemName: "square.and.arrow.up")
                    Text("Save Image")
                }.padding().padding(.horizontal, 20).background(.white.opacity(0.2)).clipShape(
                    Capsule()
                ).overlay(Capsule().stroke(.white.opacity(0.3), lineWidth: 1))
            }
            .buttonStyle(.plain).opacity(show ? 1 : 0).offset(y: show ? 0 : 20)
        }
        .onAppear { withAnimation(.spring(response: 0.8, dampingFraction: 0.7)) { show = true } }
    }

    @MainActor
    private func saveImage() {
        let renderer = ImageRenderer(content: shareCard)
        renderer.scale = 3.0  // High quality export
        if let nsImage = renderer.nsImage {
            let downloads = FileManager.default.urls(for: .downloadsDirectory, in: .userDomainMask)[
                0]
            let fileURL = downloads.appendingPathComponent("Aether-Insight.png")
            if let tiffData = nsImage.tiffRepresentation,
                let bitmap = NSBitmapImageRep(data: tiffData),
                let data = bitmap.representation(using: .png, properties: [:])
            {
                try? data.write(to: fileURL)
                NSWorkspace.shared.activateFileViewerSelecting([fileURL])
            }
        }
    }
}

struct SummaryStatBox: View {
    let title: String
    let value: String
    var body: some View {
        VStack {
            Text(value).font(.system(size: 40, weight: .bold, design: .rounded)).foregroundStyle(
                .white
            ).minimumScaleFactor(0.5)
            Text(title).font(.caption).fontWeight(.bold).foregroundStyle(.white.opacity(0.6))
        }
        .frame(maxWidth: .infinity).padding().background(.white.opacity(0.1)).clipShape(
            RoundedRectangle(cornerRadius: 12))
    }
}

struct SummaryDetailRow: View {
    let label: String
    let value: String
    var body: some View {
        HStack {
            Text(label).foregroundStyle(.white.opacity(0.7))
            Spacer()
            Text(value).fontWeight(.bold).foregroundStyle(.white)
        }
    }
}

// MARK: - Logic & Models
struct LibraryStats {
    let totalGames: Int
    let totalHours: Int
    let totalSessions: Int
    let topGames: [GameViewModel]
    let allGamesSorted: [GameViewModel]
    let topGenres: [(String, Int)]
    let activeDayCount: Int
}

func calculateStats(games: [GameViewModel], serverStats: Aether_LibraryStatsResponse?)
    -> LibraryStats
{
    let sortedByTime = games.sorted { $0.totalPlaytime > $1.totalPlaytime }
    let topGames = Array(sortedByTime.prefix(3))

    // Default / Legacy calculation
    var totalHours = Int(games.reduce(0) { $0 + $1.totalPlaytime }) / 3600
    var totalSessions = games.reduce(0) { $0 + $1.playCount }
    var activeDayCount = max(1, totalSessions / 2)  // Rough estimate fallback
    var topGenres: [(String, Int)] = []

    // Override with Server Stats if available
    if let stats = serverStats {
        totalHours = Int(stats.totalPlaytimeSeconds) / 3600
        totalSessions = Int(stats.totalSessions)
        activeDayCount = Int(stats.activeDayCount)
        topGenres = stats.topGenres.map { ($0.genre, Int($0.count)) }
    } else {
        // Local calculation fallback for genres
        var genreCounts: [String: Int] = [:]
        for game in games { for genre in game.genres { genreCounts[genre, default: 0] += 1 } }
        topGenres = genreCounts.sorted { $0.value > $1.value }.prefix(5).map { ($0.key, $0.value) }
    }

    return LibraryStats(
        totalGames: games.count,
        totalHours: totalHours,
        totalSessions: totalSessions,
        topGames: topGames,
        allGamesSorted: sortedByTime,
        topGenres: topGenres,
        activeDayCount: activeDayCount
    )
}

// MARK: - Preview
#Preview {
    InsightsView().environmentObject(MockData.appState)
}
