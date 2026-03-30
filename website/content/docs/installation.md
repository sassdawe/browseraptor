---
title: "Installation"
description: "How to download and install BrowserAptor as your default Windows browser"
weight: 10
---

## System Requirements

| Requirement | Details |
|-------------|---------|
| **Operating System** | Windows 10 (version 1809+) or Windows 11 |
| **Architecture** | x64 (64-bit) |
| **.NET Runtime** | Not required — the release download is self-contained |
| **Permissions** | Standard user account (no administrator rights needed) |

---

## Step 1: Download

1. Go to the [GitHub Releases page](https://github.com/sassdawe/browseraptor/releases/latest).
2. Under **Assets**, download **`BrowserAptor.exe`**.

The downloaded file is a single self-contained executable — no installer, no setup wizard.
Place it anywhere you like (e.g. `C:\Tools\BrowserAptor\BrowserAptor.exe` or your Desktop).

> **Tip:** For easy command-line access, add the folder containing `BrowserAptor.exe` to
> your system `PATH`.

---

## Step 2: First Launch

Double-click `BrowserAptor.exe`. On first launch:

1. BrowserAptor registers itself in the Windows registry (`HKCU` — current user only,
   no elevation required).
2. A welcome dialog appears explaining the next step.

If Windows Defender SmartScreen shows a warning ("Windows protected your PC"), click
**More info** → **Run anyway**. This happens because BrowserAptor is a new, unsigned
executable. It is safe to run.

---

## Step 3: Set as Default Browser

### Windows 11

1. Open **Settings** (Win + I).
2. Go to **Apps → Default apps**.
3. In the search box, type **BrowserAptor**.
4. Click on **BrowserAptor** in the results.
5. Set it as the default for **HTTP**, **HTTPS**, and optionally **FTP**.

### Windows 10

1. Open **Settings** (Win + I).
2. Go to **Apps → Default apps**.
3. Scroll down to **Web browser**.
4. Click the current browser name and select **BrowserAptor** from the list.

---

## Verifying the Installation

Click any link from an external application (email client, Slack, Teams, etc.). The
BrowserAptor selector window should appear. Select a browser and click **Open**.

You can also verify from the command line:

```powershell
BrowserAptor.exe --list-browsers
```

This should print a list of all detected browsers and profiles.

---

## Updating BrowserAptor

BrowserAptor does not auto-update. To update:

1. Download the new `BrowserAptor.exe` from the Releases page.
2. Replace the old file with the new one (in the same location).
3. Run `BrowserAptor.exe` once — it will re-register with the new executable path
   automatically.

---

## Uninstalling

### Option 1: CLI

```powershell
BrowserAptor.exe --unregister
```

Then delete `BrowserAptor.exe` and the settings folder
(`%APPDATA%\BrowserAptor\`).

### Option 2: Manual

1. Open **regedit.exe**.
2. Delete the key `HKCU\Software\Classes\BrowserAptor.Url`.
3. Delete the key `HKCU\Software\Clients\StartMenuInternet\BrowserAptor`.
4. Delete the value `BrowserAptor` under
   `HKCU\Software\RegisteredApplications`.
5. Delete the `BrowserAptor.exe` file and the `%APPDATA%\BrowserAptor\` folder.

After unregistering, Windows will prompt you to choose a new default browser the next time
you click a link.

---

## Building from Source

If you prefer to build BrowserAptor yourself:

```powershell
# Prerequisites: .NET 10 SDK
git clone https://github.com/sassdawe/browseraptor.git
cd browseraptor
dotnet publish src/BrowserAptor/BrowserAptor.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o publish/
```

The output is in `publish/BrowserAptor.exe`.
