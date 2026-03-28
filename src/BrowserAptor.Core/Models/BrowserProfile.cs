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
