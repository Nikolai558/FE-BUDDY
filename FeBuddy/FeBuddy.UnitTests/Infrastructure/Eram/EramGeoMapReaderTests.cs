using System.Text;

using FeBuddy.Core.Infrastructure.Eram;
using FeBuddy.Core.Infrastructure.Eram.Models;

using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Infrastructure.Eram;

/// <summary>
/// Covers <see cref="EramGeoMapReader"/> and <see cref="EramProperties"/>: maps, objects,
/// defaults, lines, symbols (and their labels), text, SAAs, positions, and what cannot be used.
/// Every Geomaps file here is made up.
/// </summary>
public sealed class EramGeoMapReaderTests
{
	/// <summary>A Geomaps_Records file wrapped round the given records.</summary>
	private static string Records(string records) =>
		"""
		<Geomaps_Records xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="Geomaps.xsd">
		 <!-- Local SITE ID : ZXX -->
		""" + records + """
		</Geomaps_Records>
		""";

	private static EramGeoMapFile Parse(string xml) =>
		EramGeoMapReader.Parse(new MemoryStream(Encoding.UTF8.GetBytes(xml)), "TEST.xml");

	/// <summary>One record, <c>ZXXMAP</c>, holding one object of type THING in map group 1.</summary>
	private static EramGeoMapFile ParseObject(string objectContent) => Parse(Records($"""
		<GeoMapRecord>
		  <GeomapId>ZXXMAP</GeomapId>
		  <BCGMenuName>DEFAULT</BCGMenuName>
		  <GeoMapObjectType>
		    <MapObjectType>THING</MapObjectType>
		    <MapGroupId>1</MapGroupId>
		{objectContent}
		  </GeoMapObjectType>
		</GeoMapRecord>
		"""));

	[Fact]
	public void reads_maps_objects_defaults_and_every_kind_of_element()
	{
		EramGeoMapFile file = Parse(Records("""
			<GeoMapRecord>
			  <GeomapId>ZXXMAP</GeomapId>
			  <BCGMenuName>DEFAULT</BCGMenuName>
			  <FilterMenuName>DEFAULT</FilterMenuName>
			  <LabelLine1>ZXX</LabelLine1>
			  <MinLatitude>40000000N</MinLatitude>
			  <GeoMapObjectType>
			    <MapObjectType>SECTOR</MapObjectType>
			    <MapGroupId>4</MapGroupId>
			    <DefaultSymbolProperties>
			      <SymbolStyle>VOR</SymbolStyle>
			      <BCGGroup>3</BCGGroup>
			      <Color>White</Color>
			      <FontSize>1</FontSize>
			      <GeoSymbolFilters><FilterGroup>3</FilterGroup></GeoSymbolFilters>
			    </DefaultSymbolProperties>
			    <DefaultLineProperties>
			      <LineStyle>Solid</LineStyle>
			      <BCGGroup>1</BCGGroup>
			      <Color>White</Color>
			      <Thickness>2</Thickness>
			      <GeoLineFilters><FilterGroup>1</FilterGroup><FilterGroup>2</FilterGroup></GeoLineFilters>
			    </DefaultLineProperties>
			    <TextDefaultProperties>
			      <BCGGroup>4</BCGGroup>
			      <Color>White</Color>
			      <FontSize>1</FontSize>
			      <Underline>false</Underline>
			      <DisplaySetting>true</DisplaySetting>
			      <XPixelOffset>5</XPixelOffset>
			      <YPixelOffset>-5</YPixelOffset>
			      <GeoTextFilters><FilterGroup>4</FilterGroup></GeoTextFilters>
			    </TextDefaultProperties>
			    <GeoMapLine>
			      <LineObjectId>ZXX01</LineObjectId>
			      <StartLatitude>40000000N</StartLatitude>
			      <StartLongitude>100000000W</StartLongitude>
			      <EndLatitude>40300000N</EndLatitude>
			      <EndLongitude>100000000W</EndLongitude>
			      <StartXSpherical>-0.1</StartXSpherical>
			    </GeoMapLine>
			    <GeoMapSymbol>
			      <SymbolId>ZXX</SymbolId>
			      <Latitude>40060000N</Latitude>
			      <Longitude>100060000W</Longitude>
			      <GeoMapText>
			        <GeoTextStrings><TextLine>ZXX</TextLine></GeoTextStrings>
			      </GeoMapText>
			    </GeoMapSymbol>
			    <GeoMapText>
			      <TextObjectId>ZXXLABEL</TextObjectId>
			      <Latitude>40120000N</Latitude>
			      <Longitude>100120000W</Longitude>
			      <GeoTextStrings><TextLine>ZXX</TextLine><TextLine>SECTOR 4  </TextLine></GeoTextStrings>
			    </GeoMapText>
			  </GeoMapObjectType>
			</GeoMapRecord>
			<GeoMapRecord />
			"""));

		Assert.Equal(["ZXXMAP", string.Empty], file.Maps.Select(m => m.Name));
		Assert.Empty(file.Maps[1].Objects);
		Assert.Empty(file.Problems);
		Assert.Equal("TEST.xml", file.SourcePath);

		EramGeoMapObject sector = Assert.Single(file.Maps[0].Objects);
		Assert.Equal("SECTOR", sector.ObjectType);
		Assert.Equal(4, sector.MapGroupId);
		Assert.Equal("SECTOR_4", sector.Name);
		Assert.Equal(new EramProperties { Bcg = 1, Filters = [1, 2], Style = "Solid", Thickness = 2 }, sector.LineDefaults);
		Assert.Equal(new EramProperties { Bcg = 3, Filters = [3], Style = "VOR", Size = 1 }, sector.SymbolDefaults);
		Assert.Equal(
			new EramProperties { Bcg = 4, Filters = [4], Size = 1, Underline = false, XOffset = 5, YOffset = -5 },
			sector.TextDefaults);

		Assert.Equal(
			[EramElementKind.Line, EramElementKind.Symbol, EramElementKind.Text, EramElementKind.Text],
			sector.Elements.Select(e => e.Kind));

		EramElement line = sector.Elements[0];
		Assert.Equal(new Coordinate(-100, 40), line.Start);
		Assert.Equal(new Coordinate(-100, 40.5), line.End);
		Assert.Same(EramProperties.None, line.Overrides);
		Assert.Null(line.TextLines);

		// The symbol's own label sits on the symbol, as it gives no position of its own.
		Assert.Equal(new Coordinate(-100.1, 40.1), sector.Elements[1].Start);
		Assert.Null(sector.Elements[1].End);
		Assert.Equal(sector.Elements[1].Start, sector.Elements[2].Start);
		Assert.Equal(["ZXX"], sector.Elements[2].TextLines!);

		Assert.Equal(["ZXX", "SECTOR 4"], sector.Elements[3].TextLines!);
	}

