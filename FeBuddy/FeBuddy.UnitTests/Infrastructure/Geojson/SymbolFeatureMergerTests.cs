using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.Geojson;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Infrastructure.Geojson;

/// <summary>
/// Covers <see cref="SymbolFeatureMerger"/>: symbols carrying the same properties become one
/// MultiPoint Feature in the place of the first, and nothing else is touched - not a label, not an
/// isDefaults Feature, not a line, and not a symbol with properties of its own.
/// </summary>
public sealed class SymbolFeatureMergerTests
{
	private static Feature Symbol(double lat, double lon, params (string Name, object? Value)[] properties) =>
		new(Wgs84.Point(lat, lon), Table(properties));

	private static AttributesTable Table(params (string Name, object? Value)[] properties)
	{
		AttributesTable table = [];
		foreach ((string name, object? value) in properties)
		{
			table.Add(name, value);
		}

		return table;
	}

	[Fact]
	public void symbols_with_the_same_properties_become_one_multipoint_with_every_point_in_order()
	{
		FeatureCollection merged = SymbolFeatureMerger.Merge(
		[
			Symbol(41, -81, ("style", "vor")),
			Symbol(42, -82, ("style", "vor")),
			Symbol(43, -83, ("style", "vor")),
		]);

		IFeature only = Assert.Single(merged);
		MultiPoint points = Assert.IsType<MultiPoint>(only.Geometry);
		Assert.Equal([(-81.0, 41.0), (-82.0, 42.0), (-83.0, 43.0)], points.Coordinates.Select(c => (c.X, c.Y)));
		Assert.Equal("vor", only.Attributes["style"]);
	}

	[Fact]
	public void symbols_with_different_properties_stay_apart_and_a_lone_one_stays_a_point()
	{
		FeatureCollection merged = SymbolFeatureMerger.Merge(
		[
			Symbol(41, -81, ("style", "vor")),
			Symbol(42, -82, ("style", "ndb")),
			Symbol(43, -83, ("style", "vor")),
		]);

		Assert.Equal(2, merged.Count);
		Assert.IsType<MultiPoint>(merged[0].Geometry);
		Assert.Equal(2, merged[0].Geometry.NumPoints);
		Assert.IsType<Point>(merged[1].Geometry);
		Assert.Equal("ndb", merged[1].Attributes["style"]);
	}

	/// <summary>A property unique to each point, such as an identifier, leaves every symbol on its own.</summary>
	[Fact]
	public void a_property_unique_to_each_symbol_keeps_every_one_a_point()
	{
		FeatureCollection merged = SymbolFeatureMerger.Merge(
		[
			Symbol(41, -81, ("feb.navId", "CGT")),
			Symbol(42, -82, ("feb.navId", "DJB")),
		]);

		Assert.Equal(2, merged.Count);
		Assert.All(merged, feature => Assert.IsType<Point>(feature.Geometry));
	}

	[Fact]
	public void labels_isdefaults_features_and_lines_pass_through_in_their_places()
	{
		Feature defaults = Symbol(180, 90, ("isSymbolDefaults", true), ("bcg", 1), ("filters", new[] { 1 }));
		Feature line = new(Wgs84.Factory.CreateLineString([new Coordinate(-81, 41), new Coordinate(-82, 42)]), Table());
		Feature label = Symbol(41, -81, ("text", new[] { "CGT" }));
		Feature sameLabel = Symbol(42, -82, ("text", new[] { "CGT" }));

		FeatureCollection merged = SymbolFeatureMerger.Merge(
			[defaults, Symbol(41, -81), line, label, Symbol(42, -82), sameLabel]);

		Assert.Equal(5, merged.Count);
		Assert.Same(defaults, merged[0]);
		Assert.IsType<MultiPoint>(merged[1].Geometry);
		Assert.Same(line, merged[2]);
		Assert.Same(label, merged[3]);
		Assert.Same(sameLabel, merged[4]);
	}

	[Fact]
	public void a_point_repeated_in_a_group_is_drawn_once()
	{
		FeatureCollection merged = SymbolFeatureMerger.Merge([Symbol(41, -81), Symbol(41, -81), Symbol(42, -82)]);

		Assert.Equal(2, Assert.Single(merged).Geometry.NumPoints);
	}

	[Fact]
	public void identical_symbols_at_one_spot_leave_one_point()
	{
		Feature first = Symbol(41, -81, ("style", "vor"));

		FeatureCollection merged = SymbolFeatureMerger.Merge([first, Symbol(41, -81, ("style", "vor"))]);

		Assert.Same(first, Assert.Single(merged));
	}

	[Fact]
	public void arrays_match_by_their_elements_and_properties_in_any_order()
	{
		FeatureCollection merged = SymbolFeatureMerger.Merge(
		[
			Symbol(41, -81, ("filters", new[] { 1, 2 }), ("bcg", 3)),
			Symbol(42, -82, ("bcg", 3), ("filters", new[] { 1, 2 })),
			Symbol(43, -83, ("bcg", 3), ("filters", new[] { 2, 1 })),
		]);

		Assert.Equal(2, merged.Count);
		Assert.Equal(2, merged[0].Geometry.NumPoints);
	}

	[Fact]
	public void nothing_in_gives_nothing_out()
	{
		Assert.Empty(SymbolFeatureMerger.Merge([]));
		Assert.Throws<ArgumentNullException>(() => SymbolFeatureMerger.Merge(null!));
	}
}
