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
        "Pull the current NASR cycle and build the whole CRC facility package - maps, " +
        "alias files, reference - clipped to your region of interest. vSTARS / vERAM / " +
        "VRC and DXF are retired; everything below is sample data.";

    public string CycleDetail => "Next cycle 2510  ·  effective 02 OCT 2025  ·  18 days  ·  APRA verified";

    public ObservableCollection<StatusRow> GeneratorStatus { get; } =
    [
        new("NASR dataset", "Cycle 2509 · downloaded", StatusKind.Ok),
        new("Region of interest", "ZOB boundary + 150 nm", StatusKind.Ok),
        new("Last cycle build", "2 days ago · 61 files", StatusKind.Ok),
        new("Validator", "0 errors on last check", StatusKind.Ok),
    ];

    public ObservableCollection<OutputFile> RecentOutput { get; } =
    [
        new("CRC/AWY-HIGH_lines.geojson", "412 KB", "2d ago"),
        new("CRC/APT_symbols.geojson", "88 KB", "2d ago"),
        new("CRC/STARs/000_All_STAR_Combined.geojson", "1.2 MB", "2d ago"),
        new("ALIAS/AWY_ALIAS.txt", "36 KB", "2d ago"),
    ];

    public string NewsTitle => "What's new in v3.0";

    public string NewsBody =>
        "Every generator now clips to your ROI. New: GeoJSON validator + clean-up, " +
        "facility profiles, and a modern UI. Retired: vSTARS / vERAM / VRC and DXF output.";
}
