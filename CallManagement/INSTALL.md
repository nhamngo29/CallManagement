# Call Management - Installation Guide

<p align="center">
  <strong>Version 1.0.0</strong><br>
  Ứng dụng quản lý cuộc gọi - Call Center Contact Management
</p>

---

## 📋 Table of Contents

- [Windows Installation](#-windows-installation)
- [macOS Installation](#-macos-installation)
- [Troubleshooting](#-troubleshooting)
- [Uninstallation](#-uninstallation)
- [Building from Source](#-building-from-source)

---

## 🪟 Windows Installation

### System Requirements
- Windows 10 (version 1809 or later) or Windows 11
- x64 processor
- 200 MB free disk space
- No .NET runtime required (self-contained)

### Installation Steps

1. **Download the installer**
   - Get `CallManagement-1.0.0-Setup.exe` from the release

2. **Run the installer**
   - Double-click `CallManagement-1.0.0-Setup.exe`
   - If Windows SmartScreen appears, click **More info** → **Run anyway**

3. **Follow the installation wizard**
   
   | Step | Action |
   |------|--------|
   | Welcome | Click **Next** |
   | License Agreement | Read and accept, click **Next** |
   | Select Destination | Choose install folder or keep default, click **Next** |
   | Select Start Menu Folder | Keep default or customize, click **Next** |
   | Additional Tasks | Check "Create desktop shortcut" if desired, click **Next** |
   | Ready to Install | Review settings, click **Install** |
   | Completing | Check "Launch Call Management", click **Finish** |

4. **Launch the app**
   - From Start Menu: **Call Management**
   - From Desktop: Double-click the **Call Management** shortcut
   - From install folder: `C:\Program Files\Call Management\CallManagement.exe`

### Windows SmartScreen Warning

Since the app is not signed with a code signing certificate, you may see a warning:

> "Windows protected your PC"

**To proceed:**
1. Click **More info**
2. Click **Run anyway**

This warning appears because the app hasn't been recognized by Microsoft SmartScreen. It's safe to proceed.

---

## 🍎 macOS Installation

### System Requirements
- macOS 11.0 (Big Sur) or later
- Apple Silicon (M1/M2/M3/M4) or Intel processor
- 300 MB free disk space
- No .NET runtime required (self-contained)

### Installation Steps

1. **Download the DMG**
   - Get `CallManagement-1.0.0.dmg` from the release

2. **Mount the DMG**
   - Double-click `CallManagement-1.0.0.dmg`
   - A new Finder window will open

3. **Install the app**
   - Drag **CallManagement.app** to the **Applications** folder
   - Wait for the copy to complete

4. **Eject the DMG**
   - Right-click the mounted volume → **Eject**
   - Or drag it to Trash

5. **Launch the app**
   - Open **Launchpad** → Click **Call Management**
   - Or open **Finder** → **Applications** → Double-click **CallManagement**

### macOS Gatekeeper Warning

On first launch, macOS may show:

> "CallManagement.app cannot be opened because it is from an unidentified developer"

**Solution 1: Right-click to Open**
1. Right-click (or Control-click) on **CallManagement.app**
2. Select **Open** from the context menu
3. Click **Open** in the dialog

**Solution 2: System Settings**
1. Go to **System Settings** → **Privacy & Security**
2. Scroll down to the Security section
3. Click **Open Anyway** next to the CallManagement message
4. Click **Open** in the confirmation dialog

**Solution 3: Terminal Command**
```bash
xattr -cr /Applications/CallManagement.app
```

### Why does this happen?

The app is not notarized with Apple. Notarization requires an Apple Developer account ($99/year) and a code signing certificate. For internal or personal use, bypassing Gatekeeper is safe.

---

## 🔧 Troubleshooting

### Windows Issues

| Problem | Solution |
|---------|----------|
| Installer won't start | Right-click → Run as administrator |
| App won't launch | Check Windows Event Viewer for errors |
| Missing DLL error | Reinstall the application |
| Antivirus blocks app | Add exception for install folder |

### macOS Issues

| Problem | Solution |
|---------|----------|
| "App is damaged" error | Run: `xattr -cr /Applications/CallManagement.app` |
| App crashes on launch | Check Console.app for crash logs |
| Can't drag to Applications | Check disk permissions |
| Rosetta 2 prompt (Intel) | Click Install if prompted |

### General Issues

| Problem | Solution |
|---------|----------|
| App data corrupted | Delete app data folder and restart |
| Excel import fails | Ensure file is .xlsx format |
| Settings not saved | Check write permissions to app data |

### App Data Locations

| Platform | Path |
|----------|------|
| Windows | `%LOCALAPPDATA%\CallManagement\` |
| macOS | `~/Library/Application Support/CallManagement/` |

---

## 🗑️ Uninstallation

### Windows

**Method 1: Control Panel**
1. Open **Control Panel** → **Programs** → **Programs and Features**
2. Find **Call Management** in the list
3. Click **Uninstall**
4. Follow the uninstall wizard

**Method 2: Settings**
1. Open **Settings** → **Apps** → **Installed apps**
2. Find **Call Management**
3. Click **⋯** → **Uninstall**

**Method 3: Start Menu**
1. Open Start Menu
2. Find **Call Management** folder
3. Click **Uninstall Call Management**

**Remove app data (optional):**
```powershell
Remove-Item -Recurse "$env:LOCALAPPDATA\CallManagement"
```

### macOS

**Method 1: Drag to Trash**
1. Open **Finder** → **Applications**
2. Drag **CallManagement.app** to **Trash**
3. Empty Trash

**Remove app data (optional):**
```bash
rm -rf ~/Library/Application\ Support/CallManagement
rm -rf ~/Library/Preferences/com.callmanagement.app.plist
```

---

## 🛠️ Building from Source

### Prerequisites

- .NET 9 SDK
- Git
- Windows: Inno Setup 6 (for installer)
- macOS: Xcode Command Line Tools

### Clone and Build

```bash
# Clone repository
git clone https://github.com/your-repo/call-management.git
cd call-management/CallManagement

# Restore dependencies
dotnet restore

# Build and run (development)
dotnet run

# Build release
./build-release.ps1 -Target all
```

### Build Commands

```powershell
# Build all platforms
./build-release.ps1

# Build Windows only
./build-release.ps1 -Target windows

# Build macOS only
./build-release.ps1 -Target macos

# Publish only (no installer)
./build-release.ps1 -Target publish-only
```

### Output Structure

```
release/
├── windows/
│   ├── publish/                    # Raw publish output
│   └── CallManagement-1.0.0-Setup.exe  # Windows installer
│
└── macos/
    ├── publish-arm64/              # Raw publish output (ARM)
    ├── CallManagement.app/         # App bundle (after script)
    ├── CallManagement-1.0.0.dmg    # DMG installer (after script)
    ├── create-app-bundle.sh        # Script to create .app
    ├── create-dmg.sh               # Script to create .dmg
    └── BUILD-DMG-README.txt        # Instructions
```

---

## 📞 Support

If you encounter issues:

1. Check the [Troubleshooting](#-troubleshooting) section
2. Search existing issues on GitHub
3. Create a new issue with:
   - OS version
   - App version
   - Error message or screenshot
   - Steps to reproduce

---

<p align="center">
  <strong>© 2024 Call Management. All rights reserved.</strong>
</p>
- Yêu cầu Windows 10/11 x64
- Không cần cài .NET Runtime (self-contained)

### macOS
- Yêu cầu macOS 11.0 (Big Sur) trở lên
- Chỉ hỗ trợ Apple Silicon (M1/M2/M3/M4)
- Nếu cần hỗ trợ Intel Mac, build với `-r osx-x64`

### Phân phối chuyên nghiệp
Để phân phối trên App Store hoặc tránh cảnh báo Gatekeeper:
- Cần Apple Developer Account ($99/năm)
- Code Signing certificate
- Notarization từ Apple
