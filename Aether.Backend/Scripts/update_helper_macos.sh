#!/bin/bash
# macOS Update Helper Script
# Arguments: PID NEW_PATH APP_PATH

PID=$1
NEW_PATH=$2
APP_PATH=$3

echo "Aether Update Helper (macOS)"
echo "Waiting for process $PID to exit..."

# Wait for the app to exit
while kill -0 "$PID" 2>/dev/null; do
    sleep 0.5
done

echo "Process exited. Updating application..."

# Backup old app (optional safety measure)
if [ -d "$APP_PATH" ]; then
    rm -rf "${APP_PATH}.old"
    mv "$APP_PATH" "${APP_PATH}.old"
fi

# Copy new files
cp -R "$NEW_PATH/"* "$(dirname "$APP_PATH")/"

# Clean up temp files
rm -rf "$(dirname "$NEW_PATH")"

# Clean up backup after successful copy
rm -rf "${APP_PATH}.old"

echo "Update complete. Relaunching..."

# Relaunch the app
open -a "$APP_PATH"
