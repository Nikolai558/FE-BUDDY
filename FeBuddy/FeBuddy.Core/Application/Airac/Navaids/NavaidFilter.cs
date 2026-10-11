using FeBuddy.Core.Domain.Navaids.Models;

namespace FeBuddy.Core.Application.Airac.Navaids;

/// <summary>
/// The settings-driven <c>ExcludedTypes</c> filter, which narrows the NAVAIDs GeoJSON only.
/// </summary>
/// <remarks>
/// Like the ROI (<see cref="NavaidGeojsonWriter.FilterToRoi"/>), an excluded type is left out of the
/// GeoJSON but not the alias file, which has every NAVAID. It runs first, on the full list, and the
/// ROI then narrows what it keeps.
/// </remarks>
public static class NavaidFilter
{
	/// <summary>Drops every NAVAID whose type is in <paramref name="excludedTypes"/>.</summary>
	/// <param name="navaids">Every built NAVAID.</param>
	/// <param name="excludedTypes">The NASR type names to exclude, matched ignoring case.</param>
	/// <returns>The NAVAIDs that remain.</returns>
	public static IReadOnlyList<Navaid> ExcludeTypes(IReadOnlyList<Navaid> navaids, IReadOnlyCollection<string> excludedTypes)
	{
		ArgumentNullException.ThrowIfNull(navaids);
		ArgumentNullException.ThrowIfNull(excludedTypes);

		return excludedTypes.Count == 0
			? navaids
			: [.. navaids.Where(navaid => !excludedTypes.Contains(navaid.NavType))];
	}
}
