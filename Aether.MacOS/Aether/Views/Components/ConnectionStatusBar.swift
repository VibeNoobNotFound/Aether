import SwiftUI

/// A subtle bottom bar showing backend connection status
struct ConnectionStatusBar: View {
    @ObservedObject var backendManager = BackendManager.shared

    @State private var showConnectedToast = false

    var body: some View {
        Group {
            switch backendManager.connectionState {
            case .disconnected:
                EmptyView()

            case .connecting:
                HStack(spacing: 8) {
                    ProgressView()
                        .controlSize(.small)
                    Text("Connecting to backend...")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
                .padding(.horizontal, 16)
                .padding(.vertical, 8)
                .background(.ultraThinMaterial)
                .clipShape(Capsule())
                .transition(.move(edge: .bottom).combined(with: .opacity))

            case .connected:
                if showConnectedToast {
                    HStack(spacing: 6) {
                        Image(systemName: "checkmark.circle.fill")
                            .foregroundStyle(.green)
                        Text("Connected")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                    .padding(.horizontal, 16)
                    .padding(.vertical, 8)
                    .background(.ultraThinMaterial)
                    .clipShape(Capsule())
                    .transition(.move(edge: .bottom).combined(with: .opacity))
                    .onAppear {
                        // Auto-dismiss after 2 seconds
                        DispatchQueue.main.asyncAfter(deadline: .now() + 2) {
                            withAnimation(.easeOut(duration: 0.3)) {
                                showConnectedToast = false
                            }
                        }
                    }
                }

            case .error(let message):
                HStack(spacing: 12) {
                    Image(systemName: "exclamationmark.triangle.fill")
                        .foregroundStyle(.red)

                    Text(message)
                        .font(.caption)
                        .foregroundStyle(.white)

                    Button {
                        backendManager.retryConnection()
                    } label: {
                        Text("Retry")
                            .font(.caption.bold())
                            .padding(.horizontal, 12)
                            .padding(.vertical, 4)
                            .background(Color.blue)
                            .foregroundStyle(.white)
                            .clipShape(Capsule())
                    }
                    .buttonStyle(.plain)
                }
                .padding(.horizontal, 16)
                .padding(.vertical, 10)
                .background(Color.red.opacity(0.9))
                .clipShape(Capsule())
                .transition(.move(edge: .bottom).combined(with: .opacity))
            }
        }
        .animation(
            .spring(response: 0.4, dampingFraction: 0.8), value: backendManager.connectionState
        )
        .onChange(of: backendManager.connectionState) { oldValue, newValue in
            if case .connected = newValue, case .connecting = oldValue {
                withAnimation {
                    showConnectedToast = true
                }
            }
        }
    }
}
