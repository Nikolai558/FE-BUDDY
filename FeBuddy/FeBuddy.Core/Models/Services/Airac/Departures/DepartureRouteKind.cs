namespace FeBuddy.Core.Models.Services.Airac.Departures;

/// <summary>Which portion of a departure procedure a route is (<c>DP_RTE.ROUTE_PORTION_TYPE</c>).</summary>
public enum DepartureRouteKind
{
	/// <summary>
	/// A body: the part flown from the runway. A procedure can have several, one per runway or
	/// runway group, and <c>DP_APT</c> says which airport uses which.
	/// </summary>
	Body = 0,

	/// <summary>A transition: the part flown from the body out to an en-route fix. Shared by every airport.</summary>
	Transition = 1,
}
