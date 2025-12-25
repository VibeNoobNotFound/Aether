# 🚀 Distributing Aether

This guide explains how to package and share Aether with others **without** a paid Apple Developer Account ("Ad-Hoc" / Unsigned distribution).

> [!WARNING]
> Since this is a free, open-source project and I (**NoobNotFound**) do not monetize it, I cannot justify (afford) the **$99/year** Apple Developer fee required for notarization.
>
> **Consequence:** macOS Gatekeeper will block the app by default. Your users must know how to bypass this check.

---

## 1. Bundle the Backend

First, ensure the .NET backend is compiled and embedded into the app.

```bash
# Run from repository root
./build_all.sh --bundle --version 1.0.0
```
This creates a self-contained executable `AetherBackend` inside `Aether.MacOS/Aether/Resources`.

## 2. Archive the App (Xcode)

1.  Open `Aether.MacOS/Aether.xcodeproj`.
2.  Select **Product** > **Archive**.
3.  Once the archive finishes, the **Organizer** window will open.
4.  Select your archive and click **Distribute App**.
5.  Select **Custom** (or "Copy App").
6.  Select **Copy App**.
7.  Save the `Aether.app` to your Desktop.

## 3. Compress for Sharing

Always zip the app before uploading to Google Drive, Discord, etc. This preserves file permissions.

```bash
# Terminal
cd ~/Desktop
zip -r Aether_v1.0.0.zip Aether.app
```

---

## 4. Instructions for Your Users

When people download your zip, they cannot just double-click to open it correctly on modern macOS. Give them these instructions:

### The "App is Damaged" or "Unidentified Developer" Error
macOS sees the app hasn't been notarized by Apple.

### Option A: The "Right-Click" Trick (Old macOS)
1.  **Right-Click** (or Control-Click) `Aether.app`.
2.  Select **Open**.
3.  Click **Open** in the dialog box.

### Option B: System Settings (macOS 13+)
1.  Double-click to open (and fail).
2.  Go to **System Settings** > **Privacy & Security**.
3.  Scroll down to the Security section.
4.  Click **Open Anyway** next to the message about Aether.
5.  Enter your password.

### Option C: The Nuclear Option (Terminal)
If macOS keeps saying the app is "Damaged", it's a quarantine issue.
1.  Open **Terminal**.
2.  Paste this command (don't hit enter yet): `xattr -cr `
3.  Drag `Aether.app` into the terminal window to fill the path.
4.  Hit Enter.
5.  Launch the app.
