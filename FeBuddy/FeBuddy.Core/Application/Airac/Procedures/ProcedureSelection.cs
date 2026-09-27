using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Domain.Procedures.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// Applies the user's additive selection - whole facilities, ROI airports, specific airports,
/// specific procedures, specific airport + procedure pairs - to every built
/// <see cref="ProcedureAirport"/>.
/// </summary>
/// <remarks>
/// Inclusion is decided per (airport, procedure): a whole-airport match (facility, ROI, or an
/// explicit <c>Airports</c> pick) includes every procedure of that airport whose chart type is one
/// of <see cref="ProcedureSettings.ChartTypes"/>; a <c>Procedures</c> or <c>AirportProcedures</c>
/// pick includes that one procedure regardless of chart type or whole-airport inclusion. An airport
/// left with no included procedure is dropped.
/// </remarks>
public static class ProcedureSelection
{
	private const string LogSource = "ProcedureSelection";

	/// <summary>
	/// Selects the included airports and, for each, its included procedures.
	/// </summary>
	/// <param name="airports">Every airport <c>ProcedureBuilder</c> built.</param>
	/// <param name="settings">The parsed settings.</param>
	/// <param name="messages">Receives one warning for each pick that matched nothing.</param>
	/// <returns>
	/// The included airports (each with only its included procedures, in the original chartseq
	/// order), in the order <paramref name="airports"/> supplied them.
	/// </returns>
	public static IReadOnlyList<ProcedureAirport> Select(
		IReadOnlyList<ProcedureAirport> airports,
		ProcedureSettings settings,
		List<ServiceMessage> messages)
	{
		ArgumentNullException.ThrowIfNull(airports);
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(messages);

		HashSet<string> facilities = new(settings.Facilities, StringComparer.OrdinalIgnoreCase);
		HashSet<string> chartTypes = new(settings.ChartTypes, StringComparer.OrdinalIgnoreCase);

		List<string> airportPicks = [.. settings.Airports];
		bool[] airportPickMatched = new bool[airportPicks.Count];

		List<string> procedurePicks = [.. settings.Procedures];
		bool[] procedurePickMatched = new bool[procedurePicks.Count];

		List<ProcedureAirportPick> airportProcedurePicks = [.. settings.AirportProcedures];
		bool[] airportProcedurePickMatched = new bool[airportProcedurePicks.Count];

		List<ProcedureAirport> result = [];

		foreach (ProcedureAirport airport in airports)
		{
			bool wholeAirport = airport.ResponsibleArtcc is { Length: > 0 } artcc && facilities.Contains(artcc);

			if (!wholeAirport && settings.IncludeRoiAirports && settings.Roi is { } roi
				&& airport.Latitude is { } latitude && airport.Longitude is { } longitude)
			{
				wholeAirport = RoiFilter.Contains(roi, latitude, longitude);
			}

			// Checked for every airport, whole-airport match or not, so a pick that also names an
			// already-included airport is still marked matched (no false "not found" warning).
			for (int i = 0; i < airportPicks.Count; i++)
			{
				if (MatchesAirport(airport, airportPicks[i]))
				{
					airportPickMatched[i] = true;
					wholeAirport = true;
				}
			}

			HashSet<Procedure> included = new(ReferenceEqualityComparer.Instance);

			if (wholeAirport)
			{
				foreach (Procedure procedure in airport.Procedures)
				{
					if (chartTypes.Contains(procedure.ChartCode))
					{
						included.Add(procedure);
					}
				}
			}

			foreach (Procedure procedure in airport.Procedures)
			{
				for (int i = 0; i < procedurePicks.Count; i++)
				{
					if (procedure.Name.Equals(procedurePicks[i], StringComparison.OrdinalIgnoreCase))
					{
						procedurePickMatched[i] = true;
						included.Add(procedure);
					}
				}
			}

			for (int i = 0; i < airportProcedurePicks.Count; i++)
			{
				ProcedureAirportPick pick = airportProcedurePicks[i];

				if (!MatchesAirport(airport, pick.Airport))
				{
					continue;
				}

				Procedure? procedure = airport.Procedures.FirstOrDefault(
					p => p.Name.Equals(pick.ProcedureName, StringComparison.OrdinalIgnoreCase));

				if (procedure is not null)
				{
					airportProcedurePickMatched[i] = true;
					included.Add(procedure);
				}
			}

			if (included.Count > 0)
			{
				result.Add(airport with { Procedures = [.. airport.Procedures.Where(included.Contains)] });
			}
		}

		for (int i = 0; i < airportPicks.Count; i++)
		{
			if (!airportPickMatched[i])
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"Airport '{airportPicks[i]}' is not in this cycle's d-TPP metafile."));
			}
		}

		for (int i = 0; i < procedurePicks.Count; i++)
		{
			if (!procedurePickMatched[i])
			{
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"Procedure '{procedurePicks[i]}' is not in this cycle's d-TPP metafile."));
			}
		}

		for (int i = 0; i < airportProcedurePicks.Count; i++)
		{
			if (!airportProcedurePickMatched[i])
			{
				ProcedureAirportPick pick = airportProcedurePicks[i];
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource,
					$"Airport '{pick.Airport}' has no procedure '{pick.ProcedureName}' in this cycle's d-TPP metafile."));
			}
		}

		return result;
	}

	/// <summary>Whether a user-typed identifier names this airport, matching its FAA or ICAO identifier ignoring case.</summary>
	private static bool MatchesAirport(ProcedureAirport airport, string identifier) =>
		airport.AptIdent.Equals(identifier, StringComparison.OrdinalIgnoreCase)
		|| (airport.IcaoIdent is not null && airport.IcaoIdent.Equals(identifier, StringComparison.OrdinalIgnoreCase));
}
