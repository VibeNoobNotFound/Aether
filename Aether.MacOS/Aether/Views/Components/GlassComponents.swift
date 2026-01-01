import SwiftUI

// MARK: - Design Tokens

/// Unified "Liquid Glass" design tokens following macOS 26 language
enum LiquidGlass {
    static let cornerRadius: CGFloat = 16
    static let nestedCornerRadius: CGFloat = 12
    static let smallCornerRadius: CGFloat = 8
    static let padding: CGFloat = 12

    // Opacities for layered depth
    static let strokeOpacity: Double = 0.3
    static let highlightOpacity: Double = 0.4
    static let tintOpacity: Double = 0.12
    static let borderOpacity: Double = 0.2

    // Standard animation
    static let animation: Animation = .spring(response: 0.4, dampingFraction: 0.75)
}

// MARK: - Core Primitive: GlassCard

/// A foundational container that implements the "Liquid Glass" physical material effect.
/// Features: Layered translucency, refraction stroke, glow shadow, and interactive hover state.
struct GlassCard<Content: View>: View {
    let tint: Color?
    let padding: CGFloat
    let cornerRadius: CGFloat
    let isHoverable: Bool
    let action: (() -> Void)?
    @ViewBuilder let content: Content

    @State private var isHovered = false

    init(
        tint: Color? = nil,
        padding: CGFloat = LiquidGlass.padding,
        cornerRadius: CGFloat = LiquidGlass.cornerRadius,
        isHoverable: Bool = false,
        action: (() -> Void)? = nil,
        @ViewBuilder content: () -> Content
    ) {
        self.tint = tint
        self.padding = padding
        self.cornerRadius = cornerRadius
        self.isHoverable = isHoverable || action != nil
        self.action = action
        self.content = content()
    }

    var body: some View {
        Group {
            if let action = action {
                Button(action: action) {
                    innerContent
                }
                .buttonStyle(.plain)
            } else {
                innerContent
            }
        }
        .scaleEffect(isHovered && isHoverable ? 1.01 : 1.0)
        .animation(LiquidGlass.animation, value: isHovered)
        .onHover { padding in
            if isHoverable { isHovered = padding }
        }
    }

    private var innerContent: some View {
        content
            .padding(padding)
            .background {
                ZStack {
                    // 1. Base Material (Frost)
                    RoundedRectangle(cornerRadius: cornerRadius)
                        .fill(.ultraThinMaterial)

                    // 2. Tint Overlay (Dynamic Color)
                    if let tint = tint {
                        RoundedRectangle(cornerRadius: cornerRadius)
                            .fill(tint.opacity(LiquidGlass.tintOpacity))
                    }

                    // 3. Highlight Stroke (Simulated Refraction)
                    RoundedRectangle(cornerRadius: cornerRadius)
                        .strokeBorder(
                            LinearGradient(
                                colors: [
                                    .white.opacity(isHovered ? 0.6 : 0.4),  // Top-left light
                                    .white.opacity(0.1),
                                    .white.opacity(0.05),
                                ],
                                startPoint: .topLeading,
                                endPoint: .bottomTrailing
                            ),
                            lineWidth: 1
                        )
                }
            }
            // 4. Depth Shadows (Glow + Drop)
            .shadow(
                color: (tint ?? .black).opacity(isHovered ? 0.25 : 0.15),
                radius: isHovered ? 16 : 8,
                x: 0,
                y: 4
            )
    }
}

// MARK: - Components

struct GlassSection<Content: View>: View {
    let title: String
    let content: Content

    init(title: String, @ViewBuilder content: () -> Content) {
        self.title = title
        self.content = content()
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            if !title.isEmpty {
                Text(title)
                    .font(.subheadline.weight(.medium))
                    .foregroundStyle(.secondary)
                    .padding(.leading, 4)
            }

            GlassCard {
                VStack(spacing: 12) {
                    content
                }
            }
        }
    }
}

struct GlassTextField: View {
    let title: String
    @Binding var text: String

    @FocusState private var isFocused: Bool
    @State private var isHovered = false

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
                .background {
                    ZStack {
                        // Input Background (Darker than card)
                        RoundedRectangle(cornerRadius: LiquidGlass.nestedCornerRadius)
                            .fill(Color.black.opacity(0.2))

                        // Active Border
                        RoundedRectangle(cornerRadius: LiquidGlass.nestedCornerRadius)
                            .stroke(
                                Color.white.opacity(isFocused ? 0.5 : (isHovered ? 0.3 : 0.1)),
                                lineWidth: 1
                            )
                    }
                }
                .focused($isFocused)
                .onHover { isHovered = $0 }
                .animation(LiquidGlass.animation, value: isHovered)
                .animation(LiquidGlass.animation, value: isFocused)
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
            .background {
                ZStack {
                    // Tinted Glass Background
                    RoundedRectangle(cornerRadius: LiquidGlass.smallCornerRadius)
                        .fill(.ultraThinMaterial)

                    RoundedRectangle(cornerRadius: LiquidGlass.smallCornerRadius)
                        .fill(tint.opacity(0.4))

                    // Inner Highlight
                    RoundedRectangle(cornerRadius: LiquidGlass.smallCornerRadius)
                        .strokeBorder(
                            LinearGradient(
                                colors: [.white.opacity(0.4), .clear],
                                startPoint: .top,
                                endPoint: .bottom
                            ),
                            lineWidth: 1
                        )
                }
            }
            .shadow(color: tint.opacity(0.3), radius: 8, x: 0, y: 2)
        }
        .buttonStyle(GlassButtonStyle(color: tint))
    }
}

/// A button style that mimics the Liquid Glass effect
struct GlassButtonStyle: ButtonStyle {
    var color: Color = .blue

    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .scaleEffect(configuration.isPressed ? 0.96 : 1.0)
            .animation(LiquidGlass.animation, value: configuration.isPressed)
        // Note: The background styling is usually applied by the view itself,
        // but this style handles the interaction press effect.
    }
}
