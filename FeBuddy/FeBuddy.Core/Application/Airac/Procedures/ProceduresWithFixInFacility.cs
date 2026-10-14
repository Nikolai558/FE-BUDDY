using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Domain.Procedures.Models;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// Finds the procedures whose route has a fix in the user's facility, at any airport (Feature #367),
/// for <see cref="ProcedureSelection"/> to include beside the area and the picks. Departures and
/// arrivals for now, from NASR; the routes come from <see cref="ProcedureRouteSet"/>s, so approaches
/// from the FAA CIFP can join later without changing this or the selection.
/// </summary>
/// <remarks>
/// <para>
/// A chart is matched to its route at each airport the route serves: by computer code when both
/// have one, otherwise by root name (<see cref="ProcedureNaming.RootName"/>) with punctuation left out
/// (NASR's <c>O'HARE</c> is the d-TPP's <c>OHARE NINE</c>), and always by chart type.
/// </para>
/// <para>
/// Which cycle's route counts: the selected cycle's for a current or new chart, the previous cycle's
/// for a deleted one, and either for a changed or re-added one, so a procedure that moved out of the
/// facility still shows its change. Without the previous cycle's routes, the selected cycle's are
/// used for every chart.
/// </para>
/// </remarks>
public static class ProceduresWithFixInFacility
{
	/// <summary>Every chart whose route has a fix in <paramref name="facility"/>, with the fixes.</summary>
	/// <param name="airports">Every airport <see cref="ProcedureBuilder"/> built.</param>
	/// <param name="facility">The user's facility, e.g. <c>ZDC</c>.</param>
	/// <param name="current">The selected cycle's routes.</param>
	/// <param name="previous">The previous cycle's routes, or <see langword="null"/> when they aren't available.</param>
	/// <param name="useAirports">Whether a point in neither <c>FIX_BASE</c> nor <c>NAV_BASE</c> counts by the airport it names: only with the area "None".</param>
	/// <returns>The charts that qualify, by reference, each with its fixes in the facility.</returns>
	public static IReadOnlyDictionary<Procedure, FacilityFixMatch> Find(
		IReadOnlyList<ProcedureAirport> airports,
		string facility,
		ProcedureRouteSet current,
		ProcedureRouteSet? previous,
		bool useAirports)
	{
		ArgumentNullException.ThrowIfNull(airports);
		ArgumentException.ThrowIfNullOrWhiteSpace(facility);
		ArgumentNullException.ThrowIfNull(current);

		Dictionary<Procedure, FacilityFixMatch> matches = new(ReferenceEqualityComparer.Instance);

		foreach (ProcedureAirport airport in airports)
		{
			foreach (Procedure procedure in airport.Procedures)
			{
				List<string> fixes = procedure.Change switch
				{
					ProcedureChange.Deleted => Fixes(previous ?? current, airport, procedure, facility, useAirports),
					ProcedureChange.Changed or ProcedureChange.ReAdded => Fixes(current, airport, procedure, facility, useAirports) is { Count: > 0 } now
						? now
						: previous is null ? [] : Fixes(previous, airport, procedure, facility, useAirports),
					_ => Fixes(current, airport, procedure, facility, useAirports),
				};

				if (fixes.Count > 0)
				{
					matches[procedure] = new FacilityFixMatch(facility, fixes);
				}
			}
		}

		return matches;
	}

	/// <summary>
	/// Whether <paramref name="route"/> is <paramref name="chart"/>'s: the same chart type, and the
	/// same computer code - or, when either has none, the same root name, punctuation left out.
	/// </summary>
	/// <param name="route">The route.</param>
	/// <param name="chart">The d-TPP chart.</param>
	/// <returns><see langword="true"/> when they are the same procedure.</returns>
	public static bool Matches(ProcedureRoute route, Procedure chart)
	{
		ArgumentNullException.ThrowIfNull(route);
		ArgumentNullException.ThrowIfNull(chart);

		if (!route.ChartCodes.Contains(chart.ChartCode, StringComparer.OrdinalIgnoreCase))
		{
			return false;
		}

		if (route.ComputerCode is { } code && chart.ComputerCode?.Trim() is { Length: > 0 } chartCode)
		{
			return code.Equals(chartCode, StringComparison.OrdinalIgnoreCase);
		}

		string name = NameKey(route.Name);
		return name.Length > 0 && name == NameKey(chart.Name);
	}

	/// <summary>The fixes and NAVAIDs in the facility on the routes of the chart's procedure at this airport, in route order, each once.</summary>
	private static List<string> Fixes(ProcedureRouteSet routes, ProcedureAirport airport, Procedure chart, string facility, bool useAirports)
	{
		List<string> fixes = [];

		foreach (ProcedureRoute route in routes.At(airport.AptIdent, airport.IcaoIdent).Where(route => Matches(route, chart)))
		{
			foreach (ProcedureRoutePoint point in route.Points)
			{
				if (!fixes.Contains(point.Id, StringComparer.OrdinalIgnoreCase) && routes.Points.IsInFacility(point, facility, useAirports))
				{
					fixes.Add(point.Id);
				}
			}
		}

		return fixes;
	}

	/// <summary>A name to match on: its root name, letters and digits only, upper case - <c>O'HARE</c> and <c>OHARE NINE</c> are both <c>OHARE</c>.</summary>
	private static string NameKey(string name) =>
		new([.. ProcedureNaming.RootName(name).Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant)]);
}
