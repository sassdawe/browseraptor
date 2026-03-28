using BrowserAptor.CLI;
using BrowserAptor.Registration;
using BrowserAptor.Services;
using BrowserAptor.ViewModels;
using BrowserAptor.Views;
using System.Windows;

namespace BrowserAptor;

/// <summary>
/// Application entry point. Parses command-line arguments and either:
///   • Handles CLI flags (--help, --list-browsers, --detect, --format)
///   • Shows the browser selector for a given URL
///   • Registers / unregisters the app as a browser
///   • Shows a welcome/setup message
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        string[] args = e.Args;

        // Handle CLI flags before any GUI logic.
        if (CliHandler.TryHandle(args, out int cliExitCode))
        {
            Shutdown(cliExitCode);
            return;
        }

        if (args.Length == 1 && args[0].Equals("--register", StringComparison.OrdinalIgnoreCase))
        {
            BrowserRegistrar.Register();
            MessageBox.Show(
                "BrowserAptor has been registered as a browser.\n\n" +
                "You can now go to Windows Settings → Default apps → Web browser and choose BrowserAptor.",
                "BrowserAptor – Registered",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown(0);
            return;
        }

        if (args.Length == 1 && args[0].Equals("--unregister", StringComparison.OrdinalIgnoreCase))
        {
            BrowserRegistrar.Unregister();
            MessageBox.Show(
                "BrowserAptor has been unregistered.",
                "BrowserAptor – Unregistered",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown(0);
            return;
        }

        // Auto-register on first run (non-elevated, HKCU only)
        EnsureRegistered();

        string url = args.Length >= 1 ? args[0] : string.Empty;

        if (string.IsNullOrWhiteSpace(url))
        {
            // Launched without a URL – show a welcome/info dialog
            ShowWelcomeMessage();
            Shutdown(0);
            return;
        }

        // Show the browser / profile selector
        var detectionService = new BrowserDetectionService();
        var launchService    = new BrowserLaunchService();
        var vm               = new BrowserSelectorViewModel(detectionService, launchService, url);
        var window           = new BrowserSelectorWindow(vm);
        window.ShowDialog();

        Shutdown(0);
    }

    private static void EnsureRegistered()
    {
        try
        {
            BrowserRegistrar.Register();
        }
        catch
        {
            // Non-fatal; ignore silently
        }
    }

    private static void ShowWelcomeMessage()
    {
        MessageBox.Show(
            "Welcome to BrowserAptor!\n\n" +
            "BrowserAptor lets you choose which browser (and profile) to open links with.\n\n" +
            "To set BrowserAptor as your default browser:\n" +
            "  1. Open Windows Settings\n" +
            "  2. Go to Apps → Default apps\n" +
            "  3. Search for \"BrowserAptor\" and select it for Web browser.\n\n" +
            "Once set as default, a selector window will appear every time you click a link.",
            "BrowserAptor",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
