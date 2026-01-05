import SwiftUI

// MARK: - Design Tokens

/// Unified "Liquid Glass" design tokens following macOS 26 language
enum LiquidGlass {
    static let cornerRadius: CGFloat = 16
    static let padding: CGFloat = 12
    static let sidebarPadding: CGFloat = 10
}

// MARK: - Core Primitive: GlassCard

/// A container that adopts the system "Liquid Glass" effect.
struct GlassCard<Content: View>: View {
    let tint: Color?
    let padding: CGFloat
    let cornerRadius: CGFloat
    let isHoverable: Bool

    @ViewBuilder let content: Content

    init(
        tint: Color? = nil,
        padding: CGFloat = LiquidGlass.padding,
        cornerRadius: CGFloat = LiquidGlass.cornerRadius,
        isHoverable: Bool = false,
        @ViewBuilder content: () -> Content
    ) {
        self.tint = tint
        self.padding = padding
        self.cornerRadius = cornerRadius
        self.isHoverable = isHoverable
        self.content = content()
    }

    var body: some View {
        content
            .padding(padding)
            .glassEffect(in: .rect(cornerRadius: cornerRadius))
            .tint(tint)
        // Use contentShape for interaction if needed, interactive() is standard on glassEffect in latest SDKs but if failing, we omit.
        // On macOS 26, glassEffect is interactive by default for the material area.
    }
}

// MARK: - Components

struct GlassSection<Content: View>: View {
    let title: String
    let content: Content

    @State private var isExpanded = true

    init(title: String, @ViewBuilder content: () -> Content) {
        self.title = title
        self.content = content()
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            if !title.isEmpty {
                Button {
                    withAnimation(.spring(response: 0.6, dampingFraction: 0.8)) {
                        isExpanded.toggle()
                    }
                } label: {
                    HStack(spacing: 4) {
                        Image(systemName: "chevron.right")
                            .font(.system(size: 10, weight: .bold))
                            .rotationEffect(.degrees(isExpanded ? 90 : 0))
                            .foregroundStyle(.secondary)

                        Text(title)
                            .font(.subheadline.weight(.medium))
                            .foregroundStyle(.secondary)

                        Spacer()
                    }
                    .padding(.leading, 4)
                    .contentShape(Rectangle())
                }
                .buttonStyle(.plain)
            }

            if isExpanded {
                GlassEffectContainer {
                    VStack(spacing: 12) {
                        content
                    }
                }
                .transition(.opacity.combined(with: .move(edge: .top)))
            }
        }
    }
}

struct GlassTextField: View {
    let title: String
    @Binding var text: String

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            if !title.isEmpty {
                Text(title)
                    .font(.caption.weight(.medium))
                    .foregroundStyle(.secondary)
                    .padding(.leading, 4)
            }

            TextField("", text: $text)
                .textFieldStyle(.plain)
                .padding(10)
                .glassEffect(in: .rect(cornerRadius: 12))
        }
    }
}

struct GlassButton: View {
    let title: String
    let icon: String?
    let action: () -> Void
    let tint: Color

    init(
        _ title: String, systemImage: String? = nil, tint: Color = .blue,
        action: @escaping () -> Void
    ) {
        self.title = title
        self.icon = systemImage
        self.action = action
        self.tint = tint
    }

    var body: some View {
        Button(action: action) {
            HStack(spacing: 6) {
                if let icon = icon {
                    Image(systemName: icon)
                }
                Text(title)
                    .fontWeight(.medium)
            }
            .padding(.horizontal, 16)
            .padding(.vertical, 8)
        }
        .buttonStyle(.glass)
        .tint(tint)
    }
}
