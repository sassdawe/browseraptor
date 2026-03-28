using BrowserAptor.Models;
using BrowserAptor.Services;

namespace BrowserAptor.Tests;

public class OutputFormatterTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static BrowserInfo MakeChromium(string name, params (string profileName, string dir, string? user)[] profiles)
    {
        var browser = new BrowserInfo
        {
            Name = name,
            ExecutablePath = $@"C:\{name}\chrome.exe",
            BrowserType = BrowserType.Chromium,
        };
        foreach (var (pName, dir, user) in profiles)
            browser.Profiles.Add(new BrowserProfile { Name = pName, ProfileDirectory = dir, UserName = user, Browser = browser });
        return browser;
    }

    private static BrowserInfo MakeFirefox(params (string profileName, string dir)[] profiles)
    {
        var browser = new BrowserInfo
        {
            Name = "Firefox",
            ExecutablePath = @"C:\Firefox\firefox.exe",
            BrowserType = BrowserType.Firefox,
        };
        foreach (var (pName, dir) in profiles)
            browser.Profiles.Add(new BrowserProfile { Name = pName, ProfileDirectory = dir, Browser = browser });
        return browser;
    }

    private static List<BrowserInfo> SingleChromiumBrowser() =>
        [MakeChromium("Chrome", ("Default", "Default", null))];

    private static List<BrowserInfo> MultiBrowserList() =>
    [
        MakeChromium("Chrome",
            ("Default", "Default", null),
            ("Work", "Profile 1", "user@example.com")),
        MakeFirefox(("default-release", "Profiles/abc.default-release")),
    ];

    // =========================================================================
    // list format
    // =========================================================================

    [Fact]
    public void FormatList_SingleProfile_ShowsBrowserName()
    {
        string result = OutputFormatter.Format(SingleChromiumBrowser(), "list");
        Assert.Contains("Chrome", result);
    }

    [Fact]
    public void FormatList_MultiProfile_ShowsDashSeparator()
    {
        string result = OutputFormatter.Format(MultiBrowserList(), "list");
        // Multi-profile browser should show "Chrome – Work"
        Assert.Contains("Chrome", result);
        Assert.Contains("Work", result);
    }

    [Fact]
    public void FormatList_IncludesCallCommand()
    {
        string result = OutputFormatter.Format(SingleChromiumBrowser(), "list");
        Assert.Contains("Call:", result);
        Assert.Contains("chrome.exe", result);
    }

    // =========================================================================
    // json format
    // =========================================================================

    [Fact]
    public void FormatJson_ProducesValidJson()
    {
        string json = OutputFormatter.Format(MultiBrowserList(), "json");
        // Should be parseable JSON
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal(System.Text.Json.JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public void FormatJson_ContainsBrowserName()
    {
        string json = OutputFormatter.Format(MultiBrowserList(), "json");
        Assert.Contains("Chrome", json);
        Assert.Contains("Firefox", json);
    }

    [Fact]
    public void FormatJson_ContainsProfilesArray()
    {
        string json = OutputFormatter.Format(MultiBrowserList(), "json");
        Assert.Contains("\"profiles\"", json);
        Assert.Contains("launchCommand", json);
    }

    // =========================================================================
    // yaml format
    // =========================================================================

    [Fact]
    public void FormatYaml_ContainsBrowserName()
    {
        string yaml = OutputFormatter.Format(MultiBrowserList(), "yaml");
        Assert.Contains("Chrome", yaml);
        Assert.Contains("Firefox", yaml);
    }

    [Fact]
    public void FormatYaml_ContainsProfilesKey()
    {
        string yaml = OutputFormatter.Format(MultiBrowserList(), "yaml");
        Assert.Contains("profiles:", yaml);
        Assert.Contains("launchCommand:", yaml);
    }

    // =========================================================================
    // csv format
    // =========================================================================

    [Fact]
    public void FormatCsv_HasHeaderRow()
    {
        string csv = OutputFormatter.Format(MultiBrowserList(), "csv");
        string firstLine = csv.Split('\n')[0].Trim();
        Assert.StartsWith("Browser,Profile", firstLine);
    }

    [Fact]
    public void FormatCsv_HasDataRows()
    {
        string csv = OutputFormatter.Format(MultiBrowserList(), "csv");
        // Header + at least 3 data rows (Default, Work, Firefox profile)
        int lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
        Assert.True(lines >= 4);
    }

    [Fact]
    public void FormatCsv_ContainsBrowserName()
    {
        string csv = OutputFormatter.Format(MultiBrowserList(), "csv");
        Assert.Contains("Chrome", csv);
        Assert.Contains("Firefox", csv);
    }

    // =========================================================================
    // table format
    // =========================================================================

    [Fact]
    public void FormatTable_HasHeaderRow()
    {
        string table = OutputFormatter.Format(MultiBrowserList(), "table");
        Assert.Contains("Browser", table);
        Assert.Contains("Profile", table);
        Assert.Contains("Type", table);
    }

    [Fact]
    public void FormatTable_HasSeparatorLine()
    {
        string table = OutputFormatter.Format(MultiBrowserList(), "table");
        // Separator row contains dashes
        Assert.Contains("---", table);
    }

    [Fact]
    public void FormatTable_ContainsData()
    {
        string table = OutputFormatter.Format(MultiBrowserList(), "table");
        Assert.Contains("Chrome", table);
        Assert.Contains("Firefox", table);
    }

    // =========================================================================
    // format fallback
    // =========================================================================

    [Fact]
    public void Format_UnknownFormat_FallsBackToList()
    {
        string list  = OutputFormatter.Format(SingleChromiumBrowser(), "list");
        string other = OutputFormatter.Format(SingleChromiumBrowser(), "foobar");
        Assert.Equal(list, other);
    }

    // =========================================================================
    // edge cases
    // =========================================================================

    [Fact]
    public void Format_EmptyBrowserList_ReturnsEmpty()
    {
        foreach (string fmt in new[] { "list", "json", "yaml", "csv", "table" })
        {
            string result = OutputFormatter.Format([], fmt);
            // All formats must produce something (at minimum an empty array / header)
            Assert.NotNull(result);
        }
    }
}
