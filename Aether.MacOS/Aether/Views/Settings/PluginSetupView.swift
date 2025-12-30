import AetherIPC
import GRPCCore
import NIOCore
import SwiftUI

struct PluginSetupView: View {
    let plugin: PluginViewModel
    @EnvironmentObject var appState: AppState
    @Environment(\.dismiss) var dismiss

    @State private var isLoading = true
    @State private var widgets: [Aether_PluginWidget] = []
    @State private var formLayout: FormLayout?
    @State private var parsedWidgets: [ParsedWidget] = []
    @State private var fieldValues: [String: String] = [:]
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
                        } else if !parsedWidgets.isEmpty {
                            // New format: individual widgets with their own layouts
                            // Group widgets into a single Glass Section for cleaner look
                            GlassSection(title: plugin.name) {
                                ForEach(parsedWidgets) { widget in
                                    widgetView(for: widget)
                                }
                            }
                        } else if let layout = formLayout {
                            // Legacy format: single FormLayout
                            GlassSection(title: plugin.name) {
                                ForEach(layout.fields) { field in
                                    FormFieldView(
                                        field: field,
                                        value: Binding(
                                            get: { fieldValues[field.id] ?? "" },
                                            set: { fieldValues[field.id] = $0 }
                                        ))
                                }

                                Divider()
                                    .background(Color.white.opacity(0.1))
                                    .padding(.vertical, 8)

                                ForEach(layout.actions) { action in
                                    Button {
                                        Task { await performAction(action) }
                                    } label: {
                                        HStack {
                                            Spacer()
                                            if isSubmitting {
                                                ProgressView().controlSize(.small)
                                            } else {
                                                Text(action.label)
                                            }
                                            Spacer()
                                        }
                                        .padding()
                                        .background(Color.blue)
                                        .foregroundStyle(.white)
                                        .clipShape(RoundedRectangle(cornerRadius: 8))
                                    }
                                    .buttonStyle(.plain)
                                    .disabled(isSubmitting)
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

    @ViewBuilder
    private func widgetView(for widget: ParsedWidget) -> some View {
        VStack(alignment: .leading, spacing: 10) {
            switch widget.type {
            case "section":
                if let desc = widget.description {
                    Text(desc)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .padding(.vertical, 4)
                } else {
                    Text(widget.title)
                        .font(.headline)
                        .foregroundStyle(.white)
                        .padding(.vertical, 4)
                }
            case "textfield":
                GlassTextField(
                    title: widget.title,
                    text: Binding(
                        get: { fieldValues[widget.id] ?? "" },
                        set: { fieldValues[widget.id] = $0 }
                    )
                )
            case "button":
                GlassButton(widget.title, systemImage: nil) {
                    Task { await performWidgetAction(actionId: widget.actionId ?? widget.id) }
                }
                .disabled(isSubmitting)
            default:
                Text(widget.title)
                    .foregroundStyle(.secondary)
            }
        }
    }

    private func loadWidgets() async {
        let widgets = await appState.fetchSetupWidgets(for: plugin.name)
        self.widgets = widgets

        // Try to parse as individual widgets first (new format)
        var parsed: [ParsedWidget] = []
        for widget in widgets {
            if !widget.layoutJson.isEmpty,
                let data = widget.layoutJson.data(using: .utf8),
                let layout = try? JSONDecoder().decode(WidgetLayout.self, from: data),
                layout.type != "Form"  // Ignore legacy Form type here
            {
                parsed.append(
                    ParsedWidget(
                        id: layout.id ?? widget.pluginID,
                        title: widget.title,
                        type: layout.type,
                        description: layout.description,
                        placeholder: layout.placeholder,
                        actionId: layout.actionId,
                        style: layout.style
                    ))
            }
        }

        if !parsed.isEmpty {
            self.parsedWidgets = parsed
            return
        }

        // Fallback: Try to parse legacy FormLayout from first widget
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
            // Don't show error for empty layouts
        }
    }

    private func performWidgetAction(actionId: String) async {
        isSubmitting = true
        defer { isSubmitting = false }

        guard let payloadData = try? JSONEncoder().encode(fieldValues),
            let payloadString = String(data: payloadData, encoding: .utf8)
        else {
            errorMessage = "Failed to encode form data"
            return
        }

        do {
            let response = try await appState.triggerPluginAction(
                pluginName: plugin.name,
                actionId: actionId,
                payload: payloadString
            )
            if !response.success {
                errorMessage = response.message
            }
        } catch {
            errorMessage = "Action failed: \(error.localizedDescription)"
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

// Reusing shared components from GlassComponents.swift

// Simple widget layout for new format
struct WidgetLayout: Codable {
    let type: String
    let id: String?
    let description: String?
    let placeholder: String?
    let actionId: String?
    let style: String?
}

struct ParsedWidget: Identifiable {
    let id: String
    let title: String
    let type: String
    let description: String?
    let placeholder: String?
    let actionId: String?
    let style: String?
}

struct FormFieldView: View {
    let field: FormField
    @Binding var value: String

    var body: some View {
        VStack(alignment: .leading) {
            switch field.type {
            case "FolderPicker":
                VStack(alignment: .leading, spacing: 4) {
                    Text(field.label)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .padding(.leading, 4)

                    HStack {
                        TextField("", text: $value)
                            .textFieldStyle(.plain)

                        Button("Browse") {
                            PermissionManager.shared.requestCustomFolderAccess { url in
                                if let url = url {
                                    value = url.path
                                }
                            }
                        }
                        .buttonStyle(.bordered)
                        .controlSize(.small)
                    }
                    .padding(10)
                    .background(Color.black.opacity(0.2))
                    .clipShape(RoundedRectangle(cornerRadius: 8))
                }
            case "FilePicker":
                VStack(alignment: .leading, spacing: 4) {
                    Text(field.label)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .padding(.leading, 4)

                    HStack {
                        TextField("", text: $value)
                            .textFieldStyle(.plain)

                        Button("Browse") {
                            let panel = NSOpenPanel()
                            panel.canChooseDirectories = false
                            panel.canChooseFiles = true
                            panel.allowsMultipleSelection = false
                            if panel.runModal() == .OK {
                                value = panel.url?.path ?? ""
                            }
                        }
                        .buttonStyle(.bordered)
                        .controlSize(.small)
                    }
                    .padding(10)
                    .background(Color.black.opacity(0.2))
                    .clipShape(RoundedRectangle(cornerRadius: 8))
                }
            default:
                GlassTextField(title: field.label, text: $value)
            }
        }
    }
}
