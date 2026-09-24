using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac.Airways;

/// <summary>
/// The one definition of a "reference-only" airway point: a point that NASR lists purely to
/// describe the route (a border crossing such as <c>U.S. CANADIAN BORDER-4</c>), not an
/// actual navigable waypoint.
/// </summary>
/// <remarks>
/// NASR marks these structurally: a reference-only point appears as a row's <c>FROM_POINT</c>
/// with a <b>blank</b> <c>FROM_PT_TYPE</c> (verified across cycle 2609 - 169 such rows, and
/// zero border markers ever carry a populated <c>FROM_PT_TYPE</c>). A point in this set is a
/// border crossing to skip, never an unresolved waypoint to warn about.
/// </remarks>
internal static class AirwayReferenceOnlyPoints
{
	/// <summary>
	/// Whether a raw <c>AWY_SEG_ALT</c> row describes a reference-only point at its
	/// <c>FROM_POINT</c> (its <c>FROM_PT_TYPE</c> is null or blank).
	/// </summary>
	/// <param name="row">The raw segment row.</param>
	/// <returns><see langword="true"/> if the row's from-point is reference-only.</returns>
	public static bool IsReferenceOnlyRow(AwyCsvDataModel.AwySegAlt row) =>
		string.IsNullOrWhiteSpace(row?.FromPtType);

	/// <summary>
	/// Builds the set of reference-only point IDs for one airway, from its rows whose
	/// <c>FROM_PT_TYPE</c> is blank.
	/// </summary>
	/// <param name="rawSegments">The airway's raw <c>AWY_SEG_ALT</c> rows.</param>
	/// <returns>A case-insensitive set of the trimmed <c>FROM_POINT</c> values of the reference-only rows.</returns>
	public static HashSet<string> BuildSet(IEnumerable<AwyCsvDataModel.AwySegAlt> rawSegments)
	{
		HashSet<string> set = new(StringComparer.OrdinalIgnoreCase);

		foreach (AwyCsvDataModel.AwySegAlt row in rawSegments)
		{
			if (IsReferenceOnlyRow(row) && !string.IsNullOrWhiteSpace(row.FromPoint))
			{
				set.Add(row.FromPoint.Trim());
			}
		}

		return set;
	}
}