	[Fact]
	public void an_saa_becomes_its_boundary_lines_and_its_label()
	{
		EramGeoMapFile file = ParseObject("""
			<GeoMapSaa>
			  <SaaID>R0001</SaaID>
			  <Owning_Facility>ZXX</Owning_Facility>
			  <GeoMapSaaAltitude><DisplaySetting>true</DisplaySetting></GeoMapSaaAltitude>
			  <GeoMapSaaLabel>
			    <SaaLabel>R0001</SaaLabel>
			    <FontSize>2</FontSize>
			    <DisplaySetting>false</DisplaySetting>
			    <Latitude>40150000N</Latitude>
			    <Longitude>100150000W</Longitude>
			  </GeoMapSaaLabel>
			  <GeoMapSaaBoundary>
			    <LineStyle>ShortDashed</LineStyle>
			    <GeoSaaLinesSegments>
			      <GeoMapSaaLine>
			        <ModuleID>A</ModuleID><Seq_num>1</Seq_num>
			        <StartLatitude>40000000N</StartLatitude><StartLongitude>100000000W</StartLongitude>
			        <EndLatitude>40300000N</EndLatitude><EndLongitude>100000000W</EndLongitude>
			      </GeoMapSaaLine>
			      <GeoMapSaaLine>
			        <ModuleID>A</ModuleID><Seq_num>2</Seq_num>
			        <StartLatitude>40300000N</StartLatitude><StartLongitude>100000000W</StartLongitude>
			        <EndLatitude>40300000N</EndLatitude><EndLongitude>100300000W</EndLongitude>
			      </GeoMapSaaLine>
			    </GeoSaaLinesSegments>
			  </GeoMapSaaBoundary>
			</GeoMapSaa>
			<GeoMapSaa><SaaID>EMPTY</SaaID></GeoMapSaa>
			<GeoMapSaa>
			  <SaaID>NOLABEL</SaaID>
			  <GeoMapSaaLabel><Latitude>40150000N</Latitude><Longitude>100150000W</Longitude></GeoMapSaaLabel>
			</GeoMapSaa>
			""");

		IReadOnlyList<EramElement> elements = file.Maps[0].Objects[0].Elements;

		Assert.Empty(file.Problems);
		Assert.Equal(
			[EramElementKind.Line, EramElementKind.Line, EramElementKind.Text, EramElementKind.Text],
			elements.Select(e => e.Kind));
		Assert.All(elements.Take(2), segment => Assert.Equal(new EramProperties { Style = "ShortDashed" }, segment.Overrides));
		Assert.Equal(new Coordinate(-100.5, 40.5), elements[1].End);
		Assert.Equal(["R0001"], elements[2].TextLines!);
		Assert.Equal(new EramProperties { Size = 2 }, elements[2].Overrides);

		// A label with no text is kept; that it has nothing to show is the writer's to say.
		Assert.Empty(elements[3].TextLines!);
	}

