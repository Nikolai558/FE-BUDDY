using System.Diagnostics;

using FeBuddy.Core.Models.NASR.CSV;
using FeBuddy.Core.Models.Services.Airac.Airways;
using FeBuddy.Core.Services.Airac.Airways;

namespace FeBuddy.Harness;

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
