using BrowserAptor.Models;
using Microsoft.Win32;
using System.IO;
using System.Runtime.Versioning;
using System.Text.Json;

namespace BrowserAptor.Services;

/// <summary>
/// Detects installed browsers and their profiles on Windows by checking
/// the registry and well-known installation paths.
/// </summary>
[SupportedOSPlatform("windows")]
public class BrowserDetectionService : IBrowserDetectionService
{
    // Known Chromium-based browser data-directory locations relative to %LOCALAPPDATA%
    private static readonly (string SubPath, string Name)[] ChromiumLocalPaths =
    {
        (@"Google\Chrome\User Data",                   "Google Chrome"),
        (@"Google\Chrome Beta\User Data",              "Google Chrome Beta"),
        (@"Google\Chrome Dev\User Data",               "Google Chrome Dev"),
        (@"Google\Chrome SxS\User Data",               "Google Chrome Canary"),
        (@"Microsoft\Edge\User Data",                  "Microsoft Edge"),
        (@"Microsoft\Edge Beta\User Data",             "Microsoft Edge Beta"),
        (@"Microsoft\Edge Dev\User Data",              "Microsoft Edge Dev"),
        (@"Microsoft\Edge SxS\User Data",              "Microsoft Edge Canary"),
        (@"BraveSoftware\Brave-Browser\User Data",     "Brave Browser"),
        (@"BraveSoftware\Brave-Browser-Beta\User Data","Brave Browser Beta"),
        (@"BraveSoftware\Brave-Browser-Nightly\User Data", "Brave Browser Nightly"),
        (@"Vivaldi\User Data",                         "Vivaldi"),
        (@"Opera Software\Opera Stable",               "Opera"),
        (@"Opera Software\Opera Next",                 "Opera Next"),
        (@"Opera Software\Opera GX Stable",            "Opera GX"),
        (@"Chromium\User Data",                        "Chromium"),
        (@"Arc\User Data",                             "Arc"),
        (@"Thorium\User Data",                         "Thorium"),
        (@"CentBrowser\User Data",                     "Cent Browser"),
        (@"Comodo\Dragon\User Data",                   "Comodo Dragon"),
        (@"MapleStudio\ChromePlus\User Data",          "ChromePlus"),
        (@"Torch\User Data",                           "Torch"),
        (@"Yandex\YandexBrowser\User Data",            "Yandex Browser"),
    };

    // Well-known registry keys for StartMenuInternet registrations
    private static readonly string[] StartMenuInternetKeys =
    {
        @"SOFTWARE\Clients\StartMenuInternet",
        @"SOFTWARE\WOW6432Node\Clients\StartMenuInternet",
    };

    public IReadOnlyList<BrowserInfo> DetectBrowsers()
    {
        var browsers = new List<BrowserInfo>();

        DetectChromiumBrowsers(browsers);
        DetectFirefoxBrowsers(browsers);
        DetectViaBrowserRegistry(browsers);

        // De-duplicate by executable path
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
        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        foreach (var (subPath, defaultName) in ChromiumLocalPaths)
        {
            string userDataDir = Path.Combine(localApp, subPath);
            if (!Directory.Exists(userDataDir))
                continue;

            string? execPath = FindChromiumExecutable(userDataDir);
            if (execPath == null)
                continue;

            var browser = new BrowserInfo
            {
                Name = defaultName,
                ExecutablePath = execPath,
                IconPath = execPath,
                BrowserType = BrowserType.Chromium,
                Profiles = new List<BrowserProfile>()
            };

            browser.Profiles.AddRange(ReadChromiumProfiles(userDataDir, browser));

            if (browser.Profiles.Count == 0)
            {
                // Add a default profile even if no profile dirs found
                browser.Profiles.Add(new BrowserProfile
                {
                    Name = "Default",
                    ProfileDirectory = "Default",
                    Browser = browser
                });
            }

            browsers.Add(browser);
        }
    }

