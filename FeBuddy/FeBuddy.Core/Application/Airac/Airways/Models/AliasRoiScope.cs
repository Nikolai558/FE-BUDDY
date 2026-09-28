namespace FeBuddy.Core.Application.Airac.Airways.Models;

/// <summary>
/// Which airways the <c>Airways.txt</c> alias file covers.
/// </summary>
public enum AliasRoiScope
{
	/// <summary>Every airway, whatever the ROI (the historical behaviour).</summary>
	All = 0,

	/// <summary>
	/// Exactly the airways the GeoJSON draws: those whose line crosses the ROI, even when none
	/// of their waypoints is inside it (<c>Airway.CrossesRoi</c>). Each gets its whole waypoint
	/// list, since the alias is meant to draw the entire airway - the clipping only ever shapes
	/// the GeoJSON line.
	/// </summary>
	RoiAirways = 1,
}
