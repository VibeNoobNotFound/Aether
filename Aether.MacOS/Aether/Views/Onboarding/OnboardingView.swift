import SwiftUI
import Combine

// MARK: - Aurora Background

struct AuroraBackground: View {
    var body: some View {
        ZStack {
            Color.black.ignoresSafeArea()

            GeometryReader { proxy in
                // Blob 1: Blue-ish top left - HUGE and SPREAD
                Circle()
                    .fill(Color(nsColor: .systemBlue).opacity(0.3))
                    .frame(width: proxy.size.width * 1.5, height: proxy.size.width * 1.5)
                    .blur(radius: 200)
                    .position(x: 0, y: 0)

                // Blob 2: Purple-ish bottom right - HUGE and SPREAD
                Circle()
                    .fill(Color(nsColor: .systemPurple).opacity(0.3))
                    .frame(width: proxy.size.width * 1.2, height: proxy.size.width * 1.2)
                    .blur(radius: 180)
                    .position(x: proxy.size.width, y: proxy.size.height)

                // Blob 3: Teal center - HUGE and SPREAD
                Circle()
                    .fill(Color(nsColor: .systemTeal).opacity(0.2))
                    .frame(width: proxy.size.width, height: proxy.size.width)
                    .blur(radius: 150)
                    .position(x: proxy.size.width * 0.5, y: proxy.size.height * 0.5)
            }
        }
        .ignoresSafeArea()
    }
}

// MARK: - Onboarding View

struct OnboardingView: View {
    @Environment(\.dismiss) private var dismiss
    @EnvironmentObject var appState: AppState
    @AppStorage("hasCompletedOnboarding") var hasCompletedOnboarding: Bool = false
    @State private var selectedPage = 0
    @Namespace private var animation  // For matched geometry effects if needed

