#!/bin/bash
# Linux Update Helper Script
# Arguments: PID NEW_PATH APP_PATH

PID=$1
NEW_PATH=$2
APP_PATH=$3

echo "Aether Update Helper (Linux)"
echo "Waiting for process $PID to exit..."

# Wait for the app to exit
while kill -0 "$PID" 2>/dev/null; do
    sleep 0.5
done

echo "Process exited. Updating application..."

# Remove old files and copy new
rm -rf "$APP_PATH"/*
cp -R "$NEW_PATH/"* "$APP_PATH/"

# Make main executable executable
chmod +x "$APP_PATH/Aether" 2>/dev/null || chmod +x "$APP_PATH/AetherBackend" 2>/dev/null

# Clean up temp files
rm -rf "$(dirname "$NEW_PATH")"

echo "Update complete. Relaunching..."

# Relaunch
"$APP_PATH/Aether" &
