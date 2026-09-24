using FeBuddy.Wpf.Infrastructure;

using FeBuddy.Core.Application.Airac.Airports.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// The FE-Buddy custom properties an airport Feature can carry, with the names the settings
/// block and the GeoJSON keys use.
/// </summary>
/// <remarks>
/// The names here must match <c>AirportSettingsParser</c>'s own list exactly - it rejects a name
/// it does not recognize - so this is the GUI-side half of one contract, kept in one place
/// rather than spelled out in XAML. The enum value beside each name is the Core property
/// the parser maps it to.
/// </remarks>
public static class AirportFebPropertyNames
{
    /// <summary>Every property, in the order the tab lists them.</summary>
    public static IReadOnlyList<(AirportFebProperty Property, string Name, string Description)> All { get; } = new[]
    {
        (AirportFebProperty.FaaId, "faaId", "FAA identifier, e.g. SEA. Not written to the Text file, which already labels it."),
        (AirportFebProperty.IcaoId, "icaoId", "ICAO identifier, e.g. KSEA. Blank for airports that have none."),
        (AirportFebProperty.Name, "name", "Airport name. Not written to the Text file, which already labels it."),
        (AirportFebProperty.Elev, "elev", "Field elevation in feet. Blank when NASR publishes none."),
        (AirportFebProperty.RespArtcc, "respArtcc", "Responsible ARTCC identifier."),
        (AirportFebProperty.TfcPtrnAlt, "tfcPtrnAlt", "Traffic pattern altitude in feet."),
        (AirportFebProperty.FssId, "fssId", "Tie-in Flight Service Station identifier."),
        (AirportFebProperty.TwrType, "twrType", "Tower type, e.g. TWR or No-TWR."),
        (AirportFebProperty.RwyId, "rwyId", "Runway IDs, e.g. 16L/34R, in the same order as the runway lines. Runways Lines only."),
    };
}
