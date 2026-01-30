# Aether 🌌

**The Ultimate Universal Game Launcher.**

Aether is an open-source, next-generation game library manager. It exists to solve one problem: **fragmentation**. Your games are scattered across Steam, Epic, GOG, App Store, and disc images. Aether brings them home.

> *Designed by VibeNoobNotFound.*
> *Powered by .NET 10 & Swift with gRPC.*

[![Join our Discord](https://img.shields.io/badge/Discord-7289DA?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/NzJmjvvEgP) 
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/VibeNoobNotFound/Aether/nightly.yml?style=for-the-badge&label=Nightly%20Build)](https://github.com/VibeNoobNotFound/Aether/actions/workflows/nightly.yml) 
[![GitHub Release](https://img.shields.io/github/v/release/VibeNoobNotFound/Aether?include_prereleases&sort=date&display_name=release&style=for-the-badge)](https://github.com/VibeNoobNotFound/Aether/releases)

<img width="670" alt="Aether Library View" src="https://github.com/user-attachments/assets/de3f5bc4-5c59-49e2-8b24-c049d484228a" />

---


<details>
<summary>🌟 Features</summary>


### 🎮 Universal Library
*   **Unified Interface**: One grid to rule them all.
*   **Auto-Import**: Instantly detects games from **Steam**, **Epic Games Store**, and **macOS App Store**.
*   **Manual Import**: Add any executable, script, or App Bundle manually.
*   **Real-time Scanning**: Background watchers keep your library in sync.

### 🖼 Premium Experience
*   **Native Aesthetics**: Built with SwiftUI for 120fps animations and platform-perfect blurs.
*   **Immersive Detail View**: Parallax headers and logo overlays.
*   **Media Gallery**: Auto-fetched trailers (HLS/MP4) and high-res screenshots.
*   **Dark Mode**: Hand-crafted dark theme.

### 🧠 Intelligent Metadata
*   **IGDB Integration**: Industry-standard database for covers and metadata.
*   **Customizable**: Drag and drop your own covers. Edit titles and descriptions.

### 🛡 System-Level Access
*   **Deep Scanning**: Runs as Root (optional) to bypass macOS TCC restrictions.
*   **Sandbox-Free**: Manages your entire system's library.

</details>

## 🚀 Quick Start

### For Users
1.  Download:
    - **[Latest Stable](https://github.com/VibeNoobNotFound/Aether/releases/latest/download/Aether-macos.zip)** — Recommended
    - **[Pre-release](https://github.com/VibeNoobNotFound/Aether/releases)** — Beta features, may be unstable
2.  Drag to Applications.
3.  Launch Aether.
4.  First launch: Go to System Settings > Privacy & Security to allow Aether to be opened.  
(*This is needed because the app isn't signed with an Apple Developer certificate*)
#### If You Get "App Can't Be Opened" Error But Doesn't Show Up in Privacy & Security Settings,
```bash
# Remove quarantine attribute
xattr -d com.apple.quarantine /Applications/Aether.app

# If still doesn't work
xattr -cr /path/to/Aether.app
```
---

### Nightly Builds

Automated builds are generated on every push to `main`. These are **unsigned** and intended for testing.

**Download:** Go to [Actions](../../actions) → Select latest **Nightly Build** → Download artifact.

**Installation:** After unzipping, run:
```bash
xattr -cr /path/to/Aether.app
```

### For Developers
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


## 📚 Documentation

*   **[🏗 Architecture](docs/ARCHITECTURE.md)**: How we mix C# and Swift.
*   **[🧩 Plugin Guide](docs/PLUGINS.md)**: Write your own Importers and Metadata Providers.
*   **[🚀 Distribution](docs/DISTRIBUTION.md)**: Share the app without paying Apple $99.
*   **[🛣 Roadmap](docs/ROADMAP.md)**: Plans for Windows, Linux, and Retro Emulation.
*   **[🛠 Build Guide](docs/BUILD.md)**: Compile from source.

## 🤝 Contributing

We welcome contributions!
*   **Frontend**: `Aether.MacOS/` (SwiftUI)
*   **Backend**: `Aether.Backend/` (C# .NET 10)
*   **Plugins**: `Plugins/` (C# Class Libraries)

---


<details>
<summary>📸 More Screenshots</summary>

<img width="670" alt="Game Detail View" src="https://github.com/user-attachments/assets/4ea8c2c7-39ad-435b-bcfe-ad5a7be144e9" />

<img width="670" alt="Settings View" src="https://github.com/user-attachments/assets/6377c0e7-f778-45bf-984b-0c27db0d78a9" />

</details>


## © Copyright & Legal

**Developed by:** VibeNoobNotFound

**AI Co-Authors:**
*   **Gemini 3 Pro** (Architecture & Strategy)
*   **Claude 4.5 Opus** (Code Generation & emotional support)

*Copyright (c) 2025 VibeNoobNotFound. All rights reserved by our Robot Overlords.*
