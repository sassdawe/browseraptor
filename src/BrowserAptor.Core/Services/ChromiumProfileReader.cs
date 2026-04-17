using System.IO;
using System.Text.Json;
using BrowserAptor.Models;

namespace BrowserAptor.Services;

/// <summary>
/// Reads Chromium profile information from a User Data directory.
/// This parser is platform-agnostic and works with any Chromium User Data layout.
/// </summary>
public static class ChromiumProfileReader
{
    /// <summary>
    /// Reads all profiles from a Chromium User Data directory.
    /// First tries the <c>Local State</c> JSON file; falls back to scanning
    /// sub-directories named <c>Default</c> or <c>Profile N</c>.
    /// </summary>
    /// <param name="userDataDir">Full path to the browser's User Data directory.</param>
    /// <param name="browser">The browser instance to associate profiles with.</param>
    public static List<BrowserProfile> ReadProfilesFromDir(string userDataDir, BrowserInfo browser)
    {
        var profiles = new List<BrowserProfile>();

        // Primary: read Local State JSON which lists all profiles
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
                        string profileDir  = profileEntry.Name;
                        string profileName = profileDir;
                        string? userName   = null;
                        string? avatarPath = null;
                        string? themeColor = null;

                        if (profileEntry.Value.TryGetProperty("name", out var nameEl))
                            profileName = nameEl.GetString() ?? profileDir;

                        if (profileEntry.Value.TryGetProperty("user_name", out var userNameEl))
                            userName = userNameEl.GetString();

                        if (profileEntry.Value.TryGetProperty(
                                "last_downloaded_gaia_picture_url_with_size", out var avatarEl))
                            avatarPath = avatarEl.GetString();

                        // Extract the profile's frame color from the Chromium theme palette.
                        // Chromium stores colors as packed signed ARGB int32 values.
                        if (profileEntry.Value.TryGetProperty("theme_colors", out var themeColorsEl) &&
                            themeColorsEl.TryGetProperty("frame", out var frameEl) &&
                            frameEl.TryGetInt32(out int packedArgb))
                        {
                            themeColor = PackedArgbToHex(packedArgb);
                        }

                        profiles.Add(new BrowserProfile
                        {
                            Name             = profileName,
                            ProfileDirectory = profileDir,
                            UserName         = userName,
                            AvatarIconPath   = avatarPath,
                            ThemeColor       = themeColor,
                            Browser          = browser,
                        });
                    }
                }
            }
            catch (Exception)
            {
                // Fall through to directory scan on any parse error
            }
        }

        // Fallback: scan subdirectories named Default or "Profile N"
        if (profiles.Count == 0)
        {
            foreach (string dir in Directory.EnumerateDirectories(userDataDir))
            {
                string dirName = Path.GetFileName(dir);
                if (dirName != "Default" &&
                    !dirName.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase))
                    continue;

                string prefsFile  = Path.Combine(dir, "Preferences");
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
                    Name             = displayName,
                    ProfileDirectory = dirName,
                    Browser          = browser,
                });
            }
        }

        return profiles;
    }

    /// <summary>
    /// Converts a Chromium packed ARGB signed int32 to an <c>#RRGGBB</c> hex string.
    /// </summary>
    private static string PackedArgbToHex(int packedArgb)
    {
        uint argb = (uint)packedArgb;
        byte r = (byte)((argb >> 16) & 0xFF);
        byte g = (byte)((argb >> 8)  & 0xFF);
        byte b = (byte)(argb         & 0xFF);
        return $"#{r:X2}{g:X2}{b:X2}";
    }
}
