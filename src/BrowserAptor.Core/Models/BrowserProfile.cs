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
    /// A stable, human-readable identifier of the form <c>{browserId}/{profileSlug}</c>.
    /// For Chromium profiles the slug is derived from the profile directory (e.g.
    /// "Default" → "default", "Profile 1" → "profile-1"). For Firefox the profile
    /// directory contains random characters so the profile name is used instead.
    /// Examples: "microsoft-edge/default", "mozilla-firefox/default-release"
    /// </summary>
    public string Id
    {
        get
        {
            string browserSlug = Browser?.Id ?? "unknown";
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
            // Firefox uses -P "profile name" to select profile
            return $"-P \"{Name}\" \"{url}\"";
        }

        // Chromium-based: --profile-directory="Default" or "Profile 1" etc.
        if (!string.IsNullOrEmpty(ProfileDirectory))
        {
            return $"--profile-directory=\"{ProfileDirectory}\" \"{url}\"";
        }

        return $"\"{url}\"";
    }

    public override string ToString() =>
        string.IsNullOrEmpty(UserName) ? Name : $"{Name} ({UserName})";
}
