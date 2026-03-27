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

    public override string ToString() => Name;
}
