using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Airways.Fixtures;
using FeBuddy.UnitTests.Application.Airac.Departures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac;

/// <summary>
/// Exercises the <see cref="AiracService"/> orchestrator: it dispatches each sub-service
/// (Airways, Airports, Departures) whose block is present and aggregates its result, and it is
/// a safe no-op (with a warning) when nothing is selected.
/// </summary>
public sealed class AiracServiceTests
{
	private static AiracCycleInfo Cycle => new("2610", "01_Oct_2026", new DateOnly(2026, 10, 1));

	/// <summary>No sub-service block selected: the run completes with a warning and no sub-results.</summary>
	[Fact]
	public async Task run_async_no_sub_service_selected_warns_and_does_nothing()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			Airways = null,
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, new NasrCsvDataCollection());

		Assert.Null(result.Airways);
		Assert.Contains(result.Warnings, w => w.Contains("no sub-service", StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>With an Airways block, the orchestrator runs the pipeline and surfaces its result.</summary>
	[Fact]
	public async Task run_async_with_airways_block_runs_the_pipeline_and_aggregates()
	{
		NasrCsvDataCollection data = AirwayTestDataBuilder.Build(
			fixes: [("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0), ("CCCCC", 42.0, -82.0)],
			awyId: "J1",
			segments:
			[
				AirwayTestDataBuilder.Segment("J1", 10, "AAAAA", "WP", "BBBBB"),
				AirwayTestDataBuilder.Segment("J1", 20, "BBBBB", "WP", "CCCCC"),
			]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
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
		Assert.Empty(result.Airways.ExcludedAirwayIds);
	}

	/// <summary>With Airports and Departures blocks, both pipelines run and report progress; cancellation stops the run.</summary>
	[Fact]
	public async Task run_async_with_airports_and_departures_blocks_runs_both_and_reports_progress()
	{
		string output = Path.Combine(Path.GetTempPath(), "FeBuddyTests_AiracService_" + Guid.NewGuid().ToString("N"));

		try
		{
			NasrCsvDataCollection data = DepartureTestData.Dotss();
			AiracServiceSettings settings = new()
			{
				SelectedCycle = Cycle,
				Airports = new Dictionary<string, string> { { "OutputDirectory", output }, { "GenerateGeojson", "N" } },
				Departures = new Dictionary<string, string> { { "OutputDirectory", output }, { "GenerateGeojson", "N" } },
			};

			List<AiracServiceProgress> reports = [];
			AiracServiceResult result = await AiracService.RunAsync(settings, data, new SynchronousProgress(reports.Add));

			Assert.Null(result.Airways);
			Assert.Equal(1, result.Airports!.AirportCount);
			Assert.Equal(1, result.Departures!.AirportProcedureCount);
			Assert.Equal(
				["Airports", "Airports", "Departures", "Departures"],
				reports.Select(r => r.SubService));
			Assert.Equal(100, reports[^1].PercentComplete);

			await Assert.ThrowsAnyAsync<OperationCanceledException>(
				() => AiracService.RunAsync(settings, data, cancellationToken: new CancellationToken(canceled: true)));
		}
		finally
		{
			if (Directory.Exists(output))
			{
				Directory.Delete(output, recursive: true);
			}
		}
	}

	private sealed class SynchronousProgress(Action<AiracServiceProgress> report) : IProgress<AiracServiceProgress>
	{
		public void Report(AiracServiceProgress value) => report(value);
	}

	/// <summary>A null settings or data argument is rejected up front.</summary>
	[Fact]
	public async Task run_async_null_arguments_throw()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
		};

		await Assert.ThrowsAsync<ArgumentNullException>(() => AiracService.RunAsync((AiracServiceSettings)null!, new NasrCsvDataCollection()));
		await Assert.ThrowsAsync<ArgumentNullException>(() => AiracService.RunAsync(settings, (NasrCsvDataCollection)null!));
	}
}
