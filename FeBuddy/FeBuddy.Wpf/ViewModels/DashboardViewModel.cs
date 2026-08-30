using System.Collections.ObjectModel;
using FeBuddy.Wpf.Infrastructure;
using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// "At a glance" landing screen. All values are illustrative sample data - this
/// project has no link to the library that would supply the real numbers.
/// </summary>
public sealed class DashboardViewModel : ObservableObject
{
    public string HeroKicker => "AIRAC 2509  ·  CURRENT CYCLE";

    public string HeroTitle => "FE-Buddy, at a glance.";

    public string HeroBody =>
        "One place to pull the current NASR cycle, convert legacy facility files, " +
        "and build ERAM-ready GeoJSON. Everything below is sample data - wire the " +
        "library in to make it live.";

    public ObservableCollection<StatusRow> GeneratorStatus { get; } =
    [
        new("UserConfig", "Loaded from disk", StatusKind.Ok),
        new("NASR dataset", "Not downloaded", StatusKind.Pending),
        new("Last airway run", "Never", StatusKind.Idle),
    ];

    public ObservableCollection<OutputFile> RecentOutput { get; } =
    [
        new("Airways_High.geojson", "412 KB", "2d ago"),
        new("Airways_Low.geojson", "268 KB", "2d ago"),
        new("Airways_Other.geojson", "31 KB", "2d ago"),
    ];

    public string NewsTitle => "What's new in v3.0";

    public string NewsBody =>
        "Rebuilt airway engine with efficient LineString handling, feb.* custom " +
        "properties, and per-file ROI overrides.";
}
