import AetherIPC
import SwiftUI

struct WidgetRenderer: View {
    let widget: Aether_UIWidget
    @Binding var formValues: [String: String]
    let onAction: (String, String) -> Void  // actionId, payload

    var body: some View {
        // Apply styling if present (padding, etc) in a wrapper
        contentView
            .padding(.vertical, widget.hasStyle ? CGFloat(widget.style.paddingVertical) : 0)
            .padding(.horizontal, widget.hasStyle ? CGFloat(widget.style.paddingHorizontal) : 0)
    }

    @ViewBuilder
    var contentView: some View {
        switch widget.content {
        case .text(let textWidget):
            renderText(textWidget)

        case .button(let buttonWidget):
            renderButton(buttonWidget)

        case .textInput(let inputWidget):
            renderTextInput(inputWidget)

        case .folderPicker(let folderWidget):
            renderFolderPicker(folderWidget)

        case .filePicker(let fileWidget):
            renderFilePicker(fileWidget)

        case .toggle(let toggleWidget):
            renderToggle(toggleWidget)

        case .container(let containerWidget):
            renderContainer(containerWidget)

        case .image(let imageWidget):
            renderImage(imageWidget)

        case .none:
            EmptyView()
        }
    }

    // MARK: - Components

    @ViewBuilder
    private func renderText(_ text: Aether_TextWidget) -> some View {
        let view = Text(text.text)
            .foregroundColor(Color(hex: text.color.isEmpty ? "#FFFFFF" : text.color))

        switch text.variant {
        case .headline:
            view.font(.title2).fontWeight(.bold)
        case .caption:
            view.font(.caption).foregroundColor(.secondary)
        case .sectionHeader:
            view.font(.headline).fontWeight(.semibold).padding(.top, 8)
        case .error:
            view.font(.caption).foregroundColor(.red)
        default:  // Body
            view.font(.body)
        }
    }

    @ViewBuilder
    private func renderButton(_ button: Aether_ButtonWidget) -> some View {
        GlassButton(button.label, systemImage: button.icon.isEmpty ? nil : button.icon) {
            onAction(button.actionID, button.payloadJson)
        }
    }

    @ViewBuilder
    private func renderTextInput(_ input: Aether_TextInputWidget) -> some View {
        if input.isSecure {
            VStack(alignment: .leading, spacing: 6) {
                Text(input.label)
                    .font(.caption.weight(.medium))
                    .foregroundStyle(.secondary)
                    .padding(.leading, 4)
                SecureField(
                    "",
                    text: Binding(
                        get: { formValues[input.boundFieldID] ?? input.initialValue },
                        set: { formValues[input.boundFieldID] = $0 }
                    )
                )
                .textFieldStyle(.plain)
                .padding(10)
                .background(Color.black.opacity(0.2))
                .cornerRadius(12)
            }
        } else {
            GlassTextField(
                title: input.label,
                text: Binding(
                    get: { formValues[input.boundFieldID] ?? input.initialValue },
                    set: { formValues[input.boundFieldID] = $0 }
                )
            )
        }
    }

    @ViewBuilder
    private func renderFolderPicker(_ picker: Aether_FolderPickerWidget) -> some View {
        VStack(alignment: .leading, spacing: 8) {
            Text(picker.label).font(.caption).foregroundColor(.secondary)
            HStack {
                Text(formValues[picker.boundFieldID] ?? "Select Folder...")
                    .font(.body)
                    .foregroundColor(
                        formValues[picker.boundFieldID]?.isEmpty == false ? .primary : .secondary
                    )
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .padding(10)
                    .background(Color.black.opacity(0.2))
                    .cornerRadius(8)

                Button(action: {
                    let panel = NSOpenPanel()
                    panel.canChooseFiles = false
                    panel.canChooseDirectories = true
                    panel.allowsMultipleSelection = false
                    if panel.runModal() == .OK {
                        formValues[picker.boundFieldID] = panel.url?.path
                    }
                }) {
                    Image(systemName: "folder")
                        .padding(10)
                        .background(Color.blue.opacity(0.3))
                        .cornerRadius(8)
                }
                .buttonStyle(.plain)
            }
        }
    }