    var body: some View {
        ZStack {
            // Background Layer
            AuroraBackground()

            // Content Switcher
            Group {
                switch selectedPage {
                case 0:
                    WelcomePage()
                        .padding(16)
                        .transition(
                            .asymmetric(
                                insertion: .move(edge: .trailing), removal: .move(edge: .leading)))
                case 1:
                    UnifiedLibraryPage()
                        .padding(16)
                        .transition(
                            .asymmetric(
                                insertion: .move(edge: .trailing), removal: .move(edge: .leading)))
                case 2:
                    PluginsPage(plugins: appState.plugins)
                        .padding(16)
                        .transition(
                            .asymmetric(
                                insertion: .move(edge: .trailing), removal: .move(edge: .leading)))
                case 3:
                    StatsFeaturePage()
                        .padding(16)
                        .transition(
                            .asymmetric(
                                insertion: .move(edge: .trailing), removal: .move(edge: .leading)))
                case 4:
                    TutorialPage(
                        title: "Precision Control",
                        description:
                            "Fix missing covers, change titles, or add custom tags instantly.",
                        icon: "pencil.circle.fill",
                        color: .purple,
                        instruction:
                            "Click the Edit (Pencil) icon in the toolbar to customize any game's metadata."
                    ) {
                        // Tutorial Image
                        Image("Tutorial-Metadata")
                            .resizable()
                            .aspectRatio(contentMode: .fit)
                            .frame(maxWidth: 800, maxHeight: 500)
                            .clipShape(RoundedRectangle(cornerRadius: 16))
                            .shadow(color: .black.opacity(0.3), radius: 15, x: 0, y: 8)
                    }
                    .padding(16)
                    .transition(
                        .asymmetric(
                            insertion: .move(edge: .trailing), removal: .move(edge: .leading)))
                case 5:
                    TutorialPage(
                        title: "Organize Your Way",
                        description:
                            "Create custom collections to group games by genre, mood, or status.",
                        icon: "square.grid.3x3.fill",
                        color: .blue,
                        instruction:
                            "Use the Collections menu to create, edit, and reorder your library shelves."
                    ) {
                        // Tutorial Image
                        Image("Tutorial-Collections")
                            .resizable()
                            .aspectRatio(contentMode: .fit)
                            .frame(maxWidth: 800, maxHeight: 500)
                            .clipShape(RoundedRectangle(cornerRadius: 16))
                            .shadow(color: .black.opacity(0.3), radius: 15, x: 0, y: 8)
                    }
                    .padding(16)
                    .transition(
                        .asymmetric(
                            insertion: .move(edge: .trailing), removal: .move(edge: .leading)))
                case 6:
                    // Interactive Settings Page
                    SettingsOnboardingPage()
                        .padding(16)
                        .transition(
                            .asymmetric(
                                insertion: .move(edge: .trailing), removal: .move(edge: .leading)))
                case 7:
                    GetStartedPage {
                        completeOnboarding()
                    }
                    .padding(16)
                    .transition(
                        .asymmetric(
                            insertion: .move(edge: .trailing), removal: .move(edge: .leading)))
                default:
                    EmptyView()
                }
            }
            .id(selectedPage)  // Force transition on change
            .animation(.spring(response: 0.5, dampingFraction: 0.8), value: selectedPage)

            // Navigation Buttons (Floating)
            VStack {
                Spacer()
                HStack {
                    if selectedPage > 0 {
                        Button("Back") {
                            withAnimation { selectedPage -= 1 }
                        }
                        .buttonStyle(.plain)
                        .font(.headline)
                        .foregroundStyle(.white.opacity(0.6))
                        .padding(.horizontal, 20)
                        .padding(.vertical, 10)
                        .glassEffect()
                        .clipShape(Capsule())
                    }

                    Spacer()

                    if selectedPage < 7 {
                        Button("Next") {
                            withAnimation { selectedPage += 1 }
                        }
                        .buttonStyle(.plain)
                        .font(.headline)
                        .foregroundStyle(.white)
                        .padding(.horizontal, 24)
                        .padding(.vertical, 10)
                        .glassEffect(.regular.tint(.blue))
                        .clipShape(Capsule())
                        .shadow(color: .blue.opacity(0.4), radius: 10, x: 0, y: 5)
                    }
                }
                .padding(.horizontal, 40)
                .padding(.bottom, 40)
            }

            // Connection Status Pill
            VStack {
                ConnectionStatusBar()
                    .padding(.top, 12)
                    .padding(.horizontal, 16)
                Spacer()
            }
        }
        // Wait for backend and fetch plugins so they are ready for the Plugins page
        .task {
            await appState.fetchPlugins()
        }
        .frame(minWidth: 900, minHeight: 650)
    }

    func completeOnboarding() {
        withAnimation {
            hasCompletedOnboarding = true
        }
        dismiss()
        // Trigger initial scan
        Task {
            await appState.scanLibrary()
        }
    }
}

// MARK: - Sub Pages

struct WelcomePage: View {
    @State private var animate = false

    var body: some View {
        ZStack {
            // Floating particles
            ForEach(0..<8, id: \.self) { i in
                Circle()
                    .fill(Color.white.opacity(0.1))
                    .frame(width: CGFloat.random(in: 20...60))
                    .offset(
                        x: animate
                            ? CGFloat.random(in: -200...200) : CGFloat.random(in: -100...100),
                        y: animate ? CGFloat.random(in: -300...300) : CGFloat.random(in: -150...150)
                    )
                    .blur(radius: 10)
                    .animation(
                        .easeInOut(duration: Double.random(in: 3...6))
                            .repeatForever(autoreverses: true)
                            .delay(Double(i) * 0.2),
                        value: animate
                    )
            }

            VStack(spacing: 32) {
                // Animated app icon with pulsing rings
                ZStack {
                    // Pulsing rings
                    ForEach(0..<3) { i in
                        Circle()
                            .stroke(Color.blue.opacity(0.3), lineWidth: 2)
                            .frame(width: 140 + CGFloat(i * 40), height: 140 + CGFloat(i * 40))
                            .scaleEffect(animate ? 1.2 : 1.0)
                            .opacity(animate ? 0.0 : 0.5)
                            .animation(
                                .easeOut(duration: 2.0)
                                    .repeatForever(autoreverses: false)
                                    .delay(Double(i) * 0.3),
                                value: animate
                            )
                    }

                    Image(nsImage: NSImage(named: "Aether") ?? NSImage())
                        .resizable()
                        .frame(width: 140, height: 140)
                        .shadow(color: .black.opacity(0.3), radius: 20, x: 0, y: 10)
                        .scaleEffect(animate ? 1.05 : 1.0)
                        .animation(
                            .easeInOut(duration: 2.0).repeatForever(autoreverses: true),
                            value: animate
                        )
                }

                VStack(spacing: 12) {
                    Text("Welcome to Aether")
                        .font(.system(size: 54, weight: .bold, design: .rounded))
                        .foregroundStyle(.white)
                        .shadow(color: .purple.opacity(0.5), radius: 20)

                    Text("Your Unified Game Library")
                        .font(.title)
                        .fontWeight(.medium)
                        .foregroundStyle(.white.opacity(0.8))
                }
            }
        }
        .onAppear {
            animate = true
        }
    }
}

