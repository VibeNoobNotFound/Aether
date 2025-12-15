import SwiftUI

/// A SwiftUI view that renders HTML content as styled text
struct HTMLText: View {
    let html: String

    @State private var attributedString: AttributedString = AttributedString("")

    var body: some View {
        Text(attributedString)
            .task {
                attributedString = parseHTML(html)
            }
    }

    private func parseHTML(_ html: String) -> AttributedString {
        // First, try to convert HTML to AttributedString
        guard let data = html.data(using: .utf8) else {
            return AttributedString(html)
        }

        do {
            let nsAttributedString = try NSAttributedString(
                data: data,
                options: [
                    .documentType: NSAttributedString.DocumentType.html,
                    .characterEncoding: String.Encoding.utf8.rawValue,
                ],
                documentAttributes: nil
            )

            // Convert to AttributedString and apply our styling
            var result = AttributedString(nsAttributedString)

            // Apply consistent font
            result.font = .body
            result.foregroundColor = .secondary

            return result
        } catch {
            // Fallback: strip HTML tags manually
            return AttributedString(stripHTMLTags(html))
        }
    }

    private func stripHTMLTags(_ html: String) -> String {
        // Remove HTML tags using regex
        var result = html

        // Replace common HTML entities
        let entities: [(String, String)] = [
            ("&nbsp;", " "),
            ("&amp;", "&"),
            ("&lt;", "<"),
            ("&gt;", ">"),
            ("&quot;", "\""),
            ("&#39;", "'"),
            ("<br>", "\n"),
            ("<br/>", "\n"),
            ("<br />", "\n"),
            ("<p>", "\n"),
            ("</p>", "\n"),
        ]

        for (entity, replacement) in entities {
            result = result.replacingOccurrences(
                of: entity, with: replacement, options: .caseInsensitive)
        }

        // Remove remaining HTML tags
        if let regex = try? NSRegularExpression(pattern: "<[^>]+>", options: .caseInsensitive) {
            let range = NSRange(result.startIndex..., in: result)
            result = regex.stringByReplacingMatches(
                in: result, options: [], range: range, withTemplate: "")
        }

        // Clean up multiple newlines
        while result.contains("\n\n\n") {
            result = result.replacingOccurrences(of: "\n\n\n", with: "\n\n")
        }

        return result.trimmingCharacters(in: .whitespacesAndNewlines)
    }
}

#Preview {
    HTMLText(
        html: "<b>Bold text</b> and <i>italic</i><br><br>New paragraph with <a href='#'>link</a>"
    )
    .padding()
}
