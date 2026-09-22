using FeBuddy.Core.Models.Geojson;
using FeBuddy.Core.Services.General;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Services.General;

public class CrcEramPropertyHandlerTests
{
	private static CrcLineDefaults LineDefaults() =>
		new() { Bcg = 2, Filters = new[] { 3 }, Style = "solid", Thickness = 1 };

	private static CrcSymbolDefaults SymbolDefaults() =>
		new() { Bcg = 2, Filters = new[] { 3 }, Style = "vor", Size = 1 };

	private static CrcTextDefaults TextDefaults() =>
		new() { Bcg = 2, Filters = new[] { 3 }, Size = 1, Underline = false, Opaque = false, XOffset = 0, YOffset = 0 };

	private static void AssertIsDefaultsPoint(Feature feature)
	{
		Point point = Assert.IsType<Point>(feature.Geometry);
		Assert.Equal(90.0, point.X);
		Assert.Equal(180.0, point.Y);
	}

	[Fact]
	public void CreateDefault_for_line_uses_the_out_of_range_coordinate_and_sets_the_is_defaults_flag()
	{
		Feature feature = CrcEramPropertyHandler.CreateDefault(LineDefaults());

		AssertIsDefaultsPoint(feature);
		Assert.Equal(true, feature.Attributes["isLineDefaults"]);
		Assert.False(feature.Attributes.Exists("isSymbolDefaults"));
		Assert.False(feature.Attributes.Exists("isTextDefaults"));
	}

	[Fact]
	public void CreateDefault_for_line_writes_every_property_in_a_fixed_order()
	{
		Feature feature = CrcEramPropertyHandler.CreateDefault(LineDefaults());

		string[] expected = { "isLineDefaults", "bcg", "filters", "style", "thickness" };
		Assert.Equal(expected, feature.Attributes.GetNames());
		Assert.Equal(2, feature.Attributes["bcg"]);
		Assert.Equal(new[] { 3 }, Assert.IsType<int[]>(feature.Attributes["filters"]));
		Assert.Equal("solid", feature.Attributes["style"]);
		Assert.Equal(1, feature.Attributes["thickness"]);
	}

	[Fact]
	public void CreateDefault_for_symbol_writes_every_property_in_a_fixed_order()
	{
		Feature feature = CrcEramPropertyHandler.CreateDefault(SymbolDefaults());

		AssertIsDefaultsPoint(feature);
		string[] expected = { "isSymbolDefaults", "bcg", "filters", "style", "size" };
		Assert.Equal(expected, feature.Attributes.GetNames());
		Assert.Equal(true, feature.Attributes["isSymbolDefaults"]);
		Assert.Equal("vor", feature.Attributes["style"]);
		Assert.Equal(1, feature.Attributes["size"]);
	}

	[Fact]
	public void CreateDefault_for_text_writes_every_property_in_a_fixed_order()
	{
		Feature feature = CrcEramPropertyHandler.CreateDefault(TextDefaults());

		AssertIsDefaultsPoint(feature);
		string[] expected = { "isTextDefaults", "bcg", "filters", "size", "underline", "opaque", "xOffset", "yOffset" };
		Assert.Equal(expected, feature.Attributes.GetNames());
	}

	[Fact]
	public void CreateDefault_for_text_never_writes_a_text_attribute()
	{
		Feature feature = CrcEramPropertyHandler.CreateDefault(TextDefaults());

		Assert.False(feature.Attributes.Exists("text"));
	}

	[Fact]
	public void CreateDefault_for_text_writes_flags_and_offsets_even_when_false_or_zero()
	{
		Feature feature = CrcEramPropertyHandler.CreateDefault(TextDefaults());

		Assert.Equal(false, feature.Attributes["underline"]);
		Assert.Equal(false, feature.Attributes["opaque"]);
		Assert.Equal(0, feature.Attributes["xOffset"]);
		Assert.Equal(0, feature.Attributes["yOffset"]);
	}

	[Fact]
	public void CreateDefault_for_text_writes_negative_offsets()
	{
		CrcTextDefaults defaults = TextDefaults() with { Underline = true, Opaque = true, XOffset = -4, YOffset = -7 };

		Feature feature = CrcEramPropertyHandler.CreateDefault(defaults);

		Assert.Equal(true, feature.Attributes["underline"]);
		Assert.Equal(true, feature.Attributes["opaque"]);
		Assert.Equal(-4, feature.Attributes["xOffset"]);
		Assert.Equal(-7, feature.Attributes["yOffset"]);
	}

	[Fact]
	public void CreateDefault_throws_with_all_violations_when_defaults_are_invalid()
	{
		CrcLineDefaults defaults = LineDefaults() with { Filters = Array.Empty<int>(), Bcg = 999 };

		ArgumentException ex = Assert.Throws<ArgumentException>(() =>
			CrcEramPropertyHandler.CreateDefault(defaults));

		Assert.Contains("bcg", ex.Message);
		Assert.Contains("filters", ex.Message);
	}

	[Fact]
	public void CreateDefault_for_text_throws_when_size_is_out_of_range()
	{
		CrcTextDefaults defaults = TextDefaults() with { Size = 9 };

		ArgumentException ex = Assert.Throws<ArgumentException>(() =>
			CrcEramPropertyHandler.CreateDefault(defaults));

		Assert.Contains("size", ex.Message);
	}

	[Fact]
	public void CreateFeatureProperty_omits_null_optional_properties_so_they_inherit()
	{
		CrcLineProperties properties = new() { Filters = new[] { 1 } };

		AttributesTable attributes = CrcEramPropertyHandler.CreateFeatureProperty(CrcFeatureKind.Line, properties);

		Assert.True(attributes.Exists("filters"));
		Assert.False(attributes.Exists("bcg"));
		Assert.False(attributes.Exists("style"));
		Assert.False(attributes.Exists("thickness"));
	}

	[Fact]
	public void CreateFeatureProperty_throws_for_a_type_mismatched_with_the_feature_kind()
	{
		CrcLineProperties lineProperties = new() { Filters = new[] { 1 } };

		Assert.Throws<ArgumentException>(() =>
			CrcEramPropertyHandler.CreateFeatureProperty(CrcFeatureKind.Symbol, lineProperties));
	}
}
