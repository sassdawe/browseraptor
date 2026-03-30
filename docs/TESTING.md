# BrowserAptor Test Documentation

This document describes the BrowserAptor test suite: how to run it, how it is organised,
what each test class validates, and how to add new tests.

---

## Table of Contents

1. [Test Infrastructure](#test-infrastructure)
2. [Running Tests](#running-tests)
3. [Test Suite Organisation](#test-suite-organisation)
4. [Test Classes](#test-classes)
   - [BrowserProfileTests](#browserprofiletests)
   - [FirefoxProfileParsingTests](#firefoxprofileparsintegests)
   - [BrowserInfoTests](#browserinfotests)
   - [BrowserIdTests](#browseridtests)
   - [DisplayNameStoreTests](#displaynamestoretests)
   - [ChromiumProfileReadingTests](#chromiumprofilereadingtests)
   - [BrowserDetectionServiceTests](#browserdetectionservicetests)
   - [OutputFormatterTests](#outputformattertests)
5. [Test Patterns Used](#test-patterns-used)
6. [Cross-Platform Testing](#cross-platform-testing)
7. [Adding New Tests](#adding-new-tests)
8. [Coverage Reporting](#coverage-reporting)

---

## Test Infrastructure

| Component | Version | Purpose |
|-----------|---------|---------|
| [xUnit](https://xunit.net/) | 2.9.3 | Test framework — `[Fact]`, `[Theory]`, `[InlineData]` |
| [Microsoft.NET.Test.Sdk](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk) | 18.3.0 | Test host and runner integration |
| [xunit.runner.visualstudio](https://www.nuget.org/packages/xunit.runner.visualstudio) | 3.1.5 | Visual Studio Test Explorer integration |
| [coverlet.collector](https://github.com/coverlet-coverage/coverlet) | 8.0.1 | Code coverage collector (DataCollector mode) |

The test project targets `net10.0` (no Windows suffix). This means it can run on Linux,
macOS, and Windows. `EnableWindowsTargeting` is set to `true` to allow the project to
reference `BrowserAptor.Core` without build errors on non-Windows, but no Windows-specific
code runs in the tests themselves.

---

## Running Tests

### All Tests — CLI

```bash
dotnet test tests/BrowserAptor.Tests/BrowserAptor.Tests.csproj
```

### All Tests — with Verbose Output

```bash
dotnet test tests/BrowserAptor.Tests/BrowserAptor.Tests.csproj \
    --logger "console;verbosity=normal"
```

### Filter by Test Class

```bash
# Run only FirefoxProfileParsingTests
dotnet test tests/BrowserAptor.Tests/BrowserAptor.Tests.csproj \
    --filter "FullyQualifiedName~FirefoxProfileParsingTests"

# Run only OutputFormatterTests
dotnet test tests/BrowserAptor.Tests/BrowserAptor.Tests.csproj \
    --filter "FullyQualifiedName~OutputFormatterTests"
```

### Filter by Test Method

```bash
dotnet test tests/BrowserAptor.Tests/BrowserAptor.Tests.csproj \
    --filter "FullyQualifiedName~BuildArguments_ChromiumProfile"
```

### Visual Studio Test Explorer

Open the solution in Visual Studio 2022. The Test Explorer pane (View → Test Explorer)
will discover and run all xUnit tests. You can right-click any class or method to run or
debug individual tests.

### VS Code

Install the **C# Dev Kit** extension. Tests are discoverable via the Testing panel in the
activity bar. Individual tests can be run or debugged from the inline CodeLens UI.

---

## Test Suite Organisation

```
tests/BrowserAptor.Tests/
├── BrowserTests.cs          # ~664 lines — core model and service tests
└── OutputFormatterTests.cs  # ~286 lines — output format tests
```

Tests are grouped into **public test classes** inside these two files. Each class focuses
on one type, service, or logical feature.

---

## Test Classes

### BrowserProfileTests

**File:** `BrowserTests.cs`

Tests `BrowserProfile.BuildArguments(url)` and `BrowserProfile.ToString()`.

| Test | What it validates |
|------|-----------------|
| `ChromiumProfile_BuildArguments_IncludesProfileDirectory` | Chromium args include `--profile-directory="Profile 1"` and the quoted URL |
| `ChromiumProfile_BuildArguments_DefaultProfile` | Chromium "Default" profile produces correct flag |
| `ChromiumProfile_BuildArguments_EmptyProfileDirectory_JustUrl` | When `ProfileDirectory` is empty, only the URL is returned |
| `FirefoxProfile_BuildArguments_UsesDashP` | Firefox args use `-P "profileName"` |
| `BrowserProfile_ToString_FormatsCorrectly` (Theory) | With `UserName=null` → just `Name`; with `UserName` set → `"Name (email)"` |

---

### FirefoxProfileParsingTests

**File:** `BrowserTests.cs`

Tests `FirefoxProfileParser.ParseProfilesIni()` with real temporary files.

| Test | What it validates |
|------|-----------------|
| `ParseFirefoxProfilesIni_ParsesSingleProfile` | Single `[Profile0]` section is parsed to one `BrowserProfile` |
| `ParseFirefoxProfilesIni_ParsesMultipleProfiles` | Multiple `[ProfileN]` sections produce multiple profiles |
| `ParseFirefoxProfilesIni_EmptyFile_ReturnsNoProfiles` | Empty INI returns an empty list |
| `ParseFirefoxProfilesIni_InstallSection_Ignored` | `[Install...]` sections do not produce profiles |
| `ParseFirefoxProfilesIni_ParsesNewStyleFriendlyProfileNames` | Profile names like `default-release` are preserved verbatim |
| `ParseFirefoxProfilesIni_DuplicateNames_AreDeduplicatedByGroupBy` | Profiles with duplicate names are handled gracefully |
| `ParseFirefoxProfilesIni_LibreWolfBrowser_ParsesProfiles` | Same parser works for LibreWolf `profiles.ini` content |

Each test writes a real INI file to `Path.GetTempPath()`, calls the parser, asserts the
result, and deletes the file in a `finally` block.

---

### BrowserInfoTests

**File:** `BrowserTests.cs`

Tests basic `BrowserInfo` behaviour.

| Test | What it validates |
|------|-----------------|
| `BrowserInfo_ToString_ReturnsName` | `ToString()` returns the `Name` property |
| `BrowserInfo_DefaultProfiles_IsEmpty` | A freshly constructed `BrowserInfo` has an empty `Profiles` list |

---

### BrowserIdTests

**File:** `BrowserTests.cs`

Tests the `Id` computed property on both `BrowserInfo` and `BrowserProfile`, which uses
the internal `Slugify()` helper.

| Test | What it validates |
|------|-----------------|
| `BrowserInfo_Id_SlugifiesName` (Theory with 7 cases) | Various browser name strings produce the expected slug (e.g. `"Google Chrome"` → `"google-chrome"`, `"Mozilla Firefox"` → `"mozilla-firefox"`) |
| `BrowserProfile_Id_Chromium_UsesProfileDirectory` | Chromium default profile: ID is `"{browserSlug}/default"` |
| `BrowserProfile_Id_Chromium_WorkProfile` | Chromium "Profile 1": ID is `"{browserSlug}/profile-1"` |
| `BrowserProfile_Id_Firefox_UsesProfileName` | Firefox profile with a `Path` containing slashes (random dir): ID uses the profile `Name` slug, not the path |
| `BrowserProfile_Id_Firefox_WorkProfile` | Firefox profile named "Work Profile" → slug `"work-profile"` |

---

### DisplayNameStoreTests

**File:** `BrowserTests.cs`

Tests `DisplayNameStore` using a custom file path (injected via constructor) to avoid
touching `%APPDATA%` during tests.

| Test | What it validates |
|------|-----------------|
| `GetDisplayName_UnknownId_ReturnsNull` | Returns `null` when no custom name is set |
| `SetAndGetDisplayName_ReturnsCustomName` | Round-trips a custom name through set → get |
| `SetDisplayName_IsCaseInsensitiveOnLookup` | IDs are matched case-insensitively |
| `SetDisplayName_PersistsToDisk` | After `SetDisplayName`, a new store loaded from the same file returns the same value |
| `RemoveDisplayName_RemovesEntry` | After `RemoveDisplayName`, `GetDisplayName` returns `null` |
| `Load_CorruptFile_ReturnsEmptyStore` | A corrupt JSON file results in an empty store without throwing |

Each test creates a unique temp file path, exercises the store, and cleans up in a
`finally` block.

---

### ChromiumProfileReadingTests

**File:** `BrowserTests.cs`

Tests `ChromiumProfileReader.ReadProfilesFromDir()` using real temporary directories on
the file system.

| Test | What it validates |
|------|-----------------|
| `ReadChromiumProfilesFromDir_LocalState_SingleProfile` | Reads one profile from a hand-crafted `Local State` JSON |
| `ReadChromiumProfilesFromDir_LocalState_MultipleProfiles` | Reads multiple profiles from `info_cache` |
| `ReadChromiumProfilesFromDir_FallbackScan_FindsDefaultAndProfiles` | When `Local State` is absent, scans subdirs named `Default` and `Profile N`, reading `Preferences` JSON |
| `ReadChromiumProfilesFromDir_EmptyUserDataDir_ReturnsNoProfiles` | Empty directory returns empty list |
| `ReadChromiumProfilesFromDir_InvalidLocalStateJson_FallsBackToDirectoryScan` | Malformed JSON in `Local State` triggers the directory-scan fallback |

The setup helper `CreateUserDataDir(localStateJson)` creates a temp directory and writes
a `Local State` file, returning the directory path. Cleanup is done via `Directory.Delete`
in `finally` blocks.

---

### BrowserDetectionServiceTests

**File:** `BrowserTests.cs`

Integration tests for `BrowserDetectionService.DetectBrowsers()`. Because this service
reads from the Windows registry and `%LOCALAPPDATA%`, these tests are only meaningful on
Windows.

The tests use `Assert.SkipWhen` (or are decorated with platform checks) to skip gracefully
on non-Windows CI runners.

What is validated on Windows:
- `DetectBrowsers()` returns a non-null, non-throwing list.
- All returned `BrowserInfo` objects have a non-empty `Name` and `ExecutablePath`.
- All returned `BrowserProfile` objects have a non-null `Browser` back-reference.
- De-duplication: no two browsers share the same `ExecutablePath` (case-insensitive).
- Each browser has at least one profile.

---

### OutputFormatterTests

**File:** `OutputFormatterTests.cs`

Tests all five output formats of `OutputFormatter.Format()`. Uses in-memory `BrowserInfo`
and `BrowserProfile` objects — no file system access required.

#### Helper methods

- `MakeChromium(name, params profiles)` — creates a Chromium `BrowserInfo` with given profiles
- `MakeFirefox(params profiles)` — creates a Firefox `BrowserInfo` with given profiles
- `SingleChromiumBrowser()` — Chrome with one "Default" profile
- `MultiBrowserList()` — Chrome (2 profiles) + Firefox (1 profile)

#### FormatList tests

| Test | What it validates |
|------|-----------------|
| `FormatList_SingleProfile_ShowsBrowserName` | Browser name appears in output |
| `FormatList_MultiProfile_ShowsBrowserAndProfileName` | Multi-profile browser shows `BrowserName – ProfileName` |
| `FormatList_ShowsExecutablePath` | Output contains the exe path |
| `FormatList_ShowsProfileId` | Output contains the profile ID |
| `FormatList_WithDisplayName_ShowsCustomName` | Custom name from `DisplayNameStore` overrides default |

#### FormatJson tests

| Test | What it validates |
|------|-----------------|
| `FormatJson_ProducesValidJson` | Output parses without exception |
| `FormatJson_ContainsBrowserName` | Browser name appears in JSON values |
| `FormatJson_ContainsProfileId` | Profile ID appears in JSON |
| `FormatJson_WithDisplayName_OverridesLabel` | Custom display name replaces default label |

#### FormatYaml tests

| Test | What it validates |
|------|-----------------|
| `FormatYaml_ContainsBrowsersKey` | Output starts with `browsers:` |
| `FormatYaml_ContainsProfileId` | Profile ID appears in YAML |
| `FormatYaml_ContainsBrowserName` | Browser name appears in YAML |

#### FormatCsv tests

| Test | What it validates |
|------|-----------------|
| `FormatCsv_HasHeaderRow` | First line contains `Id,BrowserName,...` |
| `FormatCsv_HasDataRow` | At least one data row is present |
| `FormatCsv_CorrectNumberOfColumns` | Each row has the expected column count |
| `FormatCsv_QuotesValuesWithCommas` | Values containing commas are double-quoted |

#### FormatTable tests

| Test | What it validates |
|------|-----------------|
| `FormatTable_HasHeaderRow` | Column headers appear in the output |
| `FormatTable_HasSeparatorLine` | A line of dashes separates headers from data |
| `FormatTable_DataRowsPresent` | At least one data row after the separator |
| `FormatTable_ColumnsAligned` | Columns are padded to uniform width |

#### Edge cases

| Test | What it validates |
|------|-----------------|
| `Format_EmptyBrowserList_ReturnsEmptyOrHeader` | No browsers → empty string or header only (no crash) |
| `Format_UnknownFormat_FallsBackToList` | Unrecognised format string uses the default `list` format |
| `Format_NullDisplayNames_DoesNotThrow` | Passing `null` for `displayNames` works without exception |

---

## Test Patterns Used

### Real File System with Temp Files

Tests that validate file parsing (Firefox INI, Chromium Local State, display name JSON)
create actual files in a temporary directory. This avoids mocking the file system and
tests the real I/O path.

Pattern:
```csharp
string path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.ini");
File.WriteAllText(path, content);
try
{
    var result = Parser.Parse(path, browser);
    Assert.Equal(expected, result.Count);
}
finally
{
    File.Delete(path); // or Directory.Delete(dir, recursive: true)
}
```

### In-Memory Object Construction

For formatter and argument tests, `BrowserInfo` and `BrowserProfile` are constructed
directly without touching any external resource:

```csharp
var browser = new BrowserInfo
{
    Name = "Chrome",
    ExecutablePath = @"C:\chrome.exe",
    BrowserType = BrowserType.Chromium,
};
var profile = new BrowserProfile
{
    Name = "Default",
    ProfileDirectory = "Default",
    Browser = browser,
};
browser.Profiles.Add(profile);
```

### Theory + InlineData

Parameterised tests use `[Theory]` and `[InlineData]` to avoid test method duplication:

```csharp
[Theory]
[InlineData("Google Chrome",   "google-chrome")]
[InlineData("Mozilla Firefox", "mozilla-firefox")]
[InlineData("Microsoft Edge",  "microsoft-edge")]
public void BrowserInfo_Id_SlugifiesName(string name, string expectedId)
{
    var browser = new BrowserInfo { Name = name };
    Assert.Equal(expectedId, browser.Id);
}
```

### No Mocking Framework

There is no mocking library in this project. Interface boundaries (`IBrowserDetectionService`,
`IBrowserLaunchService`) allow test doubles to be written as simple hand-coded stubs when
needed.

---

## Cross-Platform Testing

The test project targets `net10.0` and runs on all platforms where the .NET 10 SDK is
installed. CI runs tests on:

- **Windows** — full test suite including any Windows-specific detection integration tests
- **Linux (Ubuntu)** — all Core model, service, and formatter tests; Windows-specific
  tests are skipped via platform guards

To run tests locally on Linux or macOS:

```bash
dotnet test tests/BrowserAptor.Tests/BrowserAptor.Tests.csproj
```

Tests that are Windows-only are either wrapped in `if (OperatingSystem.IsWindows())` or
decorated with `[SupportedOSPlatform("windows")]` which causes the xUnit runner to skip
them gracefully on other platforms.

---

## Adding New Tests

### Adding a test to an existing class

1. Open the relevant test file (`BrowserTests.cs` or `OutputFormatterTests.cs`).
2. Add a new `[Fact]` or `[Theory]` method to the appropriate class.
3. Follow the naming convention: `MethodName_Condition_ExpectedResult`.
4. Run `dotnet test` to confirm it passes.

### Adding a new test class

1. Add a new `public class MyFeatureTests { }` block at the bottom of `BrowserTests.cs`
   (or create a new `.cs` file in the test project if the class is large).
2. Add the necessary `using` directives.
3. Write your tests.

### Testing a new browser detection path

Use the pattern from `ChromiumProfileReadingTests`:

```csharp
[Fact]
public void ReadProfilesFromDir_MyNewBrowser_FindsProfiles()
{
    string dir = Path.Combine(Path.GetTempPath(), $"TestUserData_{Guid.NewGuid()}");
    Directory.CreateDirectory(dir);
    // Write the expected Local State or profiles.ini structure
    File.WriteAllText(Path.Combine(dir, "Local State"), myJson);
    try
    {
        var browser = new BrowserInfo { Name = "MyBrowser", BrowserType = BrowserType.Chromium };
        var profiles = ChromiumProfileReader.ReadProfilesFromDir(dir, browser);
        Assert.Single(profiles);
        Assert.Equal("Default", profiles[0].ProfileDirectory);
    }
    finally
    {
        Directory.Delete(dir, recursive: true);
    }
}
```

---

## Coverage Reporting

The test project includes `coverlet.collector` (version 8.0.1) as a data collector. To
generate a coverage report in Cobertura format:

```bash
dotnet test tests/BrowserAptor.Tests/BrowserAptor.Tests.csproj \
    --collect "XPlat Code Coverage" \
    --results-directory coverage/
```

This writes a `coverage.cobertura.xml` file under `coverage/`. You can then generate an
HTML report using the `reportgenerator` tool:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
    -reports:"coverage/**/coverage.cobertura.xml" \
    -targetdir:"coverage/html" \
    -reporttypes:Html
```

Open `coverage/html/index.html` in a browser to view per-class and per-method coverage.

In CI, coverage is collected automatically and reported as part of the workflow run
artifacts.
