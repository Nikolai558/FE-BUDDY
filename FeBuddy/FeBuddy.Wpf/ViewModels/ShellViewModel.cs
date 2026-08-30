using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Threading;
using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Backs <c>ShellWindow</c>: navigation, the status-bar Zulu clock, and the
/// systems-health popover. All values are sample data.
/// </summary>
public sealed class ShellViewModel : ObservableObject
{
    // Segoe Fluent Icons code-points (see Theme/Icons.xaml for the same set in XAML).
    private const string GlyphDashboard = ""; // Home
    private const string GlyphAirac     = ""; // Refresh
    private const string GlyphMap       = ""; // MapPin
    private const string GlyphConvert   = ""; // Switch
    private const string GlyphFiles     = ""; // Folder
    private const string GlyphSettings  = ""; // Setting
    private const string GlyphInfo      = ""; // Info

    private object? _current;
    private string _zuluClock = string.Empty;
    private bool _navCollapsed;

    public ShellViewModel()
    {
        PrimaryNav =
        [
            Nav("Dashboard",      GlyphDashboard, () => new DashboardViewModel()),
            Nav("AIRAC Data",     GlyphAirac,     () => new AiracViewModel()),
            Nav("Map",            GlyphMap,       () => new MapViewModel()),
            Nav("Conversions",    GlyphConvert,   () => new PlaceholderViewModel(
                "Conversions", "DAT / KML / SCT2 / vSTARS-vERAM to GeoJSON.")),
            Nav("Facility Files", GlyphFiles,     () => new PlaceholderViewModel(
                "Facility Files", "Alias maintenance and facility admin tools.")),
        ];

        SystemNav =
        [
            Nav("Settings", GlyphSettings, () => new SettingsViewModel()),
            Nav("Info",     GlyphInfo,     () => new InfoViewModel()),
        ];

        SystemHealth =
        [
            new HealthRow("NASR data source", "nfdc.faa.gov · reachable", StatusKind.Ok),
            new HealthRow("Time service", "timeapi.io · 41 ms", StatusKind.Ok),
            new HealthRow("Updates (GitHub)", "v3.0.1 available on dev", StatusKind.Warn),
            new HealthRow("Output folder", @"…\FE-Buddy\Output · writable", StatusKind.Ok),
        ];

        RecheckCommand = new RelayCommand(() =>
            Toast.Info("Systems re-checked", "All endpoints responded."));

        ToggleNavCommand = new RelayCommand(() => NavCollapsed = !NavCollapsed);

        PrimaryNav[0].IsActive = true;

        // Zulu clock, ticked once a second on the UI thread.
        UpdateZulu();
        var clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        clock.Tick += (_, _) => UpdateZulu();
        clock.Start();
    }

    public ObservableCollection<NavItem> PrimaryNav { get; }

    public ObservableCollection<NavItem> SystemNav { get; }

    public ObservableCollection<HealthRow> SystemHealth { get; }

    public ICommand RecheckCommand { get; }

    public ICommand ToggleNavCommand { get; }

    /// <summary>Icons-only nav rail when true.</summary>
    public bool NavCollapsed
    {
        get => _navCollapsed;
        set => SetProperty(ref _navCollapsed, value);
    }

    /// <summary>View-model of the section currently on screen.</summary>
    public object? Current
    {
        get => _current;
        private set => SetProperty(ref _current, value);
    }

    public string AiracLabel => "AIRAC 2509  ·  eff 04 SEP 2025";

    public string VersionLabel => "v3.0.0-dev";

    /// <summary>e.g. <c>1543Z · Tue 30 Aug</c>.</summary>
    public string ZuluClock
    {
        get => _zuluClock;
        private set => SetProperty(ref _zuluClock, value);
    }

    /// <summary>Worst state across <see cref="SystemHealth"/> — drives the nav dot colour.</summary>
    public StatusKind HealthWorst =>
        SystemHealth.Any(h => h.Kind == StatusKind.Down) ? StatusKind.Down :
        SystemHealth.Any(h => h.Kind == StatusKind.Warn) ? StatusKind.Warn :
        StatusKind.Ok;

    public string HealthSummary
    {
        get
        {
            var issues = SystemHealth.Count(h => h.Kind is StatusKind.Warn or StatusKind.Down);
            return issues switch
            {
                0 => "All systems nominal",
                1 => "1 needs attention",
                _ => $"{issues} need attention",
            };
        }
    }

    private void UpdateZulu()
        => ZuluClock = DateTime.UtcNow.ToString("HHmm'Z'  ·  ddd dd MMM", CultureInfo.InvariantCulture);

    private NavItem Nav(string title, string glyph, Func<object> factory)
        => new(title, glyph, factory, OnActivated);

    private void OnActivated(NavItem item)
    {
        foreach (var other in PrimaryNav.Concat(SystemNav))
        {
            if (!ReferenceEquals(other, item))
            {
                other.IsActive = false;
            }
        }

        Current = item.ViewModel;
    }
}
