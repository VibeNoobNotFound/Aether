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
                GlassCard(tint: .blue, isHoverable: false) {
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
                    GlassCard(tint: .green, isHoverable: false) {
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
                GlassCard(tint: .red, isHoverable: true) {
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
        .onChange(of: backendManager.connectionState) { _, newValue in
            if case .connected = newValue {
                withAnimation(.spring(response: 0.5, dampingFraction: 0.75)) {
                    showConnectedToast = true
                }
            }
        }
    }
}

#Preview {
#if DEBUG
    ZStack {
        Color.black
        VStack {
            Spacer()
            ConnectionStatusBar()
        }
    }
    #endif
}
