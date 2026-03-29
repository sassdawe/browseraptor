---
title: "Configuration"
description: "Customise display names, manage settings, and configure BrowserAptor behaviour"
weight: 30
---

## Display Names

BrowserAptor assigns each browser/profile a default display name based on the browser
name and profile name. You can override any of these with a custom display name that is
meaningful to you.

### Finding Profile IDs

Each profile has a stable ID in the format `{browser-slug}/{profile-slug}`. To list all
IDs:

```powershell
BrowserAptor.exe --list-browsers
```

Example output:
```
[google-chrome/default]         Google Chrome          → "C:\...\chrome.exe" --profile-directory="Default" "<url>"
[google-chrome/profile-1]       Google Chrome – Work   → "C:\...\chrome.exe" --profile-directory="Profile 1" "<url>"
[microsoft-edge/default]        Microsoft Edge         → "C:\...\msedge.exe" --profile-directory="Default" "<url>"
[mozilla-firefox/default-release] Firefox              → "C:\...\firefox.exe" -P "default-release" "<url>"
```

The text inside `[...]` is the profile ID.

### Setting a Custom Display Name

```powershell
BrowserAptor.exe --set-displayname google-chrome/profile-1 "Work Chrome"
BrowserAptor.exe --set-displayname microsoft-edge/default "Edge – Personal"
BrowserAptor.exe --set-displayname mozilla-firefox/default-release "Firefox – Privacy"
```

After setting a custom name, the selector window and all CLI output will use your custom
name instead of the auto-generated one.

### Getting a Display Name

```powershell
BrowserAptor.exe --get-displayname google-chrome/profile-1
# Output: Work Chrome
```

### Clearing Display Names

To remove a single custom name:

```powershell
BrowserAptor.exe --set-displayname google-chrome/profile-1 ""
```

To clear **all** custom display names:

```powershell
BrowserAptor.exe --clear-displaynames
```

### Where Display Names Are Stored

Custom display names are persisted to:

```
%APPDATA%\BrowserAptor\displaynames.json
```

This is a plain JSON file that you can edit manually if needed:

```json
{
  "google-chrome/profile-1": "Work Chrome",
  "microsoft-edge/default": "Edge – Personal"
}
```

---

## Registration

BrowserAptor registers itself as a browser automatically on first launch. You can
re-register or unregister manually:

```powershell
# Re-register (useful after moving BrowserAptor.exe to a new location)
BrowserAptor.exe --register

# Unregister (removes all BrowserAptor registry entries)
BrowserAptor.exe --unregister
```

After running `--register`, go to **Settings → Apps → Default apps** and set
BrowserAptor as your web browser if it was not already.

---

## Output Format

When using the CLI to list browsers, you can choose from five output formats:

| Format | Flag | Description |
|--------|------|-------------|
| `list` | *(default)* | Human-readable list with IDs and launch commands |
| `json` | `--format json` | JSON array — useful for scripting |
| `yaml` | `--format yaml` | YAML list |
| `csv` | `--format csv` | CSV with header row — importable into spreadsheets |
| `table` | `--format table` | Aligned text table — good for terminal reading |

Example:

```powershell
BrowserAptor.exe --list-browsers --format json
BrowserAptor.exe --list-browsers --format table
```

---

## Settings File Location

| File | Location | Purpose |
|------|----------|---------|
| Display names | `%APPDATA%\BrowserAptor\displaynames.json` | Custom profile display names |

BrowserAptor does not have a general-purpose config file; all behaviour is controlled via
CLI flags and the display-names store.

---

## Environment Variables

BrowserAptor respects the standard Windows environment variables:

| Variable | Used For |
|----------|---------|
| `%LOCALAPPDATA%` | Scanning Chromium browser data directories |
| `%APPDATA%` | Reading Firefox `profiles.ini`; storing `displaynames.json` |

These are set automatically by Windows for every user session. You do not need to
configure them manually.
