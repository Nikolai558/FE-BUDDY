using FeBuddy.Core.Domain.Geo;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Infrastructure.Geojson;

/// <summary>
/// Efficient symbol group handling: the symbol Features of a file that carry exactly the same
/// properties become one MultiPoint Feature, which CRC draws as the same symbol at every point.
/// A file of 2,000 airport symbols with nothing but the file's isDefaults look is then one Feature.
/// </summary>
/// <remarks>
/// <para>
/// Only a symbol is grouped: a Point Feature that is neither Text (it has a <c>text</c> property)
/// nor an isDefaults Feature. Lines, labels and isDefaults Features pass through untouched, in
/// their place. A group takes the place of its first symbol, its points keep the order they came
/// in, and a point repeated in a group is drawn once. A symbol that shares its properties with no
/// other stays a Point.
/// </para>
/// <para>
/// Properties match as <see cref="AttributesSignature"/> says: the same names and values, in any
/// order. So a property unique to each point, such as <c>feb.navId</c>, leaves every symbol a
/// Feature of its own.
/// </para>
/// </remarks>
public static class SymbolFeatureMerger
{
	private static readonly string[] DefaultsFlags = ["isLineDefaults", "isSymbolDefaults", "isTextDefaults"];

	/// <summary>Groups the symbols in <paramref name="features"/> that carry the same properties.</summary>
	/// <param name="features">A file's Features, in order.</param>
	/// <returns>The Features to write: the same, with each group of matching symbols as one Feature.</returns>
	public static FeatureCollection Merge(IEnumerable<IFeature> features)
	{
		ArgumentNullException.ThrowIfNull(features);

		List<IFeature> output = [];
		Dictionary<string, Group> groups = new(StringComparer.Ordinal);

		foreach (IFeature feature in features)
		{
			if (feature.Geometry is not Point point || !IsSymbol(feature.Attributes))
			{
				output.Add(feature);
				continue;
			}

			string key = AttributesSignature.Of(feature.Attributes);

			if (groups.TryGetValue(key, out Group? group))
			{
				group.Add(point.Coordinate);
				continue;
			}

			group = new Group(output.Count, feature);
			group.Add(point.Coordinate);
			groups.Add(key, group);
			output.Add(feature);
		}

		foreach (Group group in groups.Values.Where(group => group.Points.Count > 1))
		{
			output[group.Index] = new Feature(
				Wgs84.Factory.CreateMultiPointFromCoords([.. group.Points]),
				group.First.Attributes);
		}

		FeatureCollection merged = [];

		foreach (IFeature feature in output)
		{
			merged.Add(feature);
		}

		return merged;
	}

	private static bool IsSymbol(IAttributesTable? attributes) =>
		attributes is null || (!attributes.Exists("text") && !DefaultsFlags.Any(attributes.Exists));

	/// <summary>One set of matching symbols: where it goes in the file, its first Feature and its distinct points.</summary>
	private sealed class Group(int index, IFeature first)
	{
		private readonly HashSet<(double X, double Y)> _seen = [];

		public int Index { get; } = index;

		public IFeature First { get; } = first;

		public List<Coordinate> Points { get; } = [];

		public void Add(Coordinate coordinate)
		{
			if (_seen.Add((coordinate.X, coordinate.Y)))
			{
				Points.Add(coordinate.Copy());
			}
		}
	}
}
