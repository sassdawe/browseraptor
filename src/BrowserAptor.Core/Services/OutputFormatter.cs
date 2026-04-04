using System.Text;
using System.Text.Json;
using BrowserAptor.Models;

namespace BrowserAptor.Services;

/// <summary>
/// Formats a collection of detected browsers and their profiles into various text formats.
/// </summary>
public static class OutputFormatter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Formats the browser list in the specified format.
    /// Supported formats: list (default), json, yaml, csv, table.
    /// </summary>
    /// <param name="browsers">Browsers to format.</param>
    /// <param name="format">Output format name.</param>
    /// <param name="displayNames">
    /// Optional store of custom display names.  When provided, any custom name
    /// set for a profile's ID overrides the default derived label.
    /// </param>
    public static string Format(IEnumerable<BrowserInfo> browsers, string format,
                                DisplayNameStore? displayNames = null)
    {
        var list = browsers.ToList();
        return format.ToLowerInvariant() switch
        {
            "json"  => FormatJson(list, displayNames),
            "yaml"  => FormatYaml(list, displayNames),
            "csv"   => FormatCsv(list, displayNames),
            "table" => FormatTable(list, displayNames),
            "grid"  => FormatGrid(list, displayNames),
            _       => FormatList(list, displayNames),
        };
    }

    // -------------------------------------------------------------------------
    // list (default)
    // -------------------------------------------------------------------------
    private static string FormatList(List<BrowserInfo> browsers, DisplayNameStore? displayNames)
    {
        var sb = new StringBuilder();
        foreach (var browser in browsers)
        {
            foreach (var profile in browser.Profiles)
            {
                string defaultLabel = browser.Profiles.Count == 1
                    ? browser.Name
                    : $"{browser.Name} \u2013 {profile}";

                string label = displayNames?.GetDisplayName(profile.Id) ?? defaultLabel;

                string profileArgs = profile.BuildArguments("<url>").Trim();
                string callCmd = $"\"{browser.ExecutablePath}\" {profileArgs}".TrimEnd();

                sb.AppendLine($"[{profile.Id}] {label}");
                sb.AppendLine($"  Call: {callCmd}");
                sb.AppendLine();
            }
        }
        return sb.ToString().TrimEnd();
    }

    // -------------------------------------------------------------------------
    // json
    // -------------------------------------------------------------------------
    private static string FormatJson(List<BrowserInfo> browsers, DisplayNameStore? displayNames)
    {
        var data = browsers.Select(b => new
        {
            id         = b.Id,
            name       = b.Name,
            executable = b.ExecutablePath,
            type       = b.BrowserType.ToString(),
            profiles   = b.Profiles.Select(p => new
            {
                id               = p.Id,
                name             = p.Name,
                displayName      = displayNames?.GetDisplayName(p.Id),
                profileDirectory = p.ProfileDirectory,
                username         = p.UserName,
                launchCommand    = $"\"{b.ExecutablePath}\" {p.BuildArguments("<url>")}".TrimEnd(),
            }).ToArray(),
        }).ToArray();

        return JsonSerializer.Serialize(data, JsonOptions);
    }

    // -------------------------------------------------------------------------
    // yaml  (hand-rolled; no external dependency)
    // -------------------------------------------------------------------------
    private static string FormatYaml(List<BrowserInfo> browsers, DisplayNameStore? displayNames)
    {
        var sb = new StringBuilder();
        foreach (var b in browsers)
        {
            sb.AppendLine($"- id: {YamlString(b.Id)}");
            sb.AppendLine($"  name: {YamlString(b.Name)}");
            sb.AppendLine($"  executable: {YamlString(b.ExecutablePath)}");
            sb.AppendLine($"  type: {b.BrowserType}");
            sb.AppendLine("  profiles:");
            foreach (var p in b.Profiles)
            {
                string launchCmd = $"\"{b.ExecutablePath}\" {p.BuildArguments("<url>")}".TrimEnd();
                string? customName = displayNames?.GetDisplayName(p.Id);
                sb.AppendLine($"  - id: {YamlString(p.Id)}");
                sb.AppendLine($"    name: {YamlString(p.Name)}");
                if (customName != null)
                    sb.AppendLine($"    displayName: {YamlString(customName)}");
                sb.AppendLine($"    profileDirectory: {YamlString(p.ProfileDirectory)}");
                sb.AppendLine($"    username: {(p.UserName is null ? "null" : YamlString(p.UserName))}");
                sb.AppendLine($"    launchCommand: {YamlString(launchCmd)}");
            }
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>Quotes a YAML scalar value if it contains special characters.</summary>
    private static string YamlString(string value)
    {
        if (string.IsNullOrEmpty(value)) return "''";

        bool needsQuotes = value.Contains(':') || value.Contains('#') ||
                           value.Contains('\'') || value.Contains('"') ||
                           value.Contains('\\') || value.StartsWith(' ') ||
                           value.EndsWith(' ');
        if (!needsQuotes) return value;

        // Use double-quote style, escaping backslashes and double-quotes
        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    // -------------------------------------------------------------------------
    // csv
    // -------------------------------------------------------------------------
    private static string FormatCsv(List<BrowserInfo> browsers, DisplayNameStore? displayNames)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Browser,Profile,Username,Executable,ProfileDirectory,LaunchCommand,Id");
        foreach (var b in browsers)
        {
            foreach (var p in b.Profiles)
            {
                string launchCmd = $"\"{b.ExecutablePath}\" {p.BuildArguments("<url>")}".TrimEnd();
                string displayName = displayNames?.GetDisplayName(p.Id) ?? p.Name;
                sb.AppendLine(string.Join(',',
                    CsvField(b.Name),
                    CsvField(displayName),
                    CsvField(p.UserName ?? string.Empty),
                    CsvField(b.ExecutablePath),
                    CsvField(p.ProfileDirectory),
                    CsvField(launchCmd),
                    CsvField(p.Id)));
            }
        }
        return sb.ToString().TrimEnd();
    }

    private static string CsvField(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    // -------------------------------------------------------------------------
    // table
    // -------------------------------------------------------------------------
    private static string FormatTable(List<BrowserInfo> browsers, DisplayNameStore? displayNames)
    {
        // Flatten to rows first so we can compute column widths
        var rows = browsers
            .SelectMany(b => b.Profiles.Select(p => new[]
            {
                b.Name,
                displayNames?.GetDisplayName(p.Id) ?? p.ToString(),
                p.UserName ?? string.Empty,
                p.ProfileDirectory,
                b.BrowserType.ToString(),
                p.Id,
            }))
            .ToList();

        string[] headers = ["Browser", "Profile", "Username", "Profile Dir", "Type", "ID"];
        int cols = headers.Length;
        int[] widths = Enumerable.Range(0, cols)
            .Select(i => rows.Count == 0
                ? headers[i].Length
                : Math.Max(headers[i].Length, rows.Max(r => r[i].Length)))
            .ToArray();

        var sb = new StringBuilder();
        AppendTableRow(sb, headers, widths);
        AppendTableSeparator(sb, widths);
        foreach (var row in rows)
            AppendTableRow(sb, row, widths);

        return sb.ToString().TrimEnd();
    }

    private static void AppendTableRow(StringBuilder sb, string[] cells, int[] widths)
    {
        sb.Append('|');
        for (int i = 0; i < cells.Length; i++)
            sb.Append($" {cells[i].PadRight(widths[i])} |");
        sb.AppendLine();
    }

    private static void AppendTableSeparator(StringBuilder sb, int[] widths)
    {
        sb.Append('|');
        foreach (int w in widths)
            sb.Append(new string('-', w + 2) + "|");
        sb.AppendLine();
    }

    // -------------------------------------------------------------------------
    // grid
    // -------------------------------------------------------------------------

    /// <summary>
    /// Formats browsers as a grid: one row per browser (version), columns showing
    /// each available profile.  Regular profiles are prefixed with ●; private/incognito
    /// profiles are prefixed with ⊘.
    /// </summary>
    private static string FormatGrid(List<BrowserInfo> browsers, DisplayNameStore? displayNames)
    {
        if (browsers.Count == 0)
            return "(no browsers detected)";

        // Determine max number of profile columns needed
        int maxCols = browsers.Max(b => b.Profiles.Count);

        // Build display labels for every cell
        string GetCellLabel(BrowserProfile p) =>
            p.IsIncognito
                ? $"\u2298 {p.Name}"       // ⊘ Private Window / Incognito
                : $"\u25cf {p.Name}";      // ● profile-name

        // Compute column widths
        string browserHeader = "Browser";
        int browserWidth = Math.Max(browserHeader.Length, browsers.Max(b => b.Name.Length));

        int[] colWidths = new int[maxCols];
        for (int col = 0; col < maxCols; col++)
        {
            colWidths[col] = 0;
            foreach (var b in browsers)
            {
                if (col < b.Profiles.Count)
                    colWidths[col] = Math.Max(colWidths[col], GetCellLabel(b.Profiles[col]).Length);
            }
        }

        var sb = new StringBuilder();

        // Header row
        sb.Append(' ');
        sb.Append(browserHeader.PadRight(browserWidth));
        sb.Append(' ');
        for (int col = 0; col < maxCols; col++)
        {
            sb.Append("| ");
            sb.Append(string.Empty.PadRight(colWidths[col]));
            sb.Append(' ');
        }
        sb.AppendLine();

        // Separator
        sb.Append(new string('-', browserWidth + 2));
        for (int col = 0; col < maxCols; col++)
            sb.Append('+').Append(new string('-', colWidths[col] + 2));
        sb.AppendLine();

        // Browser rows
        foreach (var b in browsers)
        {
            sb.Append(' ');
            sb.Append(b.Name.PadRight(browserWidth));
            sb.Append(' ');
            for (int col = 0; col < maxCols; col++)
            {
                sb.Append("| ");
                if (col < b.Profiles.Count)
                {
                    string label = GetCellLabel(b.Profiles[col]);
                    sb.Append(label.PadRight(colWidths[col]));
                }
                else
                {
                    sb.Append(string.Empty.PadRight(colWidths[col]));
                }
                sb.Append(' ');
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
