import Foundation

enum AppScreen: String, CaseIterable, Identifiable {
    case home = "Home"
    case library = "Library"
    case store = "Store"
    case settings = "Settings"

    var id: String { rawValue }

    var icon: String {
        switch self {
        case .home: return "house.fill"
        case .library: return "square.grid.2x2.fill"
        case .store: return "storefront.fill"
        case .settings: return "gearshape.fill"
        }
    }
}
