using BrowserAptor.Models;
using System.Diagnostics;

namespace BrowserAptor.Services;

/// <summary>
/// Launches a URL in the specified browser profile using Process.Start.
/// </summary>
public class BrowserLaunchService : IBrowserLaunchService
{
    public void Launch(BrowserProfile profile, string url)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        string exePath = profile.Browser.ExecutablePath;
        string arguments = profile.BuildArguments(url);

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            UseShellExecute = false,
        };

        Process.Start(startInfo);
    }
}
