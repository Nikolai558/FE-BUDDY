using FeBuddy.Core.Application.Airac.Navaids.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// The FE-Buddy custom properties the NAVAIDs tab offers, in display order, with their tooltip text.
/// </summary>
/// <remarks>
/// Each property's name comes from Core (<see cref="Core.Application.Airac.FebProperties.Name{TProperty}"/>),
/// the same name <c>NavaidSettingsParser</c> accepts, so only the order and the wording live here.
/// </remarks>
public static class NavaidFebPropertyOptions
{
	/// <summary>Every property, in the order the tab lists them.</summary>
	public static IReadOnlyList<(NavaidFebProperty Property, string Description)> All { get; } =
	[
		(NavaidFebProperty.NavId, "NAVAID identifier. Symbols file only - the Text label already shows it."),
		(NavaidFebProperty.NavType, "NAVAID type, e.g. VORTAC or NDB. Symbols file only."),
		(NavaidFebProperty.Name, "NAVAID name. Symbols file only."),
		(NavaidFebProperty.Freq, "Frequency as NASR publishes it (MHz, or kHz for NDBs)."),
		(NavaidFebProperty.LowAltArtccId, "ARTCC whose low-altitude boundary the NAVAID is in."),
		(NavaidFebProperty.HighAltArtccId, "ARTCC whose high-altitude boundary the NAVAID is in."),
	];
}