struct UnifiedLibraryPage: View {
    @State private var animate = false

    var body: some View {
        VStack(spacing: 50) {
            // Visual Animation
            ZStack {
                // Central Hub
                Circle()
                    .fill(
                        LinearGradient(
                            colors: [.blue, .purple], startPoint: .topLeading,
                            endPoint: .bottomTrailing)
                    )
                    .frame(width: 140, height: 140)
                    .shadow(color: .blue.opacity(0.6), radius: 30)
                    .overlay(
                        Image(systemName: "gamecontroller.fill")
                            .font(.system(size: 60))
                            .foregroundStyle(.white)
                    )

                // Orbiting Sources
                ForEach(0..<4) { i in
                    Circle()
                        .fill(.ultraThinMaterial)
                        .frame(width: 70, height: 70)
                        .overlay(
                            Image(systemName: iconName(for: i))
                                .font(.largeTitle)
                                .foregroundStyle(.white)
                        )
                        .offset(x: 160)
                        .rotationEffect(.degrees(animate ? 360 : 0))
                        .rotationEffect(.degrees(Double(i) * 90))
                }
            }
            .frame(height: 400)
            .onAppear {
                withAnimation(.linear(duration: 20).repeatForever(autoreverses: false)) {
                    animate = true
                }
            }

            VStack(spacing: 16) {
                Text("All Your Games, One Place")
                    .font(.largeTitle)
                    .fontWeight(.bold)
                    .foregroundStyle(.white)

                Text(
                    "Seamlessly bring together your Steam, Epic, and CrossOver libraries into a single, beautiful interface."
                )
                .font(.title3)
                .multilineTextAlignment(.center)
                .foregroundStyle(.white.opacity(0.7))
                .padding(.horizontal, 60)
                .frame(maxWidth: 600)
            }
        }
    }

    func iconName(for index: Int) -> String {
        switch index {
        case 0: return "arrow.down.circle.fill"  // Steam-ish
        case 1: return "app.gift.fill"  // Epic-ish
        case 2: return "macwindow"  // Crossover-ish
        case 3: return "folder.fill"  // Local
        default: return "circle"
        }
    }
}

struct PluginsPage: View {
    let plugins: [PluginViewModel]

