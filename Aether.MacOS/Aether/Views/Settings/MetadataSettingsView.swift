import AetherIPC
import GRPCCore
import SwiftProtobuf
import SwiftUI

struct MetadataSettingsView: View {
    @Environment(\.dismiss) var dismiss
    @State private var priority: [String] = []
    @State private var available: [String] = []
    @State private var isLoading = true
    @State private var errorMessage: String?

    var body: some View {
        VStack(alignment: .leading, spacing: 20) {

            VStack(alignment: .leading, spacing: 4) {
                Text("Metadata Providers")
                    .font(.headline)
                Text(
                    "Drag to reorder. Aether will check providers in this order when scanning for game metadata."
                )
                .font(.caption)
                .foregroundStyle(.secondary)
            }
            .padding(.horizontal)
            .padding(.top)

            if isLoading {
                ProgressView()
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            } else if let error = errorMessage {
                Text("Error: \(error)")
                    .foregroundStyle(.red)
                    .padding()
            } else {
                List {
                    ForEach(priority, id: \.self) { provider in
                        HStack {
                            Image(systemName: "line.3.horizontal")
                                .foregroundStyle(.tertiary)

                            // Icon heuristic
                            Image(systemName: iconFor(provider))
                                .frame(width: 20)

                            Text(provider)
                                .fontWeight(.medium)

                            Spacer()

                            if provider == "Steam" {
                                Text("Recommended")
                                    .font(.caption2)
                                    .foregroundStyle(.white)
                                    .padding(.horizontal, 6)
                                    .padding(.vertical, 2)
                                    .background(Capsule().fill(Color.blue))
                            }
                        }
                        .padding(.vertical, 4)
                        .contentShape(Rectangle())
                    }
                    .onMove(perform: move)
                }
                .listStyle(.inset)
                .frame(minHeight: 200)
                .background(Color(nsColor: .controlBackgroundColor))
                .cornerRadius(10)
                .overlay(
                    RoundedRectangle(cornerRadius: 10).stroke(Color.gray.opacity(0.2), lineWidth: 1)
                )
                .padding(.horizontal)
            }

            Spacer()

            HStack {
                Spacer()
                Button("Apply Changes") {
                    save()
                }
                .buttonStyle(.borderedProminent)
            }
            .padding()
        }
        .onAppear(perform: load)
        .frame(minWidth: 400, minHeight: 400)
    }

    func iconFor(_ name: String) -> String {
        switch name {
        case "Steam": return "steam.logo"  // SF Symbol doesn't have steam, assume specialized font or generic
        case "IGDB": return "gamecontroller"
        case "Web": return "globe"
        default: return "shippingbox"
        }
    }

    func move(from source: IndexSet, to destination: Int) {
        priority.move(fromOffsets: source, toOffset: destination)
    }

    func load() {
        Task {
            do {
                let settings = try await GrpcClient.shared.client.getMetadataSettings(
                    Aether_Empty())
                await MainActor.run {
                    self.priority = Array(settings.providerPriority)  // Needs to be Array for List
                    self.available = Array(settings.availableProviders)

                    // Merge new available providers to bottom
                    for p in self.available {
                        if !self.priority.contains(p) {
                            self.priority.append(p)
                        }
                    }

                    self.isLoading = false
                }
            } catch {
                await MainActor.run {
                    self.errorMessage = error.localizedDescription
                    self.isLoading = false
                }
            }
        }
    }

    func save() {
        Task {
            do {
                var settings = Aether_MetadataSettings()
                settings.providerPriority = self.priority
                _ = try await GrpcClient.shared.client.setMetadataSettings(settings)

                await MainActor.run {
                    dismiss()
                }
            } catch {
                await MainActor.run {
                    self.errorMessage = "Failed to save: \(error.localizedDescription)"
                }
            }
        }
    }
}
