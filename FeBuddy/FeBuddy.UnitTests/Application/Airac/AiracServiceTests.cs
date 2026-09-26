using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.WxStations.Models;

using FeBuddy.UnitTests.Application.Airac.Airways.Fixtures;
using FeBuddy.UnitTests.Application.Airac.Arrivals.Fixtures;
using FeBuddy.UnitTests.Application.Airac.ArtccBoundaries.Fixtures;
using FeBuddy.UnitTests.Application.Airac.Departures.Fixtures;
using FeBuddy.UnitTests.Application.Airac.Fixes.Fixtures;
using FeBuddy.UnitTests.Application.Airac.Navaids.Fixtures;
using FeBuddy.UnitTests.Application.Airac.WxStations.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac;

/// <summary>
/// Exercises the <see cref="AiracService"/> orchestrator: it dispatches each sub-service
/// (Airways, Airports, Departures) whose block is present and aggregates its result, writes every
/// sub-service into the one <c>AIRAC_&lt;cycle&gt;</c> folder, handles an earlier run's files as
/// asked, and is a safe no-op (with a warning) when nothing is selected.
/// </summary>
public sealed class AiracServiceTests : IDisposable
{
	private readonly string _output = Path.Combine(Path.GetTempPath(), "FeBuddyTests_AiracService_" + Guid.NewGuid().ToString("N"));

	private static AiracCycleInfo Cycle => new("2610", "01_Oct_2026", new DateOnly(2026, 10, 1));

	public void Dispose()
	{
		if (Directory.Exists(_output))
		{
			Directory.Delete(_output, recursive: true);
		}
	}

