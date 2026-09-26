using FeBuddy.Core.Application.Airac.ArtccBoundaries;
using FeBuddy.Core.Domain.ArtccBoundaries.Models;

namespace FeBuddy.UnitTests.Application.Airac.ArtccBoundaries;

/// <summary>
/// Covers <see cref="ArtccBoundaryFilter.ByLocation"/>: the settings-driven <c>LocationFilter</c>,
/// which applies to every ARTCC Boundaries output alike.
/// </summary>
public sealed class ArtccBoundaryFilterTests
{
	private static ArtccBoundaryRing Ring(string locationId) => new()
	{
		Location = new ArtccBoundaryLocation { LocationId = locationId },
		Altitude = ArtccBoundaryAltitude.High,
		Type = "ARTCC",
		Points = [new ArtccBoundaryPoint(0, 0), new ArtccBoundaryPoint(1, 1), new ArtccBoundaryPoint(0, 0)],
	};

	[Fact]
	public void an_empty_location_filter_keeps_every_ring_and_returns_the_same_instance()
	{
		IReadOnlyList<ArtccBoundaryRing> rings = [Ring("ZOB"), Ring("ZAK")];

		IReadOnlyList<ArtccBoundaryRing> kept = ArtccBoundaryFilter.ByLocation(rings, []);

		Assert.Same(rings, kept);
	}

	[Fact]
	public void a_populated_filter_keeps_only_the_listed_locations()
	{
		IReadOnlyList<ArtccBoundaryRing> rings = [Ring("ZOB"), Ring("ZAK"), Ring("ZLA")];

		IReadOnlyList<ArtccBoundaryRing> kept = ArtccBoundaryFilter.ByLocation(rings, ["ZOB", "ZLA"]);

		Assert.Equal(["ZOB", "ZLA"], kept.Select(r => r.Location.LocationId));
	}

	[Fact]
	public void the_filter_matches_ignoring_case_when_given_a_case_insensitive_set()
	{
		IReadOnlyList<ArtccBoundaryRing> rings = [Ring("ZOB")];

		IReadOnlyList<ArtccBoundaryRing> kept =
			ArtccBoundaryFilter.ByLocation(rings, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "zob" });

		Assert.Single(kept);
	}

	[Fact]
	public void filtering_out_every_location_leaves_nothing()
	{
		IReadOnlyList<ArtccBoundaryRing> rings = [Ring("ZOB")];

		IReadOnlyList<ArtccBoundaryRing> kept = ArtccBoundaryFilter.ByLocation(rings, ["ZAK"]);

		Assert.Empty(kept);
	}

	[Fact]
	public void by_location_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => ArtccBoundaryFilter.ByLocation(null!, []));
		Assert.Throws<ArgumentNullException>(() => ArtccBoundaryFilter.ByLocation([], null!));
	}
}