	[Theory]
	[InlineData("40000000N", 40.0)]
	[InlineData("40300000S", -40.5)]
	[InlineData("40083197N", 40.142213888888889)]
	[InlineData("40596000N", 41.0)]
	[InlineData("90000000N", 90.0)]
	public void latitudes_are_read_as_degrees_minutes_seconds_and_hundredths(string value, double expected)
	{
		Assert.True(EramGeoMapReader.TryParseLatitude(value, out double degrees));
		Assert.Equal(expected, degrees, 9);
	}

	[Theory]
	[InlineData("100000000W", -100.0)]
	[InlineData("100300000E", 100.5)]
	[InlineData("180000000w", -180.0)]
	public void longitudes_are_read_as_degrees_minutes_seconds_and_hundredths(string value, double expected)
	{
		Assert.True(EramGeoMapReader.TryParseLongitude(value, out double degrees));
		Assert.Equal(expected, degrees, 9);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("4000000N")]
	[InlineData("400000000N")]
	[InlineData("40000000E")]
	[InlineData("4O000000N")]
	[InlineData("40600000N")]
	[InlineData("40006100N")]
	[InlineData("40006001N")]
	[InlineData("90000001N")]
	public void bad_latitudes_are_refused(string? value)
	{
		Assert.False(EramGeoMapReader.TryParseLatitude(value, out _));
	}

	[Theory]
	[InlineData("10000000W")]
	[InlineData("100000000N")]
	[InlineData("180000001E")]
	public void bad_longitudes_are_refused(string value)
	{
		Assert.False(EramGeoMapReader.TryParseLongitude(value, out _));
	}

	[Fact]
	public void element_overrides_are_read_and_repeated_filters_kept_once()
	{
		EramGeoMapFile file = ParseObject("""
			<GeoMapLine>
			  <LineObjectId>ZXX01</LineObjectId>
			  <LineStyle>LongDashed</LineStyle>
			  <BCGGroup>7</BCGGroup>
			  <Thickness>3</Thickness>
			  <StartLatitude>40000000N</StartLatitude><StartLongitude>100000000W</StartLongitude>
			  <EndLatitude>41000000N</EndLatitude><EndLongitude>100000000W</EndLongitude>
			  <GeoLineFilters><FilterGroup>5</FilterGroup><FilterGroup>5</FilterGroup><FilterGroup> 6 </FilterGroup></GeoLineFilters>
			</GeoMapLine>
			<GeoMapSymbol>
			  <SymbolStyle>Airport</SymbolStyle>
			  <FontSize>2</FontSize>
			  <Latitude>40000000N</Latitude><Longitude>100000000W</Longitude>
			  <GeoSymbolFilters><FilterGroup>8</FilterGroup></GeoSymbolFilters>
			  <GeoMapText>
			    <FontSize>1</FontSize>
			    <Underline>true</Underline>
			    <XPixelOffset>-2</XPixelOffset>
			    <Latitude>40010000N</Latitude><Longitude>100010000W</Longitude>
			    <GeoTextFilters><FilterGroup>9</FilterGroup></GeoTextFilters>
			    <GeoTextStrings><TextLine>ZXX</TextLine></GeoTextStrings>
			  </GeoMapText>
			</GeoMapSymbol>
			""");

		IReadOnlyList<EramElement> elements = file.Maps[0].Objects[0].Elements;

		Assert.Equal(new EramProperties { Bcg = 7, Filters = [5, 6], Style = "LongDashed", Thickness = 3 }, elements[0].Overrides);
		Assert.Equal(new EramProperties { Filters = [8], Style = "Airport", Size = 2 }, elements[1].Overrides);

		// A symbol's label with a position of its own is drawn there, with its own overrides.
		Assert.Equal(new Coordinate(-(100 + 1 / 60.0), 40 + 1 / 60.0), elements[2].Start);
		Assert.Equal(new EramProperties { Filters = [9], Size = 1, Underline = true, XOffset = -2 }, elements[2].Overrides);
	}

