using BrowserAptor.Models;
using BrowserAptor.Services;

namespace BrowserAptor.Tests;

public class BrowserProfileTests
{
    private static BrowserInfo MakeChromiumBrowser(string name = "Chrome") =>
        new() { Name = name, ExecutablePath = @"C:\chrome.exe", BrowserType = BrowserType.Chromium };

    private static BrowserInfo MakeFirefoxBrowser() =>
        new() { Name = "Firefox", ExecutablePath = @"C:\firefox.exe", BrowserType = BrowserType.Firefox };

    [Fact]
    public void ChromiumProfile_BuildArguments_IncludesProfileDirectory()
    {
        var browser = MakeChromiumBrowser();
        var profile = new BrowserProfile { Name = "Work", ProfileDirectory = "Profile 1", Browser = browser };

        string args = profile.BuildArguments("https://example.com");

        Assert.Contains("--profile-directory=\"Profile 1\"", args);
        Assert.Contains("\"https://example.com\"", args);
    }

    [Fact]
    public void ChromiumProfile_BuildArguments_DefaultProfile()
    {
        var browser = MakeChromiumBrowser();
        var profile = new BrowserProfile { Name = "Default", ProfileDirectory = "Default", Browser = browser };

        string args = profile.BuildArguments("https://example.com");

        Assert.Contains("--profile-directory=\"Default\"", args);
        Assert.Contains("\"https://example.com\"", args);
    }

    [Fact]
    public void ChromiumProfile_BuildArguments_EmptyProfileDirectory_JustUrl()
    {
        var browser = MakeChromiumBrowser();
        var profile = new BrowserProfile { Name = "Default", ProfileDirectory = string.Empty, Browser = browser };

        string args = profile.BuildArguments("https://example.com");

        Assert.Equal("\"https://example.com\"", args);
    }

    [Fact]
    public void FirefoxProfile_BuildArguments_UsesDashP()
    {
        var browser = MakeFirefoxBrowser();
        var profile = new BrowserProfile { Name = "myprofile", ProfileDirectory = "path/to/profile", Browser = browser };

        string args = profile.BuildArguments("https://example.com");

        Assert.Contains("-P \"myprofile\"", args);
        Assert.Contains("\"https://example.com\"", args);
    }

    [Theory]
    [InlineData("Work", null, "Work")]
    [InlineData("Default", "user@example.com", "Default (user@example.com)")]
    public void BrowserProfile_ToString_FormatsCorrectly(string name, string? userName, string expected)
    {
        var browser = MakeChromiumBrowser();
        var profile = new BrowserProfile { Name = name, UserName = userName, Browser = browser };

        Assert.Equal(expected, profile.ToString());
    }
}

