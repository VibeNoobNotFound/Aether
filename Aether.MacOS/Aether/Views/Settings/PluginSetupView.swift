import AetherIPC
import GRPCCore
import NIOCore
import SwiftUI

struct PluginSetupView: View {
    let plugin: PluginViewModel
    @EnvironmentObject var appState: AppState
    @Environment(\.dismiss) var dismiss

    @State private var isLoading = true
    @State private var widgets: [Aether_UIWidget] = []
    @State private var formValues: [String: String] = [:]  // Values for all fields in the view

    @State private var errorMessage: String?
    @State private var isSubmitting = false

    var body: some View {
        NavigationStack {
            ZStack {
                // Background
                Color.black.ignoresSafeArea()

                GeometryReader { proxy in
                    Circle()
                        .fill(Color.blue.opacity(0.1))
                        .frame(width: 400, height: 400)
                        .blur(radius: 100)
                        .position(x: 0, y: 0)

                    Circle()
                        .fill(Color.purple.opacity(0.1))
                        .frame(width: 300, height: 300)
                        .blur(radius: 80)
                        .position(x: proxy.size.width, y: proxy.size.height)
                }
                .ignoresSafeArea()

                ScrollView {
                    VStack(spacing: 24) {
                        if isLoading {
                            ProgressView("Loading settings...")
                                .padding(.top, 50)
                        } else if !widgets.isEmpty {
                            // Render top-level widgets
                            GlassSection(title: plugin.name) {
                                ForEach(widgets, id: \.id) { widget in
                                    WidgetRenderer(
                                        widget: widget, formValues: $formValues,
                                        onAction: handleAction)
                                }
                            }
                        } else {
                            ContentUnavailableView(
                                "No Settings",
                                systemImage: "gear",
                                description: Text("\(plugin.name) does not require configuration.")
                            )
                            .padding(.top, 50)
                        }

                        if let error = errorMessage {
                            Text(error)
                                .foregroundStyle(.white)
                                .padding()
                                .background(Color.red.opacity(0.8))
                                .clipShape(RoundedRectangle(cornerRadius: 12))
                        }
                    }
                    .padding()
                }
            }
            .navigationTitle("Setup \(plugin.name)")
            .toolbarBackground(.automatic, for: .windowToolbar)
            .task {
                await loadWidgets()
                isLoading = false
            }
        }
    }

    private func loadWidgets() async {
        let widgets = await appState.fetchWidgets(for: plugin.name, location: .settings)
        // Sort by sortOrder
        self.widgets = widgets.sorted { $0.sortOrder < $1.sortOrder }
    }

    private func handleAction(actionId: String, payload: String) {
        Task {
            isSubmitting = true
            defer { isSubmitting = false }

            // If payload == "Submit", we send the form values
            // Or if payload is empty, we assume it's a simple button click
            // But for our Forms (e.g. CustomPlugin), the actionType is "Submit"
            // So payload passed from renderer is "Submit" (action.type)

            var finalPayload = payload
            // If payload == "Submit", we send the form values
            // Or if payload is empty and we have form values (implicit submission context)
            if payload == "Submit" || (payload.isEmpty && !formValues.isEmpty) {
                // Serialize form data
                if let data = try? JSONEncoder().encode(formValues),
                    let json = String(data: data, encoding: .utf8)
                {
                    finalPayload = json
                } else {
                    errorMessage = "Failed to encode form data"
                    return
                }
            }

            do {
                let response = try await appState.triggerPluginAction(
                    pluginName: plugin.name,
                    actionId: actionId,
                    payload: finalPayload
                )

                if response.success {
                    dismiss()
                    await appState.refreshLibrary()
                } else {
                    errorMessage = "\(response.message) and final payload: \(finalPayload)"
                }
            } catch {
                errorMessage =
                    "Action failed: \(error.localizedDescription), final payload: \(finalPayload)"
            }
        }
    }
}

#Preview {
#if DEBUG
    PluginSetupView(plugin: MockData.plugins[0])
        .environmentObject(MockData.appState)
    #endif
}
