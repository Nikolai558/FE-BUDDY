using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Geojson;

using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Infrastructure.Geojson;

// In the non-parallel "AppLog" collection because these tests set the process-wide DevMode and
// OutputFormatting statics, which AppLogTests and others also read and set.
[Collection("AppLog")]
public class GeojsonFileWriterTests : IDisposable
{
	private readonly string _directory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_" + Guid.NewGuid().ToString("N"));

	private readonly bool _originalDevMode = DevMode.IsEnabled;
	private readonly bool _originalPrettyPrint = OutputFormatting.PrettyPrintGeojson;

	public void Dispose()
	{
		DevMode.IsEnabled = _originalDevMode;
		OutputFormatting.PrettyPrintGeojson = _originalPrettyPrint;

		if (Directory.Exists(_directory))
		{
			Directory.Delete(_directory, recursive: true);
		}
	}

	private static Feature MakePointFeature() =>
		new(Wgs84.Factory.CreatePoint(new Coordinate(-80.0, 40.0)), new AttributesTable());

	[Theory]
	[InlineData(5, "-80.12346")]
	[InlineData(6, "-80.123457")]
	[InlineData(7, "-80.1234568")]
	public void write_rounds_coordinates_to_the_requested_precision(int decimals, string expectedLon)
	{
		FeatureCollection collection =
		[
			new Feature(
				Wgs84.Factory.CreatePoint(new Coordinate(-80.12345678, 40.0)),
				new AttributesTable()),
		];

		string path = GeojsonFileWriter.Write(collection, renderedFeatureCount: 1, _directory, "Prec.geojson", maxDecimalPlaces: decimals)!;

		string json = File.ReadAllText(path);
		Assert.Contains(expectedLon, json);
	}

	[Fact]
	public void write_does_not_round_when_precision_is_zero_or_less()
	{
		FeatureCollection collection =
		[
			new Feature(
				Wgs84.Factory.CreatePoint(new Coordinate(-80.12345678, 40.0)),
				new AttributesTable()),
		];

		string path = GeojsonFileWriter.Write(collection, renderedFeatureCount: 1, _directory, "NoPrec.geojson", maxDecimalPlaces: 0)!;

		Assert.Contains("-80.12345678", File.ReadAllText(path));
	}

	[Fact]
	public void write_returns_null_and_writes_nothing_when_rendered_count_is_zero()
	{
		FeatureCollection collection = [MakePointFeature()]; // an isDefaults-only file

		string? path = GeojsonFileWriter.Write(collection, renderedFeatureCount: 0, _directory, "Test.geojson");

		Assert.Null(path);
		Assert.False(Directory.Exists(_directory));
	}

	[Fact]
	public void write_creates_the_directory_and_returns_the_full_path()
	{
		FeatureCollection collection = [MakePointFeature()];

		string? path = GeojsonFileWriter.Write(collection, renderedFeatureCount: 1, _directory, "Test.geojson");

		Assert.NotNull(path);
		Assert.True(File.Exists(path));
		Assert.Equal(Path.Combine(_directory, "Test.geojson"), path);
	}

	[Fact]
	public void write_produces_single_line_output_when_dev_mode_is_disabled()
	{
		DevMode.IsEnabled = false;
		OutputFormatting.PrettyPrintGeojson = false;
		FeatureCollection collection = [MakePointFeature()];

		string? path = GeojsonFileWriter.Write(collection, renderedFeatureCount: 1, _directory, "Test.geojson");

		string content = File.ReadAllText(path!);
		Assert.DoesNotContain('\n', content);
	}

	[Fact]
	public void write_produces_indented_output_when_dev_mode_is_enabled()
	{
		DevMode.IsEnabled = true;
		OutputFormatting.PrettyPrintGeojson = false; // dev mode wins over the saved single-line choice
		FeatureCollection collection = [MakePointFeature()];

		string? path = GeojsonFileWriter.Write(collection, renderedFeatureCount: 1, _directory, "Test.geojson");

		string content = File.ReadAllText(path!);
		Assert.Contains('\n', content);
	}

	[Fact]
	public void write_produces_indented_output_when_the_user_chose_pretty_print()
	{
		DevMode.IsEnabled = false;
		OutputFormatting.PrettyPrintGeojson = true;
		FeatureCollection collection = [MakePointFeature()];

		string? path = GeojsonFileWriter.Write(collection, renderedFeatureCount: 1, _directory, "Pretty.geojson");

		string content = File.ReadAllText(path!);
		Assert.Contains('\n', content);
	}

	[Theory]
	[InlineData(false, false, false)]
	[InlineData(false, true, true)]
	[InlineData(true, false, true)]
	[InlineData(true, true, true)]
	public void geojson_is_indented_when_either_dev_mode_or_the_preference_asks_for_it(
		bool devMode, bool prettyPrint, bool expected)
	{
		DevMode.IsEnabled = devMode;
		OutputFormatting.PrettyPrintGeojson = prettyPrint;

		Assert.Equal(expected, OutputFormatting.WriteIndentedGeojson);
	}

	[Theory]
	[InlineData(" ", "file.geojson", "directory")]
	[InlineData("out", " ", "fileName")]
	public void write_rejects_a_blank_directory_or_file_name(string directory, string fileName, string parameter)
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() =>
			GeojsonFileWriter.Write([], 1, directory, fileName));

		Assert.Equal(parameter, ex.ParamName);
	}
}
