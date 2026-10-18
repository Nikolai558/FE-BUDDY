using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac;

/// <summary>
/// Covers how <see cref="AiracService"/> loads the previous cycle's NASR data for Procedure Charts
/// from <see cref="AiracCycleDataCache.Instance"/>: the data is optional, so a parse that fails while
/// the run waits for it only warns.
/// </summary>
[Collection("AppLog")]
public sealed class AiracServicePreviousNasrTests : IDisposable
{
	private static readonly AiracCycleInfo Previous = new("2609", "03_Sep_2026", new DateOnly(2026, 9, 3));
	private static readonly AiracCycleInfo Current = new("2610", "01_Oct_2026", new DateOnly(2026, 10, 1));
	private static readonly AiracCycleInfo Next = new("2611", "29_Oct_2026", new DateOnly(2026, 10, 29));

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_AiracPreviousNasr_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the log at a throwaway folder.</summary>
	public AiracServicePreviousNasrTests() => AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));

	/// <summary>Restores the real cache and log, and deletes the folder.</summary>
	public void Dispose()
	{
		AiracCycleDataCache.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch (IOException)
		{
			// Best-effort cleanup.
		}
	}

	[Fact]
	public async Task a_previous_cycle_whose_parse_fails_while_the_run_waits_only_warns()
	{
		TaskCompletionSource<NasrCsvDataCollection> previousParse = new(TaskCreationOptions.RunContinuationsAsynchronously);
		NasrCsvDataCollection nasr = ProcedureTestData.RouteNasr([ProcedureTestData.AptBaseRow("AAA", respArtccId: "ZOB")]);
		DtppMetafileDataCollection dtpp = ProcedureTestData.Dtpp("2610",
			airports: [ProcedureTestData.AirportRow("AAA", alnum: 100)],
			records: [ProcedureTestData.RecordRow("AAA", 10, "IAP", "ILS RWY 1", "00100ILS1.PDF", userAction: "A")]);

		AiracCycleDataCache cache = new(
			probe: (_, _) => Task.FromResult(AiracCyclePublicationState.Published),
			download: (cycle, _) => Task.FromResult(cycle.AiracCycleId),
			parse: (directory, _) => directory == Previous.AiracCycleId ? previousParse.Task : Task.FromResult(nasr),
			loadDtpp: (directory, token) =>
			{
				// The run reads the previous cycle's metafile just before its NASR data: fail that cycle's
				// parse once the run is waiting for it, as a parser does on a bad file.
				if (directory == Previous.AiracCycleId)
				{
					_ = Task.Run(async () =>
					{
						await Task.Delay(200);
						previousParse.SetException(new FormatException("Bad row."));
					});
				}

				return Task.FromResult(directory == Current.AiracCycleId ? dtpp : null);
			});
		AiracCycleDataCache.ConfigureForTesting(cache);

		Task prepare = cache.PrepareCyclesAsync(Previous, Current, Next);

		AiracServiceSettings settings = new()
		{
			SelectedCycle = Current,
			OutputDirectory = Path.Combine(_root, "out"),
			Procedures = new Dictionary<string, string> { ["Facilities"] = "ZOB", ["PrimaryFacility"] = "ZOB" },
		};

		AiracServiceResult result = await AiracService.RunAsync(settings).WaitAsync(TimeSpan.FromSeconds(10));
		await prepare.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.NotNull(result.Procedures);
		Assert.Contains(result.Procedures!.FilesWritten, file => file.EndsWith("Procedure_Changes.md", StringComparison.Ordinal));
		Assert.Contains(AppLog.Entries, entry => entry.Message == "Cycle 2609's NASR data couldn't be loaded for Procedure Charts: Bad row.");
		Assert.Equal(CycleDataState.Failed, cache.GetEntry("2609")!.State);
	}
}
