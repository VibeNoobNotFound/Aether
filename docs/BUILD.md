# 🛠 Building Aether

## Prerequisites

### 🍏 macOS (Required for Frontend)
*   **Xcode 15+**: Required to compile the SwiftUI frontend.
*   **Command Line Tools**: `xcode-select --install`

### 💻 Backend Tools (Required for All)
*   **.NET 10 SDK**: The backend targets the bleeding edge .NET 10 preview. Download it from the [Microsoft .NET site](https://dotnet.microsoft.com/download/dotnet/10.0).
*   **Protobuf Compiler (`protoc`)**:
    *   Install via Homebrew: `brew install protobuf`
    *   **Swift Plugins**: `brew install swift-protobuf grpc-swift` (Required for generating Swift gRPC code).

---

## 🏗 The Build Process

The project uses a unified build script to handle the complexity of compiling `.NET` plugins, the `.NET` backend, and the `Swift` frontend dependencies.

### 1. Generate Protocol Buffers
**Crucial Step:** Before building code, you must generate the C# and Swift code from the `.proto` definitions.

```bash
# From the repository root
./generate_proto.sh
```
*This will run `protoc` and populate `Protos/Generated` and `Aether.MacOS/AetherIPC/Sources`.*

### 2. Build Backend & Plugins
Use the master build script to compile the backend and all official plugins:

```bash
./build_all.sh
```
*   Compiles `Aether.PluginSDK`
*   Compiles `Aether.Backend`
*   Compiles all plugins (`Steam`, `Epic`, `IGDB`, etc.)
*   Moves `.dll` files to the correct output `plugins/` directory.

### 3. Build & Run Frontend
1.  Open `Aether.MacOS/Aether.xcodeproj` (or `.xcworkspace` if using CocoaPods, though this project uses SPM).
2.  Select the **Aether** target.
3.  Press **Cmd + R** to Run.

> **Note:** The Xcode project is configured to look for the backend executable at `../Aether.Backend/bin/Debug/net10.0/Aether.Backend`. Ensure you ran `./build_all.sh` at least once!
