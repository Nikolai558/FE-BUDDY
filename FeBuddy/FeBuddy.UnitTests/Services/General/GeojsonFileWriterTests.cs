using FeBuddy.Core.Configuration;
using FeBuddy.Core.Services.Airac.Airways;
using FeBuddy.Core.Services.General;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Services.General;

public class GeojsonFileWriterTests : IDisposable
{
	private readonly string _directory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_" + Guid.NewGuid().ToString("N"));

	private readonly bool _originalDevMode = DevMode.IsEnabled;

	public void Dispose()
	{
		DevMode.IsEnabled = _originalDevMode;

		if (Directory.Exists(_directory))
		{
			Directory.Delete(_directory, recursive: true);
		}
	}

	private static Feature MakePointFeature() =>
		new(AirwayGeometryBuilder.GeometryFactory.CreatePoint(new Coordinate(-80.0, 40.0)), new AttributesTable());

	[Theory]
	[InlineData(5, "-80.12346")]
	[InlineData(6, "-80.123457")]
	[InlineData(7, "-80.1234568")]
	public void write_rounds_coordinates_to_the_requested_precision(int decimals, string expectedLon)
	{
		FeatureCollection collection = new()
		{
			new Feature(
				AirwayGeometryBuilder.GeometryFactory.CreatePoint(new Coordinate(-80.12345678, 40.0)),
				new AttributesTable()),
		};

		string path = GeojsonFileWriter.Write(collection, renderedFeatureCount: 1, _directory, "Prec.geojson", maxDecimalPlaces: decimals)!;

		string json = File.ReadAllText(path);
		Assert.Contains(expectedLon, json);
	}

	[Fact]
	public void write_does_not_round_when_precision_is_zero_or_less()
	{
		FeatureCollection collection = new()
		{
			new Feature(
				AirwayGeometryBuilder.GeometryFactory.CreatePoint(new Coordinate(-80.12345678, 40.0)),
				new AttributesTable()),
		};

		string path = GeojsonFileWriter.Write(collection, renderedFeatureCount: 1, _directory, "NoPrec.geojson", maxDecimalPlaces: 0)!;

		Assert.Contains("-80.12345678", File.ReadAllText(path));
	}

	[Fact]
	public void write_returns_null_and_writes_nothing_when_rendered_count_is_zero()
	{
		FeatureCollection collection = new() { MakePointFeature() }; // an isDefaults-only file

		string? path = GeojsonFileWriter.Write(collection, renderedFeatureCount: 0, _directory, "Test.geojson");

		Assert.Null(path);
		Assert.False(Directory.Exists(_directory));
	}

	[Fact]
	public void write_creates_the_directory_and_returns_the_full_path()
	{
		FeatureCollection collection = new() { MakePointFeature() };

		string? path = GeojsonFileWriter.Write(collection, renderedFeatureCount: 1, _directory, "Test.geojson");

		Assert.NotNull(path);
		Assert.True(File.Exists(path));
		Assert.Equal(Path.Combine(_directory, "Test.geojson"), path);
	}

	[Fact]
	public void write_produces_single_line_output_when_dev_mode_is_disabled()
	{
		DevMode.IsEnabled = false;
		FeatureCollection collection = new() { MakePointFeature() };

		string? path = GeojsonFileWriter.Write(collection, renderedFeatureCount: 1, _directory, "Test.geojson");

		string content = File.ReadAllText(path!);
		Assert.DoesNotContain('\n', content);
	}

	[Fact]
	public void write_produces_indented_output_when_dev_mode_is_enabled()
	{
		DevMode.IsEnabled = true;
		FeatureCollection collection = new() { MakePointFeature() };

		string? path = GeojsonFileWriter.Write(collection, renderedFeatureCount: 1, _directory, "Test.geojson");

		string content = File.ReadAllText(path!);
		Assert.Contains('\n', content);
	}
}
