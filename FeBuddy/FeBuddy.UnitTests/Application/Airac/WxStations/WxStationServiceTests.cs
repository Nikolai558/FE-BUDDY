using FeBuddy.Core.Application.Airac.WxStations;
using FeBuddy.Core.Application.Airac.WxStations.Models;
using FeBuddy.Core.Infrastructure.WxStations.Models;

using FeBuddy.UnitTests.Application.Airac.WxStations.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.WxStations;

/// <summary>
/// Runs the whole Wx Stations pipeline (<see cref="WxStationService.Run"/>): the built/GeoJSON
/// counts, that the ROI narrows the GeoJSON output only, the no-files-written advisory, that
/// builder messages flow through, and that missing-data/invalid-settings errors propagate.
/// </summary>
public sealed class WxStationServiceTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_WxStationService_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_outputDirectory))
		{
			Directory.Delete(_outputDirectory, recursive: true);
		}
	}

	private Dictionary<string, string> Settings(params (string Key, string Value)[] overrides)
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = _outputDirectory,
		};

		foreach ((string key, string value) in overrides)
		{
			settings[key] = value;
		}

		return settings;
	}

	[Fact]
	public void run_counts_and_writes_geojson_for_every_included_station()
	{
		WxStationDataCollection data = WxStationTestData.Build(
		[
			WxStationTestData.DtwRow(),
			WxStationTestData.PhxRow(),
			WxStationTestData.CanadianRow(),
			WxStationTestData.NoIcaoRow(),
		]);

		WxStationServiceResult result = WxStationService.Run(data, Settings());

		Assert.Equal(4, result.TotalStationCount);
		Assert.Equal(2, result.StationCount);
		Assert.Equal(2, result.GeojsonStationCount);
		Assert.Equal(2, result.GeojsonFilesWritten.Count);
		Assert.Contains(result.GeojsonFilesWritten, f => f.EndsWith("Wx_Symbols.geojson", StringComparison.Ordinal));
		Assert.Contains(result.GeojsonFilesWritten, f => f.EndsWith("Wx_Text.geojson", StringComparison.Ordinal));
		Assert.Equal(result.GeojsonFilesWritten.Count, result.GeojsonFeatureCountsByFile.Count);
	}

	[Fact]
	public void the_roi_narrows_the_geojson_count_but_not_the_station_count()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.DtwRow(), WxStationTestData.PhxRow()]);

		// A box around KDTW (42.212, -83.353) only - not KPHX (33.434, -112.012).
		WxStationServiceResult result = WxStationService.Run(data, Settings(
			("FilterByRoi", "Y"),
			("RoiSwLat", "40.0"), ("RoiSwLon", "-85.0"), ("RoiNeLat", "44.0"), ("RoiNeLon", "-81.0")));

		Assert.Equal(2, result.StationCount);
		Assert.Equal(1, result.GeojsonStationCount);
	}

	[Fact]
	public void an_advisory_message_explains_when_nothing_was_written()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.DtwRow()]);

		// An ROI that excludes every station is the only way to survive building but leave the run
		// with nothing written.
		WxStationServiceResult result = WxStationService.Run(data, Settings(
			("FilterByRoi", "Y"),
			("RoiSwLat", "0.0"), ("RoiSwLon", "0.0"), ("RoiNeLat", "1.0"), ("RoiNeLon", "1.0")));

		Assert.Equal(1, result.StationCount);
		Assert.Equal(0, result.GeojsonStationCount);
		Assert.Empty(result.GeojsonFilesWritten);
		Assert.Contains(result.Messages, m =>
			m.IsAdvisory && m.Text.Contains("No weather stations matched your filters", StringComparison.Ordinal));
	}

	[Fact]
	public void builder_messages_flow_into_the_result()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.PlaceholderCoordinatesRow("KPLHD")]);

		WxStationServiceResult result = WxStationService.Run(data, Settings());

		Assert.Contains(result.Messages, m => m.Text.Contains("KPLHD", StringComparison.Ordinal));
	}

	[Fact]
	public void run_rejects_a_null_settings_argument() =>
		Assert.Throws<ArgumentNullException>(() => WxStationService.Run(WxStationTestData.Build(), null!));

	[Fact]
	public void run_throws_when_wx_station_data_was_never_downloaded() =>
		Assert.Throws<InvalidOperationException>(() => WxStationService.Run(null, Settings()));

	[Fact]
	public void invalid_settings_errors_propagate_from_run()
	{
		WxStationDataCollection data = WxStationTestData.Build([WxStationTestData.DtwRow()]);

		Assert.Throws<ArgumentException>(() => WxStationService.Run(data, Settings(("EmitSymbols", "N"), ("EmitText", "N"))));
	}
}
