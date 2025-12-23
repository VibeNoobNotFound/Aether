import AVKit
import SwiftUI

struct AutoplayVideoPlayer: View {
    let url: URL
    @State private var player: AVPlayer?
    @State private var isVisible = false

    var body: some View {
        GeometryReader { geo in
            ZStack {
                if let player = player {
                    VideoPlayer(player: player)
                        .disabled(true)  // Disable controls for "preview" feel
                } else {
                    Rectangle().fill(Color.black)
                }
            }
            .onChange(of: geo.frame(in: .global)) { oldFrame, newFrame in
                checkVisibility(frame: newFrame)
            }
            .onAppear {
                setupPlayer()
            }
            .onDisappear {
                player?.pause()
            }
        }
        .aspectRatio(16 / 9, contentMode: .fit)
        .clipShape(RoundedRectangle(cornerRadius: 12))
        .overlay(
            RoundedRectangle(cornerRadius: 12)
                .stroke(Color.white.opacity(0.1), lineWidth: 1)
        )
    }

    private func setupPlayer() {
        let playerItem = AVPlayerItem(url: url)
        player = AVPlayer(playerItem: playerItem)
        player?.isMuted = true  // Autoplay muted usually
        player?.actionAtItemEnd = .none

        // Loop video
        NotificationCenter.default.addObserver(
            forName: .AVPlayerItemDidPlayToEndTime,
            object: playerItem,
            queue: .main
        ) { _ in
            player?.seek(to: .zero)
            player?.play()
        }
    }

    private func checkVisibility(frame: CGRect) {
        let screenHeight = NSScreen.main?.frame.height ?? 800
        // Simple check: if center of video is within screen bounds
        let midY = frame.midY
        let isNowVisible = midY > 0 && midY < screenHeight

        if isNowVisible != isVisible {
            isVisible = isNowVisible
            if isVisible {
                player?.play()
            } else {
                player?.pause()
            }
        }
    }
}
