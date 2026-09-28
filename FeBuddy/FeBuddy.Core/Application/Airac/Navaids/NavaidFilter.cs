using FeBuddy.Core.Domain.Navaids.Models;

namespace FeBuddy.Core.Application.Airac.Navaids;

/// <summary>
/// The settings-driven <c>ExcludedTypes</c> filter, which applies to every NAVAIDs output alike.
/// </summary>
/// <remarks>
/// Unlike the ROI (<see cref="NavaidGeojsonWriter.FilterToRoi"/>, which limits the GeoJSON output
/// only), an excluded type is left out of the GeoJSON <em>and</em> the alias file - so this runs
/// before either writer sees the NAVAIDs, on the full, ROI-independent list.
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
