using System.Runtime.CompilerServices;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Infrastructure.Nasr;

/// <summary>
/// Finds a waypoint's coordinates by identifier in parsed NASR data: fixes, navaids and airports.
/// </summary>
/// <remarks>
/// Each lookup table is built the first time it is needed and cached per parsed dataset, so
/// resolving every point on every airway is a dictionary lookup, not a scan of the whole file.
/// When an identifier appears more than once, the first row in the file wins.
/// </remarks>
internal static class WaypointLocator
{
	/// <summary>Which NASR table to search.</summary>
	internal enum WaypointType
	{
		/// <summary><c>FIX_BASE</c>, by <c>FIX_ID</c>.</summary>
		Fix,

		/// <summary><c>NAV_BASE</c>, by <c>NAV_ID</c>.</summary>
		Navaid,

		/// <summary><c>APT_BASE</c>, by <c>ICAO_ID</c> or <c>ARPT_ID</c>.</summary>
		Airport
	}

	/// <summary>
	/// A single resolved waypoint lookup entry: latitude, longitude, and which NASR data
	/// source the waypoint was found in.
	/// </summary>
	private readonly record struct CoordinateEntry(double Lat, double Lon, string FoundIn);

	/// <summary>
	/// Holds the lazily-built lookup dictionaries for one <see cref="NasrCsvDataCollection"/>
	/// instance, so repeated lookups against the same parsed dataset do not repeatedly scan
	/// the underlying lists.
	/// </summary>
	private sealed class CoordinateIndexSet
	{
		public Dictionary<string, CoordinateEntry>? FixIndex;
		public Dictionary<string, CoordinateEntry>? NavaidIndex;
		public Dictionary<string, CoordinateEntry>? AirportIndex;
	}

	/// <summary>
	/// One <see cref="CoordinateIndexSet"/> per parsed dataset. A weak table, so the cache never
	/// keeps a (large) dataset alive after its owner lets go of it.
	/// </summary>
	private static readonly ConditionalWeakTable<NasrCsvDataCollection, CoordinateIndexSet> IndexCache = [];

	/// <summary>
	/// Finds a waypoint's latitude and longitude.
	/// </summary>
	/// <param name="allNasrCsvData">The parsed NASR data to search.</param>
	/// <param name="waypointId">The identifier, matched ignoring case and surrounding spaces.</param>
	/// <param name="waypointType">
	/// The table to search, when known. When <see langword="null"/>: a 5-character identifier is
	/// a fix; anything else is tried as a navaid first, then as an airport.
	/// </param>
	/// <returns>
	/// The coordinates and the table they came from (<c>fix</c>, <c>navaid</c> or
	/// <c>airport</c>), or <see langword="null"/> when not found.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="allNasrCsvData"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="waypointId"/> is blank.</exception>
	internal static (double waypointLat, double waypointLon, string foundIn)? Find(
		NasrCsvDataCollection allNasrCsvData,
		string waypointId,
		WaypointType? waypointType = null)
	{
		if (allNasrCsvData is null)
			throw new ArgumentNullException(nameof(allNasrCsvData));

		if (string.IsNullOrWhiteSpace(waypointId))
			throw new ArgumentException(
				"Waypoint ID cannot be null, empty, or whitespace.",
				nameof(waypointId));

		waypointId = waypointId.Trim();

		CoordinateIndexSet indexSet =
			IndexCache.GetValue(
				allNasrCsvData,
				_ => new CoordinateIndexSet());

		// If the waypoint type is known, search only that data source.
		if (waypointType.HasValue)
		{
			return waypointType.Value switch
			{
				WaypointType.Fix =>
					FindFix(allNasrCsvData, indexSet, waypointId),

				WaypointType.Navaid =>
					FindNavaid(allNasrCsvData, indexSet, waypointId),

				WaypointType.Airport =>
					FindAirport(allNasrCsvData, indexSet, waypointId),

				_ => null
			};
		}

		// Waypoint type is unknown...

		// If the ID contains exactly 5 characters, search FIX data only.
		if (waypointId.Length == 5)
		{
			return FindFix(allNasrCsvData, indexSet, waypointId);
		}

		// Otherwise, NAVAID has priority over airport.
		var navaidCoordinates = FindNavaid(allNasrCsvData, indexSet, waypointId);

		if (navaidCoordinates.HasValue)
			return navaidCoordinates;

		return FindAirport(allNasrCsvData, indexSet, waypointId);
	}

