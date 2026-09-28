using FeBuddy.Core.Domain.ArtccBoundaries.Models;

namespace FeBuddy.Core.Application.Airac.ArtccBoundaries;

/// <summary>
/// The settings-driven <c>LocationFilter</c>, which applies to every ARTCC Boundaries output alike.
/// </summary>
public static class ArtccBoundaryFilter
{
	/// <summary>Keeps only the rings whose location is in <paramref name="locationFilter"/>.</summary>
	/// <param name="rings">Every built ring.</param>
	/// <param name="locationFilter">The LocationIds to keep, matched ignoring case. Empty keeps every ring.</param>
	/// <returns>The rings that remain.</returns>
	public static IReadOnlyList<ArtccBoundaryRing> ByLocation(
		IReadOnlyList<ArtccBoundaryRing> rings,
		IReadOnlyCollection<string> locationFilter)
	{
		ArgumentNullException.ThrowIfNull(rings);
		ArgumentNullException.ThrowIfNull(locationFilter);

		return locationFilter.Count == 0
			? rings
			: [.. rings.Where(ring => locationFilter.Contains(ring.Location.LocationId))];
	}
}
