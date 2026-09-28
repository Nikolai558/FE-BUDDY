namespace FeBuddy.Core.Application.Airac.Arrivals.Models;

/// <summary>How the Region of Interest decides whether an airport's arrival is in scope.</summary>
public enum ArrivalRoiMode
{
	/// <summary>
	/// Every arrival for an airport whose reference point is inside the ROI. An airport with no
	/// APT_BASE record has no location, so it is left out whenever this mode is in use.
	/// </summary>
	Airport = 0,

	/// <summary>Any arrival with at least one of its points inside the ROI.</summary>
	Waypoint = 1,
}
