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
}