    @ViewBuilder
    private func renderFilePicker(_ picker: Aether_FilePickerWidget) -> some View {
        VStack(alignment: .leading, spacing: 8) {
            Text(picker.label).font(.caption).foregroundColor(.secondary)
            HStack {
                Text(formValues[picker.boundFieldID] ?? "Select File...")
                    .font(.body)
                    .foregroundColor(
                        formValues[picker.boundFieldID]?.isEmpty == false ? .primary : .secondary
                    )
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .padding(10)
                    .background(Color.black.opacity(0.2))
                    .cornerRadius(8)

                Button(action: {
                    let panel = NSOpenPanel()
                    panel.canChooseFiles = true
                    panel.canChooseDirectories = false
                    panel.allowsMultipleSelection = false

                    if panel.runModal() == .OK {
                        formValues[picker.boundFieldID] = panel.url?.path
                    }
                }) {
                    Image(systemName: "doc")
                        .padding(10)
                        .background(Color.blue.opacity(0.3))
                        .cornerRadius(8)
                }
                .buttonStyle(.plain)
            }
        }
    }

    @ViewBuilder
    private func renderToggle(_ toggle: Aether_ToggleWidget) -> some View {
        Toggle(
            toggle.label,
            isOn: Binding(
                get: { (formValues[toggle.boundFieldID] ?? String(toggle.initialValue)) == "true" },
                set: { formValues[toggle.boundFieldID] = String($0) }
            )
        )
        .toggleStyle(SwitchToggleStyle(tint: .blue))
    }

    @ViewBuilder
    private func renderImage(_ image: Aether_ImageWidget) -> some View {
        if let url = URL(string: image.url) {
            AsyncImage(url: url) { image in
                image.resizable().scaledToFit()
            } placeholder: {
                ProgressView()
            }
            .cornerRadius(8)
        }
    }

    @ViewBuilder
    private func renderContainer(_ container: Aether_ContainerWidget) -> some View {
        let content = Group {
            ForEach(container.children, id: \.id) { child in
                WidgetRenderer(widget: child, formValues: $formValues, onAction: onAction)
            }

            // Render Actions associated with container
            if !container.actions.isEmpty {
                HStack(spacing: 12) {
                    ForEach(container.actions, id: \.id) { action in
                        GlassButton(
                            action.label,
                            tint: action.type == "Submit" ? .blue : .white.opacity(0.2)
                        ) {
                            onAction(action.id, action.type)
                        }
                    }
                }
                .padding(.top, 8)
            }
        }

        if container.orientation == .horizontal {
            HStack(spacing: 12) { content }
        } else {
            VStack(alignment: .leading, spacing: 12) { content }
        }
    }
}

// Helper for Color Hex
extension Color {
    init(hex: String) {
        let hex = hex.trimmingCharacters(in: CharacterSet.alphanumerics.inverted)
        var int: UInt64 = 0
        Scanner(string: hex).scanHexInt64(&int)
        let a: UInt64
        let r: UInt64
        let g: UInt64
        let b: UInt64
        switch hex.count {
        case 3:  // RGB (12-bit)
            (a, r, g, b) = (255, (int >> 8) * 17, (int >> 4 & 0xF) * 17, (int & 0xF) * 17)
        case 6:  // RGB (24-bit)
            (a, r, g, b) = (255, int >> 16, int >> 8 & 0xFF, int & 0xFF)
        case 8:  // ARGB (32-bit)
            (a, r, g, b) = (int >> 24, int >> 16 & 0xFF, int >> 8 & 0xFF, int & 0xFF)
        default:
            (a, r, g, b) = (1, 1, 1, 0)
        }

        self.init(
            .sRGB,
            red: Double(r) / 255,
            green: Double(g) / 255,
            blue: Double(b) / 255,
            opacity: Double(a) / 255
        )
    }
}
