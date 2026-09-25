using System.Text;

using FeBuddy.Core.Infrastructure.Veram;
using FeBuddy.Core.Infrastructure.Veram.Models;

using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Infrastructure.Veram;

/// <summary>
/// Covers <see cref="VeramGeoMapReader"/> and <see cref="VeramProperties"/>: maps, objects,
/// defaults, elements and their overrides, and what cannot be used. Every GeoMap here is made up.
/// </summary>
public sealed class VeramGeoMapReaderTests
{
	/// <summary>A GeoMapSet wrapped round the given GeoMap content.</summary>
	private static string GeoMapSet(string geoMaps) =>
		"""
		<?xml version="1.0" encoding="utf-8"?>
		<GeoMapSet xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" DefaultMap="CENTER">
		  <BcgMenus><BcgMenu Name="DEFAULT"><Items><BcgMenuItem Label="ONE" /></Items></BcgMenu></BcgMenus>
		  <GeoMaps>
		""" + geoMaps + """
		  </GeoMaps>
		</GeoMapSet>
		""";

	private static VeramGeoMapFile Parse(string xml) =>
		VeramGeoMapReader.Parse(new MemoryStream(Encoding.UTF8.GetBytes(xml)), "TEST.xml");

	private static VeramGeoMapFile ParseObject(string objectContent) => Parse(GeoMapSet($"""
		<GeoMap Name="CENTER" LabelLine1="CENTER" LabelLine2="MAP" BcgMenuName="DEFAULT" FilterMenuName="DEFAULT">
		  <Objects>
		    <GeoMapObject Description="THING" TdmOnly="false">
		{objectContent}
		    </GeoMapObject>
		  </Objects>
		</GeoMap>
		"""));

	[Fact]
	public void reads_maps_objects_defaults_and_every_kind_of_element()
	{
		VeramGeoMapFile file = Parse(GeoMapSet("""
			<GeoMap Name="CENTER">
			  <Objects>
			    <GeoMapObject Description="ZXX BOUNDARY" TdmOnly="true">
			      <LineDefaults Bcg="1" Filters="1, 2" Style="Solid" Thickness="2" />
			      <SymbolDefaults Bcg="3" Filters="3" Style="Vor" Size="1" />
			      <TextDefaults Bcg="4" Filters="4" Size="1" Underline="false" Opaque="true" XOffset="5" YOffset="-5" />
			      <Elements>
			        <Element xsi:type="Line" Filters="" StartLat="40.0" StartLon="-100.0" EndLat="40.5" EndLon="-100.0" />
			        <Element xsi:type="Symbol" Filters="" Lat="40.1" Lon="-100.1" />
			        <Element xsi:type="Text" Filters="" Lat="40.2" Lon="-100.2" Lines="ZXX" />
			      </Elements>
			    </GeoMapObject>
			  </Objects>
			</GeoMap>
			<GeoMap Name="EMPTY" />
			"""));

		Assert.Equal(["CENTER", "EMPTY"], file.Maps.Select(m => m.Name));
		Assert.Empty(file.Maps[1].Objects);
		Assert.Empty(file.Problems);
		Assert.Equal("TEST.xml", file.SourcePath);

		VeramGeoMapObject thing = Assert.Single(file.Maps[0].Objects);
		Assert.Equal("ZXX BOUNDARY", thing.Description);
		Assert.True(thing.TdmOnly);
		Assert.Equal(new VeramProperties { Bcg = 1, Filters = [1, 2], Style = "Solid", Thickness = 2 }, thing.LineDefaults);
		Assert.Equal(new VeramProperties { Bcg = 3, Filters = [3], Style = "Vor", Size = 1 }, thing.SymbolDefaults);
		Assert.Equal(
			new VeramProperties { Bcg = 4, Filters = [4], Size = 1, Underline = false, Opaque = true, XOffset = 5, YOffset = -5 },
			thing.TextDefaults);

		Assert.Equal(3, thing.Elements.Count);
		Assert.Equal(VeramElementKind.Line, thing.Elements[0].Kind);
		Assert.Equal(new Coordinate(-100, 40), thing.Elements[0].Start);
		Assert.Equal(new Coordinate(-100, 40.5), thing.Elements[0].End);
		Assert.Same(VeramProperties.None, thing.Elements[0].Overrides);
		Assert.Equal(VeramElementKind.Symbol, thing.Elements[1].Kind);
		Assert.Null(thing.Elements[1].End);
		Assert.Null(thing.Elements[1].Text);
		Assert.Equal("ZXX", thing.Elements[2].Text);
	}

