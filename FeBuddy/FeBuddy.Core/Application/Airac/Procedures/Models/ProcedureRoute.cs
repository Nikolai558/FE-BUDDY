namespace FeBuddy.Core.Application.Airac.Procedures.Models;

/// <summary>
/// One procedure and its route, as a route source gives it (NASR's departures and arrivals, see
/// <c>NasrProcedureRoutes</c>; later the FAA CIFP's approaches): enough to match it to its d-TPP
/// chart at each airport it serves, and to say which ARTCCs its route passes through.
/// </summary>
/// <param name="ChartCodes">The d-TPP <c>chart_code</c>s its chart can have, e.g. <c>DP</c> and <c>ODP</c>, or <c>STR</c>.</param>
/// <param name="ComputerCode">
/// Its FAA computer code, e.g. <c>CLVIN3.CLVIN</c>, trimmed; <see langword="null"/> when it has none
/// (NASR's <c>NOT ASSIGNED</c>, or blank).
/// </param>
/// <param name="Name">Its name, e.g. <c>CLVIN</c> or <c>GRAYT TWO (RNAV)</c>, for when there is no code to match on.</param>
/// <param name="Airports">The airports it serves, by FAA identifier.</param>
/// <param name="Points">Every point of every body and transition of its route, in order.</param>
public sealed record ProcedureRoute(
	IReadOnlyCollection<string> ChartCodes,
	string? ComputerCode,
	string Name,
	IReadOnlyCollection<string> Airports,
	IReadOnlyList<ProcedureRoutePoint> Points);

/// <summary>One point of a procedure's route.</summary>
/// <param name="Id">The fix's or NAVAID's identifier, e.g. <c>DUCKY</c> or <c>DCA</c>, trimmed.</param>
/// <param name="Type">
/// What it is, as NASR's <c>POINT_TYPE</c> says: a fix's use code (<c>WP</c>, <c>RP</c>...) or a NAVAID's
/// type (<c>VORTAC</c>, <c>NDB</c>...), trimmed.
/// </param>
/// <param name="Region">A fix's two-letter ICAO region code, e.g. <c>K6</c>; <see langword="null"/> for none.</param>
public sealed record ProcedureRoutePoint(string Id, string Type, string? Region);