public class FirefoxProfileParsingTests
{
    [Fact]
    public void ParseFirefoxProfilesIni_ParsesSingleProfile()
    {
        string iniContent = """
            [General]
            StartWithLastProfile=1

            [Profile0]
            Name=default-release
            IsRelative=1
            Path=Profiles/abc123.default-release

            """;

        string iniPath = Path.Combine(Path.GetTempPath(), $"profiles_{Guid.NewGuid()}.ini");
        File.WriteAllText(iniPath, iniContent);

        try
        {
            var browser = new BrowserInfo
            {
                Name = "Firefox",
                ExecutablePath = @"C:\firefox.exe",
                BrowserType = BrowserType.Firefox
            };

            var profiles = FirefoxProfileParser.ParseProfilesIni(iniPath, browser);

            Assert.Single(profiles);
            Assert.Equal("default-release", profiles[0].Name);
            Assert.Equal("Profiles/abc123.default-release", profiles[0].ProfileDirectory);
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    [Fact]
    public void ParseFirefoxProfilesIni_ParsesMultipleProfiles()
    {
        string iniContent = """
            [Profile0]
            Name=default-release
            IsRelative=1
            Path=Profiles/abc.default-release

            [Profile1]
            Name=Work
            IsRelative=1
            Path=Profiles/def.Work

            """;

        string iniPath = Path.Combine(Path.GetTempPath(), $"profiles_{Guid.NewGuid()}.ini");
        File.WriteAllText(iniPath, iniContent);

        try
        {
            var browser = new BrowserInfo
            {
                Name = "Firefox",
                ExecutablePath = @"C:\firefox.exe",
                BrowserType = BrowserType.Firefox
            };

            var profiles = FirefoxProfileParser.ParseProfilesIni(iniPath, browser);

            Assert.Equal(2, profiles.Count);
            Assert.Equal("default-release", profiles[0].Name);
            Assert.Equal("Work", profiles[1].Name);
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    [Fact]
    public void ParseFirefoxProfilesIni_EmptyFile_ReturnsNoProfiles()
    {
        string iniPath = Path.Combine(Path.GetTempPath(), $"profiles_{Guid.NewGuid()}.ini");
        File.WriteAllText(iniPath, string.Empty);

        try
        {
            var browser = new BrowserInfo
            {
                Name = "Firefox",
                ExecutablePath = @"C:\firefox.exe",
                BrowserType = BrowserType.Firefox
            };

            var profiles = FirefoxProfileParser.ParseProfilesIni(iniPath, browser);

            Assert.Empty(profiles);
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    [Fact]
    public void ParseFirefoxProfilesIni_InstallSection_Ignored()
    {
        string iniContent = """
            [Install12345]
            Default=Profiles/abc.default-release
            Locked=1

            [Profile0]
            Name=default-release
            IsRelative=1
            Path=Profiles/abc.default-release

            """;

        string iniPath = Path.Combine(Path.GetTempPath(), $"profiles_{Guid.NewGuid()}.ini");
        File.WriteAllText(iniPath, iniContent);

        try
        {
            var browser = new BrowserInfo
            {
                Name = "Firefox",
                ExecutablePath = @"C:\firefox.exe",
                BrowserType = BrowserType.Firefox
            };

            var profiles = FirefoxProfileParser.ParseProfilesIni(iniPath, browser);

            Assert.Single(profiles);
            Assert.Equal("default-release", profiles[0].Name);
        }
        finally
        {
            File.Delete(iniPath);
        }
    }

    /// <summary>
    /// Regression test: Firefox 128+ profile manager creates profiles with friendly
    /// user-supplied names (e.g. "blue", "Green", "orange", "Original profile").
    /// The parser must handle any Name= value, not just the legacy default* names.
    /// </summary>
    [Fact]
    public void ParseFirefoxProfilesIni_ParsesNewStyleFriendlyProfileNames()
    {
        string iniContent = """
            [Profile0]
            Name=default-release
            IsRelative=1
            Path=Profiles/abc.default-release
            Default=1

            [Profile1]
            Name=blue
            IsRelative=1
            Path=Profiles/def.blue

            [Profile2]
            Name=Green
            IsRelative=1
            Path=Profiles/ghi.Green

            [Profile3]
            Name=orange
            IsRelative=1
            Path=Profiles/jkl.orange

            [Profile4]
            Name=Original profile
            IsRelative=1
            Path=Profiles/mno.original-profile

            """;

        string iniPath = Path.Combine(Path.GetTempPath(), $"profiles_{Guid.NewGuid()}.ini");
        File.WriteAllText(iniPath, iniContent);

        try
        {
            var browser = new BrowserInfo
            {
                Name = "Mozilla Firefox",
                ExecutablePath = @"C:\Program Files\Mozilla Firefox\firefox.exe",
                BrowserType = BrowserType.Firefox
            };

            var profiles = FirefoxProfileParser.ParseProfilesIni(iniPath, browser);

            Assert.Equal(5, profiles.Count);
            Assert.Contains(profiles, p => p.Name == "default-release");
            Assert.Contains(profiles, p => p.Name == "blue");
            Assert.Contains(profiles, p => p.Name == "Green");
            Assert.Contains(profiles, p => p.Name == "orange");
            Assert.Contains(profiles, p => p.Name == "Original profile");
        }
        finally
        {
            if (File.Exists(iniPath)) File.Delete(iniPath);
        }
    }

    /// <summary>
    /// When the same profiles.ini content is parsed twice (e.g. both Roaming and
    /// LocalAppData copies contain the same profile), de-duplication by Name should
    /// yield a single entry.  This test exercises the GroupBy logic that guards
    /// <see cref="FirefoxProfileParser.ParseProfilesIni"/>.
    /// </summary>
    [Fact]
    public void ParseFirefoxProfilesIni_DuplicateNames_AreDeduplicatedByGroupBy()
    {
        string iniContent = """
            [Profile0]
            Name=default-release
            IsRelative=1
            Path=Profiles/abc.default-release

            """;

        string iniPath = Path.Combine(Path.GetTempPath(), $"profiles_{Guid.NewGuid()}.ini");
        File.WriteAllText(iniPath, iniContent);

        try
        {
            var browser = new BrowserInfo
            {
                Name = "Mozilla Firefox",
                ExecutablePath = @"C:\firefox.exe",
                BrowserType = BrowserType.Firefox
            };

            // Simulate reading the same ini from two locations and merging
            var fromRoaming = FirefoxProfileParser.ParseProfilesIni(iniPath, browser);
            var fromLocal   = FirefoxProfileParser.ParseProfilesIni(iniPath, browser);
            var merged = fromRoaming
                .Concat(fromLocal)
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            Assert.Single(merged);
            Assert.Equal("default-release", merged[0].Name);
        }
        finally
        {
            if (File.Exists(iniPath)) File.Delete(iniPath);
        }
    }

    /// <summary>
    /// LibreWolf uses an identical profiles.ini format to Firefox.
    /// The parser must return profiles regardless of the browser object passed in.
    /// </summary>
    [Fact]
    public void ParseFirefoxProfilesIni_LibreWolfBrowser_ParsesProfiles()
    {
        string iniContent = """
            [Profile0]
            Name=default
            IsRelative=1
            Path=Profiles/xyz.default

            [Profile1]
            Name=Work
            IsRelative=1
            Path=Profiles/uvw.Work

            """;

        string iniPath = Path.Combine(Path.GetTempPath(), $"profiles_{Guid.NewGuid()}.ini");
        File.WriteAllText(iniPath, iniContent);

        try
        {
            var browser = new BrowserInfo
            {
                Name = "LibreWolf",
                ExecutablePath = @"C:\Program Files\LibreWolf\librewolf.exe",
                BrowserType = BrowserType.Firefox
            };

            var profiles = FirefoxProfileParser.ParseProfilesIni(iniPath, browser);

            Assert.Equal(2, profiles.Count);
            Assert.Contains(profiles, p => p.Name == "default");
            Assert.Contains(profiles, p => p.Name == "Work");
            // Profile IDs should be scoped to the LibreWolf browser
            Assert.All(profiles, p => Assert.StartsWith("librewolf/", p.Id));
        }
        finally
        {
            if (File.Exists(iniPath)) File.Delete(iniPath);
        }
    }
}

public class BrowserInfoTests
{
    [Fact]
    public void BrowserInfo_ToString_ReturnsName()
    {
        var browser = new BrowserInfo { Name = "Google Chrome" };
        Assert.Equal("Google Chrome", browser.ToString());
    }

    [Fact]
    public void BrowserInfo_DefaultProfiles_IsEmpty()
    {
        var browser = new BrowserInfo();
        Assert.Empty(browser.Profiles);
    }

    [Theory]
    [InlineData("Google Chrome",              null)]
    [InlineData("Microsoft Edge",             null)]
    [InlineData("Mozilla Firefox",            null)]
    [InlineData("Microsoft Edge Beta",        "Beta")]
    [InlineData("Microsoft Edge Dev",         "Dev")]
    [InlineData("Microsoft Edge Canary",      "Canary")]
    [InlineData("Google Chrome Beta",         "Beta")]
    [InlineData("Google Chrome Dev",          "Dev")]
    [InlineData("Google Chrome Canary",       "Canary")]
    [InlineData("Firefox Nightly",            "Nightly")]
    [InlineData("Firefox Developer Edition",  "Dev")]
    [InlineData("Brave Browser Beta",         "Beta")]
    public void BrowserInfo_Channel_DetectsReleaseChannel(string name, string? expectedChannel)
    {
        var browser = new BrowserInfo { Name = name };
        Assert.Equal(expectedChannel, browser.Channel);
    }
}

public class BrowserIdTests
{
    [Theory]
    [InlineData("Google Chrome",        "google-chrome")]
    [InlineData("Microsoft Edge",       "microsoft-edge")]
    [InlineData("Mozilla Firefox",      "mozilla-firefox")]
    [InlineData("Vivaldi",              "vivaldi")]
    [InlineData("Brave Browser Beta",   "brave-browser-beta")]
    [InlineData("Firefox ESR",          "firefox-esr")]
    [InlineData("",                     "unknown")]
    public void BrowserInfo_Id_SlugifiesName(string name, string expectedId)
    {
        var browser = new BrowserInfo { Name = name };
        Assert.Equal(expectedId, browser.Id);
    }

    [Fact]
    public void BrowserProfile_Id_Chromium_UsesProfileDirectory()
    {
        var browser = new BrowserInfo { Name = "Microsoft Edge", BrowserType = BrowserType.Chromium,
                                        ExecutablePath = @"C:\msedge.exe" };
        var profile = new BrowserProfile { Name = "Personal", ProfileDirectory = "Default", Browser = browser };
        Assert.Equal("microsoft-edge/default", profile.Id);
    }

    [Fact]
    public void BrowserProfile_Id_Chromium_WorkProfile()
    {
        var browser = new BrowserInfo { Name = "Microsoft Edge", BrowserType = BrowserType.Chromium,
                                        ExecutablePath = @"C:\msedge.exe" };
        var profile = new BrowserProfile { Name = "Work", ProfileDirectory = "Profile 1", Browser = browser };
        Assert.Equal("microsoft-edge/profile-1", profile.Id);
    }

    [Fact]
    public void BrowserProfile_Id_Firefox_UsesProfileName()
    {
        // Firefox ProfileDirectory contains path separators → fall back to Name
        var browser = new BrowserInfo { Name = "Mozilla Firefox", BrowserType = BrowserType.Firefox,
                                        ExecutablePath = @"C:\firefox.exe" };
        var profile = new BrowserProfile
        {
            Name = "default-release",
            ProfileDirectory = "Profiles/abc123.default-release",
            Browser = browser
        };
        Assert.Equal("mozilla-firefox/default-release", profile.Id);
    }

    [Fact]
    public void BrowserProfile_Id_Firefox_WorkProfile()
    {
        var browser = new BrowserInfo { Name = "Mozilla Firefox", BrowserType = BrowserType.Firefox,
                                        ExecutablePath = @"C:\firefox.exe" };
        var profile = new BrowserProfile
        {
            Name = "Work",
            ProfileDirectory = "Profiles/xyz.Work",
            Browser = browser
        };
        Assert.Equal("mozilla-firefox/work", profile.Id);
    }
}

public class DisplayNameStoreTests
{
    private static string TempFile() =>
        Path.Combine(Path.GetTempPath(), $"displaynames_{Guid.NewGuid():N}.json");

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort cleanup */ }
    }

    [Fact]
    public void GetDisplayName_UnknownId_ReturnsNull()
    {
        var store = new DisplayNameStore(TempFile());
        Assert.Null(store.GetDisplayName("microsoft-edge/default"));
    }

    [Fact]
    public void SetAndGetDisplayName_ReturnsCustomName()
    {
        var store = new DisplayNameStore(TempFile());
        store.SetDisplayName("microsoft-edge/default", "My Edge");
        Assert.Equal("My Edge", store.GetDisplayName("microsoft-edge/default"));
    }

    [Fact]
    public void SetDisplayName_IsCaseInsensitiveOnLookup()
    {
        string path = TempFile();
        var store = new DisplayNameStore(path);
        store.SetDisplayName("microsoft-edge/default", "My Edge");

        // Re-load from disk to exercise the case-insensitive dict
        var store2 = new DisplayNameStore(path);
        Assert.Equal("My Edge", store2.GetDisplayName("MICROSOFT-EDGE/DEFAULT"));
    }

    [Fact]
    public void SetDisplayName_PersistsToDisk()
    {
        string path = TempFile();
        try
        {
            var store1 = new DisplayNameStore(path);
            store1.SetDisplayName("mozilla-firefox/work", "Work Browser");

            // Load from the same file in a new instance
            var store2 = new DisplayNameStore(path);
            Assert.Equal("Work Browser", store2.GetDisplayName("mozilla-firefox/work"));
        }
        finally { TryDeleteFile(path); }
    }

    [Fact]
    public void RemoveDisplayName_RemovesEntry()
    {
        string path = TempFile();
        try
        {
            var store = new DisplayNameStore(path);
            store.SetDisplayName("vivaldi/default", "My Vivaldi");
            store.RemoveDisplayName("vivaldi/default");

            var store2 = new DisplayNameStore(path);
            Assert.Null(store2.GetDisplayName("vivaldi/default"));
        }
        finally { TryDeleteFile(path); }
    }

    [Fact]
    public void Load_CorruptFile_ReturnsEmptyStore()
    {
        string path = TempFile();
        try
        {
            File.WriteAllText(path, "not valid json {{{");
            var store = new DisplayNameStore(path);
            Assert.Null(store.GetDisplayName("anything"));
        }
        finally { TryDeleteFile(path); }
    }
}

public class ChromiumProfileReadingTests
{
    private static BrowserInfo MakeChromiumBrowser() =>
        new() { Name = "Google Chrome", ExecutablePath = @"C:\chrome.exe", BrowserType = BrowserType.Chromium };

    // Writes a Local State JSON with the given profile entries to a temp dir.
    private static string CreateUserDataDir(string localStateJson)
    {
        string dir = Path.Combine(Path.GetTempPath(), $"userdata_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Local State"), localStateJson);
        return dir;
    }

    [Fact]
    public void ReadChromiumProfilesFromDir_LocalState_SingleProfile()
    {
        string json = """
            {
              "profile": {
                "info_cache": {
                  "Default": { "name": "Personal", "user_name": "user@example.com" }
                }
              }
            }
            """;
        string dir = CreateUserDataDir(json);
        try
        {
            var browser = MakeChromiumBrowser();
            var profiles = ChromiumProfileReader.ReadProfilesFromDir(dir, browser);

            Assert.Single(profiles);
            Assert.Equal("Personal", profiles[0].Name);
            Assert.Equal("Default", profiles[0].ProfileDirectory);
            Assert.Equal("user@example.com", profiles[0].UserName);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ReadChromiumProfilesFromDir_LocalState_MultipleProfiles()
    {
        string json = """
            {
              "profile": {
                "info_cache": {
                  "Default":   { "name": "Personal" },
                  "Profile 1": { "name": "Work", "user_name": "work@corp.com" },
                  "Profile 2": { "name": "School" }
                }
              }
            }
            """;
        string dir = CreateUserDataDir(json);
        try
        {
            var browser = MakeChromiumBrowser();
            var profiles = ChromiumProfileReader.ReadProfilesFromDir(dir, browser);

            Assert.Equal(3, profiles.Count);
            Assert.Contains(profiles, p => p.Name == "Personal" && p.ProfileDirectory == "Default");
            Assert.Contains(profiles, p => p.Name == "Work"     && p.ProfileDirectory == "Profile 1" && p.UserName == "work@corp.com");
            Assert.Contains(profiles, p => p.Name == "School"   && p.ProfileDirectory == "Profile 2");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ReadChromiumProfilesFromDir_FallbackScan_FindsDefaultAndProfiles()
    {
        // No Local State file → fall back to directory scan
        string dir = Path.Combine(Path.GetTempPath(), $"userdata_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);

        // Create profile directories with a Preferences file each
        string defaultDir  = Path.Combine(dir, "Default");
        string profile1Dir = Path.Combine(dir, "Profile 1");
        Directory.CreateDirectory(defaultDir);
        Directory.CreateDirectory(profile1Dir);
        File.WriteAllText(Path.Combine(defaultDir,  "Preferences"), """{"profile":{"name":"Personal"}}""");
        File.WriteAllText(Path.Combine(profile1Dir, "Preferences"), """{"profile":{"name":"Work"}}""");

        try
        {
            var browser = MakeChromiumBrowser();
            var profiles = ChromiumProfileReader.ReadProfilesFromDir(dir, browser);

            Assert.Equal(2, profiles.Count);
            Assert.Contains(profiles, p => p.Name == "Personal" && p.ProfileDirectory == "Default");
            Assert.Contains(profiles, p => p.Name == "Work"     && p.ProfileDirectory == "Profile 1");
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ReadChromiumProfilesFromDir_EmptyUserDataDir_ReturnsNoProfiles()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"userdata_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        try
        {
            var browser = MakeChromiumBrowser();
            var profiles = ChromiumProfileReader.ReadProfilesFromDir(dir, browser);
            Assert.Empty(profiles);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ReadChromiumProfilesFromDir_InvalidLocalStateJson_FallsBackToDirectoryScan()
    {
        string dir = CreateUserDataDir("not valid json {{{{");
        string profileDir = Path.Combine(dir, "Default");
        Directory.CreateDirectory(profileDir);
        File.WriteAllText(Path.Combine(profileDir, "Preferences"), """{"profile":{"name":"Personal"}}""");
        try
        {
            var browser = MakeChromiumBrowser();
            var profiles = ChromiumProfileReader.ReadProfilesFromDir(dir, browser);
            // Should fall back to directory scan and find the Default profile
            Assert.Single(profiles);
            Assert.Equal("Personal", profiles[0].Name);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ReadChromiumProfilesFromDir_ThemeColors_ExtractsFrameColor()
    {
        // Packed ARGB 0xFF3366CC = -13408564 as int32  → #3366CC
        string json = """
            {
              "profile": {
                "info_cache": {
                  "Default": {
                    "name": "Personal",
                    "theme_colors": { "frame": -13408564 }
                  }
                }
              }
            }
            """;
        string dir = CreateUserDataDir(json);
        try
        {
            var browser = MakeChromiumBrowser();
            var profiles = ChromiumProfileReader.ReadProfilesFromDir(dir, browser);

            Assert.Single(profiles);
            Assert.Equal("#3366CC", profiles[0].ThemeColor);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Fact]
    public void ReadChromiumProfilesFromDir_NoThemeColors_ThemeColorIsNull()
    {
        string json = """
            {
              "profile": {
                "info_cache": {
                  "Default": { "name": "Personal" }
                }
              }
            }
            """;
        string dir = CreateUserDataDir(json);
        try
        {
            var browser = MakeChromiumBrowser();
            var profiles = ChromiumProfileReader.ReadProfilesFromDir(dir, browser);

            Assert.Single(profiles);
            Assert.Null(profiles[0].ThemeColor);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}

public class IncognitoProfileTests
{
    private static BrowserInfo MakeChromiumBrowser() =>
        new() { Name = "Chrome", ExecutablePath = @"C:\chrome.exe", BrowserType = BrowserType.Chromium };

    private static BrowserInfo MakeFirefoxBrowser() =>
        new() { Name = "Firefox", ExecutablePath = @"C:\firefox.exe", BrowserType = BrowserType.Firefox };

    [Fact]
    public void ChromiumIncognito_BuildArguments_UsesIncognitoFlag()
    {
        var browser = MakeChromiumBrowser();
        var profile = new BrowserProfile
        {
            Name = "Incognito",
            ProfileDirectory = string.Empty,
            Browser = browser,
            IsIncognito = true,
        };

        string args = profile.BuildArguments("https://example.com");

        Assert.Contains("--incognito", args);
        Assert.Contains("\"https://example.com\"", args);
        Assert.DoesNotContain("--profile-directory", args);
    }

    [Fact]
    public void FirefoxIncognito_BuildArguments_UsesPrivateWindowFlag()
    {
        var browser = MakeFirefoxBrowser();
        var profile = new BrowserProfile
        {
            Name = "Private Window",
            ProfileDirectory = string.Empty,
            Browser = browser,
            IsIncognito = true,
        };

        string args = profile.BuildArguments("https://example.com");

        Assert.Contains("-private-window", args);
        Assert.Contains("\"https://example.com\"", args);
        Assert.DoesNotContain("-P ", args);
    }

    [Fact]
    public void IncognitoProfile_Id_HasIncognitoSlug()
    {
        var browser = MakeChromiumBrowser();
        var profile = new BrowserProfile
        {
            Name = "Incognito",
            ProfileDirectory = string.Empty,
            Browser = browser,
            IsIncognito = true,
        };

        Assert.Equal("chrome/incognito", profile.Id);
    }

    [Fact]
    public void FirefoxIncognitoProfile_Id_HasIncognitoSlug()
    {
        var browser = MakeFirefoxBrowser();
        var profile = new BrowserProfile
        {
            Name = "Private Window",
            ProfileDirectory = string.Empty,
            Browser = browser,
            IsIncognito = true,
        };

        Assert.Equal("firefox/incognito", profile.Id);
    }

    [Fact]
    public void IncognitoProfile_IsNotShownInDisplayName_AsRegularProfile()
    {
        var browser = MakeChromiumBrowser();
        var profile = new BrowserProfile
        {
            Name = "Incognito",
            Browser = browser,
            IsIncognito = true,
        };

        Assert.Equal("Incognito", profile.ToString());
    }
}

public class UserPreferencesTests
{
    [Fact]
    public void DefaultPreferences_SingleClickIsDisabled()
    {
        string path = Path.Combine(Path.GetTempPath(), $"prefs_{Guid.NewGuid()}.json");
        try
        {
            var prefs = new UserPreferences(path);
            Assert.False(prefs.SingleClickToOpen);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void SetSingleClick_PersistsToDisk()
    {
        string path = Path.Combine(Path.GetTempPath(), $"prefs_{Guid.NewGuid()}.json");
        try
        {
            var prefs = new UserPreferences(path);
            prefs.SingleClickToOpen = true;
            prefs.Save();

            var loaded = new UserPreferences(path);
            Assert.True(loaded.SingleClickToOpen);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void DisableSingleClick_PersistsToDisk()
    {
        string path = Path.Combine(Path.GetTempPath(), $"prefs_{Guid.NewGuid()}.json");
        try
        {
            var prefs = new UserPreferences(path);
            prefs.SingleClickToOpen = true;
            prefs.Save();

            prefs.SingleClickToOpen = false;
            prefs.Save();

            var loaded = new UserPreferences(path);
            Assert.False(loaded.SingleClickToOpen);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Load_CorruptFile_UsesDefaults()
    {
        string path = Path.Combine(Path.GetTempPath(), $"prefs_{Guid.NewGuid()}.json");
        File.WriteAllText(path, "not valid json {{{");
        try
        {
            var prefs = new UserPreferences(path);
            Assert.False(prefs.SingleClickToOpen);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

public class FirefoxDefaultDefaultTests
{
    [Fact]
    public void ParseFirefoxProfiles_DefaultDefault_RemovedWhenDefaultExists()
    {
        string iniContent = """
            [Profile0]
            Name=default
            IsRelative=1
            Path=Profiles/abc.default

            [Profile1]
            Name=default-default
            IsRelative=1
            Path=Profiles/def.default-default

            [Profile2]
            Name=Work
            IsRelative=1
            Path=Profiles/ghi.Work

            """;

        string iniPath = Path.Combine(Path.GetTempPath(), $"profiles_{Guid.NewGuid()}.ini");
        File.WriteAllText(iniPath, iniContent);

        try
        {
            var browser = new BrowserInfo
            {
                Name = "Firefox",
                ExecutablePath = @"C:\firefox.exe",
                BrowserType = BrowserType.Firefox
            };

            var rawProfiles = FirefoxProfileParser.ParseProfilesIni(iniPath, browser);

            // Simulate the deduplication + default-default removal that BrowserDetectionService does
            var deduplicated = rawProfiles
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            if (deduplicated.Any(p => p.Name.Equals("default", StringComparison.OrdinalIgnoreCase)))
                deduplicated.RemoveAll(p =>
                    p.Name.Equals("default-default", StringComparison.OrdinalIgnoreCase));

            Assert.Equal(2, deduplicated.Count);
            Assert.Contains(deduplicated, p => p.Name == "default");
            Assert.Contains(deduplicated, p => p.Name == "Work");
            Assert.DoesNotContain(deduplicated, p => p.Name == "default-default");
        }
        finally
        {
            File.Delete(iniPath);
        }
    }
}
