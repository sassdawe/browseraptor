using System.IO;
using System.Runtime.InteropServices;
using BrowserAptor.Services;

namespace BrowserAptor.CLI;

/// <summary>
/// Handles CLI mode when the application is invoked with command-line flags.
/// Attaches to the parent process console, processes the requested command and
/// outputs the result as text before the application exits.
/// </summary>
internal static class CliHandler
{
    // Attach to the console of the parent process (e.g. cmd.exe / PowerShell)
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeConsole();

    private const int AttachParentProcess = -1;

    // -------------------------------------------------------------------------
    // Public entry point
    // -------------------------------------------------------------------------

    /// <summary>
    /// Inspects <paramref name="args"/> and, if any CLI flag is present, handles
    /// the request and sets <paramref name="exitCode"/>.
    /// </summary>
    /// <returns>
    /// <c>true</c> when the application should exit after this call (CLI mode);
    /// <c>false</c> when no CLI flag was detected (GUI mode should continue).
    /// </returns>
    public static bool TryHandle(string[] args, out int exitCode)
    {
        exitCode = 0;

        if (!HasAnyCliFlag(args))
            return false;

        AttachConsole(AttachParentProcess);

        // Redirect Console.Out so that Console.WriteLine actually writes to the
        // attached console (WinExe apps don't have a standard output stream by default).
        var stdOut = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        Console.SetOut(stdOut);

        try
        {
            string format = ParseFormat(args);

            if (HasFlag(args, "--help", "-h"))
            {
                PrintHelp();
                return true;
            }

            if (HasFlag(args, "--list-browsers", "-l"))
            {
                ListBrowsers(format);
                return true;
            }

            if (HasFlag(args, "--detect", "-d"))
            {
                string[] names = ParseDetectNames(args);
                DetectBrowsers(names, format);
                return true;
            }

            // --format without an action → print help
            PrintHelp();
            return true;
        }
        finally
        {
            Console.Out.Flush();
            FreeConsole();
        }
    }

    // -------------------------------------------------------------------------
    // Command implementations
    // -------------------------------------------------------------------------

    private static void PrintHelp()
    {
        Console.WriteLine("BrowserAptor \u2013 Browser and profile selector");
        Console.WriteLine();
        Console.WriteLine("Usage: BrowserAptor.exe [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -h, --help                Show this help message");
        Console.WriteLine("  -l, --list-browsers       List all detected browsers and profiles");
        Console.WriteLine("  -d, --detect [<name>...]  Detect browsers (optionally filter by name)");
        Console.WriteLine("  -f, --format <format>     Output format: list (default), json, yaml, csv, table");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  BrowserAptor.exe --list-browsers");
        Console.WriteLine("  BrowserAptor.exe --list-browsers --format json");
        Console.WriteLine("  BrowserAptor.exe --detect Chrome");
        Console.WriteLine("  BrowserAptor.exe --detect --format table");
        Console.WriteLine("  BrowserAptor.exe https://example.com   Open URL in selected browser");
        Console.WriteLine("  BrowserAptor.exe --register            Register as default browser");
        Console.WriteLine("  BrowserAptor.exe --unregister          Unregister as default browser");
        Console.WriteLine();
        Console.WriteLine("Double-clicking BrowserAptor.exe (no arguments) opens a welcome dialog.");
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void ListBrowsers(string format)
    {
        var service = new BrowserDetectionService();
        var browsers = service.DetectBrowsers();
        Console.WriteLine(OutputFormatter.Format(browsers, format));
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void DetectBrowsers(string[] nameFilters, string format)
    {
        var service = new BrowserDetectionService();
        var browsers = service.DetectBrowsers();

        if (nameFilters.Length > 0)
        {
            browsers = browsers
                .Where(b => nameFilters.Any(n =>
                    b.Name.Contains(n, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        Console.WriteLine(OutputFormatter.Format(browsers, format));
    }

    // -------------------------------------------------------------------------
    // Argument helpers
    // -------------------------------------------------------------------------

    private static bool HasAnyCliFlag(string[] args) =>
        args.Any(a =>
            a.Equals("--help",          StringComparison.OrdinalIgnoreCase) ||
            a.Equals("-h",              StringComparison.OrdinalIgnoreCase) ||
            a.Equals("--list-browsers", StringComparison.OrdinalIgnoreCase) ||
            a.Equals("-l",              StringComparison.OrdinalIgnoreCase) ||
            a.Equals("--detect",        StringComparison.OrdinalIgnoreCase) ||
            a.Equals("-d",              StringComparison.OrdinalIgnoreCase) ||
            a.Equals("--format",        StringComparison.OrdinalIgnoreCase) ||
            a.Equals("-f",              StringComparison.OrdinalIgnoreCase));

    private static bool HasFlag(string[] args, string longForm, string shortForm) =>
        args.Any(a =>
            a.Equals(longForm,  StringComparison.OrdinalIgnoreCase) ||
            a.Equals(shortForm, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns the value following <c>--format</c> / <c>-f</c>, or "list" if absent.
    /// </summary>
    private static string ParseFormat(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--format", StringComparison.OrdinalIgnoreCase) ||
                args[i].Equals("-f",       StringComparison.OrdinalIgnoreCase))
            {
                return i + 1 < args.Length ? args[i + 1].ToLowerInvariant() : "list";
            }
        }
        return "list";
    }

    /// <summary>
    /// Returns browser name tokens that follow <c>--detect</c> / <c>-d</c>
    /// (stops collecting at the next flag that starts with '-').
    /// </summary>
    private static string[] ParseDetectNames(string[] args)
    {
        var names = new List<string>();
        bool collecting = false;

        foreach (string arg in args)
        {
            if (arg.Equals("--detect", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-d",       StringComparison.OrdinalIgnoreCase))
            {
                collecting = true;
                continue;
            }

            if (arg.StartsWith('-'))
            {
                collecting = false;
                continue;
            }

            if (collecting)
                names.Add(arg);
        }

        return names.ToArray();
    }
}
