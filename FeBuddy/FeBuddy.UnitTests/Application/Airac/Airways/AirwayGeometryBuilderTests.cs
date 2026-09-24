using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Domain.Airways.Models;

using NetTopologySuite.Geometries;

using FeBuddy.UnitTests.Application.Airac.Airways.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Airways;

public class AirwayGeometryBuilderTests
{
	/// <summary>
	/// GeoJSON coordinate order is [longitude, latitude]. NASR data (and this test fixture)
	/// is (lat, lon); mixing the two up is the most common bug in this codebase, so it gets
	/// its own explicit test.
	/// </summary>
	[Fact]
	public void geojson_uses_longitude_then_latitude()
	{
		var data = AirwayTestDataBuilder.Build(fixes: new[] { ("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0) });
		List<AirwaySegment> segments = new() { new("AAAAA", "BBBBB", IsGap: false, MaxAuthAlt: null) };

		AirwayGeometryBuildResult result = AirwayGeometryBuilder.Build(data, "TEST1", segments);

		LineString lineString = Assert.Single(result.LineStrings);
		Coordinate first = lineString.Coordinates[0];
		Assert.Equal(-80.0, first.X); // X = longitude
		Assert.Equal(40.0, first.Y);  // Y = latitude
	}

	[Fact]
	public void one_valid_segment_produces_one_linestring()
	{
		var data = AirwayTestDataBuilder.Build(fixes: new[] { ("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0) });
		List<AirwaySegment> segments = new() { new("AAAAA", "BBBBB", IsGap: false, MaxAuthAlt: null) };

		AirwayGeometryBuildResult result = AirwayGeometryBuilder.Build(data, "TEST1", segments);

		Assert.Single(result.LineStrings);
		Assert.Empty(result.Messages.WarningTexts());
	}

	[Fact]
	public void two_continuous_segments_become_one_linestring()
	{
		var data = AirwayTestDataBuilder.Build(fixes: new[]
		{
			("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0), ("CCCCC", 42.0, -82.0)
		});
		List<AirwaySegment> segments = new()
		{
			new("AAAAA", "BBBBB", IsGap: false, MaxAuthAlt: null),
			new("BBBBB", "CCCCC", IsGap: false, MaxAuthAlt: null),
		};

		AirwayGeometryBuildResult result = AirwayGeometryBuilder.Build(data, "TEST1", segments);

		LineString lineString = Assert.Single(result.LineStrings);
		Assert.Equal(3, lineString.NumPoints);
	}

	[Fact]
	public void a_gap_flag_splits_the_airway_into_two_linestrings()
	{
		var data = AirwayTestDataBuilder.Build(fixes: new[]
		{
			("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0), ("CCCCC", 42.0, -82.0)
		});
		List<AirwaySegment> segments = new()
		{
			new("AAAAA", "BBBBB", IsGap: false, MaxAuthAlt: null),
			new("BBBBB", "CCCCC", IsGap: true, MaxAuthAlt: null),
		};

		AirwayGeometryBuildResult result = AirwayGeometryBuilder.Build(data, "TEST1", segments);

		Assert.Equal(2, result.LineStrings.Count);
	}

	/// <summary>
	/// Post-3.2b, border crossings are normalized away before geometry building, so a
	/// remaining trailing unresolvable waypoint is a genuine data fault: it is recorded in
	/// <see cref="AirwayGeometryBuildResult.UnresolvedWaypointIds"/> so <c>AirwayBuilder</c>
	/// excludes the whole airway (remediation plan 3.2a).
	/// </summary>
	[Fact]
	public void trailing_unresolved_waypoint_marks_the_airway_for_exclusion()
	{
		var data = AirwayTestDataBuilder.Build(fixes: new[] { ("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0) });
		List<AirwaySegment> segments = new()
		{
			new("AAAAA", "BBBBB", IsGap: false, MaxAuthAlt: null),
			new("BBBBB", "NOWHERE", IsGap: false, MaxAuthAlt: null), // NOWHERE is never resolvable
		};

		AirwayGeometryBuildResult result = AirwayGeometryBuilder.Build(data, "TEST1", segments);

		Assert.Contains("NOWHERE", result.UnresolvedWaypointIds);
		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains("NOWHERE") && w.Contains("excluded from all output"));
	}

	/// <summary>
	/// A mid-airway unresolvable waypoint is recorded for exclusion too, and collecting every
	/// bad ID (not aborting on the first) lets <c>AirwayBuilder</c> report them all.
	/// </summary>
	[Fact]
	public void mid_airway_unresolved_waypoint_is_recorded_for_exclusion_and_processing_continues()
	{
		var data = AirwayTestDataBuilder.Build(fixes: new[]
		{
			("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0),
			("CCCCC", 42.0, -82.0), ("DDDDD", 43.0, -83.0)
		});
		List<AirwaySegment> segments = new()
		{
			new("AAAAA", "BBBBB", IsGap: false, MaxAuthAlt: null),
			// NOWHERE never resolves, but a fully-resolvable segment (CCCCC->DDDDD) exists
			// later, so this is a genuine mid-airway problem, not trailing truncation.
			new("BBBBB", "NOWHERE", IsGap: false, MaxAuthAlt: null),
			new("CCCCC", "DDDDD", IsGap: false, MaxAuthAlt: null),
		};

		AirwayGeometryBuildResult result = AirwayGeometryBuilder.Build(data, "TEST1", segments);

		Assert.Contains("NOWHERE", result.UnresolvedWaypointIds);
		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains("NOWHERE") && w.Contains("TEST1"));

		// Processing kept going past the bad record (the CCCCC->DDDDD leg was still built),
		// so every unresolved ID on the airway is available for the exclusion summary.
		Assert.NotEmpty(result.LineStrings);
	}
}
