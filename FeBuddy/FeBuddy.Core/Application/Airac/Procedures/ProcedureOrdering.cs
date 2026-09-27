using FeBuddy.Core.Domain.Procedures.Models;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// The facility-section and airport ordering shared by <see cref="ProcedureChangesMarkdownWriter"/>
/// and <see cref="ProceduresJsonWriter"/>.
/// </summary>
internal static class ProcedureOrdering
{
	/// <summary>The facility heading for airports with no <see cref="ProcedureAirport.ResponsibleArtcc"/>.</summary>
	public const string OtherFacility = "Other";

	/// <summary>
	/// Groups the included airports by facility, ordered: <paramref name="primaryFacility"/> first
	/// (when it has any included airport), then every other facility alphabetically, then
	/// <see cref="OtherFacility"/> (airports with no ARTCC) last. Airports within a facility are
	/// ordered by <see cref="OrderAirports"/>.
	/// </summary>
	/// <param name="airports">Every included airport.</param>
	/// <param name="primaryFacility">The primary facility, upper-cased, or <see langword="null"/> for none.</param>
	/// <returns>Each facility heading with its ordered airports, in section order.</returns>
	public static IReadOnlyList<(string Facility, IReadOnlyList<ProcedureAirport> Airports)> GroupByFacility(
		IReadOnlyList<ProcedureAirport> airports,
		string? primaryFacility)
	{
		ILookup<string, ProcedureAirport> byFacility = airports.ToLookup(
			airport => airport.ResponsibleArtcc is { Length: > 0 } artcc ? artcc : OtherFacility,
			StringComparer.OrdinalIgnoreCase);

		List<string> facilities = [.. byFacility.Select(group => group.Key)];

		IEnumerable<string> primary = facilities.Where(f => f.Equals(primaryFacility, StringComparison.OrdinalIgnoreCase));

		IEnumerable<string> others = facilities
			.Where(f => !f.Equals(primaryFacility, StringComparison.OrdinalIgnoreCase) && !f.Equals(OtherFacility, StringComparison.OrdinalIgnoreCase))
			.OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

		IEnumerable<string> other = facilities.Where(f => f.Equals(OtherFacility, StringComparison.OrdinalIgnoreCase));

		return [.. primary.Concat(others).Concat(other)
			.Select(facility => (facility, (IReadOnlyList<ProcedureAirport>)OrderAirports(byFacility[facility])))];
	}

	/// <summary>
	/// Orders airports within a facility: airspace rank (B, C, D, then everything else including E
	/// and none), then IDs without digits before IDs with digits, then ordinal alphanumeric by
	/// <see cref="ProcedureAirport.AptIdent"/>.
	/// </summary>
	/// <param name="airports">The facility's airports.</param>
	/// <returns>The airports, ordered.</returns>
	public static IReadOnlyList<ProcedureAirport> OrderAirports(IEnumerable<ProcedureAirport> airports) =>
		[.. airports
			.OrderBy(AirspaceRank)
			.ThenBy(airport => ContainsDigit(airport.AptIdent))
			.ThenBy(airport => airport.AptIdent, StringComparer.Ordinal)];

	private static int AirspaceRank(ProcedureAirport airport) => airport.AirspaceClass switch
	{
		ProcedureAirspaceClass.B => 0,
		ProcedureAirspaceClass.C => 1,
		ProcedureAirspaceClass.D => 2,
		_ => 3,
	};

	private static bool ContainsDigit(string value) => value.Any(char.IsDigit);
}
