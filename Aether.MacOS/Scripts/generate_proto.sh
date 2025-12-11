#!/bin/bash

# Configuration
PROTO_PATH="../../Protos/aether.proto"
OUTPUT_DIR="../AetherIPC/Sources/AetherIPC"

# Attempt to find protoc and plugins
PROTOC=$(which protoc)
PLUGIN_SWIFT=$(which protoc-gen-swift)
PLUGIN_GRPC=$(which protoc-gen-grpc-swift)

# Fallback to Homebrew paths if not in PATH
if [ -z "$PROTOC" ]; then
    if [ -f "/opt/homebrew/bin/protoc" ]; then PROTOC="/opt/homebrew/bin/protoc"; fi
    if [ -f "/usr/local/bin/protoc" ]; then PROTOC="/usr/local/bin/protoc"; fi
fi

if [ -z "$PLUGIN_SWIFT" ]; then
    if [ -f "/opt/homebrew/bin/protoc-gen-swift" ]; then PLUGIN_SWIFT="/opt/homebrew/bin/protoc-gen-swift"; fi
    if [ -f "/usr/local/bin/protoc-gen-swift" ]; then PLUGIN_SWIFT="/usr/local/bin/protoc-gen-swift"; fi
fi

if [ -z "$PLUGIN_GRPC" ]; then
    if [ -f "/opt/homebrew/bin/protoc-gen-grpc-swift" ]; then PLUGIN_GRPC="/opt/homebrew/bin/protoc-gen-grpc-swift"; fi
    if [ -f "/opt/homebrew/bin/protoc-gen-grpc-swift-2" ]; then PLUGIN_GRPC="/opt/homebrew/bin/protoc-gen-grpc-swift-2"; fi
    if [ -f "/usr/local/bin/protoc-gen-grpc-swift" ]; then PLUGIN_GRPC="/usr/local/bin/protoc-gen-grpc-swift"; fi
    if [ -f "/usr/local/bin/protoc-gen-grpc-swift-2" ]; then PLUGIN_GRPC="/usr/local/bin/protoc-gen-grpc-swift-2"; fi
fi

# Validation
if [ -z "$PROTOC" ] || [ -z "$PLUGIN_SWIFT" ] || [ -z "$PLUGIN_GRPC" ]; then
    echo "Error: Could not find required tools."
    echo "protoc: ${PROTOC:-MISSING}"
    echo "protoc-gen-swift: ${PLUGIN_SWIFT:-MISSING}"
    echo "protoc-gen-grpc-swift: ${PLUGIN_GRPC:-MISSING}"
    echo ""
    echo "Please run: brew install swift-protobuf grpc-swift"
    exit 1
fi

echo "Found tools:"
echo "protoc: $PROTOC"
echo "swift-plugin: $PLUGIN_SWIFT"
echo "grpc-plugin: $PLUGIN_GRPC"

# Ensure output directory exists based on relative path from script location
cd "$(dirname "$0")"
mkdir -p "$OUTPUT_DIR"

echo "Generating code from $PROTO_PATH..."

"$PROTOC" \
    --plugin=protoc-gen-grpc-swift="$PLUGIN_GRPC" \
    --proto_path=$(dirname "$PROTO_PATH") \
    --swift_out="$OUTPUT_DIR" \
    --swift_opt=Visibility=Public \
    --grpc-swift_out="$OUTPUT_DIR" \
    --grpc-swift_opt=Visibility=Public \
    "$PROTO_PATH"

if [ $? -eq 0 ]; then
    echo "Success! Generated files in $OUTPUT_DIR"
else
    echo "Failed to generate code."
    exit 1
fi