    private static string? FindChromiumExecutable(string userDataDir)
    {
        // The executable is typically one or two levels up from User Data
        string parent = Path.GetDirectoryName(userDataDir) ?? string.Empty;
        string grandParent = Path.GetDirectoryName(parent) ?? string.Empty;

        string[] candidates =
        {
            Path.Combine(parent, "Application", $"{Path.GetFileName(parent)}.exe"),
            Path.Combine(parent, $"{Path.GetFileName(parent)}.exe"),
            Path.Combine(grandParent, "Application", $"{Path.GetFileName(grandParent)}.exe"),
        };

        // Also check well-known exe names
        string[] exeNames = { "chrome.exe", "msedge.exe", "brave.exe", "vivaldi.exe",
                               "opera.exe", "chromium.exe", "arc.exe", "thorium.exe",
                               "yandex.exe" };

        foreach (string dir in new[] { parent, Path.Combine(parent, "Application") })
        {
            if (!Directory.Exists(dir)) continue;
            foreach (string exe in exeNames)
            {
                string path = Path.Combine(dir, exe);
                if (File.Exists(path)) return path;
            }
        }

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    private static List<BrowserProfile> ReadChromiumProfiles(string userDataDir, BrowserInfo browser)
    {
        var profiles = new List<BrowserProfile>();

        // Read local state file which lists all profiles
        string localStatePath = Path.Combine(userDataDir, "Local State");
        if (File.Exists(localStatePath))
        {
            try
            {
                string json = File.ReadAllText(localStatePath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("profile", out var profileSection) &&
                    profileSection.TryGetProperty("info_cache", out var infoCache))
                {
                    foreach (var profileEntry in infoCache.EnumerateObject())
                    {
                        string profileDir = profileEntry.Name;
                        string profileName = profileDir; // fallback
                        string? userName = null;

                        if (profileEntry.Value.TryGetProperty("name", out var nameEl))
                            profileName = nameEl.GetString() ?? profileDir;

                        if (profileEntry.Value.TryGetProperty("user_name", out var userNameEl))
                            userName = userNameEl.GetString();

                        string? avatarPath = null;
                        if (profileEntry.Value.TryGetProperty("last_downloaded_gaia_picture_url_with_size",
                                out var avatarEl))
                            avatarPath = avatarEl.GetString();

                        profiles.Add(new BrowserProfile
                        {
                            Name = profileName,
                            ProfileDirectory = profileDir,
                            UserName = userName,
                            AvatarIconPath = avatarPath,
                            Browser = browser
                        });
                    }
                }
            }
            catch (Exception)
            {
                // If we can't parse, fall through to directory scan
            }
        }

        // Fall back to scanning directories if Local State didn't yield profiles
        if (profiles.Count == 0)
        {
            foreach (string dir in Directory.EnumerateDirectories(userDataDir))
            {
                string dirName = Path.GetFileName(dir);
                if (dirName != "Default" &&
                    !dirName.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase))
                    continue;

                string prefsFile = Path.Combine(dir, "Preferences");
                string displayName = dirName;

                if (File.Exists(prefsFile))
                {
                    try
                    {
                        string json = File.ReadAllText(prefsFile);
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("profile", out var p) &&
                            p.TryGetProperty("name", out var n))
                            displayName = n.GetString() ?? dirName;
                    }
                    catch { /* ignore */ }
                }

                profiles.Add(new BrowserProfile
                {
                    Name = displayName,
                    ProfileDirectory = dirName,
                    Browser = browser
                });
            }
        }

