using FEBuddyLibrary.Models.Geojson;
using FEBuddyLibrary.Services.General;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace UnitTests.Services.General;

public class CrcEramPropertyHandlerTests
{
	[Fact]
	public void CreateDefault_uses_the_out_of_range_coordinate_and_sets_the_is_defaults_flag()
	{
		CrcLineProperties properties = new() { Filters = new[] { 3 }, Bcg = 2, Style = "solid", Thickness = 1 };

		Feature feature = CrcEramPropertyHandler.CreateDefault(CrcFeatureKind.Line, properties);

		Point point = Assert.IsType<Point>(feature.Geometry);
		Assert.Equal(90.0, point.X);
		Assert.Equal(180.0, point.Y);
		Assert.Equal(true, feature.Attributes["isLineDefaults"]);
		Assert.False(feature.Attributes.Exists("isSymbolDefaults"));
	}

	[Fact]
	public void CreateDefault_for_text_always_includes_opaque_even_when_unset()
	{
		CrcTextProperties properties = new() { Filters = new[] { 1 }, Text = new[] { "PLACEHOLDER" } };

		Feature feature = CrcEramPropertyHandler.CreateDefault(CrcFeatureKind.Text, properties);

		Assert.True(feature.Attributes.Exists("opaque"));
		Assert.Equal(false, feature.Attributes["opaque"]);
	}

	[Fact]
	public void CreateDefault_throws_with_all_violations_when_properties_are_invalid()
	{
		CrcLineProperties properties = new() { Filters = Array.Empty<int>(), Bcg = 999 };

		ArgumentException ex = Assert.Throws<ArgumentException>(() =>
			CrcEramPropertyHandler.CreateDefault(CrcFeatureKind.Line, properties));

		Assert.Contains("bcg", ex.Message);
		Assert.Contains("filters", ex.Message);
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
