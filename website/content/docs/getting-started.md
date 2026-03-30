---
title: "Getting Started"
description: "Your first steps with BrowserAptor — using the selector window and understanding profiles"
weight: 20
---

## Overview

Once BrowserAptor is [installed and set as your default browser](../installation/), it
intercepts every link click and shows the browser selector window. This guide walks you
through the selector UI, explains how profiles work, and shows you how to get the most out
of BrowserAptor.

---

## The Selector Window

When you click a link (from email, Slack, Teams, a document, etc.), BrowserAptor opens a
dark-themed selection window listing every browser and profile it detected on your system.

### Window Elements

- **Browser list** — each row shows:
  - The browser's icon (extracted from the executable)
  - The display name (browser name + profile name, or a custom name if you've set one)
- **Open button** — opens the selected browser/profile with the link
- **Cancel button** — dismisses the window without opening anything

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `↑` / `↓` | Move selection |
| `Enter` | Open selected browser/profile |
| `Escape` | Cancel and close |
| Double-click | Open selected browser/profile |

---

## Understanding Profiles

BrowserAptor is **profile-aware**, which means it lists each browser profile as a separate
entry. For example, if you have Google Chrome with three profiles (Default, Work, Personal),
you will see three separate entries in the selector.

### Chromium Profiles (Chrome, Edge, Brave, Vivaldi, etc.)

Chromium-based browsers store profile information in a `Local State` JSON file. BrowserAptor
reads this file to find:

- The internal profile directory name (e.g. `Default`, `Profile 1`)
- The display name you gave the profile (e.g. "Work", "Personal")
- The Google account email associated with the profile (shown in parentheses)

If a Chromium profile's display name is generic (e.g. "Person 1"), you can set a custom
name — see the [Configuration guide](../configuration/).

### Firefox Profiles

Firefox stores profiles in a `profiles.ini` file. BrowserAptor reads this file to find
each profile's name (e.g. `default-release`, `work`). Firefox is launched with the `-P`
flag to select the correct profile.

---

## Your First Link Click

1. Click a link anywhere on Windows (email, chat, document, etc.).
2. The BrowserAptor selector window appears.
3. Use the arrow keys or mouse to select your preferred browser/profile.
4. Press **Enter** or click **Open**.

The link opens in the selected browser using the correct profile flags automatically.

---

## If BrowserAptor Doesn't Appear

If clicking a link opens another browser directly instead of showing BrowserAptor:

1. **Check your default browser setting.** Open *Settings → Apps → Default apps* and
   confirm BrowserAptor is set for HTTP and HTTPS.
2. **Re-register BrowserAptor:**
   ```powershell
   BrowserAptor.exe --register
   ```
3. **Some apps ignore the system default browser** and hardcode their own browser. This
   is a limitation of those applications, not BrowserAptor.

---

## If a Browser Is Missing

BrowserAptor auto-detects browsers from known installation locations. If a browser is not
appearing:

- Make sure the browser is properly installed (try launching it from the Start menu first).
- Some browsers installed in non-standard locations may not be detected automatically but
  will still appear if they are registered under
  `HKLM\SOFTWARE\Clients\StartMenuInternet` (the Windows browser registry).
- Run `BrowserAptor.exe --list-browsers` from a terminal to see the full detected list.

---

## Next Steps

- [Configuration](../configuration/) — rename entries, set custom display names
- [Supported Browsers](../browsers/) — full list of auto-detected browsers
- [CLI Reference](../cli/) — command-line usage
