# ============================================================================
# PUBLISH SCRIPT FOR CALL MANAGEMENT APP
# Builds for Windows x64 and macOS ARM64
# ============================================================================

param(
    [ValidateSet("all", "windows", "macos")]
    [string]$Target = "all"
)

$ErrorActionPreference = "Stop"
$ProjectDir = $PSScriptRoot
$PublishDir = Join-Path $ProjectDir "publish"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Call Management - Build Script" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Clean publish directory
if (Test-Path $PublishDir) {
    Write-Host "Cleaning publish directory..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $PublishDir
}
New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null

# ============================================================================
# BUILD FOR WINDOWS x64
# ============================================================================
function Build-Windows {
    Write-Host ""
    Write-Host "[Windows x64] Building..." -ForegroundColor Green
    
    $winOutput = Join-Path $PublishDir "win-x64"
    
    dotnet publish `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o $winOutput
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[Windows x64] Build successful!" -ForegroundColor Green
        Write-Host "  Output: $winOutput\CallManagement.exe" -ForegroundColor Gray
    } else {
        Write-Host "[Windows x64] Build failed!" -ForegroundColor Red
        exit 1
    }
}

# ============================================================================
# BUILD FOR macOS ARM64 (Apple Silicon)
# ============================================================================
function Build-MacOS {
    Write-Host ""
    Write-Host "[macOS ARM64] Building..." -ForegroundColor Green
    
    $macOutput = Join-Path $PublishDir "osx-arm64"
    
    dotnet publish `
        -c Release `
        -r osx-arm64 `
        --self-contained true `
        -o $macOutput
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[macOS ARM64] Build successful!" -ForegroundColor Green
        Write-Host "  Output: $macOutput" -ForegroundColor Gray
        
        # Create .app bundle structure info
        $appBundleScript = Join-Path $macOutput "create-app-bundle.sh"
        @"
#!/bin/bash
# Run this script on macOS to create the .app bundle

APP_NAME="CallManagement"
SCRIPT_DIR="`$( cd "`$( dirname "`${BASH_SOURCE[0]}" )" && pwd )"
APP_DIR="`$SCRIPT_DIR/`${APP_NAME}.app"

# Remove old .app if exists
rm -rf "`$APP_DIR"

# Create directory structure
mkdir -p "`$APP_DIR/Contents/MacOS"
mkdir -p "`$APP_DIR/Contents/Resources"

# Copy all files to MacOS folder
cp -R "`$SCRIPT_DIR/"* "`$APP_DIR/Contents/MacOS/" 2>/dev/null || true

# Remove the script from the app bundle
rm -f "`$APP_DIR/Contents/MacOS/create-app-bundle.sh"

# Create Info.plist
cat > "`$APP_DIR/Contents/Info.plist" << 'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>CallManagement</string>
    <key>CFBundleDisplayName</key>
    <string>Call Management</string>
    <key>CFBundleIdentifier</key>
    <string>com.yourcompany.callmanagement</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>CFBundleExecutable</key>
    <string>CallManagement</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>LSMinimumSystemVersion</key>
    <string>11.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>LSArchitecturePriority</key>
    <array>
        <string>arm64</string>
    </array>
</dict>
</plist>
PLIST

# Set executable permission
chmod +x "`$APP_DIR/Contents/MacOS/CallManagement"

echo "✅ Created `$APP_DIR"
echo ""
echo "To install:"
echo "  1. Drag CallManagement.app to /Applications"
echo "  2. First launch: Right-click → Open"
echo ""
echo "If blocked by Gatekeeper, run:"
echo "  xattr -cr /Applications/CallManagement.app"
"@ | Out-File -FilePath $appBundleScript -Encoding utf8 -NoNewline
        
        Write-Host "  Created create-app-bundle.sh for macOS" -ForegroundColor Gray
    } else {
        Write-Host "[macOS ARM64] Build failed!" -ForegroundColor Red
        exit 1
    }
}

# ============================================================================
# MAIN
# ============================================================================

switch ($Target) {
    "windows" { Build-Windows }
    "macos" { Build-MacOS }
    "all" {
        Build-Windows
        Build-MacOS
    }
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Build Complete!" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Output locations:" -ForegroundColor White
Write-Host "  Windows: $PublishDir\win-x64\CallManagement.exe" -ForegroundColor Gray
Write-Host "  macOS:   $PublishDir\osx-arm64\" -ForegroundColor Gray
Write-Host ""
Write-Host "For macOS installation:" -ForegroundColor Yellow
Write-Host "  1. Copy osx-arm64 folder to Mac" -ForegroundColor Gray
Write-Host "  2. Run: chmod +x create-app-bundle.sh && ./create-app-bundle.sh" -ForegroundColor Gray
Write-Host "  3. Drag CallManagement.app to Applications" -ForegroundColor Gray
