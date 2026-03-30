---
title: "BrowserAptor"
description: "Browser and browser profile selector for Windows"
---

## What Is BrowserAptor?

BrowserAptor is a lightweight **Windows browser selector**. Instead of one browser
always opening your links, BrowserAptor intercepts every link click and shows you a
quick-pick window so you can choose exactly which browser — and which **profile** —
to open it in.

---

## Features

- 🔍 **Auto-detects all your browsers** — Chrome, Edge, Firefox, Brave, Vivaldi,
  Opera, Opera GX, Chromium, Yandex Browser, and any browser registered in Windows.
- 👤 **Profile-aware** — reads every Chromium `Local State` file and Firefox
  `profiles.ini` so each profile appears as its own entry.
- 🖱️ **Instant selection** — click, double-click, or press Enter to open. Press Escape
  to dismiss without opening anything.
- 🪟 **Registers as a Windows browser** — writes `HKCU` registry keys so Windows
  shows BrowserAptor in *Settings → Default apps → Web browser*.
- ✨ **No admin rights needed** — all registration is under the current user only.
- 🖥️ **CLI mode** — list browsers, inspect profiles, set display names, and manage
  registration from the command line.

---

## Quick Start

### 1. Download

Grab the latest release from the [GitHub Releases](https://github.com/sassdawe/browseraptor/releases) page. Download `BrowserAptor.exe` (self-contained, no .NET install required).

### 2. Register as Default Browser

Double-click `BrowserAptor.exe`. A welcome dialog will appear and BrowserAptor will
register itself automatically. Then:

1. Open **Windows Settings → Apps → Default apps**
2. Search for **BrowserAptor**
3. Set it as your **Web browser**

### 3. Click Any Link

Click a link in Slack, Teams, email, or anywhere else. The BrowserAptor selector window
appears — pick your browser and profile, then press **Enter** or click **Open**.

---

## Why BrowserAptor?

Modern workflows involve multiple browsers and multiple profiles:

- Work Chrome profile vs personal Chrome profile
- Edge for Microsoft 365 apps, Firefox for privacy-sensitive sites
- Testing a site in multiple browsers simultaneously

BrowserAptor removes the friction of manually switching browsers. Set it as default once,
and every link click asks you where to send it.

---

## License

BrowserAptor is free and open-source software, released under the
[MIT License](https://github.com/sassdawe/browseraptor/blob/main/LICENSE).
