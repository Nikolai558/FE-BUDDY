using System.Diagnostics;

using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac.Airports;
using FeBuddy.Core.Services.Airac.Airports;

namespace FeBuddy.Harness;

/// <summary>
/// Exercises the Airports service exactly the way a future GUI "Run" button would: build the
/// settings dictionary, call the one public entry point, and hand the result back for
/// reporting. Contains no airport logic of its own.
/// </summary>
internal static class AirportRunner
{
	/// <summary>
	/// Runs the Airports sub-service against already-parsed NASR data using the toggles in
	/// <see cref="HarnessSettings.AirportSettings"/>.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data, as produced once by <see cref="Program"/>.</param>
	/// <returns>What the service built and wrote, for <see cref="ConsoleReport"/> to print.</returns>
	public static AirportServiceResult Run(NasrCsvDataCollection allNasrCsvData)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();

		AirportServiceResult result = AirportService.Run(allNasrCsvData, HarnessSettings.AirportSettings());

		stopwatch.Stop();

		return result;
	}
}
