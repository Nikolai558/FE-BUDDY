using System.Globalization;

using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Arrivals;
using FeBuddy.Core.Domain.Arrivals.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using static FeBuddy.Core.Infrastructure.Nasr.Models.StarCsvDataModel;

namespace FeBuddy.Core.Application.Airac.Arrivals;

/// <summary>
/// Reads every standard terminal arrival out of <c>STAR_BASE</c>, <c>STAR_APT</c> and
/// <c>STAR_RTE</c>, then splits each one per airport and locates its points in FIX_BASE / NAV_BASE.
/// </summary>
/// <remarks>
/// The two steps are separate so the service can apply the cheap procedure-level filters in
/// between: locating is the expensive part, and a procedure the user filtered out should never
/// produce a "point not found" warning.
/// </remarks>
public static class ArrivalBuilder
{
	private const string LogSource = "ArrivalBuilder";
	private const string BodyPortion = "BODY";
	private const string TransitionPortion = "TRANSITION";

	/// <summary>The <c>POINT_TYPE</c> values that are fixes (FIX_BASE). Every other type is a navaid.</summary>
	private static readonly HashSet<string> FixPointTypes = new(StringComparer.OrdinalIgnoreCase) { "WP", "RP", "CN" };

	/// <summary>
	/// The <c>POINT_TYPE</c> values that are navaids (NAV_BASE) - every navaid type STAR_RTE uses.
	/// A type in neither set is looked up without a type, which lets the lookup decide.
	/// </summary>
	private static readonly HashSet<string> NavaidPointTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"VORTAC", "VOR/DME", "VOR", "TACAN", "DME", "NDB", "NDB/DME",
	};

	/// <summary>
	/// Reads every procedure from the STAR tables, without locating anything.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data. <c>Star</c> must not be null.</param>
	/// <returns>Every procedure, ordered by ARTCC then name, plus any messages.</returns>
	/// <exception cref="InvalidOperationException">Thrown when the STAR data has not been parsed.</exception>
	public static ArrivalProcedureReadResult ReadProcedures(NasrCsvDataCollection allNasrCsvData)
	{
		ArgumentNullException.ThrowIfNull(allNasrCsvData);

		if (allNasrCsvData.Star is null)
		{
			throw new InvalidOperationException(
				"Standard terminal arrival data (STAR) has not been parsed. The Arrivals sub-service cannot run without it.");
		}

		List<ServiceMessage> messages = [];

		// Unlike DP, STAR_APT and STAR_RTE carry no arrival name at all, so the three tables are
		// joined on STAR_COMPUTER_CODE + ARTCC instead.
		ILookup<(string, string), StarApt> aptRows = allNasrCsvData.Star.StarApt.ToLookup(
			row => ProcedureKey(row.StarComputerCode, row.Artcc), KeyComparer.Instance);

		ILookup<(string, string), StarRte> routeRows = allNasrCsvData.Star.StarRte.ToLookup(
			row => ProcedureKey(row.StarComputerCode, row.Artcc), KeyComparer.Instance);

		IReadOnlyDictionary<string, string> respArtccByAirport = RespArtccByAirport(allNasrCsvData);

		// A code + ARTCC pair identifies which STAR_APT and STAR_RTE rows belong to a procedure;
		// two STAR_BASE rows sharing one make that impossible to tell apart. Not seen in real NASR
		// data (checked against a full cycle), but every row sharing a duplicated key is skipped,
		// with one warning for the key rather than one per row.
		Dictionary<(string, string), int> keyCounts = new(KeyComparer.Instance);

		foreach (StarBase countedRow in allNasrCsvData.Star.StarBase)
		{
			string code = countedRow.StarComputerCode?.Trim() ?? string.Empty;

			if (code.Length == 0)
			{
				continue;
			}

			(string, string) countedKey = ProcedureKey(code, countedRow.Artcc);
			keyCounts[countedKey] = keyCounts.TryGetValue(countedKey, out int count) ? count + 1 : 1;
		}

		HashSet<(string, string)> duplicateKeysWarned = new(KeyComparer.Instance);
		List<ArrivalProcedure> procedures = [];

		foreach (StarBase row in allNasrCsvData.Star.StarBase)
		{
			string arrivalName = row.ArrivalName?.Trim() ?? string.Empty;
			string artccText = row.Artcc?.Trim() ?? string.Empty;
			string computerCode = row.StarComputerCode?.Trim() ?? string.Empty;

			if (computerCode.Length == 0)
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"A STAR_BASE row for ARTCC '{artccText}' ('{arrivalName}') has no STAR_COMPUTER_CODE, so its STAR_APT and STAR_RTE rows cannot be found, and it was skipped."));
				continue;
			}

			(string, string) key = ProcedureKey(computerCode, artccText);

			if (keyCounts[key] > 1)
			{
				if (duplicateKeysWarned.Add(key))
				{
					messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
						$"More than one STAR_BASE row uses STAR_COMPUTER_CODE '{computerCode}' for ARTCC '{artccText}', so their STAR_APT and STAR_RTE rows cannot be told apart, and they were skipped."));
				}

				continue;
			}

			string codeId = ArrivalNaming.CodeIdFor(computerCode, row.AmendmentNo, arrivalName, out _);

			if (codeId.Length == 0)
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"A STAR_BASE row for ARTCC '{artccText}' has no usable STAR_COMPUTER_CODE and no usable ARRIVAL_NAME, so no identifier could be derived for it, and it was skipped."));
				continue;
			}

			List<StarApt> apts = [.. aptRows[key]];
			List<StarRte> routes = [.. routeRows[key]];

			IReadOnlyList<string> artccs = SplitArtccs(artccText);
			IReadOnlyList<string> servedAirports = ServedAirports(row, apts);

			procedures.Add(new ArrivalProcedure
			{
				ArrivalName = arrivalName,
				ArtccText = artccText,
				Artccs = artccs,
				ArtccByAirport = ArtccByAirport(artccs, servedAirports, respArtccByAirport),
				ComputerCode = computerCode,
				CodeId = codeId,
				AmendmentNo = row.AmendmentNo?.Trim() ?? string.Empty,
				AmendmentEffectiveDateText = row.StarAmendEffDate?.Trim() ?? string.Empty,
				AmendmentEffectiveDate = ParseNasrDate(row.StarAmendEffDate),
				CycleEffectiveDate = ParseNasrDate(row.EffDate),
				ServedAirports = servedAirports,
				Bodies = ReadRoutes(routes, BodyPortion, ArrivalRouteKind.Body),
				Transitions = ReadRoutes(routes, TransitionPortion, ArrivalRouteKind.Transition),
				BodiesByAirport = BodiesByAirport(apts),
			});
		}

		List<ArrivalProcedure> ordered = [.. procedures
			.OrderBy(p => p.ArtccText, StringComparer.OrdinalIgnoreCase)
			.ThenBy(p => p.ArrivalName, StringComparer.OrdinalIgnoreCase)];

		return new ArrivalProcedureReadResult(ordered, messages);
	}

	/// <summary>
	/// Splits each procedure per airport and locates every point.
	/// </summary>
	/// <param name="procedures">The procedures in scope (already filtered by ARTCC and amendment date).</param>
	/// <param name="allNasrCsvData">All parsed NASR CSV data; FIX_BASE and NAV_BASE are read.</param>
	/// <returns>
	/// Every airport + procedure whose points were all found. One missing point leaves that
	/// airport + procedure out entirely, with a warning naming the point.
	/// </returns>
	public static ArrivalLocateResult Locate(IReadOnlyList<ArrivalProcedure> procedures, NasrCsvDataCollection allNasrCsvData)
	{
		ArgumentNullException.ThrowIfNull(procedures);
		ArgumentNullException.ThrowIfNull(allNasrCsvData);

		List<ServiceMessage> messages = [];
		List<ArrivalAirportProcedure> located = [];
		HashSet<(string, string)> identities = new(KeyComparer.Instance);
		int skipped = 0;

		foreach (ArrivalProcedure procedure in procedures)
		{
			if (!procedure.HasRoutes)
			{
				messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
					$"{LabelWithAirports(procedure)} has no STAR_RTE rows, so nothing is produced for it."));
				continue;
			}

			// One lookup per distinct point per procedure: every airport sharing the procedure
			// shares its transitions, so the same points recur.
			Dictionary<(string, string), ArrivalPoint?> resolved = new(KeyComparer.Instance);

			foreach (string airportId in procedure.ServedAirports)
			{
				List<ArrivalRawRoute> rawRoutes = RoutesFor(procedure, airportId);

				if (rawRoutes.Count == 0)
				{
					messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
						$"{Label(procedure)} at {airportId}: STAR_APT assigns it no body that STAR_RTE lists, and the procedure has no transitions, so nothing is produced for it."));
					continue;
				}

				if (!TryLocate(rawRoutes, resolved, allNasrCsvData, out List<ArrivalRoute> routes, out ArrivalRawPoint? missing))
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

				located.Add(new ArrivalAirportProcedure
				{
					Procedure = procedure,
					AirportId = airportId,
					Artcc = procedure.ArtccFor(airportId),
					Routes = routes,
					Points = DistinctPoints(routes),
				});
			}
		}

		List<ArrivalAirportProcedure> ordered = [.. located
			.OrderBy(p => p.AirportId, StringComparer.OrdinalIgnoreCase)
			.ThenBy(p => p.Procedure.CodeId, StringComparer.OrdinalIgnoreCase)];

		return new ArrivalLocateResult(ordered, skipped, messages);
	}

	/// <summary>A procedure as it reads in a message, e.g. <c>BLAID (AALAN.BLAID2, ZLA)</c>.</summary>
	/// <param name="procedure">The procedure.</param>
	/// <returns>The label.</returns>
	internal static string Label(ArrivalProcedure procedure) =>
		$"{procedure.ArrivalName} ({procedure.ComputerCode}, {procedure.ArtccText})";

	/// <summary>
	/// A procedure as it reads in a message that is about the whole procedure rather than one
	/// airport's copy of it, e.g. <c>BLAID (AALAN.BLAID2, ZLA-LAS)</c>. Several served airports are
	/// joined with <c>/</c>; with none, it is the same as <see cref="Label"/>.
	/// </summary>
	/// <param name="procedure">The procedure.</param>
	/// <returns>The label.</returns>
	internal static string LabelWithAirports(ArrivalProcedure procedure) =>
		procedure.ServedAirports.Count == 0
			? Label(procedure)
			: $"{procedure.ArrivalName} ({procedure.ComputerCode}, {procedure.ArtccText}-{string.Join('/', procedure.ServedAirports)})";

	/// <summary>
	/// The routes one airport gets: every transition, then the bodies <c>STAR_APT</c> assigns it
	/// (every body when it has no <c>STAR_APT</c> rows at all) - the order the procedure is flown.
	/// </summary>
	/// <param name="procedure">The procedure.</param>
	/// <param name="airportId">The airport.</param>
	/// <returns>The routes, transitions first, each group in <c>STAR_RTE</c> order.</returns>
	internal static List<ArrivalRawRoute> RoutesFor(ArrivalProcedure procedure, string airportId)
	{
		IEnumerable<ArrivalRawRoute> bodies = procedure.BodiesByAirport.TryGetValue(airportId, out IReadOnlyList<(string Name, int Sequence)>? assigned)
			? procedure.Bodies.Where(body => assigned.Any(a =>
				string.Equals(a.Name, body.Name, StringComparison.OrdinalIgnoreCase) && a.Sequence == body.Sequence))
			: procedure.Bodies;

		return [.. procedure.Transitions, .. bodies];
	}

	private static bool TryLocate(
		List<ArrivalRawRoute> rawRoutes,
		Dictionary<(string, string), ArrivalPoint?> resolved,
		NasrCsvDataCollection allNasrCsvData,
		out List<ArrivalRoute> routes,
		out ArrivalRawPoint? missing)
	{
		routes = new List<ArrivalRoute>(rawRoutes.Count);
		missing = null;

		foreach (ArrivalRawRoute raw in rawRoutes)
		{
			List<ArrivalPoint> points = new(raw.Points.Count);

			foreach (ArrivalRawPoint rawPoint in raw.Points)
			{
				(string, string) key = (rawPoint.Id, rawPoint.PointType);

				if (!resolved.TryGetValue(key, out ArrivalPoint? point))
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

			routes.Add(new ArrivalRoute(raw.Name, raw.Kind, points));
		}

		return true;
	}

	private static ArrivalPoint? Resolve(ArrivalRawPoint rawPoint, NasrCsvDataCollection allNasrCsvData)
	{
		WaypointLocator.WaypointType? type =
			FixPointTypes.Contains(rawPoint.PointType) ? WaypointLocator.WaypointType.Fix
			: NavaidPointTypes.Contains(rawPoint.PointType) ? WaypointLocator.WaypointType.Navaid
			: null;

		(double waypointLat, double waypointLon, string foundIn)? found =
			WaypointLocator.Find(allNasrCsvData, rawPoint.Id, type);

		return found is { } hit
			? new ArrivalPoint(rawPoint.Id, rawPoint.PointType, hit.waypointLat, hit.waypointLon)
			: null;
	}

	private static IReadOnlyList<ArrivalPoint> DistinctPoints(IReadOnlyList<ArrivalRoute> routes)
	{
		HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
		List<ArrivalPoint> points = [];

		foreach (ArrivalPoint point in routes.SelectMany(route => route.Points))
		{
			if (seen.Add(point.Id))
			{
				points.Add(point);
			}
		}

		return points;
	}

	private static IReadOnlyList<string> ServedAirports(StarBase row, List<StarApt> apts)
	{
		List<string> airports = [];
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

	/// <summary>Splits the raw <c>ARTCC</c> text on whitespace, trims, upper-cases and de-duplicates it, keeping publication order.</summary>
	private static IReadOnlyList<string> SplitArtccs(string artccText)
	{
		List<string> artccs = [];
		HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

		foreach (string raw in artccText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			string artcc = raw.ToUpperInvariant();

			if (seen.Add(artcc))
			{
				artccs.Add(artcc);
			}
		}

		return artccs;
	}

	/// <summary>
	/// Builds <see cref="ArrivalProcedure.ArtccByAirport"/>: empty for a single-ARTCC procedure,
	/// otherwise each served airport's own responsible ARTCC when it is one of <paramref name="artccs"/>.
	/// </summary>
	private static IReadOnlyDictionary<string, string> ArtccByAirport(
		IReadOnlyList<string> artccs,
		IReadOnlyList<string> servedAirports,
		IReadOnlyDictionary<string, string> respArtccByAirport)
	{
		if (artccs.Count <= 1)
		{
			return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}

		HashSet<string> artccSet = new(artccs, StringComparer.OrdinalIgnoreCase);
		Dictionary<string, string> byAirport = new(StringComparer.OrdinalIgnoreCase);

		foreach (string airportId in servedAirports)
		{
			if (respArtccByAirport.TryGetValue(airportId, out string? resp) && artccSet.Contains(resp))
			{
				byAirport[airportId] = resp;
			}
		}

		return byAirport;
	}

	/// <summary>Every airport's responsible ARTCC (<c>APT_BASE.RESP_ARTCC_ID</c>), first row wins.</summary>
	private static IReadOnlyDictionary<string, string> RespArtccByAirport(NasrCsvDataCollection allNasrCsvData)
	{
		Dictionary<string, string> lookup = new(StringComparer.OrdinalIgnoreCase);

		if (allNasrCsvData.Apt is null)
		{
			return lookup;
		}

		foreach (AptCsvDataModel.AptBase row in allNasrCsvData.Apt.AptBase)
		{
			string airportId = row.ArptId?.Trim() ?? string.Empty;

			if (airportId.Length == 0 || lookup.ContainsKey(airportId))
			{
				continue;
			}

			lookup[airportId] = row.RespArtccId?.Trim().ToUpperInvariant() ?? string.Empty;
		}

		return lookup;
	}

	private static IReadOnlyDictionary<string, IReadOnlyList<(string Name, int Sequence)>> BodiesByAirport(List<StarApt> apts) =>
		apts
			.Where(apt => !string.IsNullOrWhiteSpace(apt.ArptId) && !string.IsNullOrWhiteSpace(apt.BodyName))
			.GroupBy(apt => apt.ArptId.Trim(), StringComparer.OrdinalIgnoreCase)
			.ToDictionary(
				group => group.Key.ToUpperInvariant(),
				group => (IReadOnlyList<(string Name, int Sequence)>)[.. group
					.Select(apt => (Name: apt.BodyName.Trim(), Sequence: apt.AptBodySeq))
					.Distinct(BodyKeyComparer.Instance)],
				StringComparer.OrdinalIgnoreCase);

	private static IReadOnlyList<ArrivalRawRoute> ReadRoutes(List<StarRte> rows, string portion, ArrivalRouteKind kind)
	{
		// A route is identified by its name, its BODY_SEQ, and (for a transition) its code; the
		// row order in STAR_RTE is kept so bodies and transitions come out in the order NASR lists
		// them. Two real STARs reuse a body name with a different BODY_SEQ for two different
		// bodies, so BODY_SEQ is always part of the key, not just a tiebreaker.
		List<ArrivalRawRoute> routes = [];
		Dictionary<(string, string, int), List<StarRte>> byRoute = new(RouteKeyComparer.Instance);
		List<(string, string, int)> order = [];

		foreach (StarRte row in rows)
		{
			if (!string.Equals(row.RoutePortionType?.Trim(), portion, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			(string, string, int) key = (
				row.RouteName?.Trim() ?? string.Empty,
				row.TransitionComputerCode?.Trim() ?? string.Empty,
				row.RteBodySeq);

			if (!byRoute.TryGetValue(key, out List<StarRte>? routeRows))
			{
				routeRows = [];
				byRoute[key] = routeRows;
				order.Add(key);
			}

			routeRows.Add(row);
		}

		foreach ((string name, string transitionCode, int sequence) in order)
		{
			List<ArrivalRawPoint> points = [.. byRoute[(name, transitionCode, sequence)]
				.Where(row => !string.IsNullOrWhiteSpace(row.Point))
				.OrderBy(row => row.PointSeq)
				.Select(row => new ArrivalRawPoint(row.Point.Trim().ToUpperInvariant(), row.PointType?.Trim() ?? string.Empty))];

			if (points.Count == 0)
			{
				continue;
			}

			routes.Add(new ArrivalRawRoute(name, sequence, kind, transitionCode.Length > 0 ? transitionCode : null, points));
		}

		return routes;
	}

	private static DateOnly? ParseNasrDate(string? value) =>
		DateOnly.TryParseExact(value?.Trim(), "yyyy/MM/dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
			? date
			: null;

	private static (string, string) ProcedureKey(string? computerCode, string? artcc) =>
		(computerCode?.Trim() ?? string.Empty, artcc?.Trim() ?? string.Empty);

	/// <summary>Case-insensitive comparer for the two-part keys used to join the STAR tables.</summary>
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

	/// <summary>Case-insensitive comparer for the (name, transition code, sequence) keys used to group STAR_RTE rows into routes.</summary>
	private sealed class RouteKeyComparer : IEqualityComparer<(string, string, int)>
	{
		public static readonly RouteKeyComparer Instance = new();

		public bool Equals((string, string, int) x, (string, string, int) y) =>
			StringComparer.OrdinalIgnoreCase.Equals(x.Item1, y.Item1)
			&& StringComparer.OrdinalIgnoreCase.Equals(x.Item2, y.Item2)
			&& x.Item3 == y.Item3;

		public int GetHashCode((string, string, int) obj) => HashCode.Combine(
			StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1),
			StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2),
			obj.Item3);
	}

	/// <summary>Case-insensitive-on-name comparer for a (body name, sequence) pair.</summary>
	private sealed class BodyKeyComparer : IEqualityComparer<(string Name, int Sequence)>
	{
		public static readonly BodyKeyComparer Instance = new();

		public bool Equals((string Name, int Sequence) x, (string Name, int Sequence) y) =>
			StringComparer.OrdinalIgnoreCase.Equals(x.Name, y.Name) && x.Sequence == y.Sequence;

		public int GetHashCode((string Name, int Sequence) obj) =>
			HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name), obj.Sequence);
	}
}
