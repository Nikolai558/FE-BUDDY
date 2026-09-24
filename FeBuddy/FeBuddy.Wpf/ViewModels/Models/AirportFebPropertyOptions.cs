using FeBuddy.Core.Application.Airac.Airports.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// The FE-Buddy custom properties the Airports tab offers, in display order, with their tooltip text.
/// </summary>
/// <remarks>
/// Each property's name comes from Core (<see cref="Core.Application.Airac.FebProperties.Name{TProperty}"/>),
/// the same name <c>AirportSettingsParser</c> accepts, so only the order and the wording live here.
/// </remarks>
public static class AirportFebPropertyOptions
{
	/// <summary>Every property, in the order the tab lists them.</summary>
	public static IReadOnlyList<(AirportFebProperty Property, string Description)> All { get; } =
	[
		(AirportFebProperty.FaaId, "FAA identifier, e.g. SEA. Not written to the Text file, which already labels it."),
		(AirportFebProperty.IcaoId, "ICAO identifier, e.g. KSEA. Blank for airports that have none."),
		(AirportFebProperty.Name, "Airport name. Not written to the Text file, which already labels it."),
		(AirportFebProperty.Elev, "Field elevation in feet. Blank when NASR publishes none."),
		(AirportFebProperty.RespArtcc, "Responsible ARTCC identifier."),
		(AirportFebProperty.TfcPtrnAlt, "Traffic pattern altitude in feet."),
		(AirportFebProperty.FssId, "Tie-in Flight Service Station identifier."),
		(AirportFebProperty.TwrType, "Tower type, e.g. TWR or No-TWR."),
		(AirportFebProperty.RwyId, "Runway IDs, e.g. 16L/34R, in the same order as the runway lines. Runways Lines only."),
	];
}
