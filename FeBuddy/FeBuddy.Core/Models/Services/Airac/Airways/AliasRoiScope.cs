namespace FeBuddy.Core.Models.Services.Airac.Airways;

/// <summary>
/// Which airways the <c>Airways.txt</c> alias file covers.
/// </summary>
public enum AliasRoiScope
{
	/// <summary>Every airway (the historical behaviour).</summary>
	All = 0,

	/// <summary>
	/// Only airways with at least one waypoint inside the ROI - but then the whole airway's
	/// waypoint list, since the alias is meant to draw the entire airway. Uses a point-in-ROI
	/// test, never the ROI-clipped geometry.
	/// </summary>
	RoiAirways = 1,
}
