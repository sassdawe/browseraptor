using BrowserAptor.Models;
using System.IO;

namespace BrowserAptor.Services;

/// <summary>
/// Parses Firefox <c>profiles.ini</c> files to extract profile information.
/// This parser is platform-agnostic and works with any profiles.ini content.
/// </summary>
public static class FirefoxProfileParser
{
    /// <summary>
    /// Reads a Firefox <c>profiles.ini</c> file and returns all discovered profiles.
    /// </summary>
    /// <param name="iniPath">Full path to the profiles.ini file.</param>
    /// <param name="browser">The browser instance to associate profiles with.</param>
    public static List<BrowserProfile> ParseProfilesIni(string iniPath, BrowserInfo browser)
    {
        var profiles = new List<BrowserProfile>();
        string? currentName = null;
        string? currentPath = null;

        foreach (string rawLine in File.ReadLines(iniPath))
        {
            string line = rawLine.Trim();

            if (line.StartsWith("[Profile", StringComparison.OrdinalIgnoreCase))
            {
                // Save previous profile
                if (currentName != null)
                {
                    profiles.Add(new BrowserProfile
                    {
                        Name = currentName,
                        ProfileDirectory = currentPath ?? string.Empty,
                        Browser = browser
                    });
                }
                currentName = null;
                currentPath = null;
                continue;
            }

            if (line.StartsWith("["))
            {
                // Non-profile section (Install section etc.)
                if (currentName != null)
                {
                    profiles.Add(new BrowserProfile
                    {
                        Name = currentName,
                        ProfileDirectory = currentPath ?? string.Empty,
                        Browser = browser
                    });
                    currentName = null;
                    currentPath = null;
                }
                continue;
            }

            if (line.StartsWith("Name=", StringComparison.OrdinalIgnoreCase))
                currentName = line.Substring(5);
            else if (line.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
                currentPath = line.Substring(5);
        }

        // Flush final pending profile
        if (currentName != null)
        {
            profiles.Add(new BrowserProfile
            {
                Name = currentName,
                ProfileDirectory = currentPath ?? string.Empty,
                Browser = browser
            });
        }

        return profiles;
    }
}
