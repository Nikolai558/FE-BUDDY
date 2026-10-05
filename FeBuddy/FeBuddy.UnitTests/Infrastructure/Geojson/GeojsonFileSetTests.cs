using System.Text.Json;

using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.Geojson;

using NetTopologySuite.Features;

namespace FeBuddy.UnitTests.Infrastructure.Geojson;

/// <summary>
/// Covers <see cref="GeojsonFileSet"/>: a file's symbols that carry the same properties are
/// grouped as it is written, unless the caller keeps them apart, and the set records how many
/// rendered Features the file really holds.
/// </summary>
[Collection("AppLog")]
public sealed class GeojsonFileSetTests : IDisposable
{
	private readonly string _directory = Path.Combine(Path.GetTempPath(), "FeBuddyTests_FileSet_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_directory))
		{
			Directory.Delete(_directory, recursive: true);
		}
	}

	private static FeatureCollection DefaultsAndThreeSymbols()
	{
		AttributesTable defaults = new() { { "isSymbolDefaults", true }, { "bcg", 1 }, { "filters", new[] { 1 } } };
		return
		[
			new Feature(Wgs84.Point(180, 90), defaults),
			new Feature(Wgs84.Point(41, -81), new AttributesTable()),
			new Feature(Wgs84.Point(42, -82), new AttributesTable()),
			new Feature(Wgs84.Point(43, -83), new AttributesTable()),
		];
	}

	private static int FeatureCount(string path)
	{
		using JsonDocument json = JsonDocument.Parse(File.ReadAllText(path));
		return json.RootElement.GetProperty("features").GetArrayLength();
	}

	[Fact]
	public void matching_symbols_are_written_as_one_feature_and_counted_as_one()
	{
		GeojsonFileSet files = new(coordinatePrecision: 6);

		files.Write(DefaultsAndThreeSymbols(), renderedFeatureCount: 3, _directory, "Symbols.geojson");

		string path = Assert.Single(files.FilesWritten);
		Assert.Equal(2, FeatureCount(path));
		Assert.Equal(1, files.RenderedFeatureCountsByFile[path]);
	}

	[Fact]
	public void symbols_kept_apart_are_written_and_counted_one_by_one()
	{
		GeojsonFileSet files = new(coordinatePrecision: 6);

		files.Write(DefaultsAndThreeSymbols(), renderedFeatureCount: 3, _directory, "Raw.geojson", groupSymbols: false);

		string path = Assert.Single(files.FilesWritten);
		Assert.Equal(4, FeatureCount(path));
		Assert.Equal(3, files.RenderedFeatureCountsByFile[path]);
	}

	[Fact]
	public void a_file_with_nothing_rendered_is_not_written()
	{
		GeojsonFileSet files = new(coordinatePrecision: 6);

		files.Write([.. DefaultsAndThreeSymbols().Take(1)], renderedFeatureCount: 0, _directory, "Empty.geojson");

		Assert.Empty(files.FilesWritten);
		Assert.False(Directory.Exists(_directory));
	}
}
