using FeBuddy.Core.Application.Airac.Fixes;
using FeBuddy.Core.Application.Airac.Fixes.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Fixes.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Fixes;

/// <summary>
/// Runs the whole Fixes pipeline (<see cref="FixService.Run"/>): the built/GeoJSON counts, that
/// the ROI narrows the GeoJSON output only, the combination-matches-nothing warning, the
/// no-files-written advisory, and that parse errors propagate.
/// </summary>
public sealed class FixServiceTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_FixService_" + Guid.NewGuid().ToString("N"));

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
	public void run_counts_and_writes_geojson_for_every_included_fix()
	{
		NasrCsvDataCollection data = FixTestData.Build(FixTestData.AllSampleRows());

		FixServiceResult result = FixService.Run(data, Settings());

		// 7 sample rows, minus one blank FIX_ID.
		Assert.Equal(6, result.FixCount);
		Assert.Equal(6, result.GeojsonFixCount);
		Assert.Equal(2, result.GeojsonFilesWritten.Count);
		Assert.Contains(result.GeojsonFilesWritten, f => f.EndsWith("Fix_Symbols.geojson", StringComparison.Ordinal));
		Assert.Contains(result.GeojsonFilesWritten, f => f.EndsWith("Fix_Text.geojson", StringComparison.Ordinal));
		Assert.Equal(result.GeojsonFilesWritten.Count, result.GeojsonFeatureCountsByFile.Count);
	}

	[Fact]
	public void the_roi_narrows_the_geojson_count_but_not_the_fix_count()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.AcmeRow(), FixTestData.BravoRow()]); // (40,-100) and (41,-101)

		// A box around ACME only.
		FixServiceResult result = FixService.Run(data, Settings(
			("FilterByRoi", "Y"),
			("RoiSwLat", "39.0"), ("RoiSwLon", "-101.0"), ("RoiNeLat", "40.5"), ("RoiNeLon", "-99.0")));

		Assert.Equal(2, result.FixCount);
		Assert.Equal(1, result.GeojsonFixCount);
	}

	[Fact]
	public void an_advisory_message_explains_when_nothing_was_written()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.BravoRow()]); // a single COMPUTER-NAV fix

		// ExcludedFixUses only narrows the FixUse file layout - the only way for a fix to survive
		// building but leave the run with nothing written.
		FixServiceResult result = FixService.Run(data, Settings(
			("OutputBy", "FixUse"),
			("ExcludedFixUses", "COMPUTER-NAV")));

		Assert.Equal(1, result.FixCount);
		Assert.Equal(1, result.GeojsonFixCount);
		Assert.Empty(result.GeojsonFilesWritten);
		Assert.Contains(result.Messages, m =>
			m.IsAdvisory && m.Text.Contains("No fixes matched your filters", StringComparison.Ordinal));
	}

	[Fact]
	public void a_combination_matching_no_fix_in_the_cycle_warns_and_writes_nothing()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.AcmeRow()]); // a WYPNT, not a RADAR fix

		FixServiceResult result = FixService.Run(data, Settings(
			("OutputBy", "ChartAndFixUse"),
			("Combinations", "ENROUTE-LOW+RADAR")));

		Assert.Contains(result.Messages, m =>
			m.Text.Contains("ENROUTE-LOW", StringComparison.Ordinal)
			&& m.Text.Contains("RADAR", StringComparison.Ordinal)
			&& m.Text.Contains("matches no fix in this cycle", StringComparison.Ordinal));
		Assert.Empty(result.GeojsonFilesWritten);
	}

	[Fact]
	public void a_combination_whose_chart_matches_no_fix_at_all_also_warns()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.BravoRow()]); // no chart at all

		FixServiceResult result = FixService.Run(data, Settings(
			("OutputBy", "ChartAndFixUse"),
			("Combinations", "ENROUTE-LOW+WYPNT")));

		Assert.Contains(result.Messages, m =>
			m.Text.Contains("matches no fix in this cycle", StringComparison.Ordinal));
	}

	[Fact]
	public void run_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => FixService.Run(null!, Settings()));
		Assert.Throws<ArgumentNullException>(() => FixService.Run(new NasrCsvDataCollection(), null!));
	}

	[Fact]
	public void run_throws_when_fix_data_was_never_parsed() =>
		Assert.Throws<InvalidOperationException>(() => FixService.Run(new NasrCsvDataCollection(), Settings()));

	[Fact]
	public void invalid_settings_errors_propagate_from_run()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.AcmeRow()]);

		Assert.Throws<ArgumentException>(() => FixService.Run(data, Settings(("OutputBy", "Sideways"))));
	}
}
