# BrowserAptor

**Browser and browser profile selector for opening links on Windows**

BrowserAptor is a lightweight .NET 10 WPF application that registers itself as a default browser on Windows. Every time you click a link (e.g. from email, Slack, Teams, etc.), BrowserAptor intercepts it and shows you a selector window so you can choose exactly which browser **and which profile** to open the link in.

---

## Features

- 🔍 **Auto-detects all installed browsers**: Google Chrome, Microsoft Edge, Mozilla Firefox, Brave, Vivaldi, Opera, Opera GX, Chromium, Yandex Browser, and more.
- 👤 **Profile-aware**: Reads Chrome/Edge/Brave/Vivaldi `Local State` files and Firefox `profiles.ini` to list every profile individually.
- 🖱️ **Quick selection**: Click or double-click a browser/profile entry, or press Enter to open the link. Press Escape or Cancel to dismiss.
- 🪟 **Registers as a Windows browser**: BrowserAptor writes the necessary `HKCU` registry keys so Windows sees it as a browser you can set as default from **Settings → Apps → Default apps → Web browser**.
- ✨ **No admin rights required**: All registration is done under `HKCU` (current user only).

---

## Project Structure

```
browseraptor/
├── BrowserAptor.slnx                 # Solution file
├── src/
│   ├── BrowserAptor/                 # WPF application (Windows, net10.0-windows)
│   │   ├── App.xaml / App.xaml.cs    # Application entry point & startup logic
│   │   ├── Views/
│   │   │   └── BrowserSelectorWindow # Main selector UI
│   │   ├── ViewModels/
│   │   │   └── BrowserSelectorViewModel
│   │   ├── Registration/
│   │   │   └── BrowserRegistrar      # Windows registry registration helper
│   │   ├── ExeIconConverter.cs       # WPF converter: exe path → icon
│   │   └── Resources/
│   │       └── browseraptor.ico
│   └── BrowserAptor.Core/            # Platform-agnostic core library (net10.0)
│       ├── Models/
│       │   ├── BrowserInfo.cs
│       │   ├── BrowserProfile.cs
│       │   └── BrowserType.cs
│       └── Services/
│           ├── IBrowserDetectionService.cs
│           ├── BrowserDetectionService.cs   # Registry + file-system detection
│           ├── IBrowserLaunchService.cs
│           ├── BrowserLaunchService.cs      # Process.Start launcher
│           └── FirefoxProfileParser.cs      # profiles.ini parser
└── tests/
    └── BrowserAptor.Tests/           # xUnit tests (net10.0, runs on Linux/Mac/Windows)
        └── BrowserTests.cs
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10)
- Windows 10 / 11 (runtime; build works on any OS)

### Build

```powershell
dotnet build BrowserAptor.slnx
```

### Run Tests

```powershell
dotnet test tests/BrowserAptor.Tests/BrowserAptor.Tests.csproj
```

### Publish (self-contained Windows executable)

```powershell
dotnet publish src/BrowserAptor/BrowserAptor.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o publish/
```

---

## Setting as Default Browser

1. **Run BrowserAptor once** (double-click or run `BrowserAptor.exe`). It will automatically register itself in the Windows registry.
2. Open **Windows Settings → Apps → Default apps**.
3. Search for **BrowserAptor** or scroll to it under *Web browser* and select it.
4. Done! The next time you click a link, the selector window will appear.

You can also register or unregister manually:

```powershell
BrowserAptor.exe --register
BrowserAptor.exe --unregister
```

---

## How It Works

1. **Registration** (`BrowserRegistrar`): On first launch, BrowserAptor creates `HKCU` registry keys for `http`, `https`, and `ftp` protocol handlers and registers itself under `Software\RegisteredApplications` so Windows lists it in Default Apps settings.

2. **Detection** (`BrowserDetectionService`): When a URL is received, the service scans known `%LOCALAPPDATA%` paths for Chromium-based browsers (Chrome, Edge, Brave, Vivaldi, Opera…) and reads their `Local State` JSON to enumerate profiles. For Firefox, it reads `%APPDATA%\Mozilla\Firefox\profiles.ini`.

3. **Selector UI** (`BrowserSelectorWindow`): A dark-themed WPF window lists every browser+profile combination. The user selects one and clicks **Open** (or double-clicks / presses Enter).

4. **Launch** (`BrowserLaunchService`): Opens the selected browser with the correct profile flag (`--profile-directory="..."` for Chromium, `-P "name"` for Firefox) and the target URL.

---

## License

MIT
