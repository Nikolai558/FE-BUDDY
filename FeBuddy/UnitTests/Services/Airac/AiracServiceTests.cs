using FEBuddyLibrary.Models.NASR.CSV;
using FEBuddyLibrary.Models.Services.Airac;
using FEBuddyLibrary.Services.Airac;

using UnitTests.Services.Airac.Airways.Fixtures;

namespace UnitTests.Services.Airac;

/// <summary>
/// Exercises the <see cref="AiracService"/> orchestrator: it dispatches the Airways
/// sub-service when a block is present and aggregates its result, and it is a safe no-op
/// (with a warning) when nothing is selected.
/// </summary>
public sealed class AiracServiceTests
{
	private static AiracCycleInfo Cycle => new("2610", "01_Oct_2026", new DateOnly(2026, 10, 1));

	/// <summary>No sub-service block selected: the run completes with a warning and no sub-results.</summary>
	[Fact]
	public async Task RunAsync_NoSubServiceSelected_WarnsAndDoesNothing()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			ArtccId = "ZOA",
			OutputDirectory = Path.GetTempPath(),
			Airways = null,
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, new NasrCsvDataCollection());

		Assert.Null(result.Airways);
		Assert.Contains(result.Warnings, w => w.Contains("no sub-service", StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>With an Airways block, the orchestrator runs the pipeline and surfaces its result.</summary>
	[Fact]
	public async Task RunAsync_WithAirwaysBlock_RunsThePipelineAndAggregates()
	{
		NasrCsvDataCollection data = AirwayTestDataBuilder.Build(
			fixes: new[] { ("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0), ("CCCCC", 42.0, -82.0) },
			awyId: "J1",
			segments: new[]
			{
				AirwayTestDataBuilder.Segment("J1", 10, "AAAAA", "WP", "BBBBB"),
				AirwayTestDataBuilder.Segment("J1", 20, "BBBBB", "WP", "CCCCC"),
			});

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			ArtccId = "ZOA",
			OutputDirectory = Path.GetTempPath(),
			// OutputBy=None keeps the run in memory - no files are written by this test.
			Airways = new Dictionary<string, string>
			{
				{ "OutputDirectory", Path.GetTempPath() },
				{ "OutputBy", "None" },
				{ "GenerateAliasFile", "N" },
			},
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, data);

		Assert.NotNull(result.Airways);
		Assert.Equal(1, result.Airways!.AirwayCount);
		Assert.Empty(result.Airways.GeojsonFilesWritten);
		Assert.Empty(result.ExcludedAirwayIds);
	}

	/// <summary>A null settings or data argument is rejected up front.</summary>
	[Fact]
	public async Task RunAsync_NullArguments_Throw()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			ArtccId = "ZOA",
			OutputDirectory = Path.GetTempPath(),
		};

		await Assert.ThrowsAsync<ArgumentNullException>(() => AiracService.RunAsync((AiracServiceSettings)null!, new NasrCsvDataCollection()));
		await Assert.ThrowsAsync<ArgumentNullException>(() => AiracService.RunAsync(settings, (NasrCsvDataCollection)null!));
	}
}
