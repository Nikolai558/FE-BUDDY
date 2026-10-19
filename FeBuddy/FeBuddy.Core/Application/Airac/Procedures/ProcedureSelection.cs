using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Domain.Procedures.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// Applies the user's selection - the area (facilities, ROI airports, every airport, or none), then
/// specific airports, specific procedures and specific airport + procedure pairs added to it - to
/// every built <see cref="ProcedureAirport"/>.
/// </summary>
/// <remarks>
/// <para>
/// Inclusion is decided per (airport, procedure): a whole-airport match (the area, or an explicit
/// <c>Airports</c> pick) includes every procedure of that airport whose chart type is one of
/// <see cref="ProcedureSettings.ChartTypes"/>; a <c>Procedures</c> or <c>AirportProcedures</c>
/// pick includes that procedure regardless of chart type or whole-airport inclusion. An airport
/// left with no included procedure is dropped.
/// </para>
/// <para>
/// <c>Procedures</c> and <c>AirportProcedures</c> picks take departures and arrivals only
/// (<see cref="ProcedureChartTypes.DeparturesAndArrivals"/>), matched by root name
/// (<see cref="ProcedureNaming.RootName"/>): a pick of <c>BRWNZ</c> - or the
/// <c>BRWNZ FOUR (RNAV)</c> it was made from - includes <c>BRWNZ FIVE (RNAV)</c> once the FAA
/// renumbers it. A pick naming another kind of chart (an approach, say) is left out, and says so.
/// </para>
/// <para>
/// A chart whose route has a fix in the user's facility (<see cref="ProceduresWithFixInFacility"/>)
/// is included too, at any airport, when its chart type is one of
/// <see cref="ProcedureSettings.ChartTypes"/>. When that is the only reason and the airport is outside
/// the user's facility (<see cref="ProcedureSettings.PrimaryFacility"/>), the copy returned carries
/// <see cref="Procedure.IncludedForFacilityFixes"/>, so the documents say why it's there. At one of the
/// facility's own airports it is included without it, as that says nothing new (Feature #371).
/// </para>
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
	/// <param name="facilityFixes">
	/// The charts whose route has a fix in the user's facility, by reference, from
	/// <see cref="ProceduresWithFixInFacility.Find"/>; <see langword="null"/> when that rule is off.
	/// </param>
	/// <returns>
	/// The included airports (each with only its included procedures, in the original chartseq
	/// order), in the order <paramref name="airports"/> supplied them.
	/// </returns>
	public static IReadOnlyList<ProcedureAirport> Select(
		IReadOnlyList<ProcedureAirport> airports,
		ProcedureSettings settings,
		List<ServiceMessage> messages,
		IReadOnlyDictionary<Procedure, FacilityFixMatch>? facilityFixes = null)
	{
		ArgumentNullException.ThrowIfNull(airports);
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(messages);

		HashSet<string> facilities = new(settings.Facilities, StringComparer.OrdinalIgnoreCase);
		HashSet<string> chartTypes = new(settings.ChartTypes, StringComparer.OrdinalIgnoreCase);

		List<string> airportPicks = [.. settings.Airports];
		bool[] airportPickMatched = new bool[airportPicks.Count];

		List<string> procedurePicks = [.. settings.Procedures];
		string[] procedurePickRoots = [.. procedurePicks.Select(ProcedureNaming.RootName)];
		bool[] procedurePickMatched = new bool[procedurePicks.Count];
		bool[] procedurePickIsOtherChart = new bool[procedurePicks.Count];

		List<ProcedureAirportPick> airportProcedurePicks = [.. settings.AirportProcedures];
		string[] airportProcedurePickRoots = [.. airportProcedurePicks.Select(pick => ProcedureNaming.RootName(pick.ProcedureName))];
		bool[] airportProcedurePickMatched = new bool[airportProcedurePicks.Count];
		bool[] airportProcedurePickIsOtherChart = new bool[airportProcedurePicks.Count];

		List<ProcedureAirport> result = [];

		foreach (ProcedureAirport airport in airports)
		{
			bool wholeAirport = AreaIncludes(settings.Area, facilities, settings.Roi, airport.ResponsibleArtcc, airport.Latitude, airport.Longitude);

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

			// Each departure or arrival's root name, worked out once; null for any other chart.
			string?[] roots = [.. airport.Procedures.Select(procedure =>
				ProcedureChartTypes.IsDepartureOrArrival(procedure.ChartCode) ? ProcedureNaming.RootName(procedure.Name) : null)];

			for (int p = 0; p < airport.Procedures.Count; p++)
			{
				for (int i = 0; i < procedurePicks.Count; i++)
				{
					if (Matches(airport.Procedures[p], roots[p], procedurePicks[i], procedurePickRoots[i], ref procedurePickIsOtherChart[i]))
					{
						procedurePickMatched[i] = true;
						included.Add(airport.Procedures[p]);
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

				for (int p = 0; p < airport.Procedures.Count; p++)
				{
					if (Matches(airport.Procedures[p], roots[p], pick.ProcedureName, airportProcedurePickRoots[i], ref airportProcedurePickIsOtherChart[i]))
					{
						airportProcedurePickMatched[i] = true;
						included.Add(airport.Procedures[p]);
					}
				}
			}

			// A chart whose route has a fix in the facility, if its type is wanted; labeled only when
			// nothing above included it and the airport is outside the facility, since at one of its own
			// airports the label says nothing new (Feature #371).
			bool inPrimaryFacility = airport.ResponsibleArtcc is { Length: > 0 } responsibleArtcc
				&& responsibleArtcc.Equals(settings.PrimaryFacility, StringComparison.OrdinalIgnoreCase);
			Dictionary<Procedure, FacilityFixMatch> onlyForFixes = new(ReferenceEqualityComparer.Instance);

			foreach (Procedure procedure in airport.Procedures)
			{
				if (facilityFixes is not null
					&& facilityFixes.TryGetValue(procedure, out FacilityFixMatch? match)
					&& chartTypes.Contains(procedure.ChartCode)
					&& included.Add(procedure)
					&& !inPrimaryFacility)
				{
					onlyForFixes[procedure] = match;
				}
			}

			if (included.Count > 0)
			{
				result.Add(airport with
				{
					Procedures = [.. airport.Procedures
						.Where(included.Contains)
						.Select(procedure => onlyForFixes.TryGetValue(procedure, out FacilityFixMatch? match)
							? procedure with { IncludedForFacilityFixes = match }
							: procedure)],
				});
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
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource, procedurePickIsOtherChart[i]
					? $"'{procedurePicks[i]}' isn't a departure or arrival, so it's left out: Additional Procedures to Include takes departures and arrivals only."
					: $"No departure or arrival named '{procedurePicks[i]}' is in this cycle's d-TPP metafile."));
			}
		}

		for (int i = 0; i < airportProcedurePicks.Count; i++)
		{
			if (!airportProcedurePickMatched[i])
			{
				ProcedureAirportPick pick = airportProcedurePicks[i];
				messages.Add(new ServiceMessage(LogLevel.Warning, LogSource, airportProcedurePickIsOtherChart[i]
					? $"'{pick.ProcedureName}' at {pick.Airport} isn't a departure or arrival, so it's left out: Additional Airport-Specific Procedures to Include takes departures and arrivals only."
					: $"Airport '{pick.Airport}' has no departure or arrival named '{pick.ProcedureName}' in this cycle's d-TPP metafile."));
			}
		}

		return result;
	}

	/// <summary>
	/// Whether the area takes an airport whole: every airport for <see cref="SubServiceArea.Everything"/>; one
	/// whose responsible ARTCC is a facility ticked for <see cref="SubServiceArea.Artccs"/>; one inside the ROI for
	/// <see cref="SubServiceArea.Roi"/>; none for <see cref="SubServiceArea.None"/>. The run's rule, which the
	/// Procedure Charts tab also says "already included" by.
	/// </summary>
	/// <param name="area">The area chosen.</param>
	/// <param name="facilities">The facilities ticked, compared as the set compares them.</param>
	/// <param name="roi">The ROI, or <see langword="null"/>.</param>
	/// <param name="responsibleArtcc">The airport's responsible ARTCC (<c>APT_BASE.RESP_ARTCC_ID</c>), or <see langword="null"/>.</param>
	/// <param name="latitude">The airport's latitude, or <see langword="null"/>.</param>
	/// <param name="longitude">The airport's longitude, or <see langword="null"/>.</param>
	/// <returns><see langword="true"/> when the area takes the airport.</returns>
	public static bool AreaIncludes(
		SubServiceArea area,
		IReadOnlySet<string> facilities,
		RegionOfInterest? roi,
		string? responsibleArtcc,
		double? latitude,
		double? longitude)
	{
		ArgumentNullException.ThrowIfNull(facilities);

		return area switch
		{
			SubServiceArea.Everything => true,
			SubServiceArea.Artccs => responsibleArtcc?.Trim() is { Length: > 0 } artcc && facilities.Contains(artcc),
			SubServiceArea.Roi => roi is not null && latitude is { } lat && longitude is { } lon && RoiFilter.Contains(roi, lat, lon),
			_ => false,
		};
	}

	/// <summary>
	/// Whether a pick names this procedure: a departure or arrival whose root name is the pick's.
	/// Another kind of chart is never matched; when it bears the pick's exact name,
	/// <paramref name="isOtherChart"/> is set, so the warning can say why.
	/// </summary>
	/// <param name="procedure">The procedure.</param>
	/// <param name="root">Its root name, or <see langword="null"/> when it isn't a departure or arrival.</param>
	/// <param name="pick">The name the user listed.</param>
	/// <param name="pickRoot">That name's root.</param>
	/// <param name="isOtherChart">Set when the pick names a chart that isn't a departure or arrival.</param>
	/// <returns><see langword="true"/> when the pick includes the procedure.</returns>
	private static bool Matches(Procedure procedure, string? root, string pick, string pickRoot, ref bool isOtherChart)
	{
		if (root is not null)
		{
			return root.Equals(pickRoot, StringComparison.OrdinalIgnoreCase);
		}

		if (procedure.Name.Equals(ProcedureNaming.BaseName(pick), StringComparison.OrdinalIgnoreCase))
		{
			isOtherChart = true;
		}

		return false;
	}

	/// <summary>Whether a user-typed identifier names this airport, matching its FAA or ICAO identifier ignoring case.</summary>
	private static bool MatchesAirport(ProcedureAirport airport, string identifier) =>
		airport.AptIdent.Equals(identifier, StringComparison.OrdinalIgnoreCase)
		|| (airport.IcaoIdent is not null && airport.IcaoIdent.Equals(identifier, StringComparison.OrdinalIgnoreCase));
}
