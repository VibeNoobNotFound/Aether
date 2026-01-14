import SwiftUI

struct UpdateStatusPill: View {
    @ObservedObject var updateManager = UpdateManager.shared

    var body: some View {
        Group {
            if updateManager.checkStatus != .idle {
                HStack(spacing: 8) {
                    switch updateManager.checkStatus {
                    case .checking:
                        ProgressView()
                            .controlSize(.small)
                            .frame(width: 12, height: 12)
                        Text("Checking for updates...")
                    case .available:
                        Image(systemName: "arrow.down.circle.fill")
                            .foregroundStyle(.green)
                        Text("Update available!")
                    case .upToDate:
                        Image(systemName: "checkmark.circle.fill")
                            .foregroundStyle(.blue)
                        Text("You're up to date")
                    case .error(let message):
                        Image(systemName: "exclamationmark.triangle.fill")
                            .foregroundStyle(.red)
                        Text("Update failed")
                    case .idle:
                        EmptyView()
                    }
                }
                .font(.caption)
                .fontWeight(.medium)
                .padding(.horizontal, 12)
                .padding(.vertical, 8)
                .background(.ultraThinMaterial)
                .clipShape(Capsule())
                .overlay(
                    Capsule()
                        .stroke(.white.opacity(0.1), lineWidth: 1)
                )
                .padding(.bottom, 8)
                .transition(.move(edge: .bottom).combined(with: .opacity))
            }
        }
        .animation(.spring(response: 0.3, dampingFraction: 0.7), value: updateManager.checkStatus)
    }
}
