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
build_project "Plugins/Official/Aether.Importers.Epic/Aether.Importers.Epic.csproj"
build_project "Plugins/Official/Aether.Importers.AppStore/Aether.Importers.AppStore.csproj"
build_project "Plugins/Official/Aether.Importers.Custom/Aether.Importers.Custom.csproj"

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
copy_plugin "Aether.Importers.Epic" "Aether.Importers.Epic"
copy_plugin "Aether.Importers.AppStore" "Aether.Importers.AppStore"
copy_plugin "Aether.Importers.Custom" "Aether.Importers.Custom"

echo "----------------------------------------------"
echo "🎉 Build Complete!"
echo "👉 You can now run the backend:"
echo "   cd Aether.Backend && dotnet run"