    var body: some View {
        VStack(spacing: 40) {
            VStack(spacing: 16) {
                Text("Powered by Plugins")
                    .font(.largeTitle)
                    .fontWeight(.bold)
                    .foregroundStyle(.white)

                Text(
                    "Aether automatically detects installed launchers and enables plugins to sync your games."
                )
                .font(.title3)
                .multilineTextAlignment(.center)
                .foregroundStyle(.white.opacity(0.7))
                .padding(.horizontal, 60)
                .frame(maxWidth: 600)
            }

            ScrollView {
                // Glass Cards Grid
                LazyVGrid(columns: [GridItem(.adaptive(minimum: 160), spacing: 20)], spacing: 20) {
                    ForEach(plugins) { plugin in
                        VStack(spacing: 16) {
                            // Icon
                            ZStack {
                                Circle()
                                    .fill(
                                        plugin.isImporter
                                            ? Color.blue.opacity(0.2) : Color.green.opacity(0.2)
                                    )
                                    .frame(width: 60, height: 60)

                                Image(
                                    systemName: plugin.isImporter
                                        ? "arrow.down.circle.fill" : "puzzlepiece.fill"
                                )
                                .font(.system(size: 30))
                                .foregroundStyle(plugin.isImporter ? .blue : .green)
                            }

                            VStack(spacing: 4) {
                                Text(plugin.name)
                                    .font(.headline)
                                    .foregroundStyle(.white)
                                    .lineLimit(1)

                                Text("v\(plugin.version)")
                                    .font(.caption)
                                    .foregroundStyle(.secondary)
                                    .padding(.horizontal, 8)
                                    .padding(.vertical, 2)
                                    .background(.white.opacity(0.1))
                                    .clipShape(Capsule())
                            }
                        }
                        .padding(20)
                        .frame(maxWidth: .infinity)
                        .background(.ultraThinMaterial)
                        .clipShape(RoundedRectangle(cornerRadius: 20))
                        .overlay(
                            RoundedRectangle(cornerRadius: 20)
                                .stroke(.white.opacity(0.1), lineWidth: 1)
                        )
                        .shadow(color: .black.opacity(0.1), radius: 10)
                    }
                }
                .padding()
            }
            .frame(height: 350)
            .mask(
                LinearGradient(
                    colors: [.black, .black, .black, .clear], startPoint: .top, endPoint: .bottom)
            )
        }
    }
}

// MARK: - Interactive Settings Page

struct SettingsOnboardingPage: View {
    // Local bindings using Keys from SettingsView.swift
    @AppStorage("automaticallyCheckForUpdates") private var automaticallyCheckForUpdates = true
    @AppStorage("useTopNavigation") private var useTopNavigation = false  // Default: Sidebar (false)
    @AppStorage("useLiquidGlassCards") private var useLiquidGlassCards = false

    var body: some View {
        VStack(spacing: 40) {
            // Header
            VStack(spacing: 16) {
                Image(systemName: "slider.horizontal.3")
                    .font(.system(size: 60))
                    .foregroundStyle(.orange)
                    .shadow(color: .orange.opacity(0.5), radius: 20)

                Text("Make It Yours")
                    .font(.largeTitle)
                    .fontWeight(.bold)
                    .foregroundStyle(.white)

                Text("Configure your core experience now. You can change these later in Settings.")
                    .font(.title3)
                    .multilineTextAlignment(.center)
                    .foregroundStyle(.white.opacity(0.7))
                    .padding(.horizontal, 40)
            }

            // Interactive Controls
            VStack(spacing: 20) {
                // Update Check
                ToggleRow(
                    icon: "arrow.triangle.2.circlepath", color: .green, title: "Automatic Updates",
                    isOn: $automaticallyCheckForUpdates)

                // Visual Effects
                ToggleRow(
                    icon: "sparkles", color: .purple, title: "Liquid Glass Effects",
                    isOn: $useLiquidGlassCards)

                // Navigation Style
                HStack {
                    Label {
                        Text("Navigation Style")
                            .foregroundStyle(.white)
                    } icon: {
                        Image(
                            systemName: !useTopNavigation
                                ? "sidebar.left" : "menubar.rectangle"
                        )
                        .foregroundStyle(.blue)
                    }

                    Spacer()

                    Picker("", selection: $useTopNavigation) {
                        Text("Sidebar").tag(false)
                        Text("Top Bar").tag(true)
                    }
                    .pickerStyle(.segmented)
                    .frame(width: 150)
                }
                .padding()
                .background(.ultraThinMaterial)
                .clipShape(RoundedRectangle(cornerRadius: 12))
            }
            .frame(maxWidth: 500)
            .padding()
        }
    }

    struct ToggleRow: View {
        let icon: String
        let color: Color
        let title: String
        @Binding var isOn: Bool

