namespace FeBuddy.Core.Application.Airac.Departures.Models;

/// <summary>How the Region of Interest decides whether an airport's departure is in scope.</summary>
public enum DepartureRoiMode
{
	/// <summary>
	/// Every departure for an airport whose reference point is inside the ROI. An airport with
	/// no APT_BASE record has no location, so it is left out whenever this mode is in use.
	/// </summary>
	Airport = 0,

	/// <summary>Any departure with at least one of its points inside the ROI.</summary>
	Waypoint = 1,
}
