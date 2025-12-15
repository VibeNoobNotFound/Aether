# Aether 🌌

**The Ultimate Universal Game Launcher.**

Aether is an open-source, next-generation game library manager. It exists to solve one problem: **fragmentation**. Your games are scattered across Steam, Epic, GOG, App Store, and disc images. Aether brings them home.

> *Designed by VibeNoobNotFound.*
> *Powered by .NET 10 & Swift.*

---

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

---

## 📚 Documentation

We believe in transparency. Dive into our detailed documentation:

*   **[🏗 Architecture](docs/ARCHITECTURE.md)**: How we mix C# and Swift without losing our minds.
*   **[🧩 Plugin Guide](docs/PLUGINS.md)**: Learn how to write your own Importers and Metadata Providers.
*   **[🛣 Roadmap](docs/ROADMAP.md)**: Our plans for Windows, Linux, and Retro Emulation.
*   **[🛠 Build Guide](docs/BUILD.md)**: How to compile Aether and its plugins from source.

---

## 🚀 Quick Start

### For Users
1.  Download the latest release (Coming Soon).
2.  Drag to Applications.
3.  Launch and let Aether scan your drives.

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