        var body: some View {
            HStack {
                Label {
                    Text(title)
                        .foregroundStyle(.white)
                } icon: {
                    Image(systemName: icon)
                        .foregroundStyle(color)
                }

                Spacer()

                Toggle("", isOn: $isOn)
                    .toggleStyle(.switch)
                    .tint(color)
            }
            .padding()
            .background(.ultraThinMaterial)
            .clipShape(RoundedRectangle(cornerRadius: 12))
        }
    }
}

// Reuse Tutorial Page from previous implementation, but improved
struct TutorialPage<Content: View>: View {
    let title: String
    let description: String
    let icon: String
    let color: Color
    let instruction: String
    let visual: Content

    init(
        title: String, description: String, icon: String, color: Color, instruction: String,
        @ViewBuilder visual: () -> Content
    ) {
        self.title = title
        self.description = description
        self.icon = icon
        self.color = color
        self.instruction = instruction
        self.visual = visual()
    }

    var body: some View {
        VStack(spacing: 40) {
            // Header
            VStack(spacing: 16) {
                Image(systemName: icon)
                    .font(.system(size: 60))
                    .foregroundStyle(color)
                    .shadow(color: color.opacity(0.5), radius: 20)

                Text(title)
                    .font(.largeTitle)
                    .fontWeight(.bold)
                    .foregroundStyle(.white)

                Text(description)
                    .font(.title3)
                    .multilineTextAlignment(.center)
                    .foregroundStyle(.white.opacity(0.7))
                    .padding(.horizontal, 40)
            }

            // Visual Preview
            visual

            // Instruction
            HStack(spacing: 12) {
                Image(systemName: "info.circle.fill")
                    .foregroundStyle(color)
                Text(instruction)
                    .font(.body)
                    .fontWeight(.medium)
                    .foregroundStyle(.white.opacity(0.9))
            }
            .padding()
            .background(color.opacity(0.1))
            .clipShape(RoundedRectangle(cornerRadius: 12))
            .padding(.horizontal, 40)
            .frame(maxWidth: 600)
        }
    }
}

struct GetStartedPage: View {
    let action: () -> Void

    var body: some View {
        VStack(spacing: 40) {
            Spacer()

            VStack(spacing: 24) {
                Image(systemName: "checkmark.circle.fill")
                    .font(.system(size: 100))
                    .foregroundStyle(.green)
                    .shadow(color: .green.opacity(0.5), radius: 30)

                Text("Ready to Play?")
                    .font(.system(size: 54, weight: .bold, design: .rounded))
                    .foregroundStyle(.white)

                Text("Your library is waiting. Let's get started.")
                    .font(.title2)
                    .foregroundStyle(.white.opacity(0.7))
            }

            Spacer()

            Button(action: action) {
                Text("Start Using Aether")
                    .font(.title2)
                    .fontWeight(.bold)
                    .foregroundStyle(.white)
                    .frame(width: 300, height: 70)
                    .background(
                        LinearGradient(
                            colors: [.blue, .purple], startPoint: .leading, endPoint: .trailing)
                    )
                    .clipShape(Capsule())
                    .shadow(color: .blue.opacity(0.5), radius: 20, x: 0, y: 10)
            }
            .buttonStyle(.plain)
            .padding(.bottom, 60)
        }
    }
}

// MARK: - Window Accessor Helper

struct WindowAccessor: NSViewRepresentable {
    let onUpdate: (NSWindow?) -> Void

    func makeNSView(context: Context) -> NSView {
        let view = NSView()
        DispatchQueue.main.async {
            self.onUpdate(view.window)
        }
        return view
    }

    func updateNSView(_ nsView: NSView, context: Context) {
        DispatchQueue.main.async {
            self.onUpdate(nsView.window)
        }
    }
}

#Preview {
    OnboardingView()
        .environmentObject(AppState())
        .frame(width: 1000, height: 700)
}

struct StatsFeaturePage: View {
    @State private var animate = false
    @State private var playCount = 0
    @State private var hoursPlayed = 0

    // Timer for simulating counting up
    let timer = Timer.publish(every: 0.05, on: .main, in: .common).autoconnect()

