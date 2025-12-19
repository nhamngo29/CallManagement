# Call Management

A cross-platform desktop application for managing phone call sessions, tracking call results, and sending daily reports via Telegram. Built for call center teams and sales professionals who need an efficient way to organize contacts, monitor call outcomes, and automate reporting.

---

## Table of Contents

- [Key Features](#key-features)
- [Screenshots](#screenshots)
- [Technology Stack](#technology-stack)
- [System Requirements](#system-requirements)
- [Installation Guide](#installation-guide)
- [First-Time Usage Guide](#first-time-usage-guide)
- [Excel Import and Export Format](#excel-import-and-export-format)
- [Daily Report (Telegram)](#daily-report-telegram)
- [Settings](#settings)
- [Data Storage](#data-storage)
- [Project Structure](#project-structure)
- [Development Setup](#development-setup)
- [Build and Release](#build-and-release)
- [Known Limitations](#known-limitations)
- [Roadmap](#roadmap)
- [License](#license)

---

## Key Features

- **Excel Import and Export**: Import contacts from `.xlsx` files and export call results with full details
- **Session Management**: Create, save, and manage multiple call sessions independently
- **Call Status Tracking**: Mark contacts with statuses including Interested, Not Interested, No Answer, Busy, and Invalid Number
- **Daily Report**: Generate and send daily call summaries manually or automatically via Telegram
- **Telegram Bot Integration**: Send reports directly to Telegram chats or groups using Bot API
- **Dark and Light Mode**: Switch between themes based on preference
- **Toast Notifications**: In-app notifications for important events and actions
- **Offline Local Database**: All data stored locally using SQLite with no internet required for core features
- **Cross-Platform**: Runs on Windows and macOS with native look and feel

---

## Screenshots

_(Add screenshots here)_

---

## Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 9 |
| UI Framework | Avalonia UI 11.3 |
| Architecture | MVVM (CommunityToolkit.Mvvm 8.2) |
| Database | SQLite (Microsoft.Data.Sqlite 9.0) |
| Excel Processing | ClosedXML 0.104 |
| Reporting | Telegram Bot API (HTTP) |

---

## System Requirements

### Windows

- Windows 10 (version 1809 or later) or Windows 11
- x64 processor (64-bit)
- 200 MB free disk space
- No .NET runtime installation required (self-contained)

### macOS

- macOS 11.0 (Big Sur) or later
- Apple Silicon (M1/M2/M3/M4) or Intel processor
- 300 MB free disk space
- No .NET runtime installation required (self-contained)

---

## Installation Guide

### Windows

1. **Download the installer**
   - Get `CallManagement-1.0.0-Setup.exe` from the release page

2. **Run the installer**
   - Double-click the downloaded file
   - If Windows SmartScreen appears, click **More info** then **Run anyway**

3. **Follow the installation wizard**
   - Accept the license agreement
   - Choose installation location (default recommended)
   - Select whether to create a desktop shortcut
   - Click **Install** and wait for completion

4. **Launch the application**
   - From Start Menu: Search for **Call Management**
   - From Desktop: Double-click the **Call Management** shortcut

### macOS

1. **Download the DMG**
   - Get `CallManagement-1.0.0.dmg` from the release page

2. **Mount and install**
   - Double-click the DMG file to mount it
   - Drag **CallManagement.app** to the **Applications** folder

3. **Handle Gatekeeper warning (first launch)**
   - If macOS blocks the app, go to **System Preferences** > **Security & Privacy**
   - Click **Open Anyway** next to the message about CallManagement
   - Alternatively, right-click the app and select **Open**

4. **Launch the application**
   - Open **Launchpad** and click **Call Management**
   - Or open **Finder** > **Applications** > **CallManagement**

---

## First-Time Usage Guide

### Step 1: Import Contacts from Excel

1. Click **Import Excel** button in the toolbar
2. Select your `.xlsx` file containing contact data
3. Review the import summary showing successful and skipped entries

### Step 2: Start Calling and Mark Status

1. Select a contact from the list
2. After each call, click the appropriate status button:
   - **Interested** - Customer shows interest
   - **Not Interested** - Customer declines
   - **No Answer** - Call not picked up
   - **Busy** - Line is busy
   - **Invalid** - Number does not exist

### Step 3: Save Your Session

1. Sessions are automatically saved to the local database
2. Use **Export Excel** to create a backup file with all call results
3. Previous sessions appear in the sidebar for easy access

### Step 4: Send Daily Report

1. Click **Send Daily Report** to generate a summary
2. Preview the report content before sending
3. Confirm to send via Telegram

### Step 5: Configure Telegram and Auto Report

1. Open **Settings** from the menu
2. Enter your Telegram Bot Token and Chat ID
3. Enable **Auto Send Daily Report** and set the preferred time
4. The app will automatically send reports at the scheduled time

---

## Excel Import and Export Format

### Import Format

The import process automatically detects columns by header name. Headers are case-insensitive and support both English and Vietnamese.

| Column | Required | Accepted Headers |
|--------|----------|------------------|
| Name | Yes | Name, Tên, Họ tên, Full Name, Khách hàng |
| Phone Number | Yes | Phone, SĐT, Số điện thoại, Mobile, Tel |
| Company | No | Company, Công ty, Doanh nghiệp, Tổ chức |
| Note | No | Note, Ghi chú, Mô tả, Comment |

**Phone number validation**: Only digits allowed, 9-11 characters in length.

### Export Format

Exported files include all contact information plus call results.

| Column | Description |
|--------|-------------|
| Name | Contact name |
| Phone Number | Phone number |
| Company | Company name (if available) |
| Note | Notes added during the call |
| Status | Call result status |

---

## Daily Report (Telegram)

Daily reports are sent as formatted Markdown messages containing:

- **Report date and time**
- **Summary statistics**:
  - Total calls made
  - Interested count
  - Not Interested count
  - No Answer count
  - Other statuses
- **Contact list**: Names and phone numbers grouped by status

### Sample Report Format

```
DAILY CALL REPORT
Date: 18/12/2025

SUMMARY
- Total Calls: 50
- Interested: 12
- Not Interested: 25
- No Answer: 8
- Busy: 3
- Invalid: 2

INTERESTED CONTACTS
1. Nguyen Van A - 0901234567
2. Tran Thi B - 0912345678
...
```

---

## Settings

Access settings through the gear icon or menu.

### Theme

- **Light Mode**: Default bright theme
- **Dark Mode**: Dark theme for low-light environments
- **System**: Follow operating system preference

### Telegram Configuration

- **Bot Token**: Token from BotFather (format: `123456789:ABCdefGHI...`)
- **Chat ID**: Target chat or group ID for reports

### Auto Daily Report

- **Enable/Disable**: Toggle automatic report sending
- **Send Time**: Set the time for automatic daily reports (default: 18:00)

### Security

- **Password Protection**: Settings are protected by a security password

---

## Data Storage

All application data is stored locally on your computer.

### Storage Location

- **Windows**: `C:\Users\{username}\AppData\Local\CallManagement\`
- **macOS**: `/Users/{username}/.local/share/CallManagement/`

### Database

- **File**: `CallManagement.db` (SQLite format)
- **Content**: Call sessions, contacts, settings, and preferences
- **Persistence**: Data remains after application restart or update

### Offline Operation

- No internet connection required for core functionality
- Internet only needed for Telegram report sending

---

## Project Structure

```
CallManagement/
├── Views/              # AXAML UI files and code-behind
├── ViewModels/         # MVVM view models with commands
├── Models/             # Data models and enums
├── Services/           # Business logic and data access
│   ├── DatabaseService.cs      # SQLite operations
│   ├── ExcelService.cs         # Import/export logic
│   ├── TelegramReportService.cs
│   ├── DailyReportService.cs
│   └── SettingsService.cs
├── Converters/         # Value converters for UI binding
├── Styles/             # AXAML style resources
│   ├── Colors.axaml
│   ├── Controls.axaml
│   ├── Typography.axaml
│   └── Animations.axaml
├── Assets/             # Images and resources
└── installer/          # Platform-specific installers
```

---

## Development Setup

### Prerequisites

- .NET 9 SDK or later
- IDE: Visual Studio 2022, JetBrains Rider, or VS Code with C# extension

### Clone and Run

```bash
git clone <repository-url>
cd CallManagement
dotnet restore
dotnet run
```

### Build Debug Version

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

---

## Build and Release

### Windows Installer

The project uses Inno Setup for Windows installers.

```powershell
# Publish self-contained executable
dotnet publish -c Release -r win-x64 --self-contained true

# Run Inno Setup script
iscc installer/windows/CallManagement.iss
```

Output: `CallManagement-1.0.0-Setup.exe`

### macOS DMG

```bash
# Publish for macOS
dotnet publish -c Release -r osx-arm64 --self-contained true

# Create app bundle and DMG
./installer/macos/create-app-bundle.sh
./installer/macos/create-dmg.sh
```

Output: `CallManagement-1.0.0.dmg`

### Publish Options

| Option | Description |
|--------|-------------|
| `--self-contained true` | Include .NET runtime (no installation required) |
| `-r win-x64` | Target Windows 64-bit |
| `-r osx-arm64` | Target macOS Apple Silicon |
| `-r osx-x64` | Target macOS Intel |

---

## Known Limitations

- **Auto report requires app running**: Scheduled reports only send when the application is open
- **Telegram message length limit**: Reports with many contacts may be split into multiple messages (4096 character limit)
- **Local data only**: No cloud synchronization; data exists only on the installed device
- **Single user**: No multi-user or role-based access control
- **Excel format**: Only `.xlsx` files supported for import (not `.xls` or `.csv`)

---

## Roadmap

Future improvements under consideration:

- Weekly and monthly report generation
- Cloud backup and synchronization
- Multi-user support with role-based access
- CSV import support
- Call scheduling and reminders
- Statistics dashboard with charts
- Export to PDF format

---

## License

MIT License

Copyright (c) 2024 Call Management

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
