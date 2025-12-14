import Foundation

// Models for parsing the Server-Driven UI JSON
struct FormLayout: Codable {
    let type: String
    let fields: [FormField]
    let actions: [FormAction]
}

struct FormField: Codable, Identifiable {
    let id: String
    let type: String
    let label: String
    let required: Bool
    let placeholder: String?
}

struct FormAction: Codable, Identifiable {
    let id: String
    let label: String
    let actionType: String
}

enum FieldType: String {
    case text = "Text"
    case folderPicker = "FolderPicker"
    case filePicker = "FilePicker"
}
