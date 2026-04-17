using BrowserAptor.Models;
using BrowserAptor.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace BrowserAptor.ViewModels;

/// <summary>
/// ViewModel for the browser/profile selector window.
/// </summary>
public class BrowserSelectorViewModel : INotifyPropertyChanged
{
    private readonly IBrowserDetectionService _detectionService;
    private readonly IBrowserLaunchService _launchService;
    private readonly string _url;
    private readonly DisplayNameStore _displayNames = new();
    private readonly UserPreferences _preferences = new();

    private BrowserEntryViewModel? _selectedEntry;
    private bool _isGridView;

    public ObservableCollection<BrowserEntryViewModel> Entries { get; } = new();
    public ObservableCollection<BrowserGridRowViewModel> BrowserRows { get; } = new();

    public BrowserEntryViewModel? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            _selectedEntry = value;
            OnPropertyChanged();
            ((RelayCommand)OpenCommand).RaiseCanExecuteChanged();
        }
    }

    public string Url => _url;

    public bool SingleClickToOpen => _preferences.SingleClickToOpen;

    public bool IsGridView
    {
        get => _isGridView;
        set
        {
            _isGridView = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsListView));
            _preferences.IsGridView = value;
            _preferences.Save();
        }
    }

    public bool IsListView => !_isGridView;

    public ICommand OpenCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand CopyLinkCommand { get; }
    public ICommand ToggleViewCommand { get; }

    public event Action? RequestClose;

    public BrowserSelectorViewModel(
        IBrowserDetectionService detectionService,
        IBrowserLaunchService launchService,
        string url)
    {
        _detectionService = detectionService;
        _launchService = launchService;
        _url = url;

        OpenCommand     = new RelayCommand(ExecuteOpen, CanOpen);
        CancelCommand   = new RelayCommand(_ => RequestClose?.Invoke());
        CopyLinkCommand = new RelayCommand(_ => Clipboard.SetText(_url));
        ToggleViewCommand = new RelayCommand(_ => IsGridView = !IsGridView);

        // Restore last-used view mode
        _isGridView = _preferences.IsGridView;

        LoadBrowsers();
    }

    private void LoadBrowsers()
    {
        Entries.Clear();
        BrowserRows.Clear();
        var browsers = _detectionService.DetectBrowsers();

        Action<BrowserProfile> openAction = p =>
        {
            _launchService.Launch(p, _url);
            RequestClose?.Invoke();
        };

        foreach (var browser in browsers)
        {
            var rowEntries = new List<BrowserEntryViewModel>();

            if (browser.Profiles.Count == 1)
            {
                string? customName = _displayNames.GetDisplayName(browser.Profiles[0].Id);
                var entry = new BrowserEntryViewModel(browser, browser.Profiles[0], customName, openAction);
                Entries.Add(entry);
                rowEntries.Add(entry);
            }
            else
            {
                foreach (var profile in browser.Profiles)
                {
                    string? customName = _displayNames.GetDisplayName(profile.Id);
                    var entry = new BrowserEntryViewModel(browser, profile, customName, openAction);
                    Entries.Add(entry);
                    rowEntries.Add(entry);
                }
            }

            BrowserRows.Add(new BrowserGridRowViewModel(browser, rowEntries));
        }

        SelectedEntry = Entries.FirstOrDefault();
    }

    private bool CanOpen(object? _) => SelectedEntry != null;

    private void ExecuteOpen(object? _)
    {
        if (SelectedEntry == null) return;
        _launchService.Launch(SelectedEntry.Profile, _url);
        RequestClose?.Invoke();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Represents one browser row in the grid view, with all its profile tiles.
/// </summary>
public class BrowserGridRowViewModel
{
    public string Name { get; }
    public string ExePath { get; }
    public string? Channel { get; }
    public IReadOnlyList<BrowserEntryViewModel> Profiles { get; }

    public BrowserGridRowViewModel(BrowserInfo browser, IReadOnlyList<BrowserEntryViewModel> profiles)
    {
        Name     = browser.Name;
        ExePath  = browser.ExecutablePath;
        Channel  = browser.Channel;
        Profiles = profiles;
    }
}

/// <summary>
/// Represents a single browser + profile combination in the selector list.
/// </summary>
public class BrowserEntryViewModel
{
    // Channel / state colour constants (kept in one place for easy theming)
    private const string ColorBeta      = "#E07700";
    private const string ColorDev       = "#0D9488";
    private const string ColorCanary    = "#CA8A04";
    private const string ColorNightly   = "#7C3AED";
    private const string ColorIncogText = "#6C7086";   // SubtleBrush – list-view label tint
    private const string ColorIncogTile = "#45475A";   // CancelBrush – tile background
    private const string ColorSurface   = "#2A2A3C";   // SurfaceBrush – stable tile background
    private const string ColorForeground= "#CDD6F4";   // ForegroundBrush – stable label tint

    public BrowserInfo Browser { get; }
    public BrowserProfile Profile { get; }

    public string DisplayName { get; }
    public string ExePath => Browser.ExecutablePath;
    public ICommand? LaunchCommand { get; }

    /// <summary>
    /// Hex color string used to tint the display-name label in list view:
    /// grey for incognito profiles, a channel-specific color for Beta/Dev/Canary/Nightly,
    /// or the default foreground color for stable profiles.
    /// </summary>
    public string LabelColor
    {
        get
        {
            if (Profile.IsIncognito) return ColorIncogText;
            return Browser.Channel switch
            {
                "Beta"    => ColorBeta,
                "Dev"     => ColorDev,
                "Canary"  => ColorCanary,
                "Nightly" => ColorNightly,
                _         => ColorForeground,
            };
        }
    }

    /// <summary>
    /// Background hex color for the profile tile in grid view:
    /// grey for incognito, channel color for Beta/Dev/Canary/Nightly,
    /// or the default surface color for stable profiles.
    /// </summary>
    public string TileBackground
    {
        get
        {
            if (Profile.IsIncognito) return ColorIncogTile;
            return Browser.Channel switch
            {
                "Beta"    => ColorBeta,
                "Dev"     => ColorDev,
                "Canary"  => ColorCanary,
                "Nightly" => ColorNightly,
                _         => ColorSurface,
            };
        }
    }

    public BrowserEntryViewModel(BrowserInfo browser, BrowserProfile profile,
                                 string? customDisplayName = null,
                                 Action<BrowserProfile>? onLaunch = null)
    {
        Browser = browser;
        Profile = profile;

        if (customDisplayName != null)
        {
            DisplayName = customDisplayName;
        }
        else
        {
            bool hasMultipleProfiles = browser.Profiles.Count > 1;
            DisplayName = hasMultipleProfiles
                ? $"{browser.Name} \u2014 {profile}"
                : browser.Name;
        }

        if (onLaunch != null)
            LaunchCommand = new RelayCommand(_ => onLaunch(profile));
    }

    public override string ToString() => DisplayName;
}

/// <summary>
/// Simple ICommand implementation for WPF.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged;

    public void RaiseCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
