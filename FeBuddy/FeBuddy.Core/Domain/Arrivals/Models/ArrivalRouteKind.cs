namespace FeBuddy.Core.Domain.Arrivals.Models;

/// <summary>Which portion of an arrival procedure a route is (<c>STAR_RTE.ROUTE_PORTION_TYPE</c>).</summary>
public enum ArrivalRouteKind
{
	/// <summary>
	/// A body: the part flown into the airport. A procedure can have several, one per airport or
	/// runway group, and <c>STAR_APT</c> says which airport uses which.
	/// </summary>
	Body = 0,

	/// <summary>A transition: the part flown from an en-route fix into the body. Shared by every airport.</summary>
	Transition = 1,
}
