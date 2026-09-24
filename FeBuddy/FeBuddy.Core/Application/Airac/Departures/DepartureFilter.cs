using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Departures.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac.Departures;

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
	/// <param name="today">
	/// The date <see cref="DepartureAmendmentFilter.Days"/> mode counts back from. Defaults to the
	/// local date of the run (<c>DateOnly.FromDateTime(DateTime.Now)</c>); tests pass a fixed date.
	/// </param>
	/// <returns>The procedures in scope, in the same order.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when <see cref="DepartureSettings.AmendmentFilter"/> is
	/// <see cref="DepartureAmendmentFilter.Date"/> but <see cref="DepartureSettings.AmendedOnOrAfter"/> is not set.
	/// </exception>
	public static IReadOnlyList<DepartureProcedure> ByProcedure(
		IReadOnlyList<DepartureProcedure> procedures,
		DepartureSettings settings,
		List<ServiceMessage> messages,
		DateOnly? today = null)
	{
		ArgumentNullException.ThrowIfNull(procedures);
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(messages);

		if (settings.AmendmentFilter == DepartureAmendmentFilter.Date && settings.AmendedOnOrAfter is null)
		{
			throw new ArgumentException(
				"AmendmentFilter is \"Date\" but AmendedOnOrAfter is not set.", nameof(settings));
		}

		DateOnly runDate = today ?? DateOnly.FromDateTime(DateTime.Now);

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

			if (settings.AmendmentFilter != DepartureAmendmentFilter.None
				&& !IsRecentlyAmended(procedure, settings, runDate, messages))
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
				(double waypointLat, double waypointLon, string foundIn)? location = WaypointLocator.Find(
					allNasrCsvData, airportProcedure.AirportId, WaypointLocator.WaypointType.Airport);

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
	/// Whether the procedure's current amendment became effective on or after the cutoff that
	/// <see cref="DepartureSettings.AmendmentFilter"/> sets. A procedure whose dates cannot be
	/// read is left out with a warning.
	/// </summary>
	private static bool IsRecentlyAmended(
		DepartureProcedure procedure,
		DepartureSettings settings,
		DateOnly today,
		List<ServiceMessage> messages)
	{
		if (procedure.AmendmentEffectiveDate is not { } amended)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"{DepartureBuilder.Label(procedure)}: amendment date '{procedure.AmendmentEffectiveDateText}' could not be read, so the amendment-date filter left it out."));
			return false;
		}

		if (AmendmentCutoff(procedure, settings, today) is not { } cutoff)
		{
			// Cycles mode only: the cutoff counts back from the procedure's own cycle date.
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"{DepartureBuilder.Label(procedure)}: its cycle date could not be read, so the amendment-date filter left it out."));
			return false;
		}

		// A lower bound only: an amendment dated after the cutoff - even in the future - is kept.
		return amended >= cutoff;
	}

	/// <summary>
	/// The earliest amendment date <see cref="DepartureSettings.AmendmentFilter"/> keeps, or
	/// <see langword="null"/> in <see cref="DepartureAmendmentFilter.Cycles"/> mode when the
	/// procedure has no readable cycle date to count back from.
	/// </summary>
	private static DateOnly? AmendmentCutoff(DepartureProcedure procedure, DepartureSettings settings, DateOnly today) =>
		settings.AmendmentFilter switch
		{
			// The cycle the data came from counts as the first, so N cycles reach back N - 1 steps.
			DepartureAmendmentFilter.Cycles =>
				procedure.CycleEffectiveDate?.AddDays(-DaysPerCycle * (settings.AmendedWithinCycles - 1)),
			DepartureAmendmentFilter.Days => today.AddDays(-settings.AmendedWithinDays),
			DepartureAmendmentFilter.Date => settings.AmendedOnOrAfter,
			_ => throw new ArgumentOutOfRangeException(
				nameof(settings), settings.AmendmentFilter, "Unknown amendment filter mode.")
		};
}
