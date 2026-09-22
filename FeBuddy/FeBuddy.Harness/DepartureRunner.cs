using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac.Departures;
using FeBuddy.Core.Services.Airac.Departures;

namespace FeBuddy.Harness;

/// <summary>
/// Exercises the Departures service exactly the way the GUI "Run" button will: build the
/// settings dictionary, call the one public entry point, and hand the result back for
/// reporting. Contains no departure logic of its own.
/// </summary>
internal static class DepartureRunner
{
	/// <summary>
	/// Runs the Departures sub-service against already-parsed NASR data using the toggles in
	/// <see cref="HarnessSettings.DepartureSettings"/>.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data, as produced once by <see cref="Program"/>.</param>
	/// <returns>What the service built and wrote, for <see cref="ConsoleReport"/> to print.</returns>
	public static DepartureServiceResult Run(NasrCsvDataCollection allNasrCsvData) =>
		DepartureService.Run(allNasrCsvData, HarnessSettings.DepartureSettings());
}
