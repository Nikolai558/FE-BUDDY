using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Infrastructure.Geojson;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Infrastructure.Geojson;

/// <summary>
/// Covers <see cref="CrcFeatureFactory"/>: the isDefaults Feature for each kind, per-Feature
/// override properties, and the errors for values CRC cannot draw.
/// </summary>
public sealed class CrcFeatureFactoryTests
{
	private static CrcLineDefaults LineDefaults() =>
		new() { Bcg = 2, Filters = [3], Style = "solid", Thickness = 1 };

	private static CrcSymbolDefaults SymbolDefaults() =>
		new() { Bcg = 2, Filters = [3], Style = "vor", Size = 1 };

	private static CrcTextDefaults TextDefaults() =>
		new() { Bcg = 2, Filters = [3], Size = 1, Underline = false, Opaque = false, XOffset = 0, YOffset = 0 };

	private static void AssertIsDefaultsPoint(Feature feature)
	{
		Point point = Assert.IsType<Point>(feature.Geometry);
		Assert.Equal(90.0, point.X);
		Assert.Equal(180.0, point.Y);
	}

	[Fact]
	public void create_default_for_line_uses_the_out_of_range_coordinate_and_sets_the_is_defaults_flag()
	{
		Feature feature = CrcFeatureFactory.CreateDefaultsFeature(LineDefaults());

		AssertIsDefaultsPoint(feature);
		Assert.Equal(true, feature.Attributes["isLineDefaults"]);
		Assert.False(feature.Attributes.Exists("isSymbolDefaults"));
		Assert.False(feature.Attributes.Exists("isTextDefaults"));
	}

	[Fact]
	public void create_default_for_line_writes_every_property_in_a_fixed_order()
	{
		Feature feature = CrcFeatureFactory.CreateDefaultsFeature(LineDefaults());

		string[] expected = ["isLineDefaults", "bcg", "filters", "style", "thickness"];
		Assert.Equal(expected, feature.Attributes.GetNames());
		Assert.Equal(2, feature.Attributes["bcg"]);
		Assert.Equal([3], Assert.IsType<int[]>(feature.Attributes["filters"]));
		Assert.Equal("solid", feature.Attributes["style"]);
		Assert.Equal(1, feature.Attributes["thickness"]);
	}

	[Fact]
	public void create_default_for_symbol_writes_every_property_in_a_fixed_order()
	{
		Feature feature = CrcFeatureFactory.CreateDefaultsFeature(SymbolDefaults());

		AssertIsDefaultsPoint(feature);
		string[] expected = ["isSymbolDefaults", "bcg", "filters", "style", "size"];
		Assert.Equal(expected, feature.Attributes.GetNames());
		Assert.Equal(true, feature.Attributes["isSymbolDefaults"]);
		Assert.Equal("vor", feature.Attributes["style"]);
		Assert.Equal(1, feature.Attributes["size"]);
	}

	[Fact]
	public void create_default_for_text_writes_every_property_in_a_fixed_order()
	{
		Feature feature = CrcFeatureFactory.CreateDefaultsFeature(TextDefaults());

		AssertIsDefaultsPoint(feature);
		string[] expected = ["isTextDefaults", "bcg", "filters", "size", "underline", "opaque", "xOffset", "yOffset"];
		Assert.Equal(expected, feature.Attributes.GetNames());
	}

	[Fact]
	public void create_default_for_text_never_writes_a_text_attribute()
	{
		Feature feature = CrcFeatureFactory.CreateDefaultsFeature(TextDefaults());

		Assert.False(feature.Attributes.Exists("text"));
	}

	[Fact]
	public void create_default_for_text_writes_flags_and_offsets_even_when_false_or_zero()
	{
		Feature feature = CrcFeatureFactory.CreateDefaultsFeature(TextDefaults());

		Assert.Equal(false, feature.Attributes["underline"]);
		Assert.Equal(false, feature.Attributes["opaque"]);
		Assert.Equal(0, feature.Attributes["xOffset"]);
		Assert.Equal(0, feature.Attributes["yOffset"]);
	}

