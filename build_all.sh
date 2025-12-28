#!/bin/bash
set -e

# Configuration
FRAMEWORK="net10.0"
CONFIG="Debug"
BACKEND_DIR="Aether.Backend"
PLUGINS_OUT="$BACKEND_DIR/bin/$CONFIG/$FRAMEWORK/plugins"

echo "🚀 Starting Aether Build Script..."

# Function to clean and build project
build_project() {
    PROJECT_DIR=$1
    echo "📦 Building $PROJECT_DIR..."
    rm -rf "$PROJECT_DIR/bin" "$PROJECT_DIR/obj"
    dotnet build "$PROJECT_DIR" -c "$CONFIG" -f "$FRAMEWORK"
}

# 1. Clean and Build SDK
echo "------------- Building PluginSDK -------------"
build_project "Aether.PluginSDK"

# 2. Build Backend
echo "------------- Building Backend -------------"
build_project "Aether.Backend"

# 3. Create Plugins Directory
echo "------------- Preparing Plugins -------------"
mkdir -p "$PLUGINS_OUT"
echo "📂 Plugins directory: $PLUGINS_OUT"

# 4. Build and# Build Importers
build_project "Plugins/Official/Aether.Importers.Steam/Aether.Importers.Steam.csproj"
build_project "Plugins/Official/Aether.Importers.Gog/Aether.Importers.Gog.csproj"
build_project "Plugins/Official/Aether.Importers.Epic/Aether.Importers.Epic.csproj"
build_project "Plugins/Official/Aether.Importers.AppStore/Aether.Importers.AppStore.csproj"
build_project "Plugins/Official/Aether.Importers.Custom/Aether.Importers.Custom.csproj"
build_project "Plugins/Official/Aether.Importers.IGDB/Aether.Importers.IGDB.csproj"
build_project "Plugins/Official/Aether.Importers.Crossover/Aether.Importers.Crossover.csproj"
build_project "Plugins/Official/Aether.Importers.Web/Aether.Importers.Web.csproj"

# Copy Plugins to Backend Output
echo "------------- Preparing Plugins -------------"
PLUGIN_DIR="Aether.Backend/bin/Debug/net10.0/plugins"
mkdir -p "$PLUGIN_DIR"
echo "📂 Plugins directory: $PLUGIN_DIR"

copy_plugin() {
    local src_project=$1
    local dll_name=$2
    echo "------------- Processing $dll_name -------------"
    
    # Check if build output exists
    # Path logic: Plugins/Official/[Name]/bin/Debug/net10.0/[Name].dll
    local src_dll="Plugins/Official/$src_project/bin/Debug/net10.0/$dll_name.dll"
    
    if [ -f "$src_dll" ]; then
        cp "$src_dll" "$PLUGIN_DIR/"
        echo "✅ Deployed $dll_name.dll to plugins folder"
    else
        echo "❌ Error: Could not find $src_dll"
        exit 1
    fi
}

copy_plugin "Aether.Importers.Steam" "Aether.Importers.Steam"
copy_plugin "Aether.Importers.Gog" "Aether.Importers.Gog"
copy_plugin "Aether.Importers.Epic" "Aether.Importers.Epic"
copy_plugin "Aether.Importers.AppStore" "Aether.Importers.AppStore"
copy_plugin "Aether.Importers.Custom" "Aether.Importers.Custom"
copy_plugin "Aether.Importers.IGDB" "Aether.Importers.IGDB"
copy_plugin "Aether.Importers.Crossover" "Aether.Importers.Crossover"
copy_plugin "Aether.Importers.Web" "Aether.Importers.Web"

# Check for arguments
should_bundle=false
VERSION="1.0.0"

while [[ "$#" -gt 0 ]]; do
    case $1 in
        --bundle) should_bundle=true ;;
        --version) VERSION="$2"; shift ;;
        *) echo "Unknown parameter passed: $1"; exit 1 ;;
    esac
    shift
done

if [ "$should_bundle" = true ]; then
    echo "═══════════════════════════════════════════════════════════════════════════"
    echo "📦 Bundling Backend for macOS App Distribution (Version: $VERSION)"
    echo "═══════════════════════════════════════════════════════════════════════════"
    
    PUBLISH_DIR="./publish/macos-arm64"
    echo "------------- Publishing Backend -------------"
    rm -rf "$PUBLISH_DIR"
    
    # Publish as self-contained executable
    dotnet publish Aether.Backend -c Release -r osx-arm64 \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -o "$PUBLISH_DIR"
        
    # Standardize executable name (remove .Backend suffix to avoid confusion)
    mv "$PUBLISH_DIR/Aether.Backend" "$PUBLISH_DIR/AetherBackend"

    # Copy plugins to publish folder
    echo "📦 Copying plugins to publish folder..."
    cp -r "$PLUGIN_DIR" "$PUBLISH_DIR/plugins"

    # The PROPER way to embed executables on macOS:
    # Copy to a staging area that Xcode will pick up via a "Copy Files" build phase
    # Target: Contents/MacOS/ (same as main app executable)
    # This ensures the backend is code-signed with the app and avoids quarantine issues.
    
    XCODE_HELPER_DIR="./Aether.MacOS/Aether/HelperTools"
    echo "📦 Copying backend to Xcode helper tools directory..."
    mkdir -p "$XCODE_HELPER_DIR"
    
    # Clean and copy
    rm -rf "$XCODE_HELPER_DIR/AetherBackend"
    rm -f "$XCODE_HELPER_DIR"/*.dll
    
    cp "$PUBLISH_DIR/AetherBackend" "$XCODE_HELPER_DIR/AetherBackend"
    
    # Copy plugin DLLs directly (not in a subfolder) so they can be added individually to Xcode
    cp "$PUBLISH_DIR/plugins/"*.dll "$XCODE_HELPER_DIR/"
    
    echo "✅ Backend and plugins copied to $XCODE_HELPER_DIR"
    echo ""
    echo "══════════════════════════════════════════════════════════════════════════════"
    echo "⚠️  IMPORTANT: Xcode Configuration Required"
    echo "══════════════════════════════════════════════════════════════════════════════"
    echo "1. In Xcode, go to your app target > Build Phases"
    echo "2. Add a 'Copy Files' build phase"
    echo "3. Set Destination to 'Executables' (Contents/MacOS)"
    echo "4. Add ALL files from $XCODE_HELPER_DIR:"
    echo "   - AetherBackend (the executable)"
    echo "   - All .dll files (Aether.Importers.*.dll)"
    echo "5. Check 'Code Sign On Copy'"
    echo "══════════════════════════════════════════════════════════════════════════════"
else
    echo "ℹ️  Skipping bundling (use --bundle to enable)"
fi

echo "----------------------------------------------"
echo "🎉 Build Complete!"
if [ "$should_bundle" = true ]; then
    echo "👉 See instructions above to configure Xcode."
else
    echo "👉 You can now run the backend:"
    echo "   cd Aether.Backend && dotnet run"
fi

