using FeBuddy.Wpf.Map;
using FeBuddy.Wpf.Map.Models;

namespace FeBuddy.UnitTests.Wpf.Map;

/// <summary>
/// Covers <see cref="GeoJsonReader"/>: every geometry type, vNAS text labels, CRC's defaults
/// features, what is dropped as undrawable, and the short reason a file cannot be drawn.
/// </summary>
public sealed class GeoJsonReaderTests
{
	/// <summary>Anything the map cannot draw is refused with a reason short enough to show beside the file.</summary>
	[Theory]
	[InlineData("{ not json", "Not valid JSON")]
	[InlineData("""{"type":"FeatureCollection","features":[{"type":"Feature","geometry":{"type":"LineString","coordinates":[[500000,4649776],[510000,4659776]]}}]}""", "Coordinates are not longitude and latitude")]
	[InlineData("""{"type":"Point","coordinates":["-100","40"]}""", "Coordinates are not longitude and latitude")]
	[InlineData("""{"type":5,"coordinates":[1,2]}""", "No map features in this file")]
	[InlineData("""{"type":"LineString","coordinates":"x"}""", "No map features in this file")]
	[InlineData("""{"type":"Polygon","coordinates":{"a":1}}""", "No map features in this file")]
	[InlineData("""{"type":"LineString","coordinates":[[-100,40]]}""", "No map features in this file")]
	[InlineData("""{"type":"FeatureCollection","features":[]}""", "No map features in this file")]
	[InlineData("""{"type":"FeatureCollection","features":5}""", "No map features in this file")]
	[InlineData("""[1,2,3]""", "No map features in this file")]
	[InlineData("""{"type":"Feature","geometry":null}""", "No map features in this file")]
	[InlineData("""{"type":"Circle","coordinates":[1,2]}""", "No map features in this file")]
	public void a_file_that_cannot_be_drawn_says_why(string json, string message)
	{
		FormatException ex = Assert.Throws<FormatException>(() => GeoJsonReader.Read(json));

		Assert.Equal(message, ex.Message);
	}

	[Fact]
	public void invalid_json_keeps_the_parser_detail()
	{
		FormatException ex = Assert.Throws<FormatException>(() => GeoJsonReader.Read("{ not json"));

		Assert.IsAssignableFrom<System.Text.Json.JsonException>(ex.InnerException);
	}

	[Fact]
	public void every_geometry_type_is_read()
	{
		IReadOnlyList<MapGeometry> read = GeoJsonReader.Read("""
			{"type":"FeatureCollection","features":[
			  {"type":"Feature","geometry":{"type":"Point","coordinates":[-100,40]}},
			  {"type":"Feature","geometry":{"type":"MultiPoint","coordinates":[[-100,40],[-90,41]]}},
			  {"type":"Feature","geometry":{"type":"LineString","coordinates":[[-100,40],[-90,41,1000]]}},
			  {"type":"Feature","geometry":{"type":"MultiLineString","coordinates":[[[0,0],[1,1]],[[2,2],[3,3]]]}},
			  {"type":"Feature","geometry":{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1],[0,0]]]}},
			  {"type":"Feature","geometry":{"type":"MultiPolygon","coordinates":[[[[0,0],[1,0],[1,1],[0,0]]],[[[5,5],[6,5],[6,6],[5,5]]]]}},
			  {"type":"Feature","geometry":{"type":"GeometryCollection","geometries":[{"type":"Point","coordinates":[1,2]}]}}
			]}
			""");

		Assert.Equal(
			[MapGeometryKind.Point, MapGeometryKind.Point, MapGeometryKind.Line, MapGeometryKind.Line, MapGeometryKind.Polygon, MapGeometryKind.Polygon, MapGeometryKind.Point],
			read.Select(g => g.Kind));
		Assert.Equal(2, read[1].Parts.Count);
		Assert.Equal(new GeoPoint(41, -90), read[2].Parts[0][1]);
		Assert.Equal(2, read[3].Parts.Count);
		Assert.Equal(2, read[5].Parts.Count);
	}

