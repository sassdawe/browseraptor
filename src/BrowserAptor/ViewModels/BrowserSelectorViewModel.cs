using BrowserAptor.Models;
using BrowserAptor.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

    private BrowserEntryViewModel? _selectedEntry;

    public ObservableCollection<BrowserEntryViewModel> Entries { get; } = new();

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

    public ICommand OpenCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action? RequestClose;

    public BrowserSelectorViewModel(
        IBrowserDetectionService detectionService,
        IBrowserLaunchService launchService,
        string url)
    {
        _detectionService = detectionService;
        _launchService = launchService;
        _url = url;

        OpenCommand = new RelayCommand(ExecuteOpen, CanOpen);
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke());

        LoadBrowsers();
    }

    private void LoadBrowsers()
    {
        Entries.Clear();
        var browsers = _detectionService.DetectBrowsers();

        foreach (var browser in browsers)
        {
            if (browser.Profiles.Count == 1)
            {
                // Single-profile browser: show as one entry
                Entries.Add(new BrowserEntryViewModel(browser, browser.Profiles[0]));
            }
            else
            {
                // Multi-profile browser: show each profile as a separate entry
                foreach (var profile in browser.Profiles)
                {
                    Entries.Add(new BrowserEntryViewModel(browser, profile));
                }
            }
        }

        // Auto-select first entry
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
/// Represents a single browser + profile combination in the selector list.
/// </summary>
public class BrowserEntryViewModel
{
    public BrowserInfo Browser { get; }
    public BrowserProfile Profile { get; }

    public string DisplayName { get; }
    public string ExePath => Browser.ExecutablePath;

    public BrowserEntryViewModel(BrowserInfo browser, BrowserProfile profile)
    {
        Browser = browser;
        Profile = profile;

        bool hasMultipleProfiles = browser.Profiles.Count > 1;
        DisplayName = hasMultipleProfiles
            ? $"{browser.Name} — {profile}"
            : browser.Name;
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
