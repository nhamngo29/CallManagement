# ============================================================================
# CALL MANAGEMENT - MASTER BUILD PIPELINE
# ============================================================================
param(
    [ValidateSet("all", "windows", "macos", "publish-only")]
    [string]$Target = "all",
    
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    
    [switch]$SkipPublish,
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"

# Configuration
$AppName = "CallManagement"
$Version = "1.0.0"
$ProjectDir = $PSScriptRoot
$ReleaseDir = Join-Path $ProjectDir "release"
$WindowsRID = "win-x64"
$MacOSArmRID = "osx-arm64"

function Write-Step { param([string]$Message); Write-Host "`n=== $Message ===" -ForegroundColor Cyan }
function Write-Success { param([string]$Message); Write-Host "[OK] $Message" -ForegroundColor Green }
function Write-Info { param([string]$Message); Write-Host "[INFO] $Message" -ForegroundColor Gray }

function Clean-ReleaseDir {
    Write-Step "Cleaning release directory"
    if (Test-Path $ReleaseDir) { Remove-Item -Recurse -Force $ReleaseDir }
    New-Item -ItemType Directory -Path $ReleaseDir -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $ReleaseDir "windows") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $ReleaseDir "macos") -Force | Out-Null
    Write-Success "Release directory prepared"
}

function Publish-Windows {
    Write-Step "Publishing for Windows ($WindowsRID)"
    $outputDir = Join-Path $ReleaseDir "windows\publish"
    
    dotnet publish -c $Configuration -r $WindowsRID --self-contained true -p:PublishSingleFile=false -o $outputDir
    
    if ($LASTEXITCODE -ne 0) { throw "Windows publish failed" }
    Write-Success "Windows publish complete: $outputDir"
    return $outputDir
}

function Publish-MacOS {
    param([string]$RuntimeId)
    Write-Step "Publishing for macOS ($RuntimeId)"
    $outputDir = Join-Path $ReleaseDir "macos\publish-$($RuntimeId -replace 'osx-', '')"
    
    dotnet publish -c $Configuration -r $RuntimeId --self-contained true -p:PublishSingleFile=false -o $outputDir
    
    if ($LASTEXITCODE -ne 0) { throw "macOS publish failed" }
    Write-Success "macOS publish complete: $outputDir"
    return $outputDir
}

function Build-WindowsInstaller {
    Write-Step "Building Windows Installer (Inno Setup)"
    
    $innoSetupPath = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
    
    if (-not $innoSetupPath) {
        Write-Host "[WARN] Inno Setup not found!" -ForegroundColor Yellow
        Write-Host "Download: https://jrsoftware.org/isdl.php" -ForegroundColor Cyan
        return $false
    }
    
    Write-Info "Using Inno Setup: $innoSetupPath"
    $issFile = Join-Path $ProjectDir "installer\windows\CallManagement.iss"
    
    & $innoSetupPath $issFile
    
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed" }
    Write-Success "Windows installer created"
    return $true
}

function Prepare-MacOSDMG {
    Write-Step "Preparing macOS DMG Scripts"
    $macosReleaseDir = Join-Path $ReleaseDir "macos"
    $scriptsSource = Join-Path $ProjectDir "installer\macos"
    
    Copy-Item (Join-Path $scriptsSource "create-app-bundle.sh") -Destination $macosReleaseDir -Force
    Copy-Item (Join-Path $scriptsSource "create-dmg.sh") -Destination $macosReleaseDir -Force
    Copy-Item (Join-Path $scriptsSource "Info.plist") -Destination $macosReleaseDir -Force
    
    $readme = @"
macOS DMG BUILD INSTRUCTIONS
=============================

1. Copy this 'macos' folder to a macOS machine
2. Open Terminal:
   cd /path/to/release/macos
   chmod +x *.sh
   ./create-app-bundle.sh
   ./create-dmg.sh

Output: CallManagement-1.0.0.dmg
"@
    $readme | Out-File -FilePath (Join-Path $macosReleaseDir "BUILD-DMG-README.txt") -Encoding UTF8
    Write-Success "macOS build scripts prepared"
}

# Main
Write-Host "`nCALL MANAGEMENT - BUILD PIPELINE v$Version" -ForegroundColor Magenta
Write-Host "Target: $Target | Config: $Configuration`n"

$startTime = Get-Date

try {
    if (-not $SkipPublish) { Clean-ReleaseDir }
    
    if (-not $SkipPublish) {
        switch ($Target) {
            "windows" { Publish-Windows }
            "macos" { Publish-MacOS -RuntimeId $MacOSArmRID }
            "publish-only" { Publish-Windows; Publish-MacOS -RuntimeId $MacOSArmRID }
            "all" { Publish-Windows; Publish-MacOS -RuntimeId $MacOSArmRID }
        }
    }
    
    if ($Target -ne "publish-only") {
        switch ($Target) {
            "windows" { Build-WindowsInstaller }
            "macos" { Prepare-MacOSDMG }
            "all" { Build-WindowsInstaller; Prepare-MacOSDMG }
        }
    }
    
    $elapsed = (Get-Date) - $startTime
    Write-Host "`n=== BUILD COMPLETE ===" -ForegroundColor Green
    Write-Host "Time: $([math]::Round($elapsed.TotalSeconds, 1))s"
    Write-Host "Output: $ReleaseDir"
    
    if ($Target -eq "all" -or $Target -eq "windows") {
        $installer = Join-Path $ReleaseDir "windows\$AppName-$Version-Setup.exe"
        if (Test-Path $installer) { Write-Host "Windows: $installer" -ForegroundColor Green }
        else { Write-Host "Windows: Install Inno Setup for installer" -ForegroundColor Yellow }
    }
    if ($Target -eq "all" -or $Target -eq "macos") {
        Write-Host "macOS: See release\macos\BUILD-DMG-README.txt"
    }
}
catch {
    Write-Host "`n=== BUILD FAILED ===" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}
