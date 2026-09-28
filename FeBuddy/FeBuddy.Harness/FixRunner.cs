using FeBuddy.Core.Application.Airac.Fixes;
using FeBuddy.Core.Application.Airac.Fixes.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Harness;

/// <summary>
/// Exercises the Fixes service exactly the way the GUI "Run" button will: build the settings
/// dictionary, call the one public entry point, and hand the result back for reporting. Contains
/// no fix logic of its own.
/// </summary>
internal static class FixRunner
{
	/// <summary>
	/// Runs the Fixes sub-service against already-parsed NASR data using the toggles in
	/// <see cref="HarnessSettings.FixSettings"/>.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data, as produced once by <see cref="Program"/>.</param>
	/// <returns>What the service built and wrote, for <see cref="ConsoleReport"/> to print.</returns>
	public static FixServiceResult Run(NasrCsvDataCollection allNasrCsvData) =>
		FixService.Run(allNasrCsvData, HarnessSettings.FixSettings());
}