	[Fact]
	public void create_default_for_text_writes_negative_offsets()
	{
		CrcTextDefaults defaults = TextDefaults() with { Underline = true, Opaque = true, XOffset = -4, YOffset = -7 };

		Feature feature = CrcFeatureFactory.CreateDefaultsFeature(defaults);

		Assert.Equal(true, feature.Attributes["underline"]);
		Assert.Equal(true, feature.Attributes["opaque"]);
		Assert.Equal(-4, feature.Attributes["xOffset"]);
		Assert.Equal(-7, feature.Attributes["yOffset"]);
	}

	[Fact]
	public void create_default_throws_with_all_violations_when_defaults_are_invalid()
	{
		CrcLineDefaults defaults = LineDefaults() with { Filters = [], Bcg = 999 };

		ArgumentException ex = Assert.Throws<ArgumentException>(() =>
			CrcFeatureFactory.CreateDefaultsFeature(defaults));

		Assert.Contains("bcg", ex.Message);
		Assert.Contains("filters", ex.Message);
	}

	[Fact]
	public void create_default_for_text_throws_when_size_is_out_of_range()
	{
		CrcTextDefaults defaults = TextDefaults() with { Size = 9 };

		ArgumentException ex = Assert.Throws<ArgumentException>(() =>
			CrcFeatureFactory.CreateDefaultsFeature(defaults));

		Assert.Contains("size", ex.Message);
	}

	[Fact]
	public void create_override_properties_omits_null_optional_properties_so_they_inherit()
	{
		CrcLineProperties properties = new() { Filters = [1] };

		AttributesTable attributes = CrcFeatureFactory.CreateOverrideProperties(properties);

		Assert.True(attributes.Exists("filters"));
		Assert.False(attributes.Exists("bcg"));
		Assert.False(attributes.Exists("style"));
		Assert.False(attributes.Exists("thickness"));
	}

	[Fact]
	public void create_override_properties_for_a_full_line_writes_every_property()
	{
		CrcLineProperties properties = new() { Bcg = 2, Filters = [3], Style = "solid", Thickness = 1 };

		AttributesTable attributes = CrcFeatureFactory.CreateOverrideProperties(properties);

		Assert.Equal(new[] { "bcg", "filters", "style", "thickness" }, attributes.GetNames());
		Assert.Equal("solid", attributes["style"]);
	}

	[Fact]
	public void create_override_properties_for_a_symbol_writes_only_the_properties_that_are_set()
	{
		AttributesTable full = CrcFeatureFactory.CreateOverrideProperties(new CrcSymbolProperties { Bcg = 2, Filters = [3], Style = "vor", Size = 1 });
		AttributesTable sparse = CrcFeatureFactory.CreateOverrideProperties(new CrcSymbolProperties { Filters = [3] });

		Assert.Equal(new[] { "bcg", "filters", "style", "size" }, full.GetNames());
		Assert.Equal("vor", full["style"]);
		Assert.Equal(new[] { "filters" }, sparse.GetNames());
	}

	[Fact]
	public void create_override_properties_for_text_writes_only_the_properties_that_are_set_in_defaults_order()
	{
		AttributesTable full = CrcFeatureFactory.CreateOverrideProperties(new CrcTextProperties
		{
			Bcg = 2,
			Filters = [3],
			Text = ["SEA", "SEATTLE"],
			Size = 1,
			Underline = true,
			Opaque = false,
			XOffset = 1,
			YOffset = -1,
		});
		AttributesTable sparse = CrcFeatureFactory.CreateOverrideProperties(new CrcTextProperties { Filters = [3], Text = ["SEA"] });

		Assert.Equal(new[] { "bcg", "filters", "text", "size", "underline", "opaque", "xOffset", "yOffset" }, full.GetNames());
		Assert.Equal(new[] { "SEA", "SEATTLE" }, Assert.IsType<string[]>(full["text"]));
		Assert.Equal(true, full["underline"]);
		Assert.Equal(-1, full["yOffset"]);
		Assert.Equal(new[] { "filters", "text" }, sparse.GetNames());
	}

	[Fact]
	public void create_override_properties_rejects_values_crc_cannot_draw_and_names_the_context()
	{
		CrcLineProperties properties = new() { Bcg = 999, Filters = [3] };

		ArgumentException ex = Assert.Throws<ArgumentException>(() =>
			CrcFeatureFactory.CreateOverrideProperties(properties));

		Assert.StartsWith("Invalid CRC Line properties for feature override:", ex.Message, StringComparison.Ordinal);
		Assert.Equal("properties", ex.ParamName);
	}
}
