using System.IO;
using System.Text.Json;

namespace BrowserAptor.Services;

/// <summary>
/// Persists custom display names for browsers and profiles, keyed by their stable ID.
/// The store is saved as a JSON file at <c>%APPDATA%\BrowserAptor\displaynames.json</c>.
/// </summary>
public class DisplayNameStore
{
    private const string AppFolder = "BrowserAptor";
    private const string FileName  = "displaynames.json";

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly Dictionary<string, string> _names;

    /// <summary>Creates a store backed by the default per-user config file.</summary>
    public DisplayNameStore() : this(DefaultFilePath()) { }

    /// <summary>Creates a store backed by the specified file (used for testing).</summary>
    public DisplayNameStore(string filePath)
    {
        _filePath = filePath;
        _names    = Load(filePath);
    }

    /// <summary>Returns the default path for the display-names configuration file.</summary>
    public static string DefaultFilePath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, AppFolder, FileName);
    }

    /// <summary>Returns the custom display name for <paramref name="id"/>, or <c>null</c> if none is set.</summary>
    public string? GetDisplayName(string id) =>
        _names.TryGetValue(id, out string? name) ? name : null;

    /// <summary>Sets a custom display name for <paramref name="id"/> and persists it to disk.</summary>
    public void SetDisplayName(string id, string displayName)
    {
        _names[id] = displayName;
        Save();
    }

    /// <summary>Removes any custom display name for <paramref name="id"/> and persists the change.</summary>
    public void RemoveDisplayName(string id)
    {
        if (_names.Remove(id))
            Save();
    }

    private void Save()
    {
        string? dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        string json = JsonSerializer.Serialize(_names, JsonOpts);
        File.WriteAllText(_filePath, json);
    }

    private static Dictionary<string, string> Load(string path)
    {
        if (!File.Exists(path))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            string json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (loaded == null)
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Copy into a case-insensitive dictionary so IDs are matched regardless
            // of the capitalisation used in the stored file.
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (k, v) in loaded)
                result[k] = v;
            return result;
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