	[Fact]
	public void element_overrides_are_read_and_repeated_filters_kept_once()
	{
		VeramGeoMapFile file = ParseObject("""
			<Elements>
			  <Element xsi:type="Line" Filters="5,5, 6" Bcg="7" Style="ShortDashed" Thickness="3" StartLat="40" StartLon="-100" EndLat="41" EndLon="-100" />
			</Elements>
			""");

		VeramElement line = Assert.Single(file.Maps[0].Objects[0].Elements);
		Assert.Equal(new VeramProperties { Bcg = 7, Filters = [5, 6], Style = "ShortDashed", Thickness = 3 }, line.Overrides);
	}

	[Fact]
	public void longitudes_past_180_are_wrapped()
	{
		VeramGeoMapFile file = ParseObject("""
			<Elements>
			  <Element xsi:type="Symbol" Lat="13.5" Lon="190" />
			  <Element xsi:type="Symbol" Lat="13.5" Lon="-190" />
			</Elements>
			""");

		IReadOnlyList<VeramElement> elements = file.Maps[0].Objects[0].Elements;
		Assert.Equal(-170, elements[0].Start.X, 9);
		Assert.Equal(170, elements[1].Start.X, 9);
	}

	[Fact]
	public void unusable_elements_and_values_are_reported_and_the_rest_kept()
	{
		VeramGeoMapFile file = ParseObject("""
			<LineDefaults Bcg="one" Filters="1,x" Style=" " Thickness="1" />
			<TextDefaults Underline="maybe" />
			<Elements>
			  <Element xsi:type="Polygon" Lat="40" Lon="-100" />
			  <Element Lat="40" Lon="-100" />
			  <Element xsi:type="Line" StartLat="40" StartLon="-100" EndLat="95" EndLon="-100" />
			  <Element xsi:type="Symbol" Lat="40" />
			  <Element xsi:type="Text" Lat="40" Lon="-100" />
			  <Element xsi:type="Symbol" Lat="40" Lon="-900" />
			</Elements>
			""");

		VeramGeoMapObject thing = file.Maps[0].Objects[0];

		Assert.Equal(new VeramProperties { Thickness = 1 }, thing.LineDefaults);
		Assert.Same(VeramProperties.None, thing.TextDefaults);
		Assert.Null(thing.SymbolDefaults);

		// Only the Text element survives: it has a position, and an empty text is the writer's problem.
		VeramElement text = Assert.Single(thing.Elements);
		Assert.Equal(string.Empty, text.Text);

		Assert.Equal(8, file.Problems.Count);
		Assert.Contains(file.Problems, p => p.Contains("Bcg=\"one\" is not a whole number"));
		Assert.Contains(file.Problems, p => p.Contains("Filters=\"1,x\" is not a list of whole numbers"));
		Assert.Contains(file.Problems, p => p.Contains("Underline=\"maybe\" is not true or false"));
		Assert.Contains(file.Problems, p => p.Contains("type 'Polygon'"));
		Assert.Contains(file.Problems, p => p.Contains("type ''"));
		Assert.Equal(3, file.Problems.Count(p => p.Contains("missing or invalid position")));
	}

