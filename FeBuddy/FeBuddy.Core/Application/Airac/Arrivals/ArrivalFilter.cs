using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Airac;
using FeBuddy.Core.Domain.Arrivals.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac.Arrivals;

/// <summary>
/// The Arrivals filters. Every one of them applies to both outputs - GeoJSON and the alias file -
/// unlike Airports, whose ROI limits the GeoJSON only.
/// </summary>
/// <remarks>
/// Split in two because they run at different points in the pipeline: the procedure-level filters
/// before anything is located (so a filtered-out procedure never costs a lookup or raises a
/// warning), the ROI after, because it needs coordinates.
/// </remarks>
public static class ArrivalFilter
{
	private const string LogSource = "ArrivalFilter";

	/// <summary>
	/// Keeps the procedures that pass the ARTCC and amendment-date filters.
	/// </summary>
	/// <param name="procedures">Every procedure read from the STAR tables.</param>
	/// <param name="settings">The parsed settings.</param>
	/// <param name="messages">Receives a warning for any procedure the amendment filter cannot judge.</param>
	/// <param name="today">
	/// The date <see cref="ArrivalAmendmentFilter.Days"/> mode counts back from. Defaults to the
	/// local date of the run (<c>DateOnly.FromDateTime(DateTime.Now)</c>); tests pass a fixed date.
	/// </param>
	/// <returns>The procedures in scope, in the same order.</returns>
	/// <exception cref="ArgumentException">
	/// Thrown when <see cref="ArrivalSettings.AmendmentFilter"/> is
	/// <see cref="ArrivalAmendmentFilter.Date"/> but <see cref="ArrivalSettings.AmendedOnOrAfter"/> is not set.
	/// </exception>
	public static IReadOnlyList<ArrivalProcedure> ByProcedure(
		IReadOnlyList<ArrivalProcedure> procedures,
		ArrivalSettings settings,
		List<ServiceMessage> messages,
		DateOnly? today = null)
	{
		ArgumentNullException.ThrowIfNull(procedures);
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(messages);

		if (settings.AmendmentFilter == ArrivalAmendmentFilter.Date && settings.AmendedOnOrAfter is null)
		{
			throw new ArgumentException(
				"AmendmentFilter is \"Date\" but AmendedOnOrAfter is not set.", nameof(settings));
		}

		DateOnly runDate = today ?? DateOnly.FromDateTime(DateTime.Now);

		HashSet<string> artccs = new(settings.ArtccFilter, StringComparer.OrdinalIgnoreCase);
		List<ArrivalProcedure> kept = [];

		foreach (ArrivalProcedure procedure in procedures)
		{
			ArrivalProcedure candidate = procedure;

			// The ARTCC filter is per airport, not per procedure: a STAR shared by two ARTCCs
			// (see ArrivalProcedure.ArtccFor) can be in scope for one of its served airports and
			// out of scope for another.
			if (artccs.Count > 0)
			{
				List<string> keptAirports = [.. procedure.ServedAirports.Where(airport => artccs.Contains(procedure.ArtccFor(airport)))];

				if (keptAirports.Count == 0)
				{
					continue;
				}

				if (keptAirports.Count != procedure.ServedAirports.Count)
				{
					candidate = procedure with { ServedAirports = keptAirports };
				}
			}

			if (settings.AmendmentFilter != ArrivalAmendmentFilter.None
				&& !IsRecentlyAmended(candidate, settings, runDate, messages))
			{
				continue;
			}

			kept.Add(candidate);
		}

		return kept;
	}

	/// <summary>
	/// Keeps the airport + procedure pairs inside the ROI.
	/// </summary>
	/// <param name="airportProcedures">The located airport + procedure pairs.</param>
	/// <param name="settings">The parsed settings; <see cref="ArrivalSettings.Roi"/> and <see cref="ArrivalSettings.RoiMode"/> are read.</param>
	/// <param name="allNasrCsvData">All parsed NASR CSV data; APT_BASE is read in <see cref="ArrivalRoiMode.Airport"/> mode.</param>
	/// <param name="messages">Receives one message per airport that has no location to test.</param>
	/// <returns>The pairs in scope, in the same order. Everything, when there is no ROI.</returns>
	public static IReadOnlyList<ArrivalAirportProcedure> ByRoi(
		IReadOnlyList<ArrivalAirportProcedure> airportProcedures,
		ArrivalSettings settings,
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

		if (settings.RoiMode == ArrivalRoiMode.Waypoint)
		{
			return [.. airportProcedures.Where(ap => ap.Points.Any(point => RoiFilter.Contains(roi, point.Latitude, point.Longitude)))];
		}

		// Airport mode: the airport's own reference point decides. An airport NASR has no APT_BASE
		// record for (a Canadian field listed as a served airport, say) has nothing to test, so it
		// is left out - it stays a served airport for everything that does not need its location.
		Dictionary<string, bool> inside = new(StringComparer.OrdinalIgnoreCase);
		List<ArrivalAirportProcedure> kept = [];

		foreach (ArrivalAirportProcedure airportProcedure in airportProcedures)
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
						$"Airport '{airportProcedure.AirportId}' has no APT_BASE record, so its arrivals cannot be tested against the ROI and were left out."));
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
	/// <see cref="ArrivalSettings.AmendmentFilter"/> sets. A procedure whose dates cannot be read
	/// is left out with a warning.
	/// </summary>
	private static bool IsRecentlyAmended(
		ArrivalProcedure procedure,
		ArrivalSettings settings,
		DateOnly today,
		List<ServiceMessage> messages)
	{
		if (procedure.AmendmentEffectiveDate is not { } amended)
		{
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"{ArrivalBuilder.Label(procedure)}: amendment date '{procedure.AmendmentEffectiveDateText}' could not be read, so the amendment-date filter left it out."));
			return false;
		}

		if (AmendmentCutoff(procedure, settings, today) is not { } cutoff)
		{
			// Cycles mode only: the cutoff counts back from the procedure's own cycle date.
			messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
				$"{ArrivalBuilder.Label(procedure)}: its cycle date could not be read, so the amendment-date filter left it out."));
			return false;
		}

		// A lower bound only: an amendment dated after the cutoff - even in the future - is kept.
		return amended >= cutoff;
	}

	/// <summary>
	/// The earliest amendment date <see cref="ArrivalSettings.AmendmentFilter"/> keeps, or
	/// <see langword="null"/> in <see cref="ArrivalAmendmentFilter.Cycles"/> mode when the
	/// procedure has no readable cycle date to count back from.
	/// </summary>
	private static DateOnly? AmendmentCutoff(ArrivalProcedure procedure, ArrivalSettings settings, DateOnly today) =>
		settings.AmendmentFilter switch
		{
			// The cycle the data came from counts as the first, so N cycles reach back N - 1 steps.
			ArrivalAmendmentFilter.Cycles =>
				procedure.CycleEffectiveDate?.AddDays(-AiracCycleResolver.DaysPerCycle * (settings.AmendedWithinCycles - 1)),
			ArrivalAmendmentFilter.Days => today.AddDays(-settings.AmendedWithinDays),
			ArrivalAmendmentFilter.Date => settings.AmendedOnOrAfter,
			_ => throw new ArgumentOutOfRangeException(
				nameof(settings), settings.AmendmentFilter, "Unknown amendment filter mode.")
		};
}
