#!/bin/bash
# ============================================================================
# CALL MANAGEMENT - macOS APP BUNDLE CREATOR
# ============================================================================
# This script creates a proper .app bundle from the published output
# Run this script on macOS after dotnet publish
# ============================================================================

set -e  # Exit on error

# Configuration
APP_NAME="Call Management"
APP_BUNDLE_NAME="CallManagement"
VERSION="1.0.0"
BUNDLE_ID="com.callmanagement.app"
COPYRIGHT="Copyright © 2024 Call Management. All rights reserved."

# Paths
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
RELEASE_DIR="$PROJECT_ROOT/release/macos"
PUBLISH_ARM64="$RELEASE_DIR/publish-arm64"
PUBLISH_X64="$RELEASE_DIR/publish-x64"
APP_DIR="$RELEASE_DIR/$APP_BUNDLE_NAME.app"

echo "============================================"
echo "  Call Management - macOS App Bundle Creator"
echo "============================================"
echo ""

# Function to create app bundle
create_app_bundle() {
    local PUBLISH_DIR=$1
    local ARCH=$2
    
    echo "[INFO] Creating app bundle for $ARCH..."
    
    # Clean previous bundle
    rm -rf "$APP_DIR"
    
    # Create directory structure
    mkdir -p "$APP_DIR/Contents/MacOS"
    mkdir -p "$APP_DIR/Contents/Resources"
    
    # Copy all published files to MacOS folder
    echo "[INFO] Copying application files..."
    cp -R "$PUBLISH_DIR/"* "$APP_DIR/Contents/MacOS/"
    
    # Copy Info.plist
    echo "[INFO] Creating Info.plist..."
    cp "$SCRIPT_DIR/Info.plist" "$APP_DIR/Contents/"
    
    # Create PkgInfo
    echo "APPL????" > "$APP_DIR/Contents/PkgInfo"
    
    # Copy icon if exists
    if [ -f "$PROJECT_ROOT/Assets/AppIcon.icns" ]; then
        echo "[INFO] Copying app icon..."
        cp "$PROJECT_ROOT/Assets/AppIcon.icns" "$APP_DIR/Contents/Resources/"
    else
        echo "[WARN] AppIcon.icns not found in Assets folder"
    fi
    
    # Set executable permissions
    echo "[INFO] Setting permissions..."
    chmod +x "$APP_DIR/Contents/MacOS/CallManagement"
    
    # Remove quarantine attribute (ad-hoc signing)
    echo "[INFO] Applying ad-hoc code signing..."
    codesign --force --deep --sign - "$APP_DIR" 2>/dev/null || {
        echo "[WARN] Code signing failed - app may trigger Gatekeeper warning"
    }
    
    echo "[SUCCESS] App bundle created: $APP_DIR"
}

# Check which architecture to build
if [ -d "$PUBLISH_ARM64" ]; then
    create_app_bundle "$PUBLISH_ARM64" "arm64"
elif [ -d "$PUBLISH_X64" ]; then
    create_app_bundle "$PUBLISH_X64" "x64"
else
    echo "[ERROR] No publish output found!"
    echo "Please run 'dotnet publish' first, or use the build-all.ps1 script"
    exit 1
fi

echo ""
echo "============================================"
echo "  App Bundle Complete!"
echo "============================================"
echo ""
echo "Location: $APP_DIR"
echo ""
echo "To create DMG, run: ./create-dmg.sh"
