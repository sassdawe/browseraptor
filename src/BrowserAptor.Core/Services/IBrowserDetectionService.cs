using BrowserAptor.Models;

namespace BrowserAptor.Services;

/// <summary>
/// Detects all installed browsers and their profiles on the current machine.
/// </summary>
public interface IBrowserDetectionService
{
    /// <summary>
    /// Returns a list of all detected browsers with their profiles.
    /// </summary>
    IReadOnlyList<BrowserInfo> DetectBrowsers();
}
