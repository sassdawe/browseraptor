namespace BrowserAptor.Models;

/// <summary>
/// Enumerates the known browser types for profile detection strategy selection.
/// </summary>
public enum BrowserType
{
    Unknown,
    Chromium,   // Chrome, Edge, Brave, Vivaldi, Opera, Arc, etc.
    Firefox,
    Safari,
}
