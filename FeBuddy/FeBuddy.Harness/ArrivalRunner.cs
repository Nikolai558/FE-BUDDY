using FeBuddy.Core.Application.Airac.Arrivals;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Harness;

/// <summary>
/// Exercises the Arrivals service exactly the way the GUI "Run" button will: build the
/// settings dictionary, call the one public entry point, and hand the result back for
/// reporting. Contains no arrival logic of its own.
/// </summary>
internal static class ArrivalRunner
{
	/// <summary>
	/// Runs the Arrivals sub-service against already-parsed NASR data using the toggles in
	/// <see cref="HarnessSettings.ArrivalSettings"/>.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data, as produced once by <see cref="Program"/>.</param>
	/// <returns>What the service built and wrote, for <see cref="ConsoleReport"/> to print.</returns>
	public static ArrivalServiceResult Run(NasrCsvDataCollection allNasrCsvData) =>
		ArrivalService.Run(allNasrCsvData, HarnessSettings.ArrivalSettings());
}
