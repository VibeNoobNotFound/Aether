# 🗺 Project Roadmap

Status: **Active Development**
Phase: **Alpha V0.8**

## 🌐 Cross-Platform Vision

Aether is designed to be the universal game launcher. While focused on macOS initially due to the lack of good alternatives, the backend is 100% cross-platform.

### 🍎 macOS (Current Focus)
*   **Status**: Alpha / Usable
*   **Tech**: SwiftUI (Native)
*   **Goal**: Reach feature parity with native Windows launchers (Playnite, GOG Galaxy) but with superior Apple aesthetics.

### 🪟 Windows (Scheduled: Phase 4)
*   **Status**: Planned
*   **Tech**: **WinUI 3** or **WPF** (Native)
*   **Strategy**: Use the existing `.NET` backend. Create a lightweight `Aether.Windows` frontend project that binds to the same gRPC server. We will target a Fluent Design System look.

### 🐧 Linux (Scheduled: Phase 5)
*   **Status**: Planned
*   **Tech**: **Avalonia UI** or **GTK#**
*   **Strategy**: Similar to Windows, we will provide a native Linux frontend. The `.NET` backend already works on Linux.

---

## 📅 Detailed Phases

### Phase 1: Foundation (Completed ✅)
*   [x] Hybrid Architecture (gRPC + .NET + Swift)
*   [x] Plugin System Base
*   [x] Steam Importer
*   [x] Epic Games Importer (Manifest parsing)
*   [x] App Store Importer

### Phase 2: Metadata & Polish (Completed ✅)
*   [x] IGDB Metadata Provider (Covers, Metadata)
*   [x] Steam Video/Trailer support (HLS/MP4)
*   [x] "Apple-style" Game Detail View (Parallax, Blur)
*   [x] Media Lightbox

### Phase 3: The "Power User" Update (In Progress 🚧)
*   [ ] **Custom Executable Arguments**: Edit launch parameters.
*   [ ] **Game Manuals**: Auto-download PDF manuals where available.
*   [ ] **Time Tracking**: Record start/end times for play sessions.
*   [ ] **Collections/Tags**: User-defined categories (e.g., "RPGs", "Finished").

### Phase 4: Emulation (Future)
*   [ ] **RetroArch Integration**: Auto-scan ROM folders.
*   [ ] **Core Management**: Download emulator cores directly within Aether.
*   [ ] **Save Sync**: Sync saves to Cloud providers (Dropbox/iCloud).

### Phase 5: Social (Future)
*   [ ] **Friend Activity**: See what friends are playing (discord integration).
*   [ ] **Reviews**: Write personal notes/reviews for games.
