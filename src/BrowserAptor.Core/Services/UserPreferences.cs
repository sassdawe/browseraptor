using System.IO;
using System.Text.Json;

namespace BrowserAptor.Services;

/// <summary>
/// Persists user interface preferences for BrowserAptor.
/// Stored as a JSON file at <c>%APPDATA%\BrowserAptor\preferences.json</c>.
/// </summary>
public class UserPreferences
{
    private const string AppFolder = "BrowserAptor";
    private const string FileName  = "preferences.json";

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private readonly string _filePath;

    /// <summary>
    /// When <c>true</c>, a single click on a browser/profile entry opens it immediately.
    /// When <c>false</c> (default), a double-click or Enter is required.
    /// </summary>
    public bool SingleClickToOpen { get; set; }

    /// <summary>
    /// When <c>true</c>, the selector opens in grid view; when <c>false</c> (default) it opens in list view.
    /// </summary>
    public bool IsGridView { get; set; }

    /// <summary>Creates preferences backed by the default per-user config file.</summary>
    public UserPreferences() : this(DefaultFilePath()) { }

    /// <summary>Creates preferences backed by the specified file (used for testing).</summary>
    public UserPreferences(string filePath)
    {
        _filePath = filePath;
        Load();
    }

    /// <summary>Returns the default path for the preferences file.</summary>
    public static string DefaultFilePath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, AppFolder, FileName);
    }

    /// <summary>Persists the current preferences to disk.</summary>
    public void Save()
    {
        string? dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var data = new PreferencesData { SingleClickToOpen = SingleClickToOpen, IsGridView = IsGridView };
        File.WriteAllText(_filePath, JsonSerializer.Serialize(data, JsonOpts));
    }

    private void Load()
    {
        if (!File.Exists(_filePath)) return;
        try
        {
            string json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<PreferencesData>(json);
            if (data != null)
            {
                SingleClickToOpen = data.SingleClickToOpen;
                IsGridView        = data.IsGridView;
            }
        }
        catch
        {
            // Ignore corrupt files; keep defaults
        }
    }

    private sealed class PreferencesData
    {
        public bool SingleClickToOpen { get; set; }
        public bool IsGridView { get; set; }
    }
}