	/// <summary>Airports and Departures, alias files only: small, fast, and each writes one file.</summary>
	private AiracServiceSettings AliasOnlySettings(
		bool addFeBuddyOutputFolder = true,
		ExistingOutputAction existingOutput = ExistingOutputAction.Overwrite) => new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			AddFeBuddyOutputFolder = addFeBuddyOutputFolder,
			ExistingOutput = existingOutput,
			Airports = new Dictionary<string, string> { { "GenerateGeojson", "N" } },
			Departures = new Dictionary<string, string> { { "GenerateGeojson", "N" } },
		};

	private string CycleFolder => Path.Combine(_output, "FE-Buddy_Output", "AIRAC_2610");

	[Fact]
	public async Task run_async_no_sub_service_selected_warns_and_does_nothing()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Airways = null,
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, new NasrCsvDataCollection());

		Assert.Null(result.Airways);
		Assert.Contains(result.Warnings, w => w.Contains("no sub-service", StringComparison.OrdinalIgnoreCase));
	}

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
			OutputDirectory = _output,
			// OutputBy=None keeps the run in memory - no files are written by this test.
			Airways = new Dictionary<string, string>
			{
				{ "OutputBy", "None" },
				{ "GenerateAliasFile", "N" },
			},
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, data);

		Assert.NotNull(result.Airways);
		Assert.Equal(1, result.Airways!.AirwayCount);
		Assert.Empty(result.Airways.GeojsonFilesWritten);
		Assert.Empty(result.Airways.ExcludedAirwayIds);
		Assert.False(Directory.Exists(result.OutputDirectory));
	}

	[Fact]
	public async Task run_async_with_airports_and_departures_blocks_runs_both_and_reports_progress()
	{
		NasrCsvDataCollection data = DepartureTestData.Dotss();
		AiracServiceSettings settings = AliasOnlySettings();

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

	[Fact]
	public async Task every_sub_service_writes_into_the_one_cycle_folder()
	{
		AiracServiceResult result = await AiracService.RunAsync(AliasOnlySettings(), DepartureTestData.Dotss());

		Assert.Equal(CycleFolder, result.OutputDirectory);
		Assert.Equal(Path.Combine(CycleFolder, "Airports.txt"), result.Airports!.AliasFilePath);
		Assert.Equal(Path.Combine(CycleFolder, "Departures.txt"), result.Departures!.AliasFilePath);
	}

	[Fact]
	public async Task run_async_with_an_arrivals_block_runs_the_pipeline_and_aggregates()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Arrivals = new Dictionary<string, string> { { "GenerateGeojson", "N" } },
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, ArrivalTestData.Blaid());

		Assert.Null(result.Departures);
		Assert.NotNull(result.Arrivals);
		Assert.Equal(1, result.Arrivals!.AirportProcedureCount);
		Assert.Equal(Path.Combine(CycleFolder, "Arrivals.txt"), result.Arrivals.AliasFilePath);
	}

	[Fact]
	public async Task an_arrivals_block_writes_star_named_geojson_into_the_cycle_folder()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Arrivals = new Dictionary<string, string> { { "GenerateAliasFile", "N" } },
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, ArrivalTestData.Blaid());

		Assert.Equal(1, result.Arrivals!.AirportProcedureCount);
		string linesFile = Path.Combine(CycleFolder, "Geojson", "ZLA", "LAS", "LAS_BLAID_STAR_Lines.geojson");
		Assert.Contains(linesFile, result.Arrivals.GeojsonFilesWritten);
		Assert.True(File.Exists(linesFile));
	}

	[Fact]
	public async Task a_run_with_only_arrivals_selected_counts_as_something_selected()
	{
		string stale = WriteStaleFile();
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			ExistingOutput = ExistingOutputAction.DeleteExisting,
			Arrivals = new Dictionary<string, string> { { "GenerateGeojson", "N" } },
		};

		await AiracService.RunAsync(settings, ArrivalTestData.Blaid());

		// DeleteExisting only runs when something is selected: Arrivals alone must still trigger it.
		Assert.False(File.Exists(stale));
		Assert.True(File.Exists(Path.Combine(CycleFolder, "Arrivals.txt")));
	}

	[Fact]
	public async Task a_null_arrivals_block_leaves_result_arrivals_null()
	{
		AiracServiceSettings settings = AliasOnlySettings() with { Arrivals = null };

		AiracServiceResult result = await AiracService.RunAsync(settings, DepartureTestData.Dotss());

		Assert.Null(result.Arrivals);
	}

	[Fact]
	public async Task run_async_with_a_navaids_block_runs_the_pipeline_and_aggregates()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Navaids = new Dictionary<string, string> { { "GenerateGeojson", "N" } },
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, NavaidTestData.Build([NavaidTestData.CgtRow()]));

		Assert.Null(result.Arrivals);
		Assert.NotNull(result.Navaids);
		Assert.Equal(1, result.Navaids!.NavaidCount);
		Assert.Equal(Path.Combine(CycleFolder, "NAVAIDs.txt"), result.Navaids.AliasFilePath);
	}

	[Fact]
	public async Task a_navaids_block_writes_geojson_into_the_cycle_folder()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Navaids = new Dictionary<string, string> { { "GenerateAliasFile", "N" } },
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, NavaidTestData.Build([NavaidTestData.CgtRow()]));

		string symbolsFile = Path.Combine(CycleFolder, "Geojson", "NAVAIDs_Symbols.geojson");
		Assert.Contains(symbolsFile, result.Navaids!.GeojsonFilesWritten);
		Assert.True(File.Exists(symbolsFile));
	}

	[Fact]
	public async Task a_run_with_only_navaids_selected_counts_as_something_selected()
	{
		string stale = WriteStaleFile();
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			ExistingOutput = ExistingOutputAction.DeleteExisting,
			Navaids = new Dictionary<string, string> { { "GenerateGeojson", "N" } },
		};

		await AiracService.RunAsync(settings, NavaidTestData.Build([NavaidTestData.CgtRow()]));

		// DeleteExisting only runs when something is selected: NAVAIDs alone must still trigger it.
		Assert.False(File.Exists(stale));
		Assert.True(File.Exists(Path.Combine(CycleFolder, "NAVAIDs.txt")));
	}

	[Fact]
	public async Task a_null_navaids_block_leaves_result_navaids_null()
	{
		AiracServiceSettings settings = AliasOnlySettings() with { Navaids = null };

		AiracServiceResult result = await AiracService.RunAsync(settings, DepartureTestData.Dotss());

		Assert.Null(result.Navaids);
	}

	[Fact]
	public async Task run_async_with_an_artcc_boundaries_block_runs_the_pipeline_and_aggregates()
	{
		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.ZobBaseRow()],
			[.. ArtccBoundaryTestData.ZobHighRows(), .. ArtccBoundaryTestData.ZobLowRows()]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			ArtccBoundaries = new Dictionary<string, string>(),
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, data);

		Assert.NotNull(result.ArtccBoundaries);
		Assert.Equal(1, result.ArtccBoundaries!.LocationCount);
		Assert.Equal(2, result.ArtccBoundaries.RingCount);

		string highFile = Path.Combine(CycleFolder, "Geojson", "ARTCC-Boundary_High_Lines.geojson");
		Assert.Contains(highFile, result.ArtccBoundaries.GeojsonFilesWritten);
		Assert.True(File.Exists(highFile));
	}

	[Fact]
	public async Task a_run_with_only_artcc_boundaries_selected_counts_as_something_selected()
	{
		string stale = WriteStaleFile();
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			ExistingOutput = ExistingOutputAction.DeleteExisting,
			ArtccBoundaries = new Dictionary<string, string>(),
		};

		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.ZobBaseRow()],
			ArtccBoundaryTestData.ZobHighRows());

		await AiracService.RunAsync(settings, data);

		// DeleteExisting only runs when something is selected: ARTCC Boundaries alone must still trigger it.
		Assert.False(File.Exists(stale));
		Assert.True(File.Exists(Path.Combine(CycleFolder, "Geojson", "ARTCC-Boundary_High_Lines.geojson")));
	}

	[Fact]
	public async Task a_null_artcc_boundaries_block_leaves_result_artcc_boundaries_null()
	{
		AiracServiceSettings settings = AliasOnlySettings() with { ArtccBoundaries = null };

		AiracServiceResult result = await AiracService.RunAsync(settings, DepartureTestData.Dotss());

		Assert.Null(result.ArtccBoundaries);
	}

	[Fact]
	public async Task run_async_with_a_fixes_block_runs_the_pipeline_and_aggregates()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.AcmeRow()]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Fixes = new Dictionary<string, string>(),
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, data);

		Assert.Null(result.ArtccBoundaries);
		Assert.NotNull(result.Fixes);
		Assert.Equal(1, result.Fixes!.FixCount);
	}

	[Fact]
	public async Task a_fixes_block_writes_geojson_into_the_cycle_folder()
	{
		NasrCsvDataCollection data = FixTestData.Build([FixTestData.AcmeRow()]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			Fixes = new Dictionary<string, string>(),
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, data);

		string symbolsFile = Path.Combine(CycleFolder, "Geojson", "Fix_Symbols.geojson");
		Assert.Contains(symbolsFile, result.Fixes!.GeojsonFilesWritten);
		Assert.True(File.Exists(symbolsFile));
	}

	[Fact]
	public async Task a_run_with_only_fixes_selected_counts_as_something_selected()
	{
		string stale = WriteStaleFile();
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			ExistingOutput = ExistingOutputAction.DeleteExisting,
			Fixes = new Dictionary<string, string>(),
		};

		await AiracService.RunAsync(settings, FixTestData.Build([FixTestData.AcmeRow()]));

		// DeleteExisting only runs when something is selected: Fixes alone must still trigger it.
		Assert.False(File.Exists(stale));
		Assert.True(File.Exists(Path.Combine(CycleFolder, "Geojson", "Fix_Symbols.geojson")));
	}

	[Fact]
	public async Task a_null_fixes_block_leaves_result_fixes_null()
	{
		AiracServiceSettings settings = AliasOnlySettings() with { Fixes = null };

		AiracServiceResult result = await AiracService.RunAsync(settings, DepartureTestData.Dotss());

		Assert.Null(result.Fixes);
	}

	[Fact]
	public async Task run_async_with_a_wx_stations_block_runs_the_pipeline_and_aggregates()
	{
		WxStationDataCollection wxData = WxStationTestData.Build([WxStationTestData.DtwRow()]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			WxStations = new Dictionary<string, string>(),
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, new NasrCsvDataCollection(), wxData);

		Assert.Null(result.Fixes);
		Assert.NotNull(result.WxStations);
		Assert.Equal(1, result.WxStations!.StationCount);
	}

	[Fact]
	public async Task a_wx_stations_block_writes_geojson_into_the_cycle_folder()
	{
		WxStationDataCollection wxData = WxStationTestData.Build([WxStationTestData.DtwRow()]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			WxStations = new Dictionary<string, string>(),
		};

		AiracServiceResult result = await AiracService.RunAsync(settings, new NasrCsvDataCollection(), wxData);

		string symbolsFile = Path.Combine(CycleFolder, "Geojson", "Wx_Symbols.geojson");
		Assert.Contains(symbolsFile, result.WxStations!.GeojsonFilesWritten);
		Assert.True(File.Exists(symbolsFile));
	}

	[Fact]
	public async Task a_run_with_only_wx_stations_selected_counts_as_something_selected()
	{
		string stale = WriteStaleFile();
		WxStationDataCollection wxData = WxStationTestData.Build([WxStationTestData.DtwRow()]);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			ExistingOutput = ExistingOutputAction.DeleteExisting,
			WxStations = new Dictionary<string, string>(),
		};

		await AiracService.RunAsync(settings, new NasrCsvDataCollection(), wxData);

		// DeleteExisting only runs when something is selected: Wx Stations alone must still trigger it.
		Assert.False(File.Exists(stale));
		Assert.True(File.Exists(Path.Combine(CycleFolder, "Geojson", "Wx_Symbols.geojson")));
	}

	[Fact]
	public async Task a_null_wx_stations_block_leaves_result_wx_stations_null()
	{
		AiracServiceSettings settings = AliasOnlySettings() with { WxStations = null };

		AiracServiceResult result = await AiracService.RunAsync(settings, DepartureTestData.Dotss(), wxStationData: null);

		Assert.Null(result.WxStations);
	}

	[Fact]
	public async Task the_two_argument_overload_with_a_wx_stations_block_fails_with_no_weather_station_data()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
			WxStations = new Dictionary<string, string>(),
		};

		// The two-argument RunAsync(settings, nasrData) overload passes no Wx station data at all
		// (WxStationService.Run receives a null WxStationDataCollection), so a selected Wx Stations
		// block fails with WxStationBuilder.Read's "no weather station data" guard rather than
		// running, and the whole call throws before returning a result.
		InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
			() => AiracService.RunAsync(settings, new NasrCsvDataCollection()));

		Assert.Contains("No weather station data for this cycle", ex.Message, StringComparison.Ordinal);
	}

	[Fact]
	public async Task without_the_fe_buddy_output_folder_the_cycle_folder_sits_in_the_output_directory()
	{
		AiracServiceResult result = await AiracService.RunAsync(
			AliasOnlySettings(addFeBuddyOutputFolder: false), DepartureTestData.Dotss());

		Assert.Equal(Path.Combine(_output, "AIRAC_2610"), result.OutputDirectory);
		Assert.True(File.Exists(Path.Combine(_output, "AIRAC_2610", "Airports.txt")));
	}

	[Fact]
	public async Task has_existing_output_is_true_only_once_the_cycle_folder_holds_something()
	{
		AiracServiceSettings settings = AliasOnlySettings();

		Assert.False(AiracService.HasExistingOutput(settings));

		Directory.CreateDirectory(CycleFolder);
		Assert.False(AiracService.HasExistingOutput(settings));

		await AiracService.RunAsync(settings, DepartureTestData.Dotss());
		Assert.True(AiracService.HasExistingOutput(settings));
	}

	[Fact]
	public async Task overwrite_keeps_an_earlier_runs_other_files()
	{
		string stale = WriteStaleFile();

		await AiracService.RunAsync(AliasOnlySettings(), DepartureTestData.Dotss());

		Assert.True(File.Exists(stale));
		Assert.True(File.Exists(Path.Combine(CycleFolder, "Airports.txt")));
	}

	[Fact]
	public async Task delete_existing_empties_the_cycle_folder_before_writing()
	{
		string stale = WriteStaleFile();

		await AiracService.RunAsync(
			AliasOnlySettings(existingOutput: ExistingOutputAction.DeleteExisting), DepartureTestData.Dotss());

		Assert.False(File.Exists(stale));
		Assert.False(Directory.Exists(Path.GetDirectoryName(stale)));
		Assert.True(File.Exists(Path.Combine(CycleFolder, "Airports.txt")));
	}

	[Fact]
	public async Task delete_existing_leaves_the_folder_alone_when_nothing_is_selected()
	{
		string stale = WriteStaleFile();
		AiracServiceSettings settings = AliasOnlySettings(existingOutput: ExistingOutputAction.DeleteExisting) with
		{
			Airports = null,
			Departures = null,
		};

		await AiracService.RunAsync(settings, new NasrCsvDataCollection());

		Assert.True(File.Exists(stale));
	}

	private sealed class SynchronousProgress(Action<AiracServiceProgress> report) : IProgress<AiracServiceProgress>
	{
		public void Report(AiracServiceProgress value) => report(value);
	}

	[Fact]
	public async Task run_async_rejects_null_arguments_and_a_blank_output_directory()
	{
		AiracServiceSettings settings = new()
		{
			SelectedCycle = Cycle,
			OutputDirectory = _output,
		};

		await Assert.ThrowsAsync<ArgumentNullException>(() => AiracService.RunAsync((AiracServiceSettings)null!, new NasrCsvDataCollection()));
		await Assert.ThrowsAsync<ArgumentNullException>(() => AiracService.RunAsync(settings, (NasrCsvDataCollection)null!));
		await Assert.ThrowsAsync<ArgumentException>(() => AiracService.RunAsync(settings with { OutputDirectory = " " }, new NasrCsvDataCollection()));
		Assert.Throws<ArgumentNullException>(() => AiracService.HasExistingOutput(null!));
	}

	/// <summary>Leaves a file from "an earlier run" in the cycle folder, in a sub-folder, and returns its path.</summary>
	private string WriteStaleFile()
	{
		string directory = Path.Combine(CycleFolder, "Geojson");
		Directory.CreateDirectory(directory);

		string path = Path.Combine(directory, "Airways_High_Lines.geojson");
		File.WriteAllText(path, "{}");
		return path;
	}
}
