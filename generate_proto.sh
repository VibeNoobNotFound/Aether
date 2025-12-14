#!/bin/bash
# Generate Swift Protobufs for Aether

PROTO_PATH="Protos"
OUT_DIR="Aether.MacOS/AetherIPC/Sources/AetherIPC"
PLUGIN_SWIFT="/opt/homebrew/Cellar/swift-protobuf/1.33.3_1/bin/protoc-gen-swift"
PLUGIN_GRPC="/opt/homebrew/Cellar/protoc-gen-grpc-swift/2.1.1_1/bin/protoc-gen-grpc-swift-2"

mkdir -p "$OUT_DIR"

protoc \
    --plugin=protoc-gen-swift="$PLUGIN_SWIFT" \
    --plugin=protoc-gen-grpc-swift="$PLUGIN_GRPC" \
    --swift_opt=Visibility=Public \
    --grpc-swift_opt=Visibility=Public \
    --swift_out="$OUT_DIR" \
    --grpc-swift_out="$OUT_DIR" \
    --proto_path="$PROTO_PATH" \
    "$PROTO_PATH/aether.proto"


echo "✅ Generated Swift Protos in $OUT_DIR"

