using FEBuddyLibrary.Models.NASR.CSV;
using FEBuddyLibrary.Models.Services.Airac.Airways;
using FEBuddyLibrary.Services.Airac.Airways;

using NetTopologySuite.Geometries;

using UnitTests.Services.Airac.Airways.Fixtures;

namespace UnitTests.Services.Airac.Airways;

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
		Assert.Empty(result.Warnings);
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
	/// A trailing unresolved waypoint (nothing usable exists after it - e.g. an airway that
	/// continues past "U.S. CANADIAN BORDER-4") is the normal, expected case: the airway
	/// simply stops at the last resolvable point, with no warning.
	/// </summary>
	[Fact]
	public void trailing_unresolved_waypoint_stops_the_airway_without_a_warning()
	{
		var data = AirwayTestDataBuilder.Build(fixes: new[] { ("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0) });
		List<AirwaySegment> segments = new()
		{
			new("AAAAA", "BBBBB", IsGap: false, MaxAuthAlt: null),
			new("BBBBB", "NOWHERE", IsGap: false, MaxAuthAlt: null), // NOWHERE is never resolvable
		};

		AirwayGeometryBuildResult result = AirwayGeometryBuilder.Build(data, "TEST1", segments);

		LineString lineString = Assert.Single(result.LineStrings);
		Assert.Equal(2, lineString.NumPoints);
		Assert.Empty(result.Warnings);
	}

	/// <summary>
	/// An unresolvable waypoint that occurs mid-airway (usable geometry exists later) is a
	/// warning, not a thrown exception - the segment is skipped and processing continues.
	/// </summary>
	[Fact]
	public void mid_airway_unresolved_waypoint_produces_a_warning_and_continues()
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

		Assert.NotEmpty(result.Warnings);
		Assert.Contains(result.Warnings, w => w.Contains("NOWHERE") && w.Contains("TEST1"));

		// The good pieces on either side of the bad record are still produced as two
		// separate LineStrings, rather than the whole airway being aborted.
		Assert.Equal(2, result.LineStrings.Count);
		Assert.All(result.LineStrings, ls => Assert.Equal(2, ls.NumPoints));
	}
}
