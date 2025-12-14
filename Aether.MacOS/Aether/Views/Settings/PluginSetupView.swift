import AetherIPC
import GRPCCore
import NIOCore
import SwiftUI

struct PluginSetupView: View {
    let plugin: PluginViewModel
    @EnvironmentObject var appState: AppState
    @Environment(\.dismiss) var dismiss

    @State private var widgets: [Aether_PluginWidget] = []
    @State private var formLayout: FormLayout?
    @State private var fieldValues: [String: String] = [:]
    @State private var errorMessage: String?
    @State private var isSubmitting = false

    var body: some View {
        Form {
            if let layout = formLayout {
                Section(header: Text(plugin.name)) {
                    ForEach(layout.fields) { field in
                        FormFieldView(
                            field: field,
                            value: Binding(
                                get: { fieldValues[field.id] ?? "" },
                                set: { fieldValues[field.id] = $0 }
                            ))
                    }
                }

                Section {
                    ForEach(layout.actions) { action in
                        Button(action: {
                            Task { await performAction(action) }
                        }) {
                            if isSubmitting {
                                ProgressView()
                                    .controlSize(.small)
                            } else {
                                Text(action.label)
                            }
                        }
                        .disabled(isSubmitting)
                    }
                }
            } else if !widgets.isEmpty {
                Text("Loaded widgets but parsable layout not found")
            } else {
                ProgressView("Loading...")
            }

            if let error = errorMessage {
                Text(error)
                    .foregroundColor(.red)
                    .font(.caption)
            }
        }
        .formStyle(.grouped)
        .navigationTitle("Setup \(plugin.name)")
        .task {
            await loadWidgets()
        }
    }

    private func loadWidgets() async {
        let widgets = await appState.fetchSetupWidgets(for: plugin.name)
        self.widgets = widgets

        // Parse the first widget that has layout JSON
        if let widget = widgets.first(where: { !$0.layoutJson.isEmpty }) {
            parseLayout(json: widget.layoutJson)
        }
    }

    private func parseLayout(json: String) {
        guard let data = json.data(using: .utf8) else { return }
        do {
            let layout = try JSONDecoder().decode(FormLayout.self, from: data)
            self.formLayout = layout
        } catch {
            print("Failed to decode layout: \(error)")
            errorMessage = "Failed to load UI layout"
        }
    }

    private func performAction(_ action: FormAction) async {
        isSubmitting = true
        defer { isSubmitting = false }

        // Serialize form data
        guard let payloadData = try? JSONEncoder().encode(fieldValues),
            let payloadString = String(data: payloadData, encoding: .utf8)
        else {
            errorMessage = "Failed to encode form data"
            return
        }

        do {
            let response = try await appState.triggerPluginAction(
                pluginName: plugin.name,
                actionId: action.id,
                payload: payloadString
            )
            if response.success {
                dismiss()
                // Optionally trigger library rescan
            } else {
                errorMessage = response.message
            }
        } catch {
            errorMessage = "Action failed: \(error.localizedDescription)"
        }
    }
}

struct FormFieldView: View {
    let field: FormField
    @Binding var value: String

    var body: some View {
        VStack(alignment: .leading) {
            switch field.type {
            case "FolderPicker":
                HStack {
                    TextField(field.label, text: $value)
                    Button("Browse") {
                        let panel = NSOpenPanel()
                        panel.canChooseDirectories = true
                        panel.canChooseFiles = false
                        panel.allowsMultipleSelection = false
                        if panel.runModal() == .OK {
                            value = panel.url?.path ?? ""
                        }
                    }
                }
            case "FilePicker":
                HStack {
                    TextField(field.label, text: $value)
                    Button("Browse") {
                        let panel = NSOpenPanel()
                        panel.canChooseDirectories = false
                        panel.canChooseFiles = true
                        panel.allowsMultipleSelection = false
                        if panel.runModal() == .OK {
                            value = panel.url?.path ?? ""
                        }
                    }
                }
            default:
                TextField(field.label, text: $value)
            }
        }
    }
}