	/// <summary>
	/// Searches the cached FIX_BASE index for a matching 5-character fix.
	/// </summary>
	private static (double waypointLat, double waypointLon, string foundIn)? FindFix(
		NasrCsvDataCollection allNasrCsvData,
		CoordinateIndexSet indexSet,
		string waypointId)
	{
		Dictionary<string, CoordinateEntry> index =
			indexSet.FixIndex ??= BuildFixIndex(allNasrCsvData);

		if (!index.TryGetValue(waypointId, out CoordinateEntry entry))
			return null;

		return (entry.Lat, entry.Lon, entry.FoundIn);
	}

	/// <summary>
	/// Searches the cached NAV_BASE index for a matching NAVAID.
	/// </summary>
	private static (double waypointLat, double waypointLon, string foundIn)? FindNavaid(
		NasrCsvDataCollection allNasrCsvData,
		CoordinateIndexSet indexSet,
		string waypointId)
	{
		Dictionary<string, CoordinateEntry> index =
			indexSet.NavaidIndex ??= BuildNavaidIndex(allNasrCsvData);

		if (!index.TryGetValue(waypointId, out CoordinateEntry entry))
			return null;

		return (entry.Lat, entry.Lon, entry.FoundIn);
	}

	/// <summary>
	/// Searches the cached APT_BASE index for a matching airport using either ICAO ID or airport ID.
	/// </summary>
	private static (double waypointLat, double waypointLon, string foundIn)? FindAirport(
		NasrCsvDataCollection allNasrCsvData,
		CoordinateIndexSet indexSet,
		string waypointId)
	{
		Dictionary<string, CoordinateEntry> index =
			indexSet.AirportIndex ??= BuildAirportIndex(allNasrCsvData);

		if (!index.TryGetValue(waypointId, out CoordinateEntry entry))
			return null;

		return (entry.Lat, entry.Lon, entry.FoundIn);
	}

	/// <summary>
	/// Builds a case-insensitive FixId -&gt; coordinate index from FIX_BASE. When duplicate
	/// FixId values exist, the first occurrence wins.
	/// </summary>
	private static Dictionary<string, CoordinateEntry> BuildFixIndex(NasrCsvDataCollection allNasrCsvData)
	{
		Dictionary<string, CoordinateEntry> index = new(StringComparer.OrdinalIgnoreCase);

		if (allNasrCsvData.Fix?.FixBase is null)
			return index;

		foreach (var fix in allNasrCsvData.Fix.FixBase)
		{
			if (string.IsNullOrWhiteSpace(fix.FixId))
				continue;

			// First occurrence wins.
			index.TryAdd(
				fix.FixId,
				new CoordinateEntry(fix.LatDecimal, fix.LongDecimal, "fix"));
		}

		return index;
	}

	/// <summary>
	/// Builds a case-insensitive NavId -&gt; coordinate index from NAV_BASE. When duplicate
	/// NavId values exist, the first occurrence wins.
	/// </summary>
	private static Dictionary<string, CoordinateEntry> BuildNavaidIndex(NasrCsvDataCollection allNasrCsvData)
	{
		Dictionary<string, CoordinateEntry> index = new(StringComparer.OrdinalIgnoreCase);

		if (allNasrCsvData.Nav?.NavBase is null)
			return index;

		foreach (var navaid in allNasrCsvData.Nav.NavBase)
		{
			if (string.IsNullOrWhiteSpace(navaid.NavId))
				continue;

			// First occurrence wins.
			index.TryAdd(
				navaid.NavId,
				new CoordinateEntry(navaid.LatDecimal, navaid.LongDecimal, "navaid"));
		}

		return index;
	}

	/// <summary>
	/// Builds a case-insensitive index from APT_BASE keyed by both IcaoId and ArptId, since
	/// either identifier may be used to reference an airport as an airway waypoint. When
	/// an identifier appears more than once, the first occurrence wins.
	/// </summary>
	private static Dictionary<string, CoordinateEntry> BuildAirportIndex(NasrCsvDataCollection allNasrCsvData)
	{
		Dictionary<string, CoordinateEntry> index = new(StringComparer.OrdinalIgnoreCase);

		if (allNasrCsvData.Apt?.AptBase is null)
			return index;

		foreach (var airport in allNasrCsvData.Apt.AptBase)
		{
			CoordinateEntry entry = new(
				airport.BaseLatDecimal,
				airport.BaseLongDecimal,
				"airport");

			if (!string.IsNullOrWhiteSpace(airport.IcaoId))
			{
				// First occurrence wins.
				index.TryAdd(airport.IcaoId, entry);
			}

			if (!string.IsNullOrWhiteSpace(airport.ArptId))
			{
				// First occurrence wins.
				index.TryAdd(airport.ArptId, entry);
			}
		}

		return index;
	}
}
