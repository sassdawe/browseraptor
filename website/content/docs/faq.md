---
title: "FAQ"
description: "Frequently asked questions about BrowserAptor"
weight: 60
---

## General

### What is BrowserAptor?

BrowserAptor is a free, open-source Windows application that acts as a browser selector.
Instead of opening links in a fixed browser, BrowserAptor intercepts each link click and
shows a window where you can choose exactly which browser — and which profile — to open
the link in.

### Does BrowserAptor work with all browsers?

BrowserAptor auto-detects 23+ Chromium-based browsers and all registered Firefox variants.
Any browser that is registered under `HKLM\SOFTWARE\Clients\StartMenuInternet` will also
appear, even if it is not in the built-in detection list. See the
[Supported Browsers](../browsers/) page for the full list.

### Is BrowserAptor free?

Yes. BrowserAptor is free and open-source software released under the
[MIT License](https://github.com/sassdawe/browseraptor/blob/main/LICENSE).

### Does BrowserAptor send any data online?

No. BrowserAptor is a purely local application. It does not connect to the internet,
collect analytics, or send any data anywhere.

---

## Installation & Setup

### Do I need administrator rights to install BrowserAptor?

No. BrowserAptor writes only to `HKCU` (the current user's registry hive), which never
requires elevation. You can install and register BrowserAptor as a standard user.

### Windows Defender SmartScreen is blocking BrowserAptor. Is it safe?

Yes, it is safe. SmartScreen shows a warning for newly downloaded executables that don't
have an extended-validation (EV) code signing certificate. BrowserAptor is open source
— you can inspect the full source code on GitHub. Click **More info → Run anyway** to
proceed.

### BrowserAptor doesn't appear in Windows Default Apps after registering.

Try re-running the registration:

```powershell
BrowserAptor.exe --unregister
BrowserAptor.exe --register
```

Then open **Settings → Apps → Default apps** and search for "BrowserAptor". On Windows 11,
you may need to scroll past the recommended apps or type the name in the search box.

### I moved BrowserAptor.exe. Now links open the old location.

Run `BrowserAptor.exe --register` from the new location to update the registry to point
to the new path.

---

## Browser Detection

### A browser I have installed is not showing up.

1. Run `BrowserAptor.exe --list-browsers` and check whether it appears there.
2. If not, verify the browser is properly installed (launch it from the Start menu).
3. Check if the browser registers itself under
   `HKLM\SOFTWARE\Clients\StartMenuInternet` using `regedit.exe`.
4. If none of the above helps, open a
   [feature request](https://github.com/sassdawe/browseraptor/issues/new?template=feature_request.md)
   with the browser name and its installation path.

### Only some of my Chrome profiles are showing.

BrowserAptor reads profiles from Chrome's `Local State` JSON file. If a profile is very
new (created but never launched), it may not yet be written to `Local State`. Launch the
profile in Chrome at least once, then try `BrowserAptor.exe --list-browsers` again.

### My Firefox profile isn't showing.

BrowserAptor reads `%APPDATA%\Mozilla\Firefox\profiles.ini`. If your Firefox profile is
stored elsewhere (e.g. a custom `profiles.ini` path), it will not be detected. Please
open a [feature request](https://github.com/sassdawe/browseraptor/issues/new?template=feature_request.md)
with details.

### I use LibreWolf. Is it supported?

Yes. BrowserAptor also reads `%APPDATA%\librewolf\profiles.ini` for LibreWolf profiles.

---

## Using the Selector

### Can I set a default browser so BrowserAptor doesn't always ask?

No — the point of BrowserAptor is to always ask. If you want a default, use Windows
Default Apps to point directly to a browser.

### The selector window opens behind other windows.

This can happen if the link was clicked from an application that immediately takes focus.
The BrowserAptor window is always shown as a topmost dialog. If it appears behind, try
pressing `Alt+Tab` to bring it to the front.

### Can I use the keyboard to select a browser?

Yes. Use `↑` / `↓` to move the selection and press `Enter` to open. Press `Escape` to
cancel.

### Can I use BrowserAptor to open a URL from the command line?

Yes:

```powershell
BrowserAptor.exe https://example.com
```

This opens the selector window for the given URL.

---

## Display Names

### How do I rename a browser entry in the selector?

Use the `--set-displayname` command:

```powershell
BrowserAptor.exe --set-displayname google-chrome/profile-1 "Work Chrome"
```

See the [Configuration](../configuration/) page for details.

### Where are my custom display names stored?

In `%APPDATA%\BrowserAptor\displaynames.json`. This is a plain JSON file you can edit
manually or back up.

---

## Troubleshooting

### BrowserAptor opened, selected a browser, but the link didn't open.

1. Verify the browser executable path is correct:
   ```powershell
   BrowserAptor.exe --list-browsers --format json
   ```
   Check the `executablePath` field for the affected browser.
2. Make sure the browser executable exists at that path.
3. Try launching the browser manually with the same arguments:
   ```powershell
   & "C:\path\to\browser.exe" --profile-directory="Default" "https://example.com"
   ```

### BrowserAptor crashes or shows an error.

Please [open a bug report](https://github.com/sassdawe/browseraptor/issues/new?template=bug_report.md)
with:
- The exact error message or steps to reproduce.
- Your Windows version (`winver`).
- Output of `BrowserAptor.exe --list-browsers`.

### How do I completely reset BrowserAptor?

```powershell
# Remove all registry entries
BrowserAptor.exe --unregister

# Delete the settings folder
Remove-Item -Recurse "$env:APPDATA\BrowserAptor"

# Re-register
BrowserAptor.exe --register
```

---

## Contributing

### How can I contribute?

See the [Contributing guide](https://github.com/sassdawe/browseraptor/blob/main/CONTRIBUTING.md)
for how to set up a dev environment, coding conventions, and how to submit a pull request.

### I found a security issue.

Please do not open a public GitHub issue for security vulnerabilities. Instead, use
[GitHub's private vulnerability reporting](https://github.com/sassdawe/browseraptor/security/advisories/new).
