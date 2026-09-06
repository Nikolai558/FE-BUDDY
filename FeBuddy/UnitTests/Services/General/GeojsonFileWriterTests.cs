using FEBuddyLibrary.Configuration;
using FEBuddyLibrary.Services.Airways;
using FEBuddyLibrary.Services.General;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace UnitTests.Services.General;

public class GeojsonFileWriterTests : IDisposable
{
	private readonly string _directory =
		Path.Combine(Path.GetTempPath(), "FEBuddyTests_" + Guid.NewGuid().ToString("N"));

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
