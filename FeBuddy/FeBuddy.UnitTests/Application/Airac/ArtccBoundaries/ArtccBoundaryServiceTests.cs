using FeBuddy.Core.Application.Airac.ArtccBoundaries;
using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.ArtccBoundaries.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.ArtccBoundaries;

/// <summary>
/// Runs the whole ARTCC Boundaries pipeline (<see cref="ArtccBoundaryService.Run"/>): the
/// built/GeoJSON counts, that <c>LocationFilter</c> narrows every output, and the advisory message
/// when nothing matches.
/// </summary>
public sealed class ArtccBoundaryServiceTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_ArtccService_" + Guid.NewGuid().ToString("N"));

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

	/// <summary>ZOB (HIGH + LOW), ZAK (UNLIMITED, two rings) and ZZZ (HIGH, no ARB_BASE row).</summary>
	private static NasrCsvDataCollection AllSamples() => ArtccBoundaryTestData.BuildAllSamples();

	[Fact]
	public void run_counts_locations_rings_and_geojson_files_for_the_default_high_low_output()
	{
		ArtccBoundaryServiceResult result = ArtccBoundaryService.Run(AllSamples(), Settings());

		// ZOB, ZAK and ZZZ.
		Assert.Equal(3, result.LocationCount);

		// High: ZOB-High, ZAK-CTA, ZAK-FIR, ZZZ-High. Low: ZOB-Low, ZAK-CTA, ZAK-FIR.
		Assert.Equal(2, result.GeojsonFilesWritten.Count);
		Assert.Equal(7, result.RingCount);
		Assert.Contains(result.GeojsonFilesWritten, f => f.EndsWith("ARTCC-Boundary_High_Lines.geojson", StringComparison.Ordinal));
		Assert.Contains(result.GeojsonFilesWritten, f => f.EndsWith("ARTCC-Boundary_Low_Lines.geojson", StringComparison.Ordinal));
		Assert.Equal(result.GeojsonFilesWritten.Count, result.GeojsonFeatureCountsByFile.Count);
	}

	[Fact]
	public void location_filter_narrows_the_location_count_and_every_output_file()
	{
		ArtccBoundaryServiceResult result = ArtccBoundaryService.Run(AllSamples(), Settings(
			("LocationFilter", "ZOB"), ("OutputBy", "HighLowUnlimited")));

		Assert.Equal(1, result.LocationCount);
		Assert.Equal(2, result.RingCount); // ZOB High + ZOB Low
		Assert.Equal(2, result.GeojsonFilesWritten.Count);
		Assert.DoesNotContain(result.GeojsonFilesWritten, f => f.Contains("Unlimited", StringComparison.Ordinal));
	}

	[Fact]
	public void a_location_filter_matching_nothing_produces_the_advisory_and_writes_no_files()
	{
		ArtccBoundaryServiceResult result = ArtccBoundaryService.Run(AllSamples(), Settings(("LocationFilter", "ZZZZ")));

		Assert.Equal(0, result.LocationCount);
		Assert.Equal(0, result.RingCount);
		Assert.Empty(result.GeojsonFilesWritten);
		Assert.Contains(result.Messages, m =>
			m.IsAdvisory && m.Text.Contains("No ARTCC boundaries matched your filters", StringComparison.Ordinal));
	}

	[Fact]
	public void run_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => ArtccBoundaryService.Run(null!, Settings()));
		Assert.Throws<ArgumentNullException>(() => ArtccBoundaryService.Run(new NasrCsvDataCollection(), null!));
	}

	[Fact]
	public void run_throws_when_arb_data_was_never_parsed() =>
		Assert.Throws<InvalidOperationException>(() => ArtccBoundaryService.Run(new NasrCsvDataCollection(), Settings()));

	[Fact]
	public void invalid_settings_errors_propagate_from_run() =>
		Assert.Throws<ArgumentException>(() => ArtccBoundaryService.Run(AllSamples(), Settings(("OutputBy", "Sideways"))));
}
