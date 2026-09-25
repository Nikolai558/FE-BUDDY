using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Navaids.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Navaids;

/// <summary>
/// Runs the whole NAVAIDs pipeline (<see cref="NavaidService.Run"/>): the built/GeoJSON counts,
/// that <c>ExcludedTypes</c> leaves a type out of every output while the ROI narrows the GeoJSON
/// only (the alias file still covers everything included), and the advisory messages when there is
/// nothing to write.
/// </summary>
public sealed class NavaidServiceTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_NavaidService_" + Guid.NewGuid().ToString("N"));

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
	public void run_counts_and_writes_geojson_and_alias_for_every_included_navaid()
	{
		NasrCsvDataCollection data = NavaidTestData.Build(NavaidTestData.AllSampleRows());

		NavaidServiceResult result = NavaidService.Run(data, Settings());

		// 11 sample rows, minus one SHUTDOWN and one blank NAV_ID.
		Assert.Equal(9, result.NavaidCount);
		Assert.Equal(9, result.GeojsonNavaidCount);
		Assert.Equal(2, result.GeojsonFilesWritten.Count);
		Assert.Contains(result.GeojsonFilesWritten, f => f.EndsWith("NAVAIDs_Symbols.geojson", StringComparison.Ordinal));
		Assert.Contains(result.GeojsonFilesWritten, f => f.EndsWith("NAVAIDs_Text.geojson", StringComparison.Ordinal));
		Assert.Equal(Path.Combine(_outputDirectory, "NAVAIDs.txt"), result.AliasFilePath);
		Assert.True(result.AliasCommandCount > 0);
	}

	[Fact]
	public void excluded_types_are_left_out_of_every_output_but_the_roi_only_narrows_the_geojson()
	{
		NasrCsvDataCollection data = NavaidTestData.Build([NavaidTestData.CgtRow(), NavaidTestData.ElyRow(), NavaidTestData.FanMarkerRow("OM")]);

		// A box around Chicago only: ELY (Nevada) falls outside it.
		NavaidServiceResult result = NavaidService.Run(data, Settings(
			("ExcludedTypes", "FAN MARKER"),
			("FilterByRoi", "Y"),
			("RoiSwLat", "40.0"), ("RoiSwLon", "-89.0"), ("RoiNeLat", "43.0"), ("RoiNeLon", "-86.0")));

		// CGT and ELY are included (FAN MARKER excluded entirely); only CGT is inside the ROI.
		Assert.Equal(2, result.NavaidCount);
		Assert.Equal(1, result.GeojsonNavaidCount);

		string aliasContents = File.ReadAllText(result.AliasFilePath!);
		Assert.Contains(".navCGT ", aliasContents);
		Assert.Contains(".navELY ", aliasContents); // outside the ROI, but the alias file is never ROI-filtered
		Assert.DoesNotContain(".navOM ", aliasContents); // excluded entirely
	}

	[Fact]
	public void an_advisory_message_explains_why_nothing_is_in_the_roi()
	{
		NasrCsvDataCollection data = NavaidTestData.Build([NavaidTestData.CgtRow()]);

		// Nowhere near Chicago.
		NavaidServiceResult result = NavaidService.Run(data, Settings(
			("FilterByRoi", "Y"),
			("RoiSwLat", "24.0"), ("RoiSwLon", "-82.0"), ("RoiNeLat", "26.0"), ("RoiNeLon", "-80.0")));

		Assert.Equal(1, result.NavaidCount);
		Assert.Equal(0, result.GeojsonNavaidCount);
		Assert.Empty(result.GeojsonFilesWritten);
		Assert.NotNull(result.AliasFilePath);
		Assert.Contains(result.Messages, m =>
			m.IsAdvisory
			&& m.Text.Contains("No NAVAIDs are inside the region of interest", StringComparison.Ordinal)
			&& m.Text.Contains("alias file still covers", StringComparison.Ordinal));
	}

	[Fact]
	public void a_run_whose_filters_leave_nothing_says_so_without_mentioning_the_alias_file()
	{
		NasrCsvDataCollection data = NavaidTestData.Build([NavaidTestData.FanMarkerRow("OM")]);

		NavaidServiceResult result = NavaidService.Run(data, Settings(("ExcludedTypes", "FAN MARKER")));

		Assert.Equal(0, result.NavaidCount);
		Assert.Equal(0, result.GeojsonNavaidCount);
		Assert.Null(result.AliasFilePath);
		Assert.Contains(result.Messages, m =>
			m.IsAdvisory && m.Text.Contains("No NAVAIDs matched the configured filters", StringComparison.Ordinal));
	}

	[Fact]
	public void run_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => NavaidService.Run(null!, Settings()));
		Assert.Throws<ArgumentNullException>(() => NavaidService.Run(new NasrCsvDataCollection(), null!));
	}

	[Fact]
	public void invalid_settings_errors_propagate_from_run()
	{
		NasrCsvDataCollection data = NavaidTestData.Build([NavaidTestData.CgtRow()]);

		Assert.Throws<ArgumentException>(() => NavaidService.Run(data, Settings(("OutputBy", "Sideways"))));
	}
}
