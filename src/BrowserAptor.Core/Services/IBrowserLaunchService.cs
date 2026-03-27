using BrowserAptor.Models;

namespace BrowserAptor.Services;

/// <summary>
/// Launches a URL in a specified browser and profile.
/// </summary>
public interface IBrowserLaunchService
{
    /// <summary>
    /// Opens the given URL in the specified browser profile.
    /// </summary>
    void Launch(BrowserProfile profile, string url);
}
