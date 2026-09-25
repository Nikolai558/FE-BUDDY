using System.Diagnostics;

using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.Harness;

/// <summary>
/// GUI stand-in entry point. Orchestrates the harness only - all airway logic lives in
/// FeBuddy.Core, and all console formatting lives in <see cref="ConsoleReport"/>. See
/// <see cref="HarnessSettings"/> for the paths and toggles to edit.
/// </summary>
internal static class Program
{
	public static async Task Main()
	{
		DevMode.IsEnabled = HarnessSettings.DevMode;
		OutputFormatting.PrettyPrintGeojson = HarnessSettings.PrettyPrintGeojson;

		Console.WriteLine("FE-Buddy Test Harness");
		Console.WriteLine($"NASR source: {HarnessSettings.NasrSourceDirectory}");
		Console.WriteLine($"Output:      {HarnessSettings.OutputDirectory}");
		Console.WriteLine($"DevMode:     {DevMode.IsEnabled}");
		Console.WriteLine();

		try
		{
			Console.Write("Parsing NASR CSV data... ");

			Stopwatch parseStopwatch = Stopwatch.StartNew();

			NasrCsvDataCollection allNasrCsvData =
				await NasrCsvParser.ParseAllAsync(HarnessSettings.NasrSourceDirectory);

			parseStopwatch.Stop();

			Console.WriteLine("done.");
			ConsoleReport.PrintNasrParseSummary(parseStopwatch.Elapsed);

			// var geojsonResult = AirwayGeojsonRunner.Run(allNasrCsvData);
			// ConsoleReport.PrintAirwayServiceResult("Airways: HighLow GeoJSON + Alias", geojsonResult);

			// var aliasOnlyResult = AirwayAliasRunner.Run(allNasrCsvData);
			// ConsoleReport.PrintAirwayServiceResult("Airways: Alias-only (OutputBy = None)", aliasOnlyResult);

			// AirportServiceResult airportResult = AirportRunner.Run(allNasrCsvData);
			// ConsoleReport.PrintAirportServiceResult("Airports: GeoJSON + Alias", airportResult);

			// DepartureServiceResult departureResult = DepartureRunner.Run(allNasrCsvData);
			// ConsoleReport.PrintDepartureServiceResult("Departures: GeoJSON + Alias", departureResult);

			// ArrivalServiceResult arrivalResult = ArrivalRunner.Run(allNasrCsvData);
			// ConsoleReport.PrintArrivalServiceResult("Arrivals: GeoJSON + Alias", arrivalResult);

			NavaidServiceResult navaidResult = NavaidRunner.Run(allNasrCsvData);
			ConsoleReport.PrintNavaidServiceResult("NAVAIDs: GeoJSON + Alias", navaidResult);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine();
			Console.Error.WriteLine($"FATAL: {ex.GetType().Name}: {ex.Message}");
			Console.Error.WriteLine(ex.StackTrace);
			Environment.ExitCode = 1;
		}
	}
}
