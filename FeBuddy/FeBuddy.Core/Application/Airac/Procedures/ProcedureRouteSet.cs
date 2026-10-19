using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// One cycle's procedure routes and the lookup that says which ARTCCs their points are in - what the
/// procedures-with-a-fix-in-my-facility rule (<see cref="ProceduresWithFixInFacility"/>) reads.
/// Built once per run, per cycle.
/// </summary>
/// <param name="routes">The procedures and their routes, from any route source.</param>
/// <param name="points">Which ARTCCs the routes' points are in, from the same cycle.</param>
public sealed class ProcedureRouteSet(IReadOnlyList<ProcedureRoute> routes, RoutePointArtccResolver points)
{
	private readonly ILookup<string, ProcedureRoute> _byAirport = routes
		.SelectMany(route => route.Airports.Select(airport => (Airport: airport, Route: route)))
		.ToLookup(pair => pair.Airport, pair => pair.Route, StringComparer.OrdinalIgnoreCase);

	/// <summary>Which ARTCCs the routes' points are in.</summary>
	public RoutePointArtccResolver Points { get; } = points;

	/// <summary>NASR's departures and arrivals (<see cref="NasrProcedureRoutes"/>), with that cycle's fixes and NAVAIDs.</summary>
	/// <param name="nasr">Parsed NASR data with its <c>DP</c>, <c>STAR</c>, <c>FIX</c> and <c>NAV</c> groups (and <c>APT</c>, for airports).</param>
	/// <returns>The set.</returns>
	/// <exception cref="InvalidOperationException">A group it needs was not parsed.</exception>
	public static ProcedureRouteSet FromNasr(NasrCsvDataCollection nasr) =>
		new(NasrProcedureRoutes.Read(nasr), new RoutePointArtccResolver(nasr));

	/// <summary>The routes that serve an airport, by its FAA or ICAO identifier, each once.</summary>
	/// <param name="identifiers">The airport's identifiers.</param>
	/// <returns>The routes.</returns>
	public IEnumerable<ProcedureRoute> At(params string?[] identifiers) =>
		identifiers.OfType<string>().SelectMany(identifier => _byAirport[identifier]).Distinct();
}
