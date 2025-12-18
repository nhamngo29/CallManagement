#!/bin/bash
# ============================================================================
# CALL MANAGEMENT - macOS DMG CREATOR
# ============================================================================
# This script creates a professional DMG installer with:
# - App icon
# - Applications folder shortcut
# - Custom background (optional)
# - Drag-to-install experience
# ============================================================================

set -e  # Exit on error

# Configuration
APP_NAME="CallManagement"
DMG_NAME="CallManagement-1.0.0"
VOLUME_NAME="Call Management"
VERSION="1.0.0"

# Paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
RELEASE_DIR="$PROJECT_ROOT/release/macos"
APP_BUNDLE="$RELEASE_DIR/$APP_NAME.app"
DMG_OUTPUT="$RELEASE_DIR/$DMG_NAME.dmg"
DMG_TEMP="$RELEASE_DIR/temp_dmg"

echo "============================================"
echo "  Call Management - DMG Creator"
echo "============================================"
echo ""

# Check if app bundle exists
if [ ! -d "$APP_BUNDLE" ]; then
    echo "[ERROR] App bundle not found: $APP_BUNDLE"
    echo "Please run create-app-bundle.sh first"
    exit 1
fi

echo "[INFO] Creating DMG installer..."

# Clean up any existing temp directory and DMG
rm -rf "$DMG_TEMP"
rm -f "$DMG_OUTPUT"
rm -f "${DMG_OUTPUT%.dmg}-temp.dmg"

# Create temp directory for DMG contents
mkdir -p "$DMG_TEMP"

# Copy app bundle to temp directory
echo "[INFO] Copying app bundle..."
cp -R "$APP_BUNDLE" "$DMG_TEMP/"

# Create Applications symlink
echo "[INFO] Creating Applications shortcut..."
ln -s /Applications "$DMG_TEMP/Applications"

# Create README file
cat > "$DMG_TEMP/README.txt" << 'EOF'
═══════════════════════════════════════════════════════════════════════
                        CALL MANAGEMENT
                    Installation Instructions
═══════════════════════════════════════════════════════════════════════

HOW TO INSTALL:
1. Drag "CallManagement.app" to the "Applications" folder
2. Open the app from Launchpad or Applications folder

FIRST LAUNCH (Gatekeeper Warning):
If macOS shows "App is from an unidentified developer":
1. Go to System Settings → Privacy & Security
2. Scroll down and click "Open Anyway"
3. Or right-click the app → Open → Open

ALTERNATIVE: Run in Terminal:
   xattr -cr /Applications/CallManagement.app

HOW TO UNINSTALL:
1. Drag "CallManagement.app" from Applications to Trash
2. Delete app data (optional):
   rm -rf ~/Library/Application\ Support/CallManagement

═══════════════════════════════════════════════════════════════════════
                 © 2024 Call Management. All rights reserved.
═══════════════════════════════════════════════════════════════════════
EOF

# Calculate size needed for DMG (app size + 50MB buffer)
APP_SIZE=$(du -sm "$APP_BUNDLE" | cut -f1)
DMG_SIZE=$((APP_SIZE + 50))

echo "[INFO] Creating DMG (${DMG_SIZE}MB)..."

# Create temporary DMG
hdiutil create \
    -srcfolder "$DMG_TEMP" \
    -volname "$VOLUME_NAME" \
    -fs HFS+ \
    -fsargs "-c c=64,a=16,e=16" \
    -format UDRW \
    -size ${DMG_SIZE}m \
    "${DMG_OUTPUT%.dmg}-temp.dmg"

# Mount the temporary DMG
echo "[INFO] Configuring DMG appearance..."
MOUNT_DIR=$(hdiutil attach -readwrite -noverify "${DMG_OUTPUT%.dmg}-temp.dmg" | grep -E '^/dev/' | tail -1 | awk '{print $3}')

# Wait for mount
sleep 2

# Set DMG window appearance using AppleScript
osascript << EOF
tell application "Finder"
    tell disk "$VOLUME_NAME"
        open
        set current view of container window to icon view
        set toolbar visible of container window to false
        set statusbar visible of container window to false
        set bounds of container window to {400, 100, 920, 440}
        set viewOptions to the icon view options of container window
        set arrangement of viewOptions to not arranged
        set icon size of viewOptions to 80
        set position of item "$APP_NAME.app" of container window to {130, 150}
        set position of item "Applications" of container window to {390, 150}
        set position of item "README.txt" of container window to {260, 300}
        close
        open
        update without registering applications
        delay 2
    end tell
end tell
EOF

# Sync and unmount
sync
hdiutil detach "$MOUNT_DIR" -force

# Convert to compressed DMG
echo "[INFO] Compressing DMG..."
hdiutil convert \
    "${DMG_OUTPUT%.dmg}-temp.dmg" \
    -format UDZO \
    -imagekey zlib-level=9 \
    -o "$DMG_OUTPUT"

# Clean up
rm -f "${DMG_OUTPUT%.dmg}-temp.dmg"
rm -rf "$DMG_TEMP"

echo ""
echo "============================================"
echo "  DMG Created Successfully!"
echo "============================================"
echo ""
echo "Output: $DMG_OUTPUT"
echo "Size: $(du -h "$DMG_OUTPUT" | cut -f1)"
echo ""
echo "Users can now:"
echo "  1. Double-click the DMG to mount"
echo "  2. Drag the app to Applications"
echo "  3. Eject the DMG"