	/// <summary>Points and runs that can't be drawn are dropped; what is left of the geometry is kept.</summary>
	[Fact]
	public void undrawable_points_and_runs_are_dropped()
	{
		IReadOnlyList<MapGeometry> read = GeoJsonReader.Read("""
			{"type":"GeometryCollection","geometries":[
			  {"type":"LineString","coordinates":[[-100,40],[1,"x"],[-90,41],[0,999],[5]]},
			  {"type":"MultiPoint","coordinates":[[-100,40],[1],[-90,41]]},
			  {"type":"Polygon","coordinates":[[[0,0],[1,1]],[[0,0],[1,0],[1,1],[0,0]]]}
			]}
			""");

		Assert.Equal(2, read[0].Parts[0].Count);
		Assert.Equal(2, read[1].Parts.Count);
		Assert.Single(read[2].Parts);
	}

	/// <summary>A vNAS text feature's lines (or plain string) become the point's label.</summary>
	[Theory]
	[InlineData("""["A","B"]""", "A\nB")]
	[InlineData("\"CLE\"", "CLE")]
	[InlineData("7", null)]
	public void a_text_features_lines_are_its_label(string text, string? label)
	{
		MapGeometry point = Assert.Single(GeoJsonReader.Read(
			$$$"""{"type":"Feature","properties":{"text":{{{text}}}},"geometry":{"type":"Point","coordinates":[-100,40]}}"""));

		Assert.Equal(label, point.Label);
	}

	/// <summary>CRC's defaults features carry settings, not map data, so they are skipped.</summary>
	[Theory]
	[InlineData("isLineDefaults")]
	[InlineData("isSymbolDefaults")]
	[InlineData("isTextDefaults")]
	public void crc_defaults_features_are_skipped(string flag)
	{
		IReadOnlyList<MapGeometry> read = GeoJsonReader.Read($$$"""
			{"type":"FeatureCollection","features":[
			  {"type":"Feature","properties":{"{{{flag}}}":true},"geometry":{"type":"Point","coordinates":[0,180]}},
			  {"type":"Feature","properties":{"{{{flag}}}":false},"geometry":{"type":"Point","coordinates":[-100,40]}}
			]}
			""");

		Assert.Equal(new GeoPoint(40, -100), Assert.Single(read).Parts[0][0]);
	}

	/// <summary>
	/// Each shape keeps its feature for a Ctrl + click: the geometry type, its place among the file's
	/// features (a skipped defaults feature still counts) and its properties as written. A
	/// GeometryCollection's shapes share theirs; a bare geometry is a feature of its own.
	/// </summary>
	[Fact]
	public void each_shape_keeps_its_feature()
	{
		IReadOnlyList<MapGeometry> read = GeoJsonReader.Read("""
			{"type":"FeatureCollection","features":[
			  {"type":"Feature","properties":{"isLineDefaults":true},"geometry":{"type":"Point","coordinates":[0,180]}},
			  {"type":"Feature","properties":{"style":"solid","thickness":2},"geometry":{"type":"MultiLineString","coordinates":[[[0,0],[1,1]]]}},
			  {"type":"Feature","geometry":{"type":"GeometryCollection","geometries":[{"type":"Point","coordinates":[1,2]},{"type":"LineString","coordinates":[[0,0],[1,1]]}]}},
			  {"type":"Feature","properties":[1],"geometry":{"type":"Point","coordinates":[3,4]}}
			]}
			""");

		MapFeature line = read[0].Feature!;
		Assert.Equal("MultiLineString", line.Kind);
		Assert.Equal(2, line.Number);
		Assert.True(line.IsFromFile);
		Assert.Equal([new MapProperty("style", "solid"), new MapProperty("thickness", "2")], line.Properties());

		Assert.Same(read[1].Feature, read[2].Feature);
		Assert.Equal("GeometryCollection", read[1].Feature!.Kind);
		Assert.Equal(3, read[1].Feature!.Number);
		Assert.Empty(read[1].Feature!.Properties());
		Assert.Empty(read[3].Feature!.Properties());

		MapFeature bare = Assert.Single(GeoJsonReader.Read("""{"type":"LineString","coordinates":[[0,0],[1,1]]}""")).Feature!;
		Assert.Equal("LineString", bare.Kind);
		Assert.Null(bare.Number);
	}
}
