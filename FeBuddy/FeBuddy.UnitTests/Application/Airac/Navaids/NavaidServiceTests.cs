using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Navaids.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Navaids;

/// <summary>
/// Runs the whole NAVAIDs pipeline (<see cref="NavaidService.Run"/>): the built/GeoJSON counts,
/// that <c>ExcludedTypes</c> and the ROI narrow the GeoJSON only (the alias file covers every
/// NAVAID), and the advisory messages when there is nothing to write.
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
		Assert.Equal(Path.Combine(_outputDirectory, "Aliases", "Navaids.txt"), result.AliasFilePath);
		Assert.True(result.AliasCommandCount > 0);
	}

	[Fact]
	public void excluded_types_and_the_roi_narrow_the_geojson_only()
	{
		NasrCsvDataCollection data = NavaidTestData.Build([NavaidTestData.CgtRow(), NavaidTestData.ElyRow(), NavaidTestData.FanMarkerRow("OM")]);

		// A box around Chicago only: ELY (Nevada) falls outside it.
		NavaidServiceResult result = NavaidService.Run(data, Settings(
			("ExcludedTypes", "FAN MARKER"),
			("FilterByRoi", "Y"),
			("RoiSwLat", "40.0"), ("RoiSwLon", "-89.0"), ("RoiNeLat", "43.0"), ("RoiNeLon", "-86.0")));

		// CGT and ELY are left for the GeoJSON (FAN MARKER excluded); only CGT is inside the ROI.
		Assert.Equal(2, result.NavaidCount);
		Assert.Equal(1, result.GeojsonNavaidCount);

		string aliasContents = File.ReadAllText(result.AliasFilePath!);
		Assert.Contains(".navCGT ", aliasContents);
		Assert.Contains(".navELY ", aliasContents); // outside the ROI, but the alias file is never ROI-filtered
		Assert.Contains(".navOM ", aliasContents); // an excluded type, but the alias file has every type
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
	public void a_run_whose_filters_leave_no_geojson_says_so_and_still_writes_the_alias_file()
	{
		NasrCsvDataCollection data = NavaidTestData.Build([NavaidTestData.FanMarkerRow("OM")]);

		NavaidServiceResult result = NavaidService.Run(data, Settings(("ExcludedTypes", "FAN MARKER")));

		Assert.Equal(0, result.NavaidCount);
		Assert.Equal(0, result.GeojsonNavaidCount);
		Assert.NotNull(result.AliasFilePath);
		Assert.Contains(result.Messages, m =>
			m.IsAdvisory
			&& m.Text.Contains("No NAVAIDs matched the configured filters", StringComparison.Ordinal)
			&& m.Text.Contains("alias file still covers every NAVAID", StringComparison.Ordinal));
	}

	[Fact]
	public void with_no_alias_file_the_advisory_leaves_it_out()
	{
		NasrCsvDataCollection data = NavaidTestData.Build([NavaidTestData.FanMarkerRow("OM")]);

		NavaidServiceResult result = NavaidService.Run(data, Settings(("ExcludedTypes", "FAN MARKER"), ("GenerateAliasFile", "N")));

		Assert.Null(result.AliasFilePath);
		Assert.Contains(result.Messages, m =>
			m.IsAdvisory && m.Text.Contains("No NAVAIDs matched the configured filters", StringComparison.Ordinal));
		Assert.DoesNotContain(result.Messages, m => m.Text.Contains("alias file", StringComparison.Ordinal));
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

	[Fact]
	public void a_renamed_geojson_file_and_alias_file_are_written_under_their_new_names()
	{
		NasrCsvDataCollection data = NavaidTestData.Build(NavaidTestData.AllSampleRows());

		OutputFileNames fileNames = new(new Dictionary<string, string>
		{
			["NAVAIDs_Symbols"] = "ZOB Navaid Symbols",
			["Navaids.txt"] = "ZOB Navaids",
		});

		NavaidServiceResult result = NavaidService.Run(data, Settings(), fileNames);

		string renamedSymbols = Assert.Single(result.GeojsonFilesWritten, f => f.EndsWith("ZOB Navaid Symbols.geojson", StringComparison.Ordinal));
		Assert.True(File.Exists(renamedSymbols));
		Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(renamedSymbols)!, "NAVAIDs_Symbols.geojson")));

		Assert.Equal(Path.Combine(_outputDirectory, "Aliases", "ZOB Navaids.txt"), result.AliasFilePath);
		Assert.True(File.Exists(result.AliasFilePath!));
		Assert.False(File.Exists(Path.Combine(_outputDirectory, "Aliases", "Navaids.txt")));
	}
}
