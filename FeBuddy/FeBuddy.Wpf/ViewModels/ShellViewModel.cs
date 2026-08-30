using System.Collections.ObjectModel;
using FeBuddy.Wpf.Infrastructure;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Backs <c>ShellWindow</c>: owns the navigation lists and tracks which section
/// is showing. Navigation is intentionally trivial - activating a <see cref="NavItem"/>
/// makes its <see cref="NavItem.ViewModel"/> the <see cref="Current"/> content, and
/// a DataTemplate in ShellWindow.xaml maps that view-model to its view.
/// </summary>
public sealed class ShellViewModel : ObservableObject
{
    // Segoe Fluent Icons code-points (see Theme/Icons.xaml for the same set in XAML).
    private const string GlyphDashboard = ""; // Home
    private const string GlyphAirac     = ""; // Refresh
    private const string GlyphConvert   = ""; // Switch
    private const string GlyphFiles     = ""; // Folder
    private const string GlyphSettings  = ""; // Setting
    private const string GlyphInfo      = ""; // Info

    private object? _current;

    public ShellViewModel()
    {
        PrimaryNav =
        [
            Nav("Dashboard",      GlyphDashboard, () => new DashboardViewModel()),
            Nav("AIRAC Data",     GlyphAirac,     () => new AiracViewModel()),
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

        PrimaryNav[0].IsActive = true;
    }

    public ObservableCollection<NavItem> PrimaryNav { get; }

    public ObservableCollection<NavItem> SystemNav { get; }

    /// <summary>View-model of the section currently on screen.</summary>
    public object? Current
    {
        get => _current;
        private set => SetProperty(ref _current, value);
    }

    // ----- status bar (static sample text; real values would come from the library) -----

    public string AiracLabel => "AIRAC 2509  ·  eff 04 SEP 2025";

    public string VersionLabel => "v3.0.0-dev";

    private NavItem Nav(string title, string glyph, Func<object> factory)
        => new(title, glyph, factory, OnActivated);

    private void OnActivated(NavItem item)
    {
        // Enforce a single highlight across both nav groups.
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