	[Fact]
	public void missing_names_default_to_empty_and_stray_text_or_empty_filter_lists_are_ignored()
	{
		VeramGeoMapFile file = Parse(GeoMapSet("""
			<GeoMap>
			  stray text
			  <Objects>
			    <GeoMapObject>
			      <LineDefaults Filters=" , ," />
			      <Elements>
			        <Element xsi:type="Line" StartLat="95" StartLon="-100" EndLat="40" EndLon="-100" />
			        <Element xsi:type="Symbol" Lat="-95" Lon="-100" />
			        <Element xsi:type="Symbol" Lat="40" Lon="west" />
			      </Elements>
			    </GeoMapObject>
			  </Objects>
			</GeoMap>
			"""));

		VeramGeoMap map = Assert.Single(file.Maps);
		Assert.Equal(string.Empty, map.Name);

		VeramGeoMapObject thing = Assert.Single(map.Objects);
		Assert.Equal(string.Empty, thing.Description);
		Assert.False(thing.TdmOnly);
		Assert.Same(VeramProperties.None, thing.LineDefaults);
		Assert.Empty(thing.Elements);
		Assert.Equal(3, file.Problems.Count);
	}

	[Fact]
	public void an_empty_file_is_rejected()
	{
		Assert.Throws<InvalidDataException>(() => Parse(string.Empty));
	}

	[Fact]
	public void an_object_with_no_content_is_kept_empty()
	{
		VeramGeoMapFile file = Parse(GeoMapSet("""
			<GeoMap Name="CENTER"><Objects><GeoMapObject Description="NOTHING" TdmOnly="false" /></Objects></GeoMap>
			"""));

		VeramGeoMapObject nothing = Assert.Single(file.Maps[0].Objects);
		Assert.Empty(nothing.Elements);
		Assert.Null(nothing.LineDefaults);
	}

	[Fact]
	public void content_outside_a_map_or_object_is_ignored()
	{
		VeramGeoMapFile file = Parse(GeoMapSet("""
			<GeoMapObject Description="STRAY" TdmOnly="false" />
			<GeoMap Name="CENTER">
			  <LineDefaults Bcg="1" />
			  <Element xsi:type="Symbol" Lat="40" Lon="-100" />
			  <Objects />
			</GeoMap>
			"""));

		Assert.Empty(Assert.Single(file.Maps).Objects);
		Assert.Empty(file.Problems);
	}

	[Fact]
	public void a_file_that_is_not_a_geomapset_is_rejected()
	{
		InvalidDataException error = Assert.Throws<InvalidDataException>(() => Parse("<Geomaps_Records />"));

		Assert.Contains("not a vERAM GeoMaps file", error.Message);
	}

	[Fact]
	public void a_file_that_is_not_well_formed_is_rejected()
	{
		InvalidDataException error = Assert.Throws<InvalidDataException>(() => Parse("<GeoMapSet><GeoMaps>"));

		Assert.Contains("not well-formed XML", error.Message);
	}

	[Fact]
	public void read_loads_a_file_from_disk()
	{
		string path = Path.Combine(Path.GetTempPath(), $"FeBuddyTests_{Guid.NewGuid():N}.xml");
		File.WriteAllText(path, GeoMapSet("<GeoMap Name=\"CENTER\" />"));

		try
		{
			VeramGeoMapFile file = VeramGeoMapReader.Read(path);

			Assert.Equal(path, file.SourcePath);
			Assert.Single(file.Maps);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[Fact]
	public void bad_arguments_are_rejected()
	{
		Assert.Throws<ArgumentException>(() => VeramGeoMapReader.Read(" "));
		Assert.Throws<ArgumentNullException>(() => VeramGeoMapReader.Parse(null!, "x"));
	}

	[Fact]
	public void properties_compare_by_value_including_their_filters()
	{
		VeramProperties first = new() { Bcg = 1, Filters = [1, 2], Style = "Solid" };
		VeramProperties same = new() { Bcg = 1, Filters = [1, 2], Style = "Solid" };

		Assert.Equal(first, same);
		Assert.Equal(first.GetHashCode(), same.GetHashCode());
		Assert.Equal(new VeramProperties { Bcg = 1 }.GetHashCode(), new VeramProperties { Bcg = 1 }.GetHashCode());
		Assert.NotEqual(first, first with { Filters = [2, 1] });
		Assert.NotEqual(first, first with { Filters = null });
		Assert.NotEqual(new VeramProperties { Filters = [] }, new VeramProperties());
		Assert.False(first.Equals(null));
		Assert.True(VeramProperties.None.IsEmpty);
		Assert.False(first.IsEmpty);
	}
}
