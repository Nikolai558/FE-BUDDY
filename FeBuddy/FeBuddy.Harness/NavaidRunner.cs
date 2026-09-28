using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Harness;

/// <summary>
/// Exercises the NAVAIDs service exactly the way the GUI "Run" button will: build the
/// settings dictionary, call the one public entry point, and hand the result back for
/// reporting. Contains no NAVAID logic of its own.
/// </summary>
internal static class NavaidRunner
{
	/// <summary>
	/// Runs the NAVAIDs sub-service against already-parsed NASR data using the toggles in
	/// <see cref="HarnessSettings.NavaidSettings"/>.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data, as produced once by <see cref="Program"/>.</param>
	/// <returns>What the service built and wrote, for <see cref="ConsoleReport"/> to print.</returns>
	public static NavaidServiceResult Run(NasrCsvDataCollection allNasrCsvData) =>
		NavaidService.Run(allNasrCsvData, HarnessSettings.NavaidSettings());
}