    var body: some View {
        VStack(spacing: 50) {
            // Visual Animation
            ZStack {
                // Central Hub (Clock/Timer)
                Circle()
                    .fill(
                        LinearGradient(
                            colors: [.orange, .pink], startPoint: .topLeading,
                            endPoint: .bottomTrailing)
                    )
                    .frame(width: 160, height: 160)
                    .shadow(color: .orange.opacity(0.5), radius: 30)
                    .overlay(
                        VStack(spacing: 4) {
                            Text("\(hoursPlayed)h")
                                .font(.system(size: 48, weight: .bold, design: .rounded))
                                .foregroundStyle(.white)
                                .contentTransition(.numericText(value: Double(hoursPlayed)))

                            Text("PLAYED")
                                .font(.caption)
                                .fontWeight(.bold)
                                .foregroundStyle(.white.opacity(0.8))
                        }
                    )

                // Ring orbiting
                Circle()
                    .trim(from: 0, to: 0.7)
                    .stroke(
                        AngularGradient(colors: [.white, .white.opacity(0)], center: .center),
                        style: StrokeStyle(lineWidth: 6, lineCap: .round)
                    )
                    .frame(width: 200, height: 200)
                    .rotationEffect(.degrees(animate ? 360 : 0))
                    .animation(
                        .linear(duration: 8).repeatForever(autoreverses: false), value: animate)

                // Floating Stat Bubbles

                // Bubble 1: Play Count
                StatBubble(
                    icon: "play.circle.fill", value: "\(playCount)", label: "Sessions", color: .blue
                )
                .offset(x: -140, y: -40)
                .scaleEffect(animate ? 1.05 : 0.95)
                .animation(
                    .easeInOut(duration: 3).repeatForever(autoreverses: true), value: animate)

                // Bubble 2: Last Played
                StatBubble(
                    icon: "calendar.badge.clock", value: "Today", label: "Last Played",
                    color: .purple
                )
                .offset(x: 140, y: 20)
                .scaleEffect(animate ? 0.95 : 1.05)
                .animation(
                    .easeInOut(duration: 4).repeatForever(autoreverses: true).delay(0.5),
                    value: animate)

                // Bubble 3: Achievements (Visual flair only)
                StatBubble(icon: "trophy.fill", value: "Stats", label: "*Tracking", color: .yellow)
                    .offset(x: 0, y: 130)
                    .scaleEffect(animate ? 1.05 : 1.0)
                    .animation(
                        .easeInOut(duration: 2.5).repeatForever(autoreverses: true).delay(1.0),
                        value: animate)

            }
            .frame(height: 350)
            .onAppear {
                animate = true
            }
            .onReceive(timer) { _ in
                if playCount < 42 {
                    playCount += 1
                }
                if hoursPlayed < 135 {
                    // Accelerate hours
                    hoursPlayed += 3
                }
            }

            VStack(spacing: 16) {
                Text("Track Your Journey")
                    .font(.largeTitle)
                    .fontWeight(.bold)
                    .foregroundStyle(.white)

                Text(
                    "Aether automatically tracks your total playtime, launch counts, and session history across all your games."
                )
                .font(.title3)
                .multilineTextAlignment(.center)
                .foregroundStyle(.white.opacity(0.7))
                .padding(.horizontal, 60)
                .frame(maxWidth: 600)
                Text("*Tracking Achivements is currently not implemented.\nTracking playtime only works when the game is launched through Aether.")
                    .multilineTextAlignment(.center)
                    .font(.caption)
                    .foregroundStyle(.white.opacity(0.6))
            }
        }
    }

    struct StatBubble: View {
        let icon: String
        let value: String
        let label: String
        let color: Color

        var body: some View {
            VStack(spacing: 6) {
                Image(systemName: icon)
                    .font(.title2)
                    .foregroundStyle(color)

                Text(value)
                    .font(.title3)
                    .fontWeight(.bold)
                    .foregroundStyle(.white)

                Text(label)
                    .font(.caption2)
                    .textCase(.uppercase)
                    .foregroundStyle(.white.opacity(0.6))
            }
            .padding(24)
            .glassEffect(in: Circle())
            .clipShape(Circle())
            .shadow(color: .black.opacity(0.2), radius: 10)
        }
    }
}
