using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// Which ARTCCs a procedure's route point is in, from one cycle's NASR (Feature #367): a fix's
/// <c>FIX_BASE</c> <c>ARTCC_ID_HIGH</c> and <c>ARTCC_ID_LOW</c>, a NAVAID's <c>NAV_BASE</c>
/// <c>HIGH_ALT_ARTCC_ID</c> and <c>LOW_ALT_ARTCC_ID</c>. Its lookups are built once, and each point
/// is looked up once.
/// </summary>
/// <remarks>
/// <para>
/// A point is matched by its identifier and type: a fix type (<c>WP</c>, <c>RP</c>...) to
/// <c>FIX_BASE</c>, keeping only rows in the same ICAO region when both have one; any other type to
/// <c>NAV_BASE</c> rows of that NAVAID type. Not the first row with the identifier: two NAVAIDs
/// can share one (an NDB and a VOR, say), as can fixes in different regions.
/// </para>
/// <para>
/// A point still matched by more than one row is in a facility when any of them is, and is logged
/// once. A point in neither file is in no facility - unless the caller allows airports (the area
/// is "None"), when an airport with that FAA or ICAO identifier is in its responsible ARTCC.
/// </para>
/// </remarks>
public sealed class RoutePointArtccResolver
{
	private const string LogSource = "ProcedureRoutes";

	/// <summary>The route point types that are fixes: <c>FIX_BASE</c>'s use codes. Every other type is a NAVAID's.</summary>
	private static readonly HashSet<string> FixTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		"CN", "MR", "MW", "NRS", "RADAR", "RP", "VFR", "WP",
	};

	private readonly ILookup<string, (string? Region, string[] Artccs)> _fixes;
	private readonly ILookup<string, (string Type, string[] Artccs)> _navaids;
	private readonly Dictionary<string, string> _airportArtccs = new(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<ProcedureRoutePoint, string[]?> _found = [];
	private readonly List<ProcedureRoutePoint> _ambiguous = [];

	/// <summary>Builds the lookups from one cycle's NASR data.</summary>
	/// <param name="nasr">Parsed NASR data with its <c>FIX</c> and <c>NAV</c> groups; <c>APT</c>, if parsed, for the airport fallback.</param>
	/// <exception cref="InvalidOperationException">The <c>FIX</c> or <c>NAV</c> group was not parsed.</exception>
	public RoutePointArtccResolver(NasrCsvDataCollection nasr)
	{
		ArgumentNullException.ThrowIfNull(nasr);

		_fixes = nasr.RequireFix().FixBase.ToLookup(
			row => Trim(row.FixId),
			row => (Blank(row.IcaoRegionCode), Artccs(row.ArtccIdHigh, row.ArtccIdLow)),
			StringComparer.OrdinalIgnoreCase);

		_navaids = nasr.RequireNav().NavBase.ToLookup(
			row => Trim(row.NavId),
			row => (Trim(row.NavType), Artccs(row.HighAltArtccId, row.LowAltArtccId)),
			StringComparer.OrdinalIgnoreCase);

		foreach (AptCsvDataModel.AptBase airport in nasr.Apt?.AptBase ?? [])
		{
			if (Blank(airport.RespArtccId) is not { } artcc)
			{
				continue;
			}

			if (Blank(airport.ArptId) is { } faaId)
			{
				_airportArtccs.TryAdd(faaId, artcc);
			}

			if (Blank(airport.IcaoId) is { } icaoId)
			{
				_airportArtccs.TryAdd(icaoId, artcc);
			}
		}
	}

	/// <summary>The points looked up so far that more than one row still matched.</summary>
	public IReadOnlyList<ProcedureRoutePoint> AmbiguousPoints => _ambiguous;

	/// <summary>Whether <paramref name="point"/> is in <paramref name="facility"/>.</summary>
	/// <param name="point">The route point.</param>
	/// <param name="facility">The ARTCC, e.g. <c>ZDC</c>.</param>
	/// <param name="useAirports">Whether a point in neither file counts by the airport it names, if it names one.</param>
	/// <returns><see langword="true"/> when any row it matches names the facility.</returns>
	public bool IsInFacility(ProcedureRoutePoint point, string facility, bool useAirports)
	{
		ArgumentNullException.ThrowIfNull(point);
		ArgumentException.ThrowIfNullOrWhiteSpace(facility);

		if (Find(point) is { } artccs)
		{
			return artccs.Contains(facility, StringComparer.OrdinalIgnoreCase);
		}

		return useAirports
			&& _airportArtccs.TryGetValue(point.Id, out string? airportArtcc)
			&& airportArtcc.Equals(facility, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>Every ARTCC the rows a point matches name, or <see langword="null"/> when it matches none.</summary>
	private string[]? Find(ProcedureRoutePoint point)
	{
		if (_found.TryGetValue(point, out string[]? cached))
		{
			return cached;
		}

		string[][] rows = FixTypes.Contains(point.Type)
			? [.. _fixes[point.Id]
				.Where(row => point.Region is null || row.Region is null || row.Region.Equals(point.Region, StringComparison.OrdinalIgnoreCase))
				.Select(row => row.Artccs)]
			: [.. _navaids[point.Id]
				.Where(row => row.Type.Equals(point.Type, StringComparison.OrdinalIgnoreCase))
				.Select(row => row.Artccs)];

		string[] artccs = [.. rows.SelectMany(names => names).Distinct(StringComparer.OrdinalIgnoreCase)];

		if (rows.Length > 1)
		{
			_ambiguous.Add(point);
			AppLog.Info(LogSource,
				$"Route point {point.Id} ({point.Type}{(point.Region is null ? string.Empty : $", region {point.Region}")}) matches {rows.Length} rows; " +
				$"it counts as in any facility one of them is in ({(artccs.Length == 0 ? "none" : string.Join(", ", artccs))}).");
		}

		string[]? found = rows.Length == 0 ? null : artccs;
		_found[point] = found;
		return found;
	}

	private static string[] Artccs(string? high, string? low) => [.. new[] { Blank(high), Blank(low) }.OfType<string>()];

	private static string Trim(string? value) => value?.Trim() ?? string.Empty;

	private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
