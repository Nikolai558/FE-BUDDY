using FeBuddy.Core.Application.Airac.Airways.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// The FE-Buddy custom properties the Airways tab offers, in display order, with their tooltip text.
/// </summary>
/// <remarks>
/// Each property's name comes from Core (<see cref="Core.Application.Airac.FebProperties.Name{TProperty}"/>),
/// the same name <c>AirwaySettingsParser</c> accepts, so only the order and the wording live here.
/// </remarks>
public static class AirwayFebPropertyOptions
{
	/// <summary>Every property, in the order the tab lists them.</summary>
	public static IReadOnlyList<(AirwayFebProperty Property, string Description)> All { get; } =
	[
		(AirwayFebProperty.AwyId, "The airway ID. On Symbols and Text, every airway in the file that uses the point."),
		(AirwayFebProperty.PointId, "The waypoint's ID. Symbols only."),
		(AirwayFebProperty.Waypoints, "The airway's waypoint IDs, in order. Lines only."),
	];
}
