using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FeBuddy.Core.Domain.Airports.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Infrastructure.Nasr;

/// <summary>
/// Provides methods for locating waypoint coordinates from parsed NASR CSV data.
/// </summary>
internal static class WaypointLocator
{
	/// <summary>
	/// Defines the type of waypoint to search for.
	/// </summary>
	internal enum WaypointType
	{
		Fix,
		Navaid,
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
	/// Caches one <see cref="CoordinateIndexSet"/> per <see cref="NasrCsvDataCollection"/>
	/// instance. A <see cref="ConditionalWeakTable{TKey, TValue}"/> is used so the cache does
	/// not keep a parsed NASR dataset (which can be large) alive any longer than the caller
	/// already keeps it alive.
	/// </summary>
	private static readonly ConditionalWeakTable<NasrCsvDataCollection, CoordinateIndexSet> _indexCache = new();


	/*
	    Waypoint type supplied?
		│
		├── Fix     → FixBase.FixId
		├── NAVAID  → NavBase.NavId
		└── Airport → AptBase.IcaoId OR AptBase.ArptId

		Waypoint type NOT supplied?
		│
		├── ID length == 5
		│      └── FixBase only
		│
		└── ID length != 5
		       ├── NavBase first
		       └── AptBase if NAVAID wasn't found
	*/

	/// <summary>
	/// Finds the latitude and longitude for a waypoint from parsed NASR CSV data.
	/// Optionally pass in arg waypoint type ("Fix", "Navaid", or "Airport"). If waypoint type not provided,
	/// a 5-character identifier is searched in FIX data.
	/// All other identifiers are searched in NAVAID data first, followed by airport data.
	/// If the waypoint type is known, it will search only that data source.
	/// </summary>
	/// <returns>
	/// A tuple containing waypointLat and waypointLon when found; otherwise null. Will also return a string
	/// indicating which data source the waypoint was found in ("fix", "navaid", or "airport").
	/// </returns>
	/// <remarks>
	/// This method builds (and caches, per <paramref name="allNasrCsvData"/> instance) a
	/// case-insensitive dictionary index over each of FixBase, NavBase, and AptBase the first
	/// time any of that source's data is needed, so that resolving every waypoint on every
	/// airway remains an O(1) lookup per waypoint instead of an O(n) linear scan. Public
	/// behavior is unchanged from the original linear-scan implementation, including which
	/// record "wins" when duplicate identifiers exist (first occurrence in the source list).
	/// </remarks>
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
			_indexCache.GetValue(
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
	/// FixId values exist, the first occurrence in the source list wins, matching the
	/// original <c>FirstOrDefault</c> behavior.
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
	/// NavId values exist, the first occurrence in the source list wins, matching the
	/// original <c>FirstOrDefault</c> behavior.
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
	/// duplicate identifiers exist, the first occurrence in the source list wins, matching
	/// the original <c>FirstOrDefault</c> behavior (which matched on either field).
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
