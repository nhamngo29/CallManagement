# Hướng dẫn cài đặt Call Management

## 🖥️ Windows

### Cách 1: Chạy trực tiếp
1. Tải file `CallManagement.exe` từ folder `publish/win-x64/`
2. Double-click để chạy
3. Nếu Windows SmartScreen chặn: Click **More info** → **Run anyway**

### Cách 2: Tạo shortcut
1. Copy `CallManagement.exe` vào folder mong muốn (ví dụ: `C:\Program Files\CallManagement\`)
2. Click phải → **Create shortcut**
3. Kéo shortcut ra Desktop

---

## 🍎 macOS (Apple Silicon - M1/M2/M3/M4)

### Bước 1: Tạo .app bundle
1. Copy folder `osx-arm64` vào máy Mac
2. Mở Terminal và chạy:
   ```bash
   cd /path/to/osx-arm64
   chmod +x create-app-bundle.sh
   ./create-app-bundle.sh
   ```

### Bước 2: Cài đặt
1. Kéo `CallManagement.app` vào folder **Applications**
2. Lần đầu mở: **Click phải** → **Open** → **Open** (xác nhận)

### Xử lý lỗi "App is damaged" hoặc "unidentified developer"
Mở Terminal và chạy:
```bash
xattr -cr /Applications/CallManagement.app
```

Hoặc vào: **System Settings** → **Privacy & Security** → **Open Anyway**

---

## 🔧 Build từ source code

### Yêu cầu
- .NET 9 SDK

### Build script (Windows PowerShell)
```powershell
# Build tất cả platforms
./publish.ps1

# Chỉ build Windows
./publish.ps1 -Target windows

# Chỉ build macOS
./publish.ps1 -Target macos
```

### Build thủ công

**Windows:**
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish/win-x64
```

**macOS ARM64:**
```bash
dotnet publish -c Release -r osx-arm64 --self-contained true -o ./publish/osx-arm64
```

**macOS Intel:**
```bash
dotnet publish -c Release -r osx-x64 --self-contained true -o ./publish/osx-x64
```

---

## 📁 Cấu trúc output

```
publish/
├── win-x64/
│   └── CallManagement.exe          ← Windows executable
│
└── osx-arm64/
    ├── CallManagement              ← macOS binary
    ├── create-app-bundle.sh        ← Script tạo .app
    └── CallManagement.app/         ← Sau khi chạy script
        └── Contents/
            ├── Info.plist
            ├── MacOS/
            │   └── CallManagement
            └── Resources/
```

---

## ⚠️ Lưu ý

### Windows
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