	[Fact]
	public void unusable_elements_and_values_are_reported_and_the_rest_kept()
	{
		EramGeoMapFile file = ParseObject("""
			<DefaultLineProperties>
			  <BCGGroup>one</BCGGroup>
			  <LineStyle> </LineStyle>
			  <Thickness>1</Thickness>
			  <GeoLineFilters><FilterGroup>1</FilterGroup><FilterGroup>x</FilterGroup></GeoLineFilters>
			</DefaultLineProperties>
			<TextDefaultProperties><Underline>maybe</Underline></TextDefaultProperties>
			<GeoMapLine>
			  <StartLatitude>40000000N</StartLatitude><StartLongitude>100000000W</StartLongitude>
			  <EndLatitude>95000000N</EndLatitude><EndLongitude>100000000W</EndLongitude>
			</GeoMapLine>
			<GeoMapSymbol><Latitude>40000000N</Latitude></GeoMapSymbol>
			<GeoMapText><GeoTextStrings><TextLine>NOWHERE</TextLine></GeoTextStrings></GeoMapText>
			<GeoMapText><Latitude>40000000N</Latitude><Longitude>100000000W</Longitude></GeoMapText>
			<GeoMapSaa>
			  <SaaID>R0002</SaaID>
			  <GeoMapSaaLabel><SaaLabel>R0002</SaaLabel><Latitude>north</Latitude><Longitude>100000000W</Longitude></GeoMapSaaLabel>
			</GeoMapSaa>
			<GeoMapPolygon />
			""");

		EramGeoMapObject thing = file.Maps[0].Objects[0];

		Assert.Equal(new EramProperties { Thickness = 1, Filters = [1] }, thing.LineDefaults);
		Assert.Same(EramProperties.None, thing.TextDefaults);
		Assert.Null(thing.SymbolDefaults);

		// Only the text with a position survives: that it has no lines is the writer's to say.
		EramElement text = Assert.Single(thing.Elements);
		Assert.Empty(text.TextLines!);

		Assert.Equal(7, file.Problems.Count);
		Assert.Contains(file.Problems, p => p.Contains("BCGGroup \"one\" is not a whole number"));
		Assert.Contains(file.Problems, p => p.Contains("FilterGroup \"x\" is not a whole number"));
		Assert.Contains(file.Problems, p => p.Contains("Underline \"maybe\" is not true or false"));
		Assert.Contains(file.Problems, p => p.Contains("a GeoMapLine has a missing or invalid position"));
		Assert.Contains(file.Problems, p => p.Contains("a GeoMapSymbol has a missing or invalid position"));
		Assert.Contains(file.Problems, p => p.Contains("a GeoMapText has a missing or invalid position"));
		Assert.Contains(file.Problems, p => p.Contains("SAA R0002's label has a missing or invalid position"));
		Assert.All(file.Problems, p => Assert.StartsWith("Line ", p));
	}

	[Fact]
	public void missing_names_and_groups_and_empty_filter_lists_are_tolerated()
	{
		EramGeoMapFile file = Parse(Records("""
			<GeoMapRecord>
			  <GeomapId />
			  stray text
			  <GeoMapObjectType>
			    <MapObjectType>WAYPOINT</MapObjectType>
			    <DefaultLineProperties><GeoLineFilters /></DefaultLineProperties>
			  </GeoMapObjectType>
			  <GeoMapObjectType />
			</GeoMapRecord>
			"""));

		EramGeoMap map = Assert.Single(file.Maps);
		Assert.Equal(string.Empty, map.Name);
		Assert.Equal(2, map.Objects.Count);

		EramGeoMapObject waypoints = map.Objects[0];
		Assert.Null(waypoints.MapGroupId);
		Assert.Equal("WAYPOINT", waypoints.Name);
		Assert.Same(EramProperties.None, waypoints.LineDefaults);
		Assert.Empty(waypoints.Elements);

		Assert.Equal(string.Empty, map.Objects[1].Name);
		Assert.Empty(file.Problems);
	}

