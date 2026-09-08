using System.Diagnostics;

using FEBuddyLibrary.Models.NASR.CSV;
using FEBuddyLibrary.Models.Services.Airac.Airways;
using FEBuddyLibrary.Services.Airac.Airways;

namespace FEBuddyTest;

/// <summary>
/// Exercises the Airways service exactly the way a future GUI "Run" button would: build the
/// settings dictionary, call the one public entry point, and hand the result back for
/// reporting. Contains no airway logic of its own.
/// </summary>
internal static class AirwayGeojsonRunner
{
	public static AirwayServiceResult Run(NasrCsvDataCollection allNasrCsvData)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();

		AirwayServiceResult result = AirwayService.Run(allNasrCsvData, HarnessSettings.AirwaySettings());

		stopwatch.Stop();

		return result;
	}
}
