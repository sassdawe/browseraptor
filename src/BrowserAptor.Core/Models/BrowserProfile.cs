namespace BrowserAptor.Models;

/// <summary>
/// Represents a specific browser profile that can be used to open a URL.
/// </summary>
public class BrowserProfile
{
    public string Name { get; set; } = string.Empty;
    public string ProfileDirectory { get; set; } = string.Empty;
    public string? AvatarIconPath { get; set; }
    public string? UserName { get; set; }
    public BrowserInfo Browser { get; set; } = null!;

    /// <summary>
    /// Optional accent color for this profile in <c>#RRGGBB</c> hex format.
    /// Populated from the Chromium <c>Local State → profile.info_cache[dir].theme_colors.frame</c>
    /// field. <c>null</c> for Firefox profiles and Chromium profiles that use the default theme.
    /// </summary>
    public string? ThemeColor { get; set; }

    /// <summary>
    /// When <c>true</c>, this profile represents a private/incognito browsing session.
    /// </summary>
    public bool IsIncognito { get; set; }

    /// <summary>
    /// A stable, human-readable identifier of the form <c>{browserId}/{profileSlug}</c>.
    /// Incognito profiles always use the slug <c>incognito</c>.
    /// For Chromium profiles the slug is derived from the profile directory (e.g.
    /// "Default" → "default", "Profile 1" → "profile-1"). For Firefox the profile
    /// directory contains random characters so the profile name is used instead.
    /// Examples: "microsoft-edge/default", "mozilla-firefox/default-release",
    ///           "google-chrome/incognito"
    /// </summary>
    public string Id
    {
        get
        {
            string browserSlug = Browser?.Id ?? "unknown";
            if (IsIncognito)
                return $"{browserSlug}/incognito";

            // For Chromium, ProfileDirectory is stable ("Default", "Profile 1").
            // For Firefox, ProfileDirectory is a relative path with random chars;
            // use the profile Name instead.
            string profileSlug = (!string.IsNullOrEmpty(ProfileDirectory)
                                  && !ProfileDirectory.Contains('/')
                                  && !ProfileDirectory.Contains('\\'))
                ? BrowserInfo.Slugify(ProfileDirectory)
                : BrowserInfo.Slugify(Name);
            return $"{browserSlug}/{profileSlug}";
        }
    }

    /// <summary>
    /// Builds the command-line arguments to open a URL in this profile.
    /// </summary>
    public string BuildArguments(string url)
    {
        if (Browser.BrowserType == BrowserType.Firefox)
        {
            if (IsIncognito)
                return $"-private-window \"{url}\"";

            // Firefox uses -P "profile name" to select profile
            return $"-P \"{Name}\" \"{url}\"";
        }

        // Chromium-based browsers
        if (IsIncognito)
        {
            // Edge uses --inprivate; every other Chromium browser uses --incognito.
            // Use EndsWith so the check works with both / and \ path separators.
            bool isEdge = (Browser.ExecutablePath ?? string.Empty)
                .EndsWith("msedge.exe", StringComparison.OrdinalIgnoreCase);
            string flag = isEdge ? "--inprivate" : "--incognito";
            return $"{flag} \"{url}\"";
        }

        if (!string.IsNullOrEmpty(ProfileDirectory))
            return $"--profile-directory=\"{ProfileDirectory}\" \"{url}\"";

        return $"\"{url}\"";
    }

    public override string ToString() =>
        string.IsNullOrEmpty(UserName) ? Name : $"{Name} ({UserName})";
}