	[Fact]
	public void a_non_numeric_map_group_is_reported()
	{
		EramGeoMapFile file = Parse(Records("""
			<GeoMapRecord><GeomapId>ZXXMAP</GeomapId>
			  <GeoMapObjectType><MapObjectType>AIRWAY</MapObjectType><MapGroupId>three</MapGroupId></GeoMapObjectType>
			</GeoMapRecord>
			"""));

		Assert.Equal("AIRWAY", file.Maps[0].Objects[0].Name);
		Assert.Contains("MapGroupId \"three\" is not a whole number", Assert.Single(file.Problems));
	}

	[Fact]
	public void content_outside_a_record_is_ignored()
	{
		EramGeoMapFile file = Parse(Records("""
			<GeomapId>STRAY</GeomapId>
			<GeoMapObjectType><MapObjectType>STRAY</MapObjectType></GeoMapObjectType>
			<GeoMapRecord><GeomapId>ZXXMAP</GeomapId></GeoMapRecord>
			"""));

		EramGeoMap map = Assert.Single(file.Maps);
		Assert.Equal("ZXXMAP", map.Name);
		Assert.Empty(map.Objects);
	}

	[Fact]
	public void an_empty_file_is_rejected()
	{
		Assert.Throws<InvalidDataException>(() => Parse(string.Empty));
	}

	[Fact]
	public void a_file_that_is_not_a_geomaps_file_is_rejected()
	{
		InvalidDataException error = Assert.Throws<InvalidDataException>(() => Parse("<Airport_Records />"));

		Assert.Contains("is not an ERAM Geomaps file", error.Message);
		Assert.Contains("<Airport_Records>", error.Message);
	}

	[Fact]
	public void a_file_that_is_not_well_formed_is_rejected()
	{
		InvalidDataException error = Assert.Throws<InvalidDataException>(() => Parse("<Geomaps_Records><GeoMapRecord>"));

		Assert.Contains("not well-formed XML", error.Message);
	}

	[Fact]
	public void read_loads_a_file_from_disk_and_tells_a_geomaps_file_from_the_rest_of_an_export()
	{
		string folder = Path.Combine(Path.GetTempPath(), $"FeBuddyTests_{Guid.NewGuid():N}");
		Directory.CreateDirectory(folder);
		string geomaps = Path.Combine(folder, "Geomaps.xml");
		string airport = Path.Combine(folder, "Airport.xml");
		string broken = Path.Combine(folder, "Broken.xml");
		File.WriteAllText(geomaps, Records("<GeoMapRecord><GeomapId>ZXXMAP</GeomapId></GeoMapRecord>"));
		File.WriteAllText(airport, "<Airport_Records />");
		File.WriteAllText(broken, "not xml");

		try
		{
			EramGeoMapFile file = EramGeoMapReader.Read(geomaps);

			Assert.Equal(geomaps, file.SourcePath);
			Assert.Single(file.Maps);

			Assert.True(EramGeoMapReader.IsGeoMapsFile(geomaps));
			Assert.False(EramGeoMapReader.IsGeoMapsFile(airport));
			Assert.False(EramGeoMapReader.IsGeoMapsFile(broken));
			Assert.False(EramGeoMapReader.IsGeoMapsFile(Path.Combine(folder, "Missing.xml")));
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	[Fact]
	public void bad_arguments_are_rejected()
	{
		Assert.Throws<ArgumentException>(() => EramGeoMapReader.Read(" "));
		Assert.Throws<ArgumentNullException>(() => EramGeoMapReader.Parse(null!, "x"));
	}

	[Fact]
	public void properties_compare_by_value_including_their_filters()
	{
		EramProperties first = new() { Bcg = 1, Filters = [1, 2], Style = "Solid" };
		EramProperties same = new() { Bcg = 1, Filters = [1, 2], Style = "Solid" };

		Assert.Equal(first, same);
		Assert.Equal(first.GetHashCode(), same.GetHashCode());
		Assert.Equal(new EramProperties { Bcg = 1 }.GetHashCode(), new EramProperties { Bcg = 1 }.GetHashCode());
		Assert.NotEqual(first, first with { Filters = [2, 1] });
		Assert.NotEqual(first, first with { Filters = null });
		Assert.NotEqual(new EramProperties { Filters = [] }, new EramProperties());
		Assert.False(first.Equals(null));
		Assert.True(EramProperties.None.IsEmpty);
		Assert.False(first.IsEmpty);
	}
}
