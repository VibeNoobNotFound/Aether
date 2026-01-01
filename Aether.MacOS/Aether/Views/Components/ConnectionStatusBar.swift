import SwiftUI

/// A subtle bottom bar showing backend connection status with Liquid Glass styling
struct ConnectionStatusBar: View {
    @ObservedObject var backendManager = BackendManager.shared

    @State private var showConnectedToast = false

    var body: some View {
        Group {
            switch backendManager.connectionState {
            case .disconnected:
                EmptyView()

            case .connecting:
                LiquidGlassContainer {
                    HStack(spacing: 10) {
                        ProgressView()
                            .controlSize(.small)
                            .tint(.white)
                        Text("Connecting to backend...")
                            .font(.system(size: 13, weight: .medium))
                            .foregroundStyle(.white.opacity(0.9))
                    }
                }
                .transition(
                    .asymmetric(
                        insertion: .move(edge: .bottom).combined(with: .opacity).combined(
                            with: .scale(scale: 0.9)),
                        removal: .opacity.combined(with: .scale(scale: 0.95))
                    ))

            case .connected:
                if showConnectedToast {
                    LiquidGlassContainer(tint: .green) {
                        HStack(spacing: 8) {
                            Image(systemName: "checkmark.circle.fill")
                                .foregroundStyle(.green)
                                .font(.system(size: 14))
                            Text("Connected")
                                .font(.system(size: 13, weight: .medium))
                                .foregroundStyle(.white.opacity(0.9))
                        }
                    }
                    .transition(
                        .asymmetric(
                            insertion: .move(edge: .bottom).combined(with: .opacity).combined(
                                with: .scale(scale: 0.9)),
                            removal: .opacity.combined(with: .scale(scale: 0.95))
                        )
                    )
                    .onAppear {
                        // Auto-dismiss after 2 seconds
                        DispatchQueue.main.asyncAfter(deadline: .now() + 2) {
                            withAnimation(.spring(response: 0.4, dampingFraction: 0.8)) {
                                showConnectedToast = false
                            }
                        }
                    }
                }

            case .error(let message):
                LiquidGlassContainer(tint: .red, isError: true) {
                    HStack(spacing: 12) {
                        Image(systemName: "exclamationmark.triangle.fill")
                            .foregroundStyle(.red)
                            .font(.system(size: 14))

                        Text(message)
                            .font(.system(size: 13, weight: .medium))
                            .foregroundStyle(.white.opacity(0.95))

                        Button {
                            backendManager.retryConnection()
                        } label: {
                            Text("Retry")
                                .font(.system(size: 12, weight: .semibold))
                                .padding(.horizontal, 14)
                                .padding(.vertical, 6)
                                .background(
                                    Capsule()
                                        .fill(.white.opacity(0.2))
                                        .overlay(
                                            Capsule()
                                                .stroke(.white.opacity(0.3), lineWidth: 1)
                                        )
                                )
                                .foregroundStyle(.white)
                        }
                        .buttonStyle(.plain)
                    }
                }
                .transition(
                    .asymmetric(
                        insertion: .move(edge: .bottom).combined(with: .opacity).combined(
                            with: .scale(scale: 0.9)),
                        removal: .opacity.combined(with: .scale(scale: 0.95))
                    ))
            }
        }
        .animation(
            .spring(response: 0.5, dampingFraction: 0.75), value: backendManager.connectionState
        )
        .onChange(of: backendManager.connectionState) { newValue in
            if case .connected = newValue {
                withAnimation(.spring(response: 0.5, dampingFraction: 0.75)) {
                    showConnectedToast = true
                }
            }
        }
    }
}

// MARK: - Liquid Glass Container (Apple's iOS 26+ Liquid Glass Style)

/// A container that applies Apple's Liquid Glass design language
/// Inspired by iOS 26/macOS Tahoe: translucent, dynamic, with subtle depth
struct LiquidGlassContainer<Content: View>: View {
    let tint: Color
    let isError: Bool
    @ViewBuilder let content: Content

    @State private var isHovered = false

    init(tint: Color = .blue, isError: Bool = false, @ViewBuilder content: () -> Content) {
        self.tint = tint
        self.isError = isError
        self.content = content()
    }

    var body: some View {
        content
            .padding(.horizontal, 20)
            .padding(.vertical, 12)
            .background {
                ZStack {
                    // Base glass layer - deep translucent
                    RoundedRectangle(cornerRadius: 20)
                        .fill(.ultraThinMaterial)

                    // Tint overlay for color identity
                    RoundedRectangle(cornerRadius: 20)
                        .fill(
                            LinearGradient(
                                colors: [
                                    tint.opacity(isError ? 0.25 : 0.15),
                                    tint.opacity(isError ? 0.15 : 0.05),
                                ],
                                startPoint: .topLeading,
                                endPoint: .bottomTrailing
                            )
                        )

                    // Inner highlight (top edge glow - simulates light refraction)
                    RoundedRectangle(cornerRadius: 20)
                        .stroke(
                            LinearGradient(
                                colors: [
                                    .white.opacity(isHovered ? 0.5 : 0.35),
                                    .white.opacity(0.1),
                                    .clear,
                                ],
                                startPoint: .top,
                                endPoint: .bottom
                            ),
                            lineWidth: 1
                        )
                }
            }
            .shadow(color: tint.opacity(0.3), radius: isHovered ? 20 : 12, x: 0, y: 4)
            .shadow(color: .black.opacity(0.2), radius: 8, x: 0, y: 2)
            .scaleEffect(isHovered ? 1.02 : 1.0)
            .animation(.spring(response: 0.4, dampingFraction: 0.7), value: isHovered)
            .onHover { isHovered = $0 }
    }
}
