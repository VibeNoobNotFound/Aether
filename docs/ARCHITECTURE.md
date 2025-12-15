# Architecture Overview

Aether is built using a unique **Hybrid Architecture** that splits responsibilities between a high-performance backend and a native, platform-specific frontend.

## 🏗 The Hybrid Model

Unlike Electron apps that bundle an entire web browser, or pure cross-platform frameworks (Flutter/Avalonia) that sometimes feel distinct from the OS, Aether aims for 100% native look and feel while sharing core logic.

### 1. The Core Backend (`Aether.Backend`)
*   **Technology**: C# / .NET 10 (AOT Compatible)
*   **Role**: The brain of the operation.
*   **Responsibilities**:
    *   **Database**: Uses [LiteDB](https://www.litedb.org/) (NoSQL) to store game libraries, play history, and metadata.
    *   **Plugin System**: Dynamically loads `.dll` importers and metadata providers.
    *   **Business Logic**: Scanning directories, parsing manifest files, fetching API data.
    *   **gRPC Server**: Exposes all functionality via a Protocol Buffer interface.
*   **Why .NET?**: Mature ecosystem, blazing fast performance, excellent cross-platform support (Windows/Linux/macOS), and strong typing.

### 2. The Native Frontend (`Aether.MacOS`)
*   **Technology**: Swift / SwiftUI (Xcode)
*   **Role**: The face of the application.
*   **Responsibilities**:
    *   **Rendering**: Native macOS views, blurs, animations, and window management.
    *   **Process Management**: Spawns and manages the Backend process directly.
    *   **User Input**: Handles mouse, keyboard, and controller navigation.
*   **Integration**: Uses `grpc-swift` to generate a typed client that talks to the local backend server.

## 🔄 Data Flow

1.  **Startup**:
    *   The macOS App launches.
    *   It immediately finds the `Aether.Backend` executable inside its bundle and launches it as a subprocess.
    *   The Frontend waits for the gRPC server to start listening on `localhost:50051`.

2.  **User Action**:
    *   *Example*: User clicks "Scan Library".
    *   Frontend sends a typed `ScanRequest` via gRPC.
    *   Backend receives request -> Calls loaded Plugins -> Iterates Filesystem.
    *   Backend streams `ScanProgress` messages back to Frontend.
    *   Frontend updates UI in real-time.

3.  **Persistence**:
    *   All data changes (Metadata edits, Favorites) are sent to Backend.
    *   Backend writes to `library.db`.
    *   Frontend holds an ephemeral in-memory state (`AppState`) that is refreshed from Backend.

## 🧩 Plugin System

Plugins are isolated libraries implementing interfaces from `Aether.PluginSDK`:

*   `ILibraryImporter`: "I know how to find games in Folder X."
*   `IMetadataProvider`: "I know how to search for 'Halo' on the internet."

The Backend scans a `plugins/` directory at startup and uses Reflection to load these assemblies. This allows the community to write importers for obscure launchers without touching the core code.
