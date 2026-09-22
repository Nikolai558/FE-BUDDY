using FeBuddy.Core.Handlers.General;
using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac.Departures;
using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.General;

namespace FeBuddy.Core.Services.Airac.Departures;

/// <summary>
/// The Departures filters. Every one of them applies to both outputs - GeoJSON and the alias
/// file - unlike Airports, whose ROI limits the GeoJSON only.
/// </summary>
/// <remarks>
/// Split in two because they run at different points in the pipeline: the procedure-level
/// filters before anything is located (so a filtered-out procedure never costs a lookup or
/// raises a warning), the ROI after, because it needs coordinates.
/// </remarks>
public static class DepartureFilter
{
	private const string LogSource = "DepartureFilter";

	/// <summary>The length of one AIRAC cycle, used to count amendment dates back from the cycle date.</summary>
	private const int DaysPerCycle = 28;

	/// <summary>
	/// Keeps the procedures that pass the type, ARTCC and amendment-date filters.
	/// </summary>
	/// <param name="procedures">Every procedure read from the DP tables.</param>
	/// <param name="settings">The parsed settings.</param>
	/// <param name="messages">Receives a warning for any procedure the amendment filter cannot judge.</param>
	/// <returns>The procedures in scope, in the same order.</returns>
	public static IReadOnlyList<DepartureProcedure> ByProcedure(
		IReadOnlyList<DepartureProcedure> procedures,
		DepartureSettings settings,
		List<ServiceMessage> messages)
	{
		ArgumentNullException.ThrowIfNull(procedures);
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(messages);

		HashSet<string> artccs = new(settings.ArtccFilter, StringComparer.OrdinalIgnoreCase);
		List<DepartureProcedure> kept = new();

		foreach (DepartureProcedure procedure in procedures)
		{
			if (procedure.IsObstacleDeparture && !settings.IncludeObstacleDepartures)
			{
				continue;
			}

			if (artccs.Count > 0 && !artccs.Contains(procedure.Artcc))
			{
				continue;
			}

			if (settings.AmendedWithinCycles > 0 && !IsRecentlyAmended(procedure, settings.AmendedWithinCycles, messages))
			{
				continue;
			}

			kept.Add(procedure);
		}

		return kept;
	}

	/// <summary>
	/// Keeps the airport + procedure pairs inside the ROI.
	/// </summary>
	/// <param name="airportProcedures">The located airport + procedure pairs.</param>
	/// <param name="settings">The parsed settings; <see cref="DepartureSettings.Roi"/> and <see cref="DepartureSettings.RoiMode"/> are read.</param>
	/// <param name="allNasrCsvData">All parsed NASR CSV data; APT_BASE is read in <see cref="DepartureRoiMode.Airport"/> mode.</param>
	/// <param name="messages">Receives one message per airport that has no location to test.</param>
	/// <returns>The pairs in scope, in the same order. Everything, when there is no ROI.</returns>
	public static IReadOnlyList<DepartureAirportProcedure> ByRoi(
		IReadOnlyList<DepartureAirportProcedure> airportProcedures,
		DepartureSettings settings,
		NasrCsvDataCollection allNasrCsvData,
		List<ServiceMessage> messages)
	{
		ArgumentNullException.ThrowIfNull(airportProcedures);
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(allNasrCsvData);
		ArgumentNullException.ThrowIfNull(messages);

		if (settings.Roi is not { } roi)
		{
			return airportProcedures;
		}

		if (settings.RoiMode == DepartureRoiMode.Waypoint)
		{
			return airportProcedures
				.Where(ap => ap.Points.Any(point => RoiFilter.Contains(roi, point.Latitude, point.Longitude)))
				.ToList();
		}

		// Airport mode: the airport's own reference point decides. An airport NASR has no
		// APT_BASE record for (TRMML lists CYQG, a Canadian field) has nothing to test, so it is
		// left out - it stays a served airport for everything that does not need its location.
		Dictionary<string, bool> inside = new(StringComparer.OrdinalIgnoreCase);
		List<DepartureAirportProcedure> kept = new();

		foreach (DepartureAirportProcedure airportProcedure in airportProcedures)
		{
			if (!inside.TryGetValue(airportProcedure.AirportId, out bool isInside))
			{
				(double waypointLat, double waypointLon, string foundIn)? location = FindWaypointCoordinates.GetCoordinates(
					allNasrCsvData, airportProcedure.AirportId, FindWaypointCoordinates.WaypointType.Airport);

				if (location is { } found)
				{
					isInside = RoiFilter.Contains(roi, found.waypointLat, found.waypointLon);
				}
				else
				{
					isInside = false;
					messages.Add(new ServiceMessage(LogLevel.Info, LogSource,
						$"Airport '{airportProcedure.AirportId}' has no APT_BASE record, so its departures cannot be tested against the ROI and were left out."));
				}

				inside[airportProcedure.AirportId] = isInside;
			}

			if (isInside)
			{
				kept.Add(airportProcedure);
			}
		}

		return kept;
	}

	/// <summary>
	/// Whether the procedure's current amendment became effective within the last
	/// <paramref name="cycles"/> cycles, counting the cycle its data came from as the first.
	/// </summary>
	private static bool IsRecentlyAmended(DepartureProcedure procedure, int cycles, List<ServiceMessage> messages)
	{
		if (procedure.AmendmentEffectiveDate is not { } amended || procedure.CycleEffectiveDate is not { } cycleDate)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"{DepartureBuilder.Label(procedure)}: amendment date '{procedure.AmendmentEffectiveDateText}' could not be read, so the amendment-date filter left it out."));
			return false;
		}

		DateOnly cutoff = cycleDate.AddDays(-DaysPerCycle * (cycles - 1));
		return amended >= cutoff;
	}
}
