using BrowserAptor.Models;
using System.IO;

namespace BrowserAptor.Services;

/// <summary>
/// Detects installed browsers and their profiles on Linux by scanning
/// well-known executable names in PATH and XDG config directories.
/// </summary>
public class LinuxBrowserDetectionService : IBrowserDetectionService
{
    // Maps a PATH executable name to (display name, BrowserType, XDG config subdirectory)
    private static readonly (string Executable, string DisplayName, BrowserType Type, string ConfigSubDir)[] KnownBrowsers =
    [
        ("google-chrome",        "Google Chrome",        BrowserType.Chromium, "google-chrome"),
        ("google-chrome-stable", "Google Chrome",        BrowserType.Chromium, "google-chrome"),
        ("google-chrome-beta",   "Google Chrome Beta",   BrowserType.Chromium, "google-chrome-beta"),
        ("chromium",             "Chromium",             BrowserType.Chromium, "chromium"),
        ("chromium-browser",     "Chromium",             BrowserType.Chromium, "chromium"),
        ("microsoft-edge",       "Microsoft Edge",       BrowserType.Chromium, "microsoft-edge"),
        ("microsoft-edge-stable","Microsoft Edge",       BrowserType.Chromium, "microsoft-edge"),
        ("brave-browser",        "Brave Browser",        BrowserType.Chromium, "BraveSoftware/Brave-Browser"),
        ("brave",                "Brave Browser",        BrowserType.Chromium, "BraveSoftware/Brave-Browser"),
        ("vivaldi",              "Vivaldi",              BrowserType.Chromium, "vivaldi"),
        ("vivaldi-stable",       "Vivaldi",              BrowserType.Chromium, "vivaldi"),
        ("opera",                "Opera",                BrowserType.Chromium, "opera"),
        ("firefox",              "Mozilla Firefox",      BrowserType.Firefox,  string.Empty),
        ("firefox-esr",          "Mozilla Firefox ESR",  BrowserType.Firefox,  string.Empty),
        ("librewolf",            "LibreWolf",            BrowserType.Firefox,  string.Empty),
        ("waterfox",             "Waterfox",             BrowserType.Firefox,  string.Empty),
    ];

    // Candidate directories for Firefox profiles.ini
    private static readonly string[] FirefoxProfilesIniPaths =
    [
        ".mozilla/firefox/profiles.ini",
        ".var/app/org.mozilla.firefox/.mozilla/firefox/profiles.ini", // Flatpak
        ".var/app/io.gitlab.librewolf-community/.librewolf/profiles.ini", // Flatpak LibreWolf
        ".librewolf/profiles.ini",
        ".waterfox/profiles.ini",
    ];

    public IReadOnlyList<BrowserInfo> DetectBrowsers()
    {
        var browsers = new List<BrowserInfo>();

        DetectChromiumBrowsers(browsers);
        DetectFirefoxBrowsers(browsers);

        return browsers
            .GroupBy(b => b.ExecutablePath.ToLowerInvariant())
            .Select(g => g.First())
            .OrderBy(b => b.Name)
            .ToList();
    }

    // ------------------------------------------------------------------
    // Chromium-based detection
    // ------------------------------------------------------------------

    private static void DetectChromiumBrowsers(List<BrowserInfo> browsers)
    {
        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                           ?? Path.Combine(homeDir, ".config");

        foreach (var (exe, name, _, configSub) in KnownBrowsers.Where(b => b.Type == BrowserType.Chromium))
        {
            string? execPath = FindInPath(exe);
            if (execPath is null)
                continue;

            var browser = new BrowserInfo
            {
                Name           = name,
                ExecutablePath = execPath,
                BrowserType    = BrowserType.Chromium,
            };

            string userDataDir = Path.Combine(xdgConfig, configSub);
            if (Directory.Exists(userDataDir))
            {
                browser.Profiles.AddRange(ChromiumProfileReader.ReadProfilesFromDir(userDataDir, browser));
            }

            // Ensure at least a Default profile so the browser appears usable
            if (browser.Profiles.Count == 0)
            {
                browser.Profiles.Add(new BrowserProfile
                {
                    Name             = "Default",
                    ProfileDirectory = "Default",
                    Browser          = browser,
                });
            }

            browsers.Add(browser);
        }
    }

    // ------------------------------------------------------------------
    // Firefox-based detection
    // ------------------------------------------------------------------

    private static void DetectFirefoxBrowsers(List<BrowserInfo> browsers)
    {
        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        foreach (var (exe, name, _, _) in KnownBrowsers.Where(b => b.Type == BrowserType.Firefox))
        {
            string? execPath = FindInPath(exe);
            if (execPath is null)
                continue;

            var browser = new BrowserInfo
            {
                Name           = name,
                ExecutablePath = execPath,
                BrowserType    = BrowserType.Firefox,
            };

            // Try each candidate profiles.ini location
            foreach (string iniRelPath in FirefoxProfilesIniPaths)
            {
                string iniPath = Path.Combine(homeDir, iniRelPath);
                if (!File.Exists(iniPath))
                    continue;

                var profiles = FirefoxProfileParser.ParseProfilesIni(iniPath, browser);
                if (profiles.Count > 0)
                {
                    browser.Profiles.AddRange(profiles);
                    break;
                }
            }

            if (browser.Profiles.Count == 0)
            {
                browser.Profiles.Add(new BrowserProfile
                {
                    Name             = "Default",
                    ProfileDirectory = "default",
                    Browser          = browser,
                });
            }

            browsers.Add(browser);
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>
    /// Searches each directory in the PATH environment variable for the
    /// given executable name and returns its full path, or <c>null</c> if
    /// not found.
    /// </summary>
    private static string? FindInPath(string executable)
    {
        string? pathVar = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVar))
            return null;

        foreach (string dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(dir, executable);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}
