// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "AetherIPC",
    platforms: [
        .macOS(.v14)
    ],
    products: [
        .library(name: "AetherIPC", targets: ["AetherIPC"])
    ],
    dependencies: [
        .package(url: "https://github.com/grpc/grpc-swift-2.git", from: "2.0.0"),
        .package(url: "https://github.com/grpc/grpc-swift-nio-transport.git", from: "2.0.0"),
        .package(url: "https://github.com/grpc/grpc-swift-protobuf.git", from: "2.0.0"),
    ],
    targets: [
        .target(
            name: "AetherIPC",
            dependencies: [
                .product(name: "GRPCCore", package: "grpc-swift-2"),
                .product(name: "GRPCNIOTransportHTTP2", package: "grpc-swift-nio-transport"),
                .product(name: "GRPCProtobuf", package: "grpc-swift-protobuf"),
            ],

        )
    ]
)
