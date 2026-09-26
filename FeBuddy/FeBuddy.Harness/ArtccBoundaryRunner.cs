using FeBuddy.Core.Application.Airac.ArtccBoundaries;
using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Harness;

/// <summary>
/// Exercises the ARTCC Boundaries service exactly the way the GUI "Run" button will: build the
/// settings dictionary, call the one public entry point, and hand the result back for reporting.
/// Contains no ARTCC boundary logic of its own.
/// </summary>
internal static class ArtccBoundaryRunner
{
	/// <summary>
	/// Runs the ARTCC Boundaries sub-service against already-parsed NASR data using the toggles in
	/// <see cref="HarnessSettings.ArtccBoundarySettings"/>.
	/// </summary>
	/// <param name="allNasrCsvData">All parsed NASR CSV data, as produced once by <see cref="Program"/>.</param>
	/// <returns>What the service built and wrote, for <see cref="ConsoleReport"/> to print.</returns>
	public static ArtccBoundaryServiceResult Run(NasrCsvDataCollection allNasrCsvData) =>
		ArtccBoundaryService.Run(allNasrCsvData, HarnessSettings.ArtccBoundarySettings());
}
