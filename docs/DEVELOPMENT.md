# BrowserAptor Developer Documentation

This document is the primary reference for developers who want to understand the BrowserAptor
codebase, extend it, or contribute new features.

---

## Table of Contents

1. [Prerequisites and Tools](#prerequisites-and-tools)
2. [DevContainer / GitHub Codespaces](#devcontainer--github-codespaces)
3. [Repository Structure](#repository-structure)
4. [Architecture Overview](#architecture-overview)
5. [Project Details](#project-details)
   - [BrowserAptor.Core](#browseraptor-core)
   - [BrowserAptor (WPF App)](#browseraptor-wpf-app)
   - [BrowserAptor.Tests](#browseraptor-tests)
6. [Implementation Details](#implementation-details)
   - [Browser Detection Algorithm](#browser-detection-algorithm)
   - [Chromium Profile Reading](#chromium-profile-reading)
   - [Firefox Profile Parsing](#firefox-profile-parsing)
   - [Profile Argument Construction](#profile-argument-construction)
   - [Registry Registration](#registry-registration)
   - [Output Formatting Pipeline](#output-formatting-pipeline)
   - [Display Name Persistence](#display-name-persistence)
6. [Key Design Decisions](#key-design-decisions)
7. [Adding a New Browser](#adding-a-new-browser)
8. [Adding a New Output Format](#adding-a-new-output-format)
9. [Debugging Tips](#debugging-tips)

---

## Prerequisites and Tools

| Tool | Minimum Version | Purpose |
|------|-----------------|---------|
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10) | 10.0.x | Build, test, publish |
| [Visual Studio 2022](https://visualstudio.microsoft.com/) | 17.12+ | Full IDE with WPF designer; requires the *.NET desktop development* workload |
| **or** [VS Code](https://code.visualstudio.com/) with [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) | latest | Lightweight alternative; no WPF designer but full IntelliSense and testing |
| [Git](https://git-scm.com/) | 2.x+ | Source control |
| Windows 10 / 11 | — | Required *at runtime* and to build the WPF app. Core library and tests build and run cross-platform. |
| [Windows Terminal](https://aka.ms/terminal) *(optional)* | — | Recommended shell host for running .NET CLI commands |

---

## DevContainer / GitHub Codespaces

The repository includes a ready-to-use DevContainer configuration in
`.devcontainer/devcontainer.json`. It provides a fully pre-configured Linux environment
with the .NET 10 SDK, all recommended VS Code extensions, and automatic NuGet restore —
so you can start coding in under two minutes with no local setup.

> **Scope:** Because the container runs Linux, the WPF project (`BrowserAptor`,
> `net10.0-windows`) is excluded from the build. However, `BrowserAptor.Core` (the
> platform-agnostic library) and `BrowserAptor.Tests` build and run perfectly, covering
> all detection logic, profile parsing, formatting, and display-name persistence.

### GitHub Codespaces (no local install needed)

1. Navigate to the repository on GitHub.
2. Click **Code → Codespaces → Create codespace on main**.
3. GitHub spins up the container (≈ 2 minutes on first use; cached on subsequent opens).
4. The `postCreateCommand` runs `dotnet restore BrowserAptor.slnx` automatically.
5. Open a terminal and start working:

   ```bash
   dotnet build BrowserAptor.slnx
   dotnet test tests/BrowserAptor.Tests/BrowserAptor.Tests.csproj
   ```

### VS Code Dev Containers (local Docker)

1. Install [Docker Desktop](https://www.docker.com/products/docker-desktop).
2. Install the [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) VS Code extension.
3. Open the repository folder in VS Code and click **Reopen in Container** when prompted
   (or open the Command Palette → **Dev Containers: Reopen in Container**).
4. The container builds, runs `dotnet restore`, then immediately executes a build and test
   pass (`postAttachCommand`) so any issues are surfaced right away.

### What the container includes

| Component | Details |
|-----------|---------|
| Base image | `mcr.microsoft.com/devcontainers/dotnet:1-10.0-bookworm` (Debian 12) |
| .NET SDK | 10.0.x (latest patch) |
| GitHub CLI | `gh` — pre-installed via the `github-cli` feature |
| Git LFS | Pre-installed via the `git-lfs` feature |
| VS Code extensions | `ms-dotnettools.csharp`, `ms-dotnettools.csdevkit`, `editorconfig.editorconfig`, `eamodio.gitlens`, `yzhang.markdown-all-in-one`, `davidanson.vscode-markdownlint` |
| Post-create | `dotnet restore BrowserAptor.slnx` |
| Post-attach | `dotnet build` + `dotnet test` |
| Remote user | `vscode` (non-root) |

---

## Repository Structure

```
browseraptor/
├── BrowserAptor.slnx                     # Solution file (new XML format)
├── src/
│   ├── BrowserAptor/                     # WPF application (net10.0-windows)
│   │   ├── App.xaml / App.xaml.cs        # Entry point; CLI arg routing
│   │   ├── Views/
│   │   │   └── BrowserSelectorWindow.xaml/.cs   # Main selector window
│   │   ├── ViewModels/
│   │   │   └── BrowserSelectorViewModel.cs      # MVVM view model
│   │   ├── Registration/
│   │   │   └── BrowserRegistrar.cs       # HKCU registry operations
│   │   ├── CLI/
│   │   │   └── CliHandler.cs             # Console-mode flag handling
│   │   ├── ExeIconConverter.cs           # IValueConverter: exe path → BitmapSource
│   │   ├── AssemblyInfo.cs               # Assembly attributes
│   │   └── Resources/
│   │       └── browseraptor.ico          # Application icon
│   └── BrowserAptor.Core/                # Platform-agnostic library (net10.0)
│       ├── Models/
│       │   ├── BrowserInfo.cs            # Browser entity + slug-based Id
│       │   ├── BrowserProfile.cs         # Profile entity + BuildArguments()
│       │   └── BrowserType.cs            # Enum: Unknown/Chromium/Firefox/Safari
│       └── Services/
│           ├── IBrowserDetectionService.cs
│           ├── BrowserDetectionService.cs    # Main detection engine (~492 lines)
│           ├── ChromiumProfileReader.cs      # Local State JSON parser
│           ├── FirefoxProfileParser.cs       # profiles.ini parser
│           ├── IBrowserLaunchService.cs
│           ├── BrowserLaunchService.cs       # Process.Start launcher
│           ├── OutputFormatter.cs            # list/json/yaml/csv/table formatter
│           └── DisplayNameStore.cs           # Persists custom display names
└── tests/
    └── BrowserAptor.Tests/               # xUnit 2.9.3 (net10.0, cross-platform)
        ├── BrowserTests.cs               # ~664 lines
        ├── OutputFormatterTests.cs       # ~286 lines
        └── BrowserAptor.Tests.csproj
```

---

## Architecture Overview

BrowserAptor is split into two distinct layers:

```
┌────────────────────────────────────────────────┐
│          BrowserAptor (WPF, Windows-only)       │
│  App.xaml.cs  CliHandler  BrowserRegistrar      │
│  BrowserSelectorWindow + ViewModel              │
│  ExeIconConverter                               │
└─────────────────┬──────────────────────────────┘
                  │ references
┌─────────────────▼──────────────────────────────┐
│          BrowserAptor.Core (net10.0)            │
│  Models: BrowserInfo, BrowserProfile, BrowserType │
│  Services: Detection, Launch, Formatting, Names │
└────────────────────────────────────────────────┘
                  │ tested by
┌─────────────────▼──────────────────────────────┐
│          BrowserAptor.Tests (net10.0)           │
│  xUnit tests — run on Linux/macOS/Windows       │
└────────────────────────────────────────────────┘
```

**Key principle:** All browser detection logic, profile parsing, output formatting, and
display name management live in `BrowserAptor.Core`, which has **no Windows dependency**.
This keeps the core testable on any OS and makes the logic reusable if a non-WPF front-end
is ever built.

The WPF project (`BrowserAptor`) is responsible only for:
- Windows registry registration (`BrowserRegistrar`)
- The GUI window (`BrowserSelectorWindow`)
- The CLI console shim (`CliHandler`)
- The WPF value converter (`ExeIconConverter`)
- Entry-point routing (`App.xaml.cs`)

---

## Project Details

### BrowserAptor.Core

#### Models

**`BrowserType` (enum)**

```csharp
public enum BrowserType { Unknown, Chromium, Firefox, Safari }
```

Used by `BrowserDetectionService` when constructing a `BrowserInfo` and by
`BrowserProfile.BuildArguments` to choose the correct CLI flag style.

---

**`BrowserInfo`**

Represents a single installed browser. Key properties:

| Property | Type | Notes |
|----------|------|-------|
| `Name` | `string` | Human-readable name, e.g. "Google Chrome" |
| `ExecutablePath` | `string` | Absolute path to the browser executable |
| `IconPath` | `string` | Path used for the icon (usually same as `ExecutablePath`) |
| `BrowserType` | `BrowserType` | Drives profile argument strategy |
| `Profiles` | `List<BrowserProfile>` | All profiles found for this browser |
| `Id` | `string` (computed) | Stable slug, e.g. `"google-chrome"` |

The `Id` property calls the internal `Slugify(string)` helper, which lowercases the name,
replaces runs of non-alphanumeric characters with a single hyphen, and trims trailing
hyphens. This produces stable, filesystem-safe IDs regardless of locale.

---

**`BrowserProfile`**

Represents one profile within a browser. Key properties:

| Property | Type | Notes |
|----------|------|-------|
| `Name` | `string` | Display name of the profile, e.g. "Work" |
| `ProfileDirectory` | `string` | Chromium: directory name ("Default", "Profile 1"); Firefox: relative path from profiles.ini |
| `UserName` | `string?` | Google account email, if available (Chromium `user_name` field) |
| `AvatarIconPath` | `string?` | URL to Gaia profile picture (Chromium) |
| `Browser` | `BrowserInfo` | Back-reference to the owning browser |
| `Id` | `string` (computed) | Stable compound ID: `{browserId}/{profileSlug}` |

The `Id` slug derivation logic differs by browser type: for Chromium, the
`ProfileDirectory` (e.g. "Profile 1") is used as the slug source because it is stable
across renames. For Firefox, `ProfileDirectory` contains a random string (e.g.
`abc123.default-release`), so the profile `Name` is slugified instead.

`BuildArguments(string url)` returns the full arguments string for `Process.Start`:
- Chromium: `--profile-directory="Profile 1" "https://example.com"`
- Firefox: `-P "default-release" "https://example.com"`

---

#### Services

**`BrowserDetectionService`**

The main detection engine. Implements `IBrowserDetectionService`:

```csharp
public interface IBrowserDetectionService
{
    IReadOnlyList<BrowserInfo> DetectBrowsers();
}
```

`DetectBrowsers()` calls three private methods in sequence:
1. `DetectChromiumBrowsers(browsers)` — scans 23 known `%LOCALAPPDATA%` paths
2. `DetectFirefoxBrowsers(browsers)` — reads Windows registry for Firefox installs
3. `DetectViaBrowserRegistry(browsers)` — scans `SOFTWARE\Clients\StartMenuInternet`

Results are de-duplicated by executable path (case-insensitive) and sorted by name.

---

**`ChromiumProfileReader`**

Static helper called by `BrowserDetectionService.ReadChromiumProfiles()`.

`ReadProfilesFromDir(userDataDir, browser)`:
1. Looks for `{userDataDir}\Local State` (JSON file).
2. Parses `profile.info_cache` to get profile directory names, display names, user emails,
   and avatar URLs.
3. Falls back to scanning subdirectories named `Default` or `Profile N` if `Local State`
   is absent or unparseable, reading each profile's `Preferences` file for its display name.

---

**`FirefoxProfileParser`**

Static helper for parsing Firefox INI files.

`ParseProfilesIni(iniPath, browser)`:
- Reads the file line by line.
- Treats any `[ProfileN]` section as a new profile.
- Non-profile sections (e.g. `[Install...]`) flush any pending profile but are otherwise
  ignored.
- Extracts `Name=` and `Path=` values.
- Returns a `List<BrowserProfile>`.

---

**`BrowserLaunchService`**

Implements `IBrowserLaunchService`:

```csharp
void Launch(BrowserProfile profile, string url);
```

Creates a `ProcessStartInfo` with:
- `FileName` = `profile.Browser.ExecutablePath`
- `Arguments` = `profile.BuildArguments(url)`
- `UseShellExecute` = `false`

Then calls `Process.Start()`.

---

**`OutputFormatter`**

Static class that formats a browser list into a string. Entry point:

```csharp
string Format(IEnumerable<BrowserInfo> browsers, string format,
              DisplayNameStore? displayNames = null)
```

Supported formats:
- `list` (default) — one line per profile: `[id] Label → "exe.exe" args`
- `json` — JSON array with browser and profile objects
- `yaml` — YAML list
- `csv` — CSV with header row: `Id,BrowserName,ProfileName,UserName,ExecutablePath,Arguments`
- `table` — aligned text table with column headers

Custom display names from `DisplayNameStore` override the default derived label in all
formats.

---

**`DisplayNameStore`**

Persists custom display names to `%APPDATA%\BrowserAptor\displaynames.json`.

Key methods:

| Method | Description |
|--------|-------------|
| `GetDisplayName(id)` | Returns the custom name for the given profile ID, or `null` |
| `SetDisplayName(id, name)` | Saves a custom name; immediately persists to disk |
| `RemoveDisplayName(id)` | Removes the entry and persists |
| `ClearAll()` | Deletes all entries and persists |

The store is loaded lazily on first access. An unreadable or corrupt JSON file is treated
as an empty store (no exception propagated).

---

### BrowserAptor (WPF App)

**`App.xaml.cs`**

The application entry point. `OnStartup` dispatches as follows:

```
args present?
 ├─ CLI flag? → CliHandler.TryHandle() → Shutdown
 ├─ --register → BrowserRegistrar.Register() → MessageBox → Shutdown
 ├─ --unregister → BrowserRegistrar.Unregister() → MessageBox → Shutdown
 ├─ URL? → EnsureRegistered() → show BrowserSelectorWindow
 └─ (no args) → EnsureRegistered() → ShowWelcomeMessage → Shutdown
```

`EnsureRegistered()` silently calls `BrowserRegistrar.Register()` on every launch so that
the HKCU registry keys stay current even if the executable path changes (e.g. after an
update).

---

**`BrowserSelectorWindow` + `BrowserSelectorViewModel`**

MVVM pattern:

- The **ViewModel** (`BrowserSelectorViewModel`) holds:
  - `ObservableCollection<BrowserProfile> Profiles` — populated by calling
    `IBrowserDetectionService.DetectBrowsers()` at construction time.
  - `BrowserProfile? SelectedProfile` — bound to the list view selection.
  - `ICommand OpenCommand` — validates that a profile is selected and calls
    `IBrowserLaunchService.Launch(selected, url)`.
  - `ICommand CancelCommand` — closes the window.

- The **View** (`BrowserSelectorWindow.xaml`) is a dark-themed WPF window:
  - Background: `#1E1E2E` (Catppuccin Crust)
  - Accent: `#7C5CBF`
  - ListView showing browser icon, display name, and profile name.
  - Double-click or Enter key triggers `OpenCommand`.
  - Escape key triggers `CancelCommand`.

---

**`BrowserRegistrar`**

Creates four registry trees under `HKCU`:

1. `Software\Classes\BrowserAptor.Url` — ProgId with `shell\open\command` pointing to
   `BrowserAptor.exe "%1"`.
2. `Software\Clients\StartMenuInternet\BrowserAptor` — full capabilities registration
   including URL associations (`http`, `https`, `ftp`) and file associations
   (`.htm`, `.html`, `.xhtml`, `.shtml`, `.xht`, `.webp`).
3. `Software\Classes\{ext}\OpenWithProgIds` — registers the ProgId for each HTML
   extension.
4. `Software\RegisteredApplications\BrowserAptor` — points Windows to the capabilities
   key, making BrowserAptor appear in *Settings → Default apps*.

`Unregister()` deletes the ProgId subtree, the StartMenuInternet subtree, and the
`RegisteredApplications` value. All operations are scoped to `HKCU` so no elevation is
required.

---

**`CliHandler`**

`TryHandle(args, out exitCode)`:
- Returns `false` if no known CLI flag is found (GUI mode continues).
- Otherwise, calls `AttachConsole(-1)` to connect to the parent process console
  (necessary because `BrowserAptor.exe` is a WinExe, not a console app), redirects
  `Console.Out`, processes the command, and calls `FreeConsole()` before returning.

Supported flags (as of the current implementation):
- `--help` / `-h`
- `--list-browsers` / `-l`
- `--detect` / `-d` [name...]
- `--format` / `-f` `<format>`
- `--set-displayname` / `-s` `<id>` `<name>`

---

**`ExeIconConverter`**

WPF `IValueConverter` that converts an executable path string into a `BitmapSource` for
display in the browser list. Uses `System.Drawing.Icon.ExtractAssociatedIcon()` to extract
the icon embedded in the executable, then converts it to a WPF-compatible `BitmapSource`.

---

### BrowserAptor.Tests

The test project targets `net10.0` (no Windows suffix) so it can run anywhere. It uses
**xUnit 2.9.3** with no mocking framework — tests rely on real file I/O with temporary
files and in-memory object construction.

Test classes:
- `BrowserProfileTests` — `BuildArguments` and `ToString` logic for both browser types
- `FirefoxProfileParsingTests` — `FirefoxProfileParser.ParseProfilesIni` with various INI
  layouts
- `BrowserInfoTests` — `ToString` and `Profiles` default value
- `BrowserIdTests` — `BrowserInfo.Id` and `BrowserProfile.Id` slug derivation
- `DisplayNameStoreTests` — persistence, load, corrupt file handling
- `ChromiumProfileReadingTests` — `ChromiumProfileReader.ReadProfilesFromDir` with real
  temp directories
- `BrowserDetectionServiceTests` — integration tests that verify the detection pipeline
  (skipped on non-Windows via `[SupportedOSPlatform]` guards)
- `OutputFormatterTests` — all five output formats with single/multi-browser scenarios

---

## Implementation Details

### Browser Detection Algorithm

`DetectBrowsers()` runs three passes:

**Pass 1 — Chromium browsers**

For each of the 23 entries in `ChromiumLocalPaths`:
1. Build the full path `%LOCALAPPDATA%\{SubPath}`.
2. Skip if directory doesn't exist.
3. Call `FindChromiumExecutable(userDataDir)` to locate the `.exe` — checks adjacent
   `Application/` folder, parent directory, and common `Program Files` paths.
4. Construct a `BrowserInfo` with `BrowserType.Chromium`.
5. Call `ChromiumProfileReader.ReadProfilesFromDir()` to enumerate profiles.
6. If no profiles found, add a single synthetic "Default" profile.

**Pass 2 — Firefox browsers**

1. Query `HKLM\SOFTWARE\Mozilla`, `HKLM\SOFTWARE\WOW6432Node\Mozilla`, and
   `HKCU\SOFTWARE\Mozilla` for product keys whose name starts with `"Mozilla Firefox"` or
   `"Firefox"`.
2. Read `PathToExe` from the product's `Main` subkey.
3. Derive a display name from the product key name (e.g. "Mozilla Firefox" → "Firefox",
   "Firefox Nightly" → "Firefox Nightly").
4. Construct `BrowserInfo` with `BrowserType.Firefox`.
5. Discover profiles via `ReadFirefoxProfiles()` which looks for `profiles.ini` in:
   - `%APPDATA%\Mozilla\Firefox\`
   - `%LOCALAPPDATA%\Mozilla\Firefox\` (Firefox 128+ multi-process)
   - `%APPDATA%\librewolf\` (LibreWolf fork)
6. If no profiles found, add a single synthetic "Default" profile.

**Pass 3 — Registry fallback**

1. Scan `HKLM\SOFTWARE\Clients\StartMenuInternet` and the `WOW6432Node` variant.
2. For each subkey, read `shell\open\command` to get the executable path.
3. Skip any browser whose executable path already matches a browser found in passes 1–2.
4. Add a synthetic single-profile `BrowserInfo` for any new browser found.

**De-duplication and sorting**

Results are grouped by `ExecutablePath.ToLowerInvariant()`, keeping only the first entry
per unique path, then sorted by `Name`.

---

### Chromium Profile Reading

`ChromiumProfileReader.ReadProfilesFromDir(userDataDir, browser)`:

**Primary path — Local State JSON:**
```
{userDataDir}\Local State
  → $.profile.info_cache
      → key = profile directory name (e.g. "Default", "Profile 1")
      → .name         → display name
      → .user_name    → Google account email
      → .last_downloaded_gaia_picture_url_with_size → avatar URL
```

**Fallback — directory scan:**
If `Local State` is missing or throws on parse, the code scans subdirectories of
`userDataDir` for names matching `"Default"` or starting with `"Profile "`. For each, it
reads `Preferences` JSON and extracts `$.profile.name`.

---

### Firefox Profile Parsing

`FirefoxProfileParser.ParseProfilesIni(iniPath, browser)`:

The INI parser is a simple line-by-line state machine:
- `[ProfileN]` sections reset `currentName`/`currentPath` and flush any pending profile.
- Non-profile sections (e.g. `[Install...]`) also flush, then are ignored.
- `Name=` and `Path=` lines set the corresponding field.
- After the last line, any pending profile is flushed.

The parser does **not** handle the `IsRelative=` flag — `ProfileDirectory` stores the
raw `Path=` value from the INI, which may be an absolute or relative path. The caller
(`BrowserDetectionService`) does not need the absolute path; it passes the raw value to
`BrowserProfile.ProfileDirectory` and uses the profile `Name` for the Firefox `-P` flag.

---

### Profile Argument Construction

`BrowserProfile.BuildArguments(url)`:

```
if BrowserType == Firefox
    return  -P "profileName" "url"
else if ProfileDirectory is not empty
    return  --profile-directory="ProfileDirectory" "url"
else
    return  "url"
```

Both the profile directory and the URL are quoted. The URL quoting handles spaces and
special characters in custom URL schemes.

---

### Registry Registration

`BrowserRegistrar.Register()` creates the following HKCU keys:

```
HKCU\Software\Classes\BrowserAptor.Url
  (default)              = "BrowserAptor – Browser & Profile Selector"
  URL Protocol           = ""
  DefaultIcon\(default)  = "BrowserAptor.exe",0
  shell\open\command     = "BrowserAptor.exe" "%1"
  shell\edit\command     = "BrowserAptor.exe" "%1"

HKCU\Software\Clients\StartMenuInternet\BrowserAptor
  (default)              = "BrowserAptor – Browser & Profile Selector"
  Capabilities\ApplicationName        = "BrowserAptor"
  Capabilities\ApplicationDescription = "BrowserAptor – Browser & Profile Selector"
  Capabilities\ApplicationIcon        = "BrowserAptor.exe",0
  Capabilities\URLAssociations\http   = "BrowserAptor.Url"
  Capabilities\URLAssociations\https  = "BrowserAptor.Url"
  Capabilities\URLAssociations\ftp    = "BrowserAptor.Url"
  Capabilities\FileAssociations\...   = "BrowserAptor.Url"
  DefaultIcon\(default)  = "BrowserAptor.exe",0
  shell\open\command     = "BrowserAptor.exe"
  InstallInfo\...

HKCU\Software\Classes\{.htm,.html,...}\OpenWithProgIds
  BrowserAptor.Url       = (empty binary)

HKCU\Software\RegisteredApplications\BrowserAptor
  = "Software\Clients\StartMenuInternet\BrowserAptor\Capabilities"
```

All these keys are under `HKCU`, so no administrator rights are needed. Windows 10/11
reads `HKCU` registrations for the default-app picker.

`Unregister()` deletes:
- `HKCU\Software\Classes\BrowserAptor.Url` (entire tree)
- `HKCU\Software\Clients\StartMenuInternet\BrowserAptor` (entire tree)
- `HKCU\Software\RegisteredApplications\BrowserAptor` (value only)

---

### Output Formatting Pipeline

`OutputFormatter.Format(browsers, format, displayNames?)` dispatches to one of five
private methods:

**`FormatList`** (default):
```
[id]  DisplayLabel → "exe.exe" --profile-directory="X" "<url>"
```

**`FormatJson`**:
Serialises a list of objects: `{ id, browserName, profileName, userName, executablePath,
launchCommand }`. Uses `System.Text.Json` with camelCase naming and indentation.

**`FormatYaml`**:
Hand-written YAML serialiser (no third-party YAML library) producing:
```yaml
browsers:
  - id: google-chrome/default
    browserName: Google Chrome
    ...
```

**`FormatCsv`**:
Emits a header row followed by one data row per profile. Values are quoted if they contain
commas or quotes.

**`FormatTable`**:
Calculates maximum column widths, prints a header separator, then one row per profile
with padded columns.

In all formats, if `displayNames` is provided and has a custom name for a profile's `Id`,
that custom name replaces the default derived label.

---

### Display Name Persistence

`DisplayNameStore` serialises a `Dictionary<string, string>` (key = profile ID, value =
display name) to `%APPDATA%\BrowserAptor\displaynames.json`.

The file is loaded once on first access. `SetDisplayName` and `RemoveDisplayName` both
immediately rewrite the file using `System.Text.Json`. ID lookups are
case-insensitive (`StringComparer.OrdinalIgnoreCase`).

---

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Core library targets `net10.0` (no Windows suffix) | Allows xUnit tests to run cross-platform in CI on Linux runners |
| No DI container | Keeps the WPF startup simple; interfaces (`IBrowserDetectionService`, `IBrowserLaunchService`) allow constructor injection in tests |
| `[SupportedOSPlatform("windows")]` annotations | The analyser enforces that Windows-specific APIs are not accidentally called from the cross-platform Core |
| Slug-based IDs | Human-readable, stable across renames, safe for file names and JSON keys |
| HKCU-only registration | No UAC prompt required; works for standard users in enterprise environments |
| Catppuccin-inspired dark theme | High contrast on dark desktops; consistent with modern Windows 11 aesthetics |
| xUnit with no mock framework | Tests are simple enough to construct real objects; avoids a heavy mocking dependency |

---

## Adding a New Browser

### 1. Chromium-based browser

Add a new entry to the `ChromiumLocalPaths` array in `BrowserDetectionService.cs`:

```csharp
(@"Vendor\BrowserName\User Data", "Friendly Display Name"),
```

The detection, profile reading, and deduplication logic will handle the rest automatically.

### 2. Firefox-based browser (fork)

Firefox forks that use the same `profiles.ini` format need a new profiles.ini path in
`ReadFirefoxProfiles()`. Add the `profiles.ini` path to the list of INI files the method
checks, for example:

```csharp
// Waterfox
Path.Combine(appData, "Waterfox", "profiles.ini"),
```

### 3. Entirely new browser type

1. Add a new value to the `BrowserType` enum.
2. Implement a detection pass in `BrowserDetectionService` (similar to
   `DetectChromiumBrowsers` or `DetectFirefoxBrowsers`).
3. Update `BrowserProfile.BuildArguments()` with the correct CLI flag for that browser.
4. Add tests in `BrowserTests.cs`.

### 4. Write a test

Always add a test to verify the new browser can be detected. Use the pattern in
`ChromiumProfileReadingTests` (create a real temp directory with the expected structure,
call the reader, assert the profile list).

---

## Adding a New Output Format

1. Add a new `private static string FormatXxx(...)` method in `OutputFormatter.cs` that
   takes `List<BrowserInfo>` and `DisplayNameStore?`.
2. Add a new `case "xxx":` branch to the `Format` switch.
3. Add tests in `OutputFormatterTests.cs` following the existing pattern.
4. Update the help text in `CliHandler.PrintHelp()` to advertise the new format.
5. Update `docs/cli.md` (and the website) accordingly.

---

## Debugging Tips

### Running Core tests without Windows

All tests in `BrowserAptor.Tests` are cross-platform. On Linux or macOS:

```bash
dotnet test tests/BrowserAptor.Tests/BrowserAptor.Tests.csproj
```

Tests that call `BrowserDetectionService.DetectBrowsers()` are guarded with
`[SupportedOSPlatform("windows")]` and will be skipped on non-Windows platforms.

### Simulating the registry

For development without touching your real registry:
- Use `BrowserRegistrar.Register()` freely — it only writes to `HKCU` and can be reversed
  with `BrowserRegistrar.Unregister()`.
- You can also manually inspect the keys with `regedit.exe` under
  `HKCU\Software\Classes\BrowserAptor.Url` and
  `HKCU\Software\Clients\StartMenuInternet\BrowserAptor`.

### Debugging profile detection

Run the CLI command to list all detected profiles:

```powershell
.\BrowserAptor.exe --list-browsers --format table
```

Or use the JSON format to inspect IDs and paths:

```powershell
.\BrowserAptor.exe --list-browsers --format json
```

### Attaching the debugger

In Visual Studio, set `BrowserAptor` as the startup project and add a URL as the command
line argument in **Project → Properties → Debug → Application arguments**:

```
https://example.com
```

The selector window will open immediately when you press F5.

### WPF designer

Open `Views/BrowserSelectorWindow.xaml` in Visual Studio 2022 for the XAML designer. The
designer requires the Windows workload to be installed.