        return profiles;
    }

    // ------------------------------------------------------------------
    // Firefox detection
    // ------------------------------------------------------------------

    private static void DetectFirefoxBrowsers(List<BrowserInfo> browsers)
    {
        // Firefox installs: stable, ESR, Developer Edition, Nightly
        var firefoxInstalls = FindFirefoxInstalls();

        foreach (var (execPath, name) in firefoxInstalls)
        {
            var browser = new BrowserInfo
            {
                Name = name,
                ExecutablePath = execPath,
                IconPath = execPath,
                BrowserType = BrowserType.Firefox
            };

            browser.Profiles.AddRange(ReadFirefoxProfiles(browser));

            if (browser.Profiles.Count == 0)
            {
                browser.Profiles.Add(new BrowserProfile
                {
                    Name = "Default",
                    ProfileDirectory = string.Empty,
                    Browser = browser
                });
            }

            browsers.Add(browser);
        }
    }

    private static IEnumerable<(string ExecPath, string Name)> FindFirefoxInstalls()
    {
        // Check HKLM and HKCU registry for Firefox installs
        var results = new List<(string, string)>();
        var registryRoots = new[]
        {
            (RegistryHive.LocalMachine, @"SOFTWARE\Mozilla"),
            (RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Mozilla"),
            (RegistryHive.CurrentUser,  @"SOFTWARE\Mozilla"),
        };

        foreach (var (hive, subKey) in registryRoots)
        {
            using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
            using var mozKey = root.OpenSubKey(subKey);
            if (mozKey == null) continue;

            foreach (string productName in mozKey.GetSubKeyNames())
            {
                if (!productName.StartsWith("Mozilla Firefox", StringComparison.OrdinalIgnoreCase) &&
                    !productName.StartsWith("Firefox", StringComparison.OrdinalIgnoreCase))
                    continue;

                using var productKey = mozKey.OpenSubKey(productName);
                if (productKey == null) continue;

                // Look for the "bin" or "Main" subkey with "PathToExe"
                using var mainKey = productKey.OpenSubKey("Main");
                string? exePath = mainKey?.GetValue("PathToExe") as string;
                if (exePath != null && File.Exists(exePath))
                {
                    string displayName = DeriveFirefoxDisplayName(productName);
                    results.Add((exePath, displayName));
                }
            }
        }

        // Fallback: check well-known Program Files paths
        if (results.Count == 0)
        {
            string[] programDirs =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            };
            string[] firefoxDirs =
            {
                @"Mozilla Firefox\firefox.exe",
                @"Mozilla Firefox ESR\firefox.exe",
                @"Firefox Nightly\firefox.exe",
                @"Firefox Developer Edition\firefox.exe",
            };

            foreach (string progDir in programDirs)
            {
                foreach (string ffDir in firefoxDirs)
                {
                    string full = Path.Combine(progDir, ffDir);
                    if (File.Exists(full))
                    {
                        string name = DeriveFirefoxDisplayName(Path.GetDirectoryName(ffDir)!);
                        results.Add((full, name));
                    }
                }
            }
        }

        return results
            .GroupBy(r => r.Item1.ToLowerInvariant())
            .Select(g => g.First());
    }

    private static string DeriveFirefoxDisplayName(string raw)
    {
        if (raw.Contains("Nightly", StringComparison.OrdinalIgnoreCase)) return "Firefox Nightly";
        if (raw.Contains("Developer", StringComparison.OrdinalIgnoreCase)) return "Firefox Developer Edition";
        if (raw.Contains("ESR", StringComparison.OrdinalIgnoreCase)) return "Firefox ESR";
        return "Mozilla Firefox";
    }

    private static List<BrowserProfile> ReadFirefoxProfiles(BrowserInfo browser)
    {
        var profiles = new List<BrowserProfile>();
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // Try both Mozilla/Firefox and the Nightly/Dev paths
        string[] profilesIniPaths =
        {
            Path.Combine(appData, "Mozilla", "Firefox", "profiles.ini"),
            Path.Combine(appData, "Mozilla", "Firefox Nightly", "profiles.ini"),
        };

        foreach (string iniPath in profilesIniPaths)
        {
            if (!File.Exists(iniPath)) continue;
            profiles.AddRange(FirefoxProfileParser.ParseProfilesIni(iniPath, browser));
        }

        return profiles;
    }

    /// <summary>
    /// Parses a Firefox profiles.ini file and returns discovered profiles.
    /// Delegates to <see cref="FirefoxProfileParser.ParseProfilesIni"/>.
    /// </summary>
    public static List<BrowserProfile> ParseFirefoxProfilesIni(string iniPath, BrowserInfo browser)
        => FirefoxProfileParser.ParseProfilesIni(iniPath, browser);

    // ------------------------------------------------------------------
    // Registry-based fallback (StartMenuInternet)
    // ------------------------------------------------------------------

    private void DetectViaBrowserRegistry(List<BrowserInfo> browsers)
    {
        // Collect already-found executable paths to avoid duplicates
        var knownExes = new HashSet<string>(
            browsers.Select(b => b.ExecutablePath.ToLowerInvariant()));

        foreach (string keyPath in StartMenuInternetKeys)
        {
            foreach (var hive in new[] { Registry.LocalMachine, Registry.CurrentUser })
            {
                using var key = hive.OpenSubKey(keyPath);
                if (key == null) continue;

                foreach (string browserKeyName in key.GetSubKeyNames())
                {
                    using var browserKey = key.OpenSubKey(browserKeyName);
                    if (browserKey == null) continue;

                    string? displayName = browserKey.GetValue(null) as string
                                         ?? browserKeyName;

                    using var cmdKey = browserKey.OpenSubKey(@"shell\open\command");
                    string? cmd = cmdKey?.GetValue(null) as string;
                    if (string.IsNullOrWhiteSpace(cmd)) continue;

                    string exePath = ParseExeFromCommand(cmd);
                    if (!File.Exists(exePath)) continue;
                    if (knownExes.Contains(exePath.ToLowerInvariant())) continue;

                    // Skip BrowserAptor itself
                    if (exePath.Contains("BrowserAptor", StringComparison.OrdinalIgnoreCase))
                        continue;

                    knownExes.Add(exePath.ToLowerInvariant());

                    var browser = new BrowserInfo
                    {
                        Name = displayName ?? Path.GetFileNameWithoutExtension(exePath),
                        ExecutablePath = exePath,
                        IconPath = exePath,
                        BrowserType = DetermineBrowserType(exePath, displayName ?? string.Empty)
                    };

                    if (browser.BrowserType == BrowserType.Firefox)
                    {
                        browser.Profiles.AddRange(ReadFirefoxProfiles(browser));
                    }

                    if (browser.Profiles.Count == 0)
                    {
                        browser.Profiles.Add(new BrowserProfile
                        {
                            Name = "Default",
                            ProfileDirectory = string.Empty,
                            Browser = browser
                        });
                    }

                    browsers.Add(browser);
                }
            }
        }
    }

    private static string ParseExeFromCommand(string cmd)
    {
        cmd = cmd.Trim();
        if (cmd.StartsWith('"'))
        {
            int end = cmd.IndexOf('"', 1);
            return end > 0 ? cmd[1..end] : cmd.Trim('"');
        }
        int spaceIdx = cmd.IndexOf(' ');
        return spaceIdx > 0 ? cmd[..spaceIdx] : cmd;
    }

    private static BrowserType DetermineBrowserType(string exePath, string displayName)
    {
        string lower = (exePath + displayName).ToLowerInvariant();
        if (lower.Contains("firefox") || lower.Contains("waterfox")) return BrowserType.Firefox;
        return BrowserType.Chromium;
    }
}
