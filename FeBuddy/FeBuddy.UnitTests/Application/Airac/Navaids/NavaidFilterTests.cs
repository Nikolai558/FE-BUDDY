using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Domain.Navaids.Models;

using FeBuddy.UnitTests.Application.Airac.Navaids.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Navaids;

/// <summary>
/// Covers <see cref="NavaidFilter.ExcludeTypes"/>: the settings-driven type exclusion that applies
/// to every NAVAIDs output alike (unlike the ROI, which limits GeoJSON only).
/// </summary>
public sealed class NavaidFilterTests
{
	[Fact]
	public void an_empty_excluded_types_collection_keeps_every_navaid_and_returns_the_same_instance()
	{
		IReadOnlyList<Navaid> navaids = [NavaidTestData.Cgt(), NavaidTestData.FanMarker()];

		IReadOnlyList<Navaid> kept = NavaidFilter.ExcludeTypes(navaids, []);

		Assert.Same(navaids, kept);
	}

	[Fact]
	public void an_excluded_type_removes_every_navaid_of_that_type()
	{
		IReadOnlyList<Navaid> navaids = [NavaidTestData.Cgt(), NavaidTestData.FanMarker(), NavaidTestData.Consolan()];

		IReadOnlyList<Navaid> kept = NavaidFilter.ExcludeTypes(navaids, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "FAN MARKER" });

		Assert.Equal(["CGT", "NY"], kept.Select(n => n.NavId));
	}

	[Fact]
	public void excluded_types_match_ignoring_case_when_given_a_case_insensitive_set()
	{
		IReadOnlyList<Navaid> navaids = [NavaidTestData.Cgt()];

		IReadOnlyList<Navaid> kept = NavaidFilter.ExcludeTypes(navaids, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "vortac" });

		Assert.Empty(kept);
	}

	[Fact]
	public void excluding_a_type_no_navaid_has_changes_nothing()
	{
		IReadOnlyList<Navaid> navaids = [NavaidTestData.Cgt()];

		IReadOnlyList<Navaid> kept = NavaidFilter.ExcludeTypes(navaids, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "TACAN" });

		Assert.Equal(navaids, kept);
	}

	[Fact]
	public void exclude_types_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => NavaidFilter.ExcludeTypes(null!, []));
		Assert.Throws<ArgumentNullException>(() => NavaidFilter.ExcludeTypes([], null!));
	}
}
