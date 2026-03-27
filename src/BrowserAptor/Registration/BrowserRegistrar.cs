using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;

namespace BrowserAptor.Registration;

/// <summary>
/// Handles registering and unregistering BrowserAptor as a browser
/// in the Windows registry, allowing it to be set as the default browser.
/// </summary>
public static class BrowserRegistrar
{
    private const string AppName = "BrowserAptor";
    private const string AppDescription = "BrowserAptor – Browser & Profile Selector";

    // ProgId used for URL protocol associations
    private const string ProgId = "BrowserAptor.Url";

    private static string ExePath =>
        Process.GetCurrentProcess().MainModule?.FileName
        ?? Assembly.GetExecutingAssembly().Location;

    /// <summary>
    /// Returns whether this process is running with administrator privileges.
    /// </summary>
    public static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>
    /// Registers BrowserAptor in HKCU so it appears in "Choose default apps" and
    /// can be selected as the default browser without requiring admin rights.
    /// </summary>
    public static void Register()
    {
        string exePath = ExePath;

        // 1. Register ProgId under HKCU\Software\Classes\BrowserAptor.Url
        RegisterProgId(exePath);

        // 2. Register under HKCU\Software\Clients\StartMenuInternet\BrowserAptor
        RegisterStartMenuInternet(exePath);

        // 3. Register supported capabilities
        RegisterCapabilities(exePath);

        // 4. Register in RegisteredApplications
        RegisterApplication();
    }

    /// <summary>
    /// Removes all BrowserAptor registry entries from HKCU.
    /// </summary>
    public static void Unregister()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(
                $@"Software\Classes\{ProgId}", throwOnMissingSubKey: false);
        }
        catch { /* ignore */ }

        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(
                $@"Software\Clients\StartMenuInternet\{AppName}", throwOnMissingSubKey: false);
        }
        catch { /* ignore */ }

        try
        {
            using var regApps = Registry.CurrentUser.OpenSubKey(
                @"Software\RegisteredApplications", writable: true);
            regApps?.DeleteValue(AppName, throwOnMissingValue: false);
        }
        catch { /* ignore */ }
    }

    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    private static void RegisterProgId(string exePath)
    {
        // HKCU\Software\Classes\BrowserAptor.Url
        using var progId = Registry.CurrentUser.CreateSubKey(
            $@"Software\Classes\{ProgId}");
        progId.SetValue(null, AppDescription);
        progId.SetValue("URL Protocol", string.Empty);

        // Default icon
        using var icon = progId.CreateSubKey("DefaultIcon");
        icon.SetValue(null, $"\"{exePath}\",0");

        // Open command
        using var cmd = progId.CreateSubKey(@"shell\open\command");
        cmd.SetValue(null, $"\"{exePath}\" \"%1\"");

        // Edit command (same as open)
        using var editCmd = progId.CreateSubKey(@"shell\edit\command");
        editCmd.SetValue(null, $"\"{exePath}\" \"%1\"");
    }

    private static void RegisterStartMenuInternet(string exePath)
    {
        string keyPath = $@"Software\Clients\StartMenuInternet\{AppName}";

        using var appKey = Registry.CurrentUser.CreateSubKey(keyPath);
        appKey.SetValue(null, AppDescription);

        // Capabilities
        using var caps = appKey.CreateSubKey("Capabilities");
        caps.SetValue("ApplicationName", AppName);
        caps.SetValue("ApplicationDescription", AppDescription);
        caps.SetValue("ApplicationIcon", $"\"{exePath}\",0");

        using var urlAssoc = caps.CreateSubKey("URLAssociations");
        urlAssoc.SetValue("http", ProgId);
        urlAssoc.SetValue("https", ProgId);
        urlAssoc.SetValue("ftp", ProgId);

        using var fileAssoc = caps.CreateSubKey("FileAssociations");
        fileAssoc.SetValue(".htm", ProgId);
        fileAssoc.SetValue(".html", ProgId);
        fileAssoc.SetValue(".xhtml", ProgId);
        fileAssoc.SetValue(".shtml", ProgId);
        fileAssoc.SetValue(".xht", ProgId);
        fileAssoc.SetValue(".webp", ProgId);

        // Default icon
        using var icon = appKey.CreateSubKey("DefaultIcon");
        icon.SetValue(null, $"\"{exePath}\",0");

        // Open command
        using var openCmd = appKey.CreateSubKey(@"shell\open\command");
        openCmd.SetValue(null, $"\"{exePath}\"");

        // InstallInfo
        using var installInfo = appKey.CreateSubKey("InstallInfo");
        installInfo.SetValue("ReinstallCommand",
            $"\"{exePath}\" --register");
        installInfo.SetValue("HideIconsCommand",
            $"\"{exePath}\" --unregister");
        installInfo.SetValue("ShowIconsCommand",
            $"\"{exePath}\" --register");
        installInfo.SetValue("IconsVisible", 1);
    }

    private static void RegisterCapabilities(string exePath)
    {
        // Register for http, https, ftp under HKCU\Software\Classes
        foreach (string protocol in new[] { "http", "https", "ftp" })
        {
            using var protoKey = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\{protocol}");
            // We do not overwrite existing default here; we only register our ProgId
        }

        // Register HKCU\Software\Classes for .html and .htm file types
        foreach (string ext in new[] { ".htm", ".html", ".xhtml", ".shtml" })
        {
            using var extKey = Registry.CurrentUser.CreateSubKey(
                $@"Software\Classes\{ext}\OpenWithProgIds");
            extKey.SetValue(ProgId, new byte[0], RegistryValueKind.None);
        }
    }

    private static void RegisterApplication()
    {
        using var regApps = Registry.CurrentUser.CreateSubKey(
            @"Software\RegisteredApplications");
        regApps.SetValue(AppName,
            $@"Software\Clients\StartMenuInternet\{AppName}\Capabilities");
    }
}
