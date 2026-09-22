using System.Globalization;

using FeBuddy.Core.Handlers.General;
using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac.Departures;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.General;

using static FeBuddy.Core.Models.NASR.CSV.DpCsvDataModel;

namespace FeBuddy.Core.Services.Airac.Departures;

/// <summary>
/// Reads every departure procedure out of <c>DP_BASE</c>, <c>DP_APT</c> and <c>DP_RTE</c>, then
/// splits each one per airport and locates its points in FIX_BASE / NAV_BASE.
/// </summary>
/// <remarks>
/// The two steps are separate so the service can apply the cheap procedure-level filters in
/// between: locating is the expensive part, and a procedure the user filtered out should never
/// produce a "point not found" warning.
/// </remarks>
public static class DepartureBuilder
{
	private const string LogSource = "DepartureBuilder";
	private const string ObstacleType = "OBSTACLE";
	private const string BodyPortion = "BODY";
	private const string TransitionPortion = "TRANSITION";

	/// <summary>The <c>POINT_TYPE</c> values that are fixes (FIX_BASE). Every other type is a navaid.</summary>
	private static readonly HashSet<string> FixPointTypes = new(StringComparer.OrdinalIgnoreCase) { "WP", "RP", "CN" };

	/// <summary>
	/// The <c>POINT_TYPE</c> values that are navaids (NAV_BASE) - every navaid type DP_RTE uses.
	/// A type in neither set is looked up without a type, which lets the lookup decide.
	/// </summary>
	private static readonly HashSet<string> NavaidPointTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"VORTAC", "VOR/DME", "VOR", "TACAN", "DME", "NDB", "NDB/DME",
	};

	/// <summary>
	/// Reads every procedure from the DP tables, without locating anything.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data. <c>Dp</c> must not be null.</param>
	/// <returns>Every procedure, ordered by ARTCC then name, plus any messages.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the DP data has not been parsed.</exception>
	public static DepartureProcedureReadResult ReadProcedures(NasrCsvDataCollection allNasrCsvData)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);

		if (allNasrCsvData.Dp is null)
		{
			throw new InvalidOperationException(
				"Departure procedure data (DP) has not been parsed. The Departures sub-service cannot run without it.");
		}

		List<ServiceMessage> messages = new();

		ILookup<(string, string), DpApt> aptRows = allNasrCsvData.Dp.DpApt.ToLookup(
			row => ProcedureKey(row.DpName, row.Artcc), KeyComparer.Instance);

		ILookup<(string, string), DpRte> routeRows = allNasrCsvData.Dp.DpRte.ToLookup(
			row => ProcedureKey(row.DpName, row.Artcc), KeyComparer.Instance);

		List<DepartureProcedure> procedures = new();

		foreach (DpBase row in allNasrCsvData.Dp.DpBase)
		{
			string dpName = row.DpName?.Trim() ?? string.Empty;
			string artcc = row.Artcc?.Trim() ?? string.Empty;

			if (dpName.Length == 0)
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"A DP_BASE row for ARTCC '{artcc}' has no DP_NAME and was skipped."));
				continue;
			}

			(string, string) key = ProcedureKey(dpName, artcc);
			List<DpApt> apts = aptRows[key].ToList();
			List<DpRte> routes = routeRows[key].ToList();

			string computerCode = row.DpComputerCode?.Trim() ?? string.Empty;
			string codeId = DepartureNaming.CodeIdFor(computerCode, row.AmendmentNo, dpName, out _);

			procedures.Add(new DepartureProcedure
			{
				DpName = dpName,
				Artcc = artcc,
				ComputerCode = computerCode,
				CodeId = codeId,
				AmendmentNo = row.AmendmentNo?.Trim() ?? string.Empty,
				AmendmentEffectiveDateText = row.DpAmendEffDate?.Trim() ?? string.Empty,
				AmendmentEffectiveDate = ParseNasrDate(row.DpAmendEffDate),
				CycleEffectiveDate = ParseNasrDate(row.EffDate),
				IsObstacleDeparture = string.Equals(row.GraphicalDpType?.Trim(), ObstacleType, StringComparison.OrdinalIgnoreCase),
				ServedAirports = ServedAirports(row, apts),
				Bodies = ReadRoutes(routes, BodyPortion, DepartureRouteKind.Body),
				Transitions = ReadRoutes(routes, TransitionPortion, DepartureRouteKind.Transition),
				BodyNamesByAirport = BodyNamesByAirport(apts),
			});
		}

		List<DepartureProcedure> ordered = procedures
			.OrderBy(p => p.Artcc, StringComparer.OrdinalIgnoreCase)
			.ThenBy(p => p.DpName, StringComparer.OrdinalIgnoreCase)
			.ToList();

		return new DepartureProcedureReadResult(ordered, messages);
	}

	/// <summary>
	/// Splits each procedure per airport and locates every point.
	/// </summary>
	/// <param name="procedures">The procedures in scope (already filtered by type, ARTCC and amendment date).</param>
	/// <param name="allNasrCsvData">All parsed NASR CSV data; FIX_BASE and NAV_BASE are read.</param>
	/// <returns>
	/// Every airport + procedure whose points were all found. One missing point leaves that
	/// airport + procedure out entirely, with a warning naming the point.
	/// </returns>
	public static DepartureLocateResult Locate(IReadOnlyList<DepartureProcedure> procedures, NasrCsvDataCollection allNasrCsvData)
	{
		ArgumentNullException.ThrowIfNull(procedures);
		ArgumentNullException.ThrowIfNull(allNasrCsvData);

		List<ServiceMessage> messages = new();
		List<DepartureAirportProcedure> located = new();
		HashSet<(string, string)> identities = new(KeyComparer.Instance);
		int skipped = 0;

		foreach (DepartureProcedure procedure in procedures)
		{
			if (!procedure.HasRoutes)
			{
				messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
					$"{Label(procedure)} has no DP_RTE rows, so nothing is produced for it."));
				continue;
			}

			// One lookup per distinct point per procedure: every airport sharing the procedure
			// shares its transitions, so the same points recur.
			Dictionary<(string, string), DeparturePoint?> resolved = new(KeyComparer.Instance);

			foreach (string airportId in procedure.ServedAirports)
			{
				List<DepartureRawRoute> rawRoutes = RoutesFor(procedure, airportId);

				if (rawRoutes.Count == 0)
				{
					messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
						$"{Label(procedure)} at {airportId}: DP_APT assigns it no body that DP_RTE lists, and the procedure has no transitions, so nothing is produced for it."));
					continue;
				}

				if (!TryLocate(rawRoutes, resolved, allNasrCsvData, out List<DepartureRoute> routes, out DepartureRawPoint? missing))
				{
					skipped++;
					messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
						$"{Label(procedure)} at {airportId}: point '{missing!.Id}' ({missing.PointType}) was not found in FIX_BASE or NAV_BASE, so no files or alias were written for it."));
					continue;
				}

				if (!identities.Add((airportId, procedure.CodeId)))
				{
					// Not expected in real NASR data (checked against a full cycle); caught so a
					// clash overwrites nothing and says why instead.
					skipped++;
					messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
						$"{Label(procedure)} at {airportId}: another procedure at this airport already uses the identifier '{procedure.CodeId}', so this one was skipped."));
					continue;
				}

				located.Add(new DepartureAirportProcedure
				{
					Procedure = procedure,
					AirportId = airportId,
					Routes = routes,
					Points = DistinctPoints(routes),
				});
			}
		}

		List<DepartureAirportProcedure> ordered = located
			.OrderBy(p => p.AirportId, StringComparer.OrdinalIgnoreCase)
			.ThenBy(p => p.Procedure.CodeId, StringComparer.OrdinalIgnoreCase)
			.ToList();

		return new DepartureLocateResult(ordered, skipped, messages);
	}

	/// <summary>A procedure as it reads in a message, e.g. <c>DOTSS (DOTSS2.DOTSS, ZLA)</c>.</summary>
	/// <param name="procedure">The procedure.</param>
	/// <returns>The label.</returns>
	internal static string Label(DepartureProcedure procedure) =>
		$"{procedure.DpName} ({procedure.ComputerCode}, {procedure.Artcc})";

	/// <summary>
	/// The routes one airport gets: the bodies <c>DP_APT</c> assigns it (every body when it has
	/// no <c>DP_APT</c> rows at all), then every transition.
	/// </summary>
	/// <param name="procedure">The procedure.</param>
	/// <param name="airportId">The airport.</param>
	/// <returns>The routes, bodies first, each group in <c>DP_RTE</c> order.</returns>
	internal static List<DepartureRawRoute> RoutesFor(DepartureProcedure procedure, string airportId)
	{
		IEnumerable<DepartureRawRoute> bodies = procedure.BodyNamesByAirport.TryGetValue(airportId, out IReadOnlyList<string>? names)
			? procedure.Bodies.Where(body => names.Contains(body.Name, StringComparer.OrdinalIgnoreCase))
			: procedure.Bodies;

		return bodies.Concat(procedure.Transitions).ToList();
	}

	private static bool TryLocate(
		List<DepartureRawRoute> rawRoutes,
		Dictionary<(string, string), DeparturePoint?> resolved,
		NasrCsvDataCollection allNasrCsvData,
		out List<DepartureRoute> routes,
		out DepartureRawPoint? missing)
	{
		routes = new List<DepartureRoute>(rawRoutes.Count);
		missing = null;

		foreach (DepartureRawRoute raw in rawRoutes)
		{
			List<DeparturePoint> points = new(raw.Points.Count);

			foreach (DepartureRawPoint rawPoint in raw.Points)
			{
				(string, string) key = (rawPoint.Id, rawPoint.PointType);

				if (!resolved.TryGetValue(key, out DeparturePoint? point))
				{
					point = Resolve(rawPoint, allNasrCsvData);
					resolved[key] = point;
				}

				if (point is null)
				{
					missing = rawPoint;
					return false;
				}

				points.Add(point);
			}

			routes.Add(new DepartureRoute(raw.Name, raw.Kind, points));
		}

		return true;
	}

	private static DeparturePoint? Resolve(DepartureRawPoint rawPoint, NasrCsvDataCollection allNasrCsvData)
	{
		FindWaypointCoordinates.WaypointType? type =
			FixPointTypes.Contains(rawPoint.PointType) ? FindWaypointCoordinates.WaypointType.Fix
			: NavaidPointTypes.Contains(rawPoint.PointType) ? FindWaypointCoordinates.WaypointType.Navaid
			: null;

		(double waypointLat, double waypointLon, string foundIn)? found =
			FindWaypointCoordinates.GetCoordinates(allNasrCsvData, rawPoint.Id, type);

		return found is { } hit
			? new DeparturePoint(rawPoint.Id, rawPoint.PointType, hit.waypointLat, hit.waypointLon)
			: null;
	}

	private static IReadOnlyList<DeparturePoint> DistinctPoints(IReadOnlyList<DepartureRoute> routes)
	{
		HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
		List<DeparturePoint> points = new();

		foreach (DeparturePoint point in routes.SelectMany(route => route.Points))
		{
			if (seen.Add(point.Id))
			{
				points.Add(point);
			}
		}

		return points;
	}

	private static IReadOnlyList<string> ServedAirports(DpBase row, List<DpApt> apts)
	{
		List<string> airports = new();
		HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

		IEnumerable<string> fromBase = (row.ServedArpt ?? string.Empty)
			.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		IEnumerable<string> fromApt = apts
			.Select(apt => apt.ArptId?.Trim() ?? string.Empty)
			.Where(id => id.Length > 0);

		foreach (string id in fromBase.Concat(fromApt))
		{
			if (seen.Add(id))
			{
				airports.Add(id.ToUpperInvariant());
			}
		}

		return airports;
	}

	private static IReadOnlyDictionary<string, IReadOnlyList<string>> BodyNamesByAirport(List<DpApt> apts) =>
		apts
			.Where(apt => !string.IsNullOrWhiteSpace(apt.ArptId) && !string.IsNullOrWhiteSpace(apt.BodyName))
			.GroupBy(apt => apt.ArptId.Trim(), StringComparer.OrdinalIgnoreCase)
			.ToDictionary(
				group => group.Key.ToUpperInvariant(),
				group => (IReadOnlyList<string>)group
					.Select(apt => apt.BodyName.Trim())
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToList(),
				StringComparer.OrdinalIgnoreCase);

	private static IReadOnlyList<DepartureRawRoute> ReadRoutes(List<DpRte> rows, string portion, DepartureRouteKind kind)
	{
		// A route is identified by its name and, for a transition, its code; the row order in
		// DP_RTE is kept so bodies and transitions come out in the order NASR lists them.
		List<DepartureRawRoute> routes = new();
		Dictionary<(string, string), List<DpRte>> byRoute = new(KeyComparer.Instance);
		List<(string, string)> order = new();

		foreach (DpRte row in rows)
		{
			if (!string.Equals(row.RoutePortionType?.Trim(), portion, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			(string, string) key = (row.RouteName?.Trim() ?? string.Empty, row.TransitionComputerCode?.Trim() ?? string.Empty);

			if (!byRoute.TryGetValue(key, out List<DpRte>? routeRows))
			{
				routeRows = new List<DpRte>();
				byRoute[key] = routeRows;
				order.Add(key);
			}

			routeRows.Add(row);
		}

		foreach ((string name, string transitionCode) in order)
		{
			List<DepartureRawPoint> points = byRoute[(name, transitionCode)]
				.Where(row => !string.IsNullOrWhiteSpace(row.Point))
				.OrderBy(row => row.PointSeq)
				.Select(row => new DepartureRawPoint(row.Point.Trim().ToUpperInvariant(), row.PointType?.Trim() ?? string.Empty))
				.ToList();

			if (points.Count == 0)
			{
				continue;
			}

			routes.Add(new DepartureRawRoute(name, kind, transitionCode.Length > 0 ? transitionCode : null, points));
		}

		return routes;
	}

	private static DateOnly? ParseNasrDate(string? value) =>
		DateOnly.TryParseExact(value?.Trim(), "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
			? date
			: null;

	private static (string, string) ProcedureKey(string? dpName, string? artcc) =>
		(dpName?.Trim() ?? string.Empty, artcc?.Trim() ?? string.Empty);

	/// <summary>Case-insensitive comparer for the two-part keys used to join the DP tables.</summary>
	private sealed class KeyComparer : IEqualityComparer<(string, string)>
	{
		public static readonly KeyComparer Instance = new();

		public bool Equals((string, string) x, (string, string) y) =>
			StringComparer.OrdinalIgnoreCase.Equals(x.Item1, y.Item1)
			&& StringComparer.OrdinalIgnoreCase.Equals(x.Item2, y.Item2);

		public int GetHashCode((string, string) obj) => HashCode.Combine(
			StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1),
			StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2));
	}
}
