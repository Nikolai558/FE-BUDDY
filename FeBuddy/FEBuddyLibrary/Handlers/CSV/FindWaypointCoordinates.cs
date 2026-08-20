using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FEBuddyLibrary.Models.NASR.CSV;

namespace FEBuddyLibrary.Handlers.CSV;

/// <summary>
/// Provides methods for locating waypoint coordinates from parsed NASR CSV data.
/// </summary>
internal static class FindWaypointCoordinates
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
	/// A tuple containing waypointLat and waypointLon when found; otherwise null.
	/// </returns>
	internal static (double waypointLat, double waypointLon)? GetCoordinates(
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

		// If the waypoint type is known, search only that data source.
		if (waypointType.HasValue)
		{
			return waypointType.Value switch
			{
				WaypointType.Fix =>
					FindFix(allNasrCsvData, waypointId),

				WaypointType.Navaid =>
					FindNavaid(allNasrCsvData, waypointId),

				WaypointType.Airport =>
					FindAirport(allNasrCsvData, waypointId),

				_ => null
			};
		}

		// Waypoint type is unknown...

		// If the ID contains exactly 5 characters, search FIX data only.
		if (waypointId.Length == 5)
		{
			return FindFix(allNasrCsvData, waypointId);
		}

		// Otherwise, NAVAID has priority over airport.
		var navaidCoordinates = FindNavaid(allNasrCsvData, waypointId);

		if (navaidCoordinates.HasValue)
			return navaidCoordinates;

		return FindAirport(allNasrCsvData, waypointId);
	}


	/// <summary>
	/// Searches FIX_BASE for a matching 5-character fix.
	/// </summary>
	private static (double waypointLat, double waypointLon)? FindFix(
		NasrCsvDataCollection allNasrCsvData,
		string waypointId)
	{
		var fix = allNasrCsvData.Fix?.FixBase.FirstOrDefault(
			x => string.Equals(
				x.FixId,
				waypointId,
				StringComparison.OrdinalIgnoreCase));

		if (fix is null)
			return null;

		return (fix.LatDecimal, fix.LongDecimal);
	}


	/// <summary>
	/// Searches NAV_BASE for a matching NAVAID.
	/// </summary>
	private static (double waypointLat, double waypointLon)? FindNavaid(
		NasrCsvDataCollection allNasrCsvData,
		string waypointId)
	{
		var navaid = allNasrCsvData.Nav?.NavBase.FirstOrDefault(
			x => string.Equals(
				x.NavId,
				waypointId,
				StringComparison.OrdinalIgnoreCase));

		if (navaid is null)
			return null;

		return (navaid.LatDecimal, navaid.LongDecimal);
	}


	/// <summary>
	/// Searches APT_BASE for a matching airport using either ICAO ID or airport ID.
	/// </summary>
	private static (double waypointLat, double waypointLon)? FindAirport(
		NasrCsvDataCollection allNasrCsvData,
		string waypointId)
	{
		var airport = allNasrCsvData.Apt?.AptBase.FirstOrDefault(
			x =>
				string.Equals(
					x.IcaoId,
					waypointId,
					StringComparison.OrdinalIgnoreCase)
				||
				string.Equals(
					x.ArptId,
					waypointId,
					StringComparison.OrdinalIgnoreCase));

		if (airport is null)
			return null;

		return (airport.BaseLatDecimal, airport.BaseLongDecimal);
	}
}
