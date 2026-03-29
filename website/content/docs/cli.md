---
title: "CLI Reference"
description: "Complete command-line interface reference for BrowserAptor"
weight: 50
---

## Overview

BrowserAptor is primarily a GUI application, but it also supports a CLI mode for
listing browsers, managing display names, and controlling registration. CLI mode is
activated whenever a recognised flag is passed as an argument.

Because BrowserAptor is a Windows GUI application (`WinExe`), it attaches to the calling
console automatically — you can use it from `cmd.exe`, PowerShell, or Windows Terminal
without any special setup.

---

## Synopsis

```
BrowserAptor.exe [options]
BrowserAptor.exe <url>
```

---

## Options

### General

#### `--help` / `-h`

Print the help message and exit.

```powershell
BrowserAptor.exe --help
```

---

### Browser Detection

#### `--list-browsers` / `-l`

List all detected browsers and their profiles.

```powershell
BrowserAptor.exe --list-browsers
BrowserAptor.exe --list-browsers --format json
BrowserAptor.exe --list-browsers --format table
```

#### `--detect` / `-d` `[<name>...]`

Detect browsers, optionally filtering by name. Multiple name tokens are combined with OR
logic (any match is included).

```powershell
# List all browsers
BrowserAptor.exe --detect

# List only Chrome entries
BrowserAptor.exe --detect Chrome

# List Chrome and Edge entries
BrowserAptor.exe --detect Chrome Edge
```

---

### Output Format

#### `--format` / `-f` `<format>`

Controls the output format for `--list-browsers` and `--detect`. Can be combined with
either command.

| Value | Description |
|-------|-------------|
| `list` | Human-readable list (default) |
| `json` | JSON array |
| `yaml` | YAML list |
| `csv` | CSV with header row |
| `table` | Aligned text table |

```powershell
BrowserAptor.exe --list-browsers --format json
BrowserAptor.exe --detect Chrome --format table
BrowserAptor.exe --list-browsers --format csv > browsers.csv
```

**Example `list` output:**
```
[google-chrome/default]           Google Chrome                   → "C:\...\chrome.exe" --profile-directory="Default" "<url>"
[google-chrome/profile-1]         Google Chrome – Work            → "C:\...\chrome.exe" --profile-directory="Profile 1" "<url>"
[mozilla-firefox/default-release] Firefox                         → "C:\...\firefox.exe" -P "default-release" "<url>"
```

**Example `table` output:**
```
Id                                 BrowserName    ProfileName       UserName           ExecutablePath
---------------------------------  -------------  ----------------  -----------------  ---------------------------
google-chrome/default              Google Chrome  Default                              C:\...\chrome.exe
google-chrome/profile-1            Google Chrome  Work              me@company.com     C:\...\chrome.exe
mozilla-firefox/default-release    Firefox        default-release                      C:\...\firefox.exe
```

---

### Display Names

#### `--set-displayname` / `-s` `<id>` `<new-name>`

Set a custom display name for a browser profile. The `<id>` is the profile ID shown in
square brackets by `--list-browsers`.

```powershell
BrowserAptor.exe --set-displayname google-chrome/profile-1 "Work Chrome"
BrowserAptor.exe --set-displayname microsoft-edge/default "Edge – Personal"
BrowserAptor.exe --set-displayname mozilla-firefox/default-release "Firefox – Privacy"
```

After setting, the new name appears in the selector window and in all CLI output.

#### `--get-displayname` `<id>`

Get the custom display name for a profile ID, if set.

```powershell
BrowserAptor.exe --get-displayname google-chrome/profile-1
# Output: Work Chrome
```

#### `--clear-displaynames`

Remove all custom display names.

```powershell
BrowserAptor.exe --clear-displaynames
```

---

### Registration

#### `--register`

Register BrowserAptor in the Windows registry as a browser. This is done automatically
on first launch, but can be run again after moving the executable or after an update.

```powershell
BrowserAptor.exe --register
```

A message box confirms successful registration and reminds you to set BrowserAptor as
your default browser in Windows Settings.

#### `--unregister`

Remove all BrowserAptor registry entries. After running this, Windows will no longer
list BrowserAptor as a browser option.

```powershell
BrowserAptor.exe --unregister
```

---

## Opening a URL

When called with a URL as the only argument (no flags), BrowserAptor opens the selector
window for that URL. This is the mode used by Windows when BrowserAptor is the default
browser.

```powershell
BrowserAptor.exe https://example.com
BrowserAptor.exe "https://example.com/path?q=hello%20world"
```

You can use this to test BrowserAptor from the command line without clicking a link.

---

## No Arguments

Running `BrowserAptor.exe` with no arguments opens a welcome message dialog explaining
how to set BrowserAptor as your default browser. This is the behaviour when a user
double-clicks the executable.

---

## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | Invalid arguments or error processing the command |

---

## Examples

```powershell
# List all browsers in JSON format
BrowserAptor.exe --list-browsers --format json

# Export browser list to CSV
BrowserAptor.exe --list-browsers --format csv | Out-File browsers.csv

# Find only Brave entries in table format
BrowserAptor.exe --detect Brave --format table

# Give a profile a friendly name
BrowserAptor.exe --set-displayname brave-browser/default "Brave – Personal"

# Re-register after moving the exe
BrowserAptor.exe --register

# Test opening a URL
BrowserAptor.exe https://github.com
```
