using System.Diagnostics;

using FEBuddyLibrary.Models.NASR.CSV;
using FEBuddyLibrary.Models.Services.Airac.Airways;
using FEBuddyLibrary.Services.Airac.Airways;

namespace FEBuddyTest;

/// <summary>
/// Exercises the Airways service with <c>OutputBy = None</c> so only the alias file is
/// generated, proving the alias file is independent of GeoJSON generation.
/// </summary>
internal static class AirwayAliasRunner
{
	public static AirwayServiceResult Run(NasrCsvDataCollection allNasrCsvData)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();

		AirwayServiceResult result = AirwayService.Run(allNasrCsvData, HarnessSettings.AliasOnlySettings());

		stopwatch.Stop();

		return result;
	}
}
