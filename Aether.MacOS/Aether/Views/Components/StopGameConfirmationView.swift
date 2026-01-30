import AetherIPC
import SwiftUI

/// Confirmation dialog shown before force-stopping a game.
/// Displays the list of tracked processes that will be killed.
struct StopGameConfirmationView: View {
    let gameTitle: String
    let processes: [Aether_TrackedProcessInfo]
    let onConfirm: () -> Void
    let onCancel: () -> Void

    var body: some View {
        VStack(spacing: 20) {
            // Warning Icon
            Image(systemName: "exclamationmark.triangle.fill")
                .font(.system(size: 48))
                .foregroundColor(.orange)

            // Title
            Text("Stop \(gameTitle)?")
                .font(.title2)
                .fontWeight(.bold)

            // Warning Message
            Text("This will forcefully terminate the game. Any unsaved progress will be lost.")
                .font(.body)
                .foregroundColor(.secondary)
                .multilineTextAlignment(.center)
                .padding(.horizontal)

            // Process List
            if !processes.isEmpty {
                VStack(alignment: .leading, spacing: 8) {
                    Text("The following processes will be killed:")
                        .font(.subheadline)
                        .foregroundColor(.secondary)

                    ScrollView {
                        VStack(alignment: .leading, spacing: 6) {
                            ForEach(processes, id: \.processID) { process in
                                ProcessInfoRow(process: process)
                            }
                        }
                    }
                    .frame(maxHeight: 150)
                    .background(Color(nsColor: .controlBackgroundColor))
                    .cornerRadius(8)
                }
                .padding(.horizontal)
            }

            // Buttons
            HStack(spacing: 16) {
                Button("Cancel") {
                    onCancel()
                }
                .keyboardShortcut(.escape)

                Button("Force Stop") {
                    onConfirm()
                }
                .keyboardShortcut(.return)
                .buttonStyle(.borderedProminent)
                .tint(.red)
            }
            .padding(.top, 8)
        }
        .padding(24)
        .frame(minWidth: 400, maxWidth: 500)
    }
}

/// Row displaying information about a single tracked process.
struct ProcessInfoRow: View {
    let process: Aether_TrackedProcessInfo

    var body: some View {
        HStack(spacing: 12) {
            // Process Icon
            Image(systemName: "gearshape.fill")
                .foregroundColor(.secondary)
                .frame(width: 20)

            VStack(alignment: .leading, spacing: 2) {
                // Process Name and PID
                HStack {
                    Text(displayName)
                        .font(.system(.body, design: .monospaced))
                        .fontWeight(.medium)

                    Text("(PID: \(process.processID))")
                        .font(.caption)
                        .foregroundColor(.secondary)
                }

                // Executable Path
                if !process.executablePath.isEmpty {
                    Text(process.executablePath)
                        .font(.caption)
                        .foregroundColor(.secondary)
                        .lineLimit(1)
                        .truncationMode(.middle)
                }
            }

            Spacer()
        }
        .padding(.horizontal, 12)
        .padding(.vertical, 8)
    }

    private var displayName: String {
        if !process.processName.isEmpty {
            return process.processName
        } else if !process.executablePath.isEmpty {
            return (process.executablePath as NSString).lastPathComponent
        } else {
            return "Unknown Process"
        }
    }
}

#Preview {
    StopGameConfirmationView(
        gameTitle: "Cyberpunk 2077",
        processes: [],
        onConfirm: {},
        onCancel: {}
    )
}
