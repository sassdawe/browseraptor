using System.IO;
using System.Runtime.InteropServices;
using BrowserAptor.Services;

namespace BrowserAptor.CLI;

/// <summary>
/// Handles CLI mode when the application is invoked with command-line flags.
/// On Windows (WinExe) it attaches to the parent process console and fixes up
/// cursor positioning after output. On Linux/macOS the console is already
/// attached for a standard Exe, so those steps are skipped.
/// </summary>
public static class CliHandler
{
    // Windows-only: attach to the console of the parent process (e.g. cmd.exe / PowerShell).
    // DllImport declarations compile on all platforms; the methods are only *called* on Windows.
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeConsole();

    // WriteConsoleInput injects key events directly into the console input buffer —
    // the same buffer that PSReadLine reads via ReadConsoleInput.  This is more
    // reliable than SendInput (Win32 message queue), which is not always routed to
    // the console input buffer in modern terminals such as Windows Terminal.
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteConsoleInput(
        IntPtr hConsoleInput,
        [In] INPUT_RECORD[] lpBuffer,
        uint nLength,
        out uint lpNumberOfEventsWritten);

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUT_RECORD
    {
        [FieldOffset(0)] public ushort EventType;
        [FieldOffset(4)] public KEY_EVENT_RECORD KeyEvent;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct KEY_EVENT_RECORD
    {
        [MarshalAs(UnmanagedType.Bool)] public bool bKeyDown;
        public ushort wRepeatCount;
        public ushort wVirtualKeyCode;
        public ushort wVirtualScanCode;
        public char UnicodeChar;
        public uint dwControlKeyState;
    }

    private const ushort ConsoleKeyEvent  = 0x0001;
    private const ushort VkReturn         = 0x0D;
    private const ushort EnterScanCode    = 0x1C;
    private const uint   GenericReadWrite = 0x80000000 | 0x40000000; // GENERIC_READ | GENERIC_WRITE
    private const uint   FileShareRW      = 0x00000001 | 0x00000002; // FILE_SHARE_READ | FILE_SHARE_WRITE
    private const uint   OpenExisting     = 3;                       // OPEN_EXISTING
    private static readonly IntPtr InvalidHandle = new(-1);          // INVALID_HANDLE_VALUE

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

        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        if (isWindows)
        {
            AttachConsole(AttachParentProcess);
        }

        // Redirect Console.Out so that Console.WriteLine actually writes to the
        // attached console (WinExe apps don't have a standard output stream by default).
        var stdOut = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        Console.SetOut(stdOut);

        if (isWindows)
        {
            // The parent shell drew its "next prompt" on the current line before our
            // WinExe started.  Overwrite that prompt with spaces so our output appears
            // cleanly on that line instead of showing a stray "PS C:\...>" above it.
            try
            {
                int top = Console.CursorTop;
                int width = Math.Max(1, Console.WindowWidth);
                Console.Write($"\r{new string(' ', width - 1)}\r");
                Console.CursorTop = top; // stay on the same row after the spaces
            }
            catch
            {
                // Cursor manipulation failed (e.g. output redirected) — fall back to
                // a plain newline so our output at least starts on a fresh line.
                Console.WriteLine();
            }
        }

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

            if (HasFlag(args, "--set-displayname", "-s"))
            {
                var (id, name) = ParseSetDisplayName(args);
                if (id == null || name == null)
                {
                    Console.Error.WriteLine("Usage: BrowserAptor.exe --set-displayname <id> <new-name>");
                    Console.Error.WriteLine("  Use --list-browsers to discover IDs.");
                    exitCode = 1;
                    return true;
                }
                SetDisplayName(id, name);
                return true;
            }

            // --format without an action → print help
            PrintHelp();
            return true;
        }
        finally
        {
            Console.Out.Flush();

            if (isWindows)
            {
                // Inject Enter into the console input buffer *before* FreeConsole while
                // the console handle is still valid.  PSReadLine reads from this buffer
                // via ReadConsoleInput; when it processes the Enter it reads the current
                // cursor position (end of our output) and redraws its prompt there —
                // below our output — rather than at the stale prompt line it occupied
                // when we started.
                WriteEnterToConsoleInputBuffer();
                FreeConsole();
            }
        }
    }

    /// <summary>
    /// Writes a synthetic Enter key-down/key-up pair directly to the console input
    /// buffer so the parent shell (PSReadLine) processes it and redraws its prompt
    /// below our output (Windows only).
    /// <para>
    /// <c>WriteConsoleInput</c> is used rather than <c>SendInput</c> because it writes
    /// straight to the console buffer that PSReadLine reads via <c>ReadConsoleInput</c>,
    /// making it work in both <c>conhost.exe</c> and Windows Terminal.
    /// </para>
    /// </summary>
    private static void WriteEnterToConsoleInputBuffer()
    {
        // Open the console input while we are still attached (AttachConsole).
        IntPtr hInput = CreateFile(
            "CONIN$",
            GenericReadWrite,
            FileShareRW,
            IntPtr.Zero,
            OpenExisting,
            0,
            IntPtr.Zero);

        if (hInput == InvalidHandle)
            return;

        try
        {
            INPUT_RECORD[] records =
            [
                new INPUT_RECORD
                {
                    EventType = ConsoleKeyEvent,
                    KeyEvent  = new KEY_EVENT_RECORD
                    {
                        bKeyDown         = true,
                        wRepeatCount     = 1,
                        wVirtualKeyCode  = VkReturn,
                        wVirtualScanCode = EnterScanCode,
                        UnicodeChar      = '\r',
                    },
                },
                new INPUT_RECORD
                {
                    EventType = ConsoleKeyEvent,
                    KeyEvent  = new KEY_EVENT_RECORD
                    {
                        bKeyDown         = false,
                        wRepeatCount     = 1,
                        wVirtualKeyCode  = VkReturn,
                        wVirtualScanCode = EnterScanCode,
                        UnicodeChar      = '\r',
                    },
                },
            ];

            WriteConsoleInput(hInput, records, (uint)records.Length, out _);
        }
        finally
        {
            CloseHandle(hInput);
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
        Console.WriteLine("  -h, --help                              Show this help message");
        Console.WriteLine("  -l, --list-browsers                     List all detected browsers and profiles");
        Console.WriteLine("  -d, --detect [<name>...]                Detect browsers (optionally filter by name)");
        Console.WriteLine("  -f, --format <format>                   Output format: list (default), json, yaml, csv, table");
        Console.WriteLine("  -s, --set-displayname <id> <new-name>   Set a custom display name for a browser or profile");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  BrowserAptor.exe --list-browsers");
        Console.WriteLine("  BrowserAptor.exe --list-browsers --format json");
        Console.WriteLine("  BrowserAptor.exe --detect Chrome");
        Console.WriteLine("  BrowserAptor.exe --detect --format table");
        Console.WriteLine("  BrowserAptor.exe --set-displayname microsoft-edge/default \"My Edge\"");
        Console.WriteLine("  BrowserAptor.exe https://example.com   Open URL in selected browser");
        Console.WriteLine("  BrowserAptor.exe --register            Register as default browser");
        Console.WriteLine("  BrowserAptor.exe --unregister          Unregister as default browser");
        Console.WriteLine();
        Console.WriteLine("Double-clicking BrowserAptor.exe (no arguments) opens a welcome dialog.");
    }

    private static void ListBrowsers(string format)
    {
        IBrowserDetectionService service = CreateDetectionService();
        var browsers = service.DetectBrowsers();
        var displayNames = new DisplayNameStore();
        Console.WriteLine(OutputFormatter.Format(browsers, format, displayNames));
    }

    private static void DetectBrowsers(string[] nameFilters, string format)
    {
        IBrowserDetectionService service = CreateDetectionService();
        var browsers = service.DetectBrowsers();

        if (nameFilters.Length > 0)
        {
            browsers = browsers
                .Where(b => nameFilters.Any(n =>
                    b.Name.Contains(n, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var displayNames = new DisplayNameStore();
        Console.WriteLine(OutputFormatter.Format(browsers, format, displayNames));
    }

    private static void SetDisplayName(string id, string displayName)
    {
        var store = new DisplayNameStore();
        store.SetDisplayName(id, displayName);
        Console.WriteLine($"Display name for '{id}' set to '{displayName}'.");
    }

    /// <summary>
    /// Returns the appropriate <see cref="IBrowserDetectionService"/> for the
    /// current operating system.
    /// </summary>
    private static IBrowserDetectionService CreateDetectionService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // BrowserDetectionService is [SupportedOSPlatform("windows")] — safe to call here.
#pragma warning disable CA1416
            return new BrowserDetectionService();
#pragma warning restore CA1416
        }

        return new LinuxBrowserDetectionService();
    }

    // -------------------------------------------------------------------------
    // Argument helpers
    // -------------------------------------------------------------------------

    private static bool HasAnyCliFlag(string[] args) =>
        args.Any(a =>
            a.Equals("--help",             StringComparison.OrdinalIgnoreCase) ||
            a.Equals("-h",                 StringComparison.OrdinalIgnoreCase) ||
            a.Equals("--list-browsers",    StringComparison.OrdinalIgnoreCase) ||
            a.Equals("-l",                 StringComparison.OrdinalIgnoreCase) ||
            a.Equals("--detect",           StringComparison.OrdinalIgnoreCase) ||
            a.Equals("-d",                 StringComparison.OrdinalIgnoreCase) ||
            a.Equals("--format",           StringComparison.OrdinalIgnoreCase) ||
            a.Equals("-f",                 StringComparison.OrdinalIgnoreCase) ||
            a.Equals("--set-displayname",  StringComparison.OrdinalIgnoreCase) ||
            a.Equals("-s",                 StringComparison.OrdinalIgnoreCase));

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

    /// <summary>
    /// Returns the (id, new-name) pair following <c>--set-displayname</c> / <c>-s</c>.
    /// Returns (null, null) if fewer than two arguments follow the flag.
    /// </summary>
    private static (string? Id, string? Name) ParseSetDisplayName(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--set-displayname", StringComparison.OrdinalIgnoreCase) ||
                args[i].Equals("-s",                StringComparison.OrdinalIgnoreCase))
            {
                string? id   = i + 1 < args.Length ? args[i + 1] : null;
                string? name = i + 2 < args.Length ? args[i + 2] : null;
                return (id, name);
            }
        }
        return (null, null);
    }
}
