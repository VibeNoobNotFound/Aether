# Aether 🌌

**The Ultimate Universal Game Launcher.**

Aether is an open-source, next-generation game library manager. It exists to solve one problem: **fragmentation**. Your games are scattered across Steam, Epic, GOG, App Store, and disc images. Aether brings them home.

> *Designed by VibeNoobNotFound.*
> *Powered by .NET 10 & Swift.*

---
<img width="670" alt="Screenshot 2026-01-14 at 6 04 42 PM" src="https://github.com/user-attachments/assets/de3f5bc4-5c59-49e2-8b24-c049d484228a" />
<img width="670" alt="Screenshot 2026-01-14 at 6 20 44 PM" src="https://github.com/user-attachments/assets/4ea8c2c7-39ad-435b-bcfe-ad5a7be144e9" />

<img width="670" alt="Screenshot 2026-01-14 at 6 08 35 PM" src="https://github.com/user-attachments/assets/6377c0e7-f778-45bf-984b-0c27db0d78a9" />

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
1.  Download the latest release.
2.  Drag to Applications.
3.  Launch Aether.
    > **Note**: Aether asks for **Administrator privileges** by default to scan your system without restriction (bypassing macOS Sandbox/TCC issues). You can configure this behavior in the source code if building yourself.

---

## 🌙 Nightly Builds

Automated builds are generated on every push to `main`. These are **unsigned** and intended for testing.

### Download
Go to [Actions](../../actions) → Select the latest successful **Nightly Build** → Download the `Aether_*.zip` artifact.

### Installation
Since nightly builds are unsigned, macOS will quarantine them. After unzipping, run:
```bash
xattr -cr /path/to/Aether.app
```
Then drag to Applications and launch.

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
