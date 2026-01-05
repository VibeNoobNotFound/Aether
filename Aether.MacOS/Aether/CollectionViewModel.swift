import AetherIPC
import SwiftUI

struct CollectionViewModel: Identifiable, Hashable {
    let id: Int32
    let name: String
    let iconName: String
    let type: Aether_CollectionType
    let isSystem: Bool
    let platformFilter: String?
    let gameIds: [Int32]
    var sortOrder: Int32
    var isVisible: Bool
    let gameCount: Int

    // Computed unique identifier for SwiftUI
    var uniqueId: String { "col-\(id)" }

    init(from proto: Aether_Collection) {
        self.id = proto.id
        self.name = proto.name
        self.iconName = proto.iconName
        self.type = proto.type
        self.isSystem = proto.isSystem
        self.platformFilter = proto.platformFilter.isEmpty ? nil : proto.platformFilter
        self.gameIds = proto.gameIds.map { Int32($0) }
        self.sortOrder = proto.sortOrder
        self.isVisible = proto.isVisible
        self.gameCount = Int(proto.gameCount)
    }
}

struct CarouselConfig {
    let collectionId: Int32?
    let gameIds: [String]
    let maxGames: Int

    init(from proto: Aether_CarouselConfig) {
        self.collectionId = proto.hasCollectionID ? proto.collectionID : nil
        self.gameIds = proto.gameIds
        self.maxGames = Int(proto.maxGames)
    }
}
