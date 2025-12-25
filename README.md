# Aether 🌌

**The Ultimate Universal Game Launcher.**

Aether is an open-source, next-generation game library manager. It exists to solve one problem: **fragmentation**. Your games are scattered across Steam, Epic, GOG, App Store, and disc images. Aether brings them home.

> *Designed by VibeNoobNotFound.*
> *Powered by .NET 10 & Swift.*

---

<img width="1348" height="1133" alt="Screenshot 2025-12-25 at 7 48 43 PM" src="https://github.com/user-attachments/assets/e3cd02da-5e2d-4129-8920-f9f6e92ddd3e" />
<img width="1569" height="1211" alt="Screenshot 2025-12-25 at 7 45 15 PM" src="https://github.com/user-attachments/assets/37822f2f-a5e6-4fff-ba31-f9318c52bff0" />
<img width="1122" height="1211" alt="Screenshot 2025-12-25 at 7 43 31 PM" src="https://github.com/user-attachments/assets/2ca5e2ab-506e-4c55-a004-b3e8c7f4b0b8" />

## 🌟 Features

### 🎮 Universal Library
Forget launching five different apps to check your collection.
*   **Unified Interface**: One grid to rule them all.
*   **Auto-Import**: Instantly detects games from **Steam**, **Epic Games Store**, and **macOS App Store**.
*   **Manual Import**: Add any executable, script, or App Bundle manually.
*   **Real-time Scanning**: Background watchers keep your library in sync without manual refreshes.

### 🖼 Premium Experience
We believe game launchers shouldn't look like spreadsheets.
*   **Native Aesthetics**: Built with SwiftUI on macOS for 120fps smooth animations and platform-perfect blurs.
*   **Immersive Detail View**: Full visual overhaul with parallax headers and logo overlays.
*   **Media Gallery**: Watch auto-fetched trailers (HLS/MP4) and browse high-res screenshots.
*   **Dark Mode**: Hand-crafted dark theme that looks stunning at night.

### 🧠 Intelligent Metadata
*   **IGDB Integration**: Uses the industry-standard database for accurate covers, metadata, and developer info.
*   **Customizable**: Don't like a cover? Drag and drop your own. Edit titles, descriptions, and video links manually.

### 🛡 System-Level Access
*   **Deep Scanning**: Runs as Root (optional) to bypass macOS TCC restrictions, ensuring it finds *every* game on your drive.
*   **Sandbox-Free**: Built to escape the walled garden and manage your entire system's library.

---

## 📚 Documentation

We believe in transparency. Dive into our detailed documentation:

*   **[🏗 Architecture](docs/ARCHITECTURE.md)**: How we mix C# and Swift without losing our minds.
*   **[🧩 Plugin Guide](docs/PLUGINS.md)**: Learn how to write your own Importers and Metadata Providers.
*   **[🚀 Distribution](docs/DISTRIBUTION.md)**: How to share the app without paying Apple $99.
*   **[🛣 Roadmap](docs/ROADMAP.md)**: Our plans for Windows, Linux, and Retro Emulation.
*   **[🛠 Build Guide](docs/BUILD.md)**: How to compile Aether and its plugins from source.

---

## 🚀 Quick Start

### For Users
1.  Download the latest release (Coming Soon).
2.  Drag to Applications.
3.  Launch Aether.
    > **Note**: Aether asks for **Administrator privileges** by default to scan your system without restriction (bypassing macOS Sandbox/TCC issues). You can configure this behavior in the source code if building yourself.

### For Developers
Aether is a hybrid application. You need both an Xcode environment and a .NET environment.

```bash
# 1. Generate local gRPC definitions
./generate_proto.sh

# 2. Build Backend & Plugins
./build_all.sh

# 3. Open Xcode and Run
open Aether.MacOS/Aether.xcodeproj
```

*See [Build Guide](docs/BUILD.md) for detailed prerequisites.*

---

## 🤝 Contributing

We welcome contributions! Whether you're a Swift UI wizard or a .NET backend guru, there's a place for you.
*   **Frontend**: `Aether.MacOS/` (SwiftUI)
*   **Backend**: `Aether.Backend/` (C# .NET 10)
*   **Plugins**: `Plugins/` (C# Class Libraries)

---

## © Copyright & Legal

**Developed by:** VibeNoobNotFound

**AI Co-Authors:**
*   **Gemini 3 Pro** (Architecture & Strategy)
*   **Claude 4.5 Opus** (Code Generation & emotional support)

*Copyright (c) 2025 VibeNoobNotFound. All rights reserved by our Robot Overlords.*
