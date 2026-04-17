namespace BrowserAptor.Models;

/// <summary>
/// Represents an installed browser with its executable path and available profiles.
/// </summary>
public class BrowserInfo
{
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public BrowserType BrowserType { get; set; }
    public List<BrowserProfile> Profiles { get; set; } = new();

    /// <summary>
    /// A stable, human-readable identifier derived from the browser name.
    /// Example: "Microsoft Edge" → "microsoft-edge"
    /// </summary>
    public string Id => Slugify(Name);

    /// <summary>
    /// The release channel of this browser build, or <c>null</c> for stable releases.
    /// Possible values: <c>"Canary"</c>, <c>"Nightly"</c>, <c>"Dev"</c>, <c>"Beta"</c>.
    /// Derived from keywords in <see cref="Name"/>.
    /// </summary>
    public string? Channel
    {
        get
        {
            string n = Name;
            if (n.Contains("Canary",  StringComparison.OrdinalIgnoreCase)) return "Canary";
            if (n.Contains("Nightly", StringComparison.OrdinalIgnoreCase)) return "Nightly";
            if (n.Contains("Dev",     StringComparison.OrdinalIgnoreCase)) return "Dev";
            if (n.Contains("Beta",    StringComparison.OrdinalIgnoreCase)) return "Beta";
            return null;
        }
    }

    /// <summary>
    /// Converts a string to a lowercase, hyphen-separated slug containing only
    /// ASCII letters, digits, and hyphens.
    /// </summary>
    internal static string Slugify(string input)
    {
        if (string.IsNullOrEmpty(input)) return "unknown";
        var sb = new System.Text.StringBuilder(input.Length);
        bool lastWasDash = true;
        foreach (char c in input.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                lastWasDash = false;
            }
            else if (!lastWasDash)
            {
                sb.Append('-');
                lastWasDash = true;
            }
        }
        return sb.ToString().TrimEnd('-');
    }

    public override string ToString() => Name;
}
