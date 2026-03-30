---
title: "Supported Browsers"
description: "Which browsers BrowserAptor auto-detects and how profiles are discovered"
weight: 40
---

## Auto-Detected Browsers

BrowserAptor automatically detects the following browsers by scanning known installation
locations. No manual configuration is needed.

### Chromium-Based Browsers

These browsers are detected by scanning `%LOCALAPPDATA%` for known user-data directory
paths:

| Browser | Detection Path (relative to `%LOCALAPPDATA%`) |
|---------|----------------------------------------------|
| Google Chrome | `Google\Chrome\User Data` |
| Google Chrome Beta | `Google\Chrome Beta\User Data` |
| Google Chrome Dev | `Google\Chrome Dev\User Data` |
| Google Chrome Canary | `Google\Chrome SxS\User Data` |
| Microsoft Edge | `Microsoft\Edge\User Data` |
| Microsoft Edge Beta | `Microsoft\Edge Beta\User Data` |
| Microsoft Edge Dev | `Microsoft\Edge Dev\User Data` |
| Microsoft Edge Canary | `Microsoft\Edge SxS\User Data` |
| Brave Browser | `BraveSoftware\Brave-Browser\User Data` |
| Brave Browser Beta | `BraveSoftware\Brave-Browser-Beta\User Data` |
| Brave Browser Nightly | `BraveSoftware\Brave-Browser-Nightly\User Data` |
| Vivaldi | `Vivaldi\User Data` |
| Opera | `Opera Software\Opera Stable` |
| Opera Next | `Opera Software\Opera Next` |
| Opera GX | `Opera Software\Opera GX Stable` |
| Chromium | `Chromium\User Data` |
| Arc | `Arc\User Data` |
| Thorium | `Thorium\User Data` |
| Cent Browser | `CentBrowser\User Data` |
| Comodo Dragon | `Comodo\Dragon\User Data` |
| ChromePlus | `MapleStudio\ChromePlus\User Data` |
| Torch | `Torch\User Data` |
| Yandex Browser | `Yandex\YandexBrowser\User Data` |

Only browsers whose installation directory actually exists on disk are included in the
detected list.

### Firefox-Based Browsers

Firefox and its variants are detected via the Windows registry:

| Browser | Registry Path |
|---------|--------------|
| Mozilla Firefox | `HKLM\SOFTWARE\Mozilla\Mozilla Firefox` |
| Firefox ESR | `HKLM\SOFTWARE\Mozilla\Mozilla Firefox ESR` |
| Firefox Developer Edition | `HKLM\SOFTWARE\Mozilla\Firefox Developer Edition` |
| Firefox Nightly | `HKLM\SOFTWARE\Mozilla\Firefox Nightly` |

Firefox profiles are read from `profiles.ini`:
- **Standard Firefox:** `%APPDATA%\Mozilla\Firefox\profiles.ini`
- **Firefox 128+ (multi-process):** also checks `%LOCALAPPDATA%\Mozilla\Firefox\profiles.ini`
- **LibreWolf:** `%APPDATA%\librewolf\profiles.ini`

### Registry Fallback

Any browser registered under `HKLM\SOFTWARE\Clients\StartMenuInternet` that is *not*
already detected via the paths above will also appear in the list. This covers:

- Internet Explorer (if present)
- Legacy browsers
- Third-party browsers that register themselves properly in Windows

---

## Profile Detection

### Chromium Profiles

For each detected Chromium browser, BrowserAptor reads the `Local State` JSON file in the
browser's `User Data` directory:

```
%LOCALAPPDATA%\{BrowserPath}\User Data\Local State
```

The `profile.info_cache` section maps profile directory names to profile metadata:

```json
{
  "profile": {
    "info_cache": {
      "Default": {
        "name": "Personal",
        "user_name": "me@gmail.com"
      },
      "Profile 1": {
        "name": "Work",
        "user_name": "me@company.com"
      }
    }
  }
}
```

**Fallback:** If `Local State` is missing or unreadable, BrowserAptor falls back to
scanning subdirectories of `User Data` named `Default` or `Profile N`, reading each
profile's `Preferences` file for its display name.

### Firefox Profiles

BrowserAptor reads Firefox's `profiles.ini` INI file, which looks like:

```ini
[Profile0]
Name=default-release
IsRelative=1
Path=Profiles/abc123.default-release

[Profile1]
Name=work
IsRelative=1
Path=Profiles/xyz789.work
```

Each `[ProfileN]` section becomes one entry in the selector.

---

## Profile IDs

Every profile gets a stable, human-readable ID:

```
{browser-slug}/{profile-slug}
```

Examples:

| Display Name | Profile ID |
|-------------|-----------|
| Google Chrome – Default | `google-chrome/default` |
| Google Chrome – Work | `google-chrome/profile-1` |
| Microsoft Edge – Default | `microsoft-edge/default` |
| Firefox – default-release | `mozilla-firefox/default-release` |
| Brave Browser – Personal | `brave-browser/profile-2` |

The slug is derived from the browser name (for the browser part) and the profile directory
name for Chromium browsers, or the profile name for Firefox browsers.

Profile IDs are stable — they do not change when you rename a profile. This means custom
display names stay associated with the correct profile even after a rename.

---

## Adding a Browser Not in the List

If your browser is not auto-detected, check whether it appears via the registry fallback:

```powershell
BrowserAptor.exe --list-browsers
```

If it still does not appear, the browser may not be registered in the standard Windows
browser registry. In this case, you can open a
[feature request](https://github.com/sassdawe/browseraptor/issues/new?template=feature_request.md)
to add explicit detection for that browser.

You can also check whether the browser registers itself under
`HKLM\SOFTWARE\Clients\StartMenuInternet` using `regedit.exe`.
