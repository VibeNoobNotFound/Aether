import AetherIPC
import SwiftUI

struct LibraryAddMenuView: View {
    @EnvironmentObject var appState: AppState
    @Environment(\.dismiss) var dismiss

    let pluginName: String

    @State private var widgets: [Aether_UIWidget] = []
    @State private var formValues: [String: String] = [:]
    @State private var isLoading = true
    @State private var errorMessage: String?

    var body: some View {
        NavigationStack {
            VStack {
                if isLoading {
                    ProgressView("Loading...")
                } else if widgets.isEmpty {
                    ContentUnavailableView(
                        "No Options",
                        systemImage: "square.dashed",
                        description: Text("This plugin does not support adding items manually.")
                    )
                } else {
                    ScrollView {
                        VStack(spacing: 20) {
                            ForEach(widgets, id: \.id) { widget in
                                WidgetRenderer(
                                    widget: widget,
                                    formValues: $formValues,
                                    onAction: handleAction
                                )
                            }
                        }
                        .padding()
                    }
                }

                if let error = errorMessage {
                    Text(error)
                        .foregroundStyle(.red)
                        .font(.caption)
                        .padding()
                }
            }
            .navigationTitle("Add to Library")
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Cancel") { dismiss() }
                }
            }
            .task {
                await loadWidgets()
            }
        }
    }

    private func loadWidgets() async {
        let widgets = await appState.fetchWidgets(for: pluginName, location: .libraryAddMenu)
        self.widgets = widgets.sorted { $0.sortOrder < $1.sortOrder }
        isLoading = false
    }

    private func handleAction(actionId: String, payload: String) {
        Task {
            var finalPayload = payload
            // Handle form submission logic similar to PluginSetupView
            if payload == "Submit" {
                if let data = try? JSONEncoder().encode(formValues),
                    let json = String(data: data, encoding: .utf8)
                {
                    finalPayload = json
                }
            }

            do {
                let status = try await appState.triggerPluginAction(
                    pluginName: pluginName,
                    actionId: actionId,
                    payload: finalPayload
                )

                if status.success {
                    await appState.refreshLibrary()
                    dismiss()
                } else {
                    errorMessage = status.message
                }
            } catch {
                errorMessage = error.localizedDescription
            }
        }
    }
}
