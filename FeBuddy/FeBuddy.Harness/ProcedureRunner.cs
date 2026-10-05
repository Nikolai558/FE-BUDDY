using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Dtpp.Parsers;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Harness;

/// <summary>
/// Exercises the Procedures service the way an AIRAC Service run does, from a local metafile:
/// parse <see cref="HarnessSettings.DtppMetafileFile"/>, build the settings dictionary, call the
/// one public entry point, and hand the result back for reporting. Contains no Procedures logic of
/// its own.
/// </summary>
/// <remarks>
/// The harness has no previous cycle's metafile to hand it, so <c>previousDtpp</c> is always
/// <see langword="null"/> - a deleted or re-added procedure is reported without a link to its last
/// chart, the same as a real AIRAC Service run would before the previous cycle's metafile has been
/// cached.
/// </remarks>
internal static class ProcedureRunner
{
	/// <summary>
	/// Parses <see cref="HarnessSettings.DtppMetafileFile"/> and runs the Procedures sub-service
	/// against it using the toggles in <see cref="HarnessSettings.ProcedureSettings"/>.
	/// </summary>
	/// <param name="allNasrCsvData">The parsed NASR CSV data for the same cycle as the metafile.</param>
	/// <returns>What the service built and wrote, for <see cref="ConsoleReport"/> to print.</returns>
	public static ProcedureServiceResult Run(NasrCsvDataCollection allNasrCsvData)
	{
		DtppMetafileDataCollection dtppData = DtppMetafileXmlParser.Parse(HarnessSettings.DtppMetafileFile);
		return ProcedureService.Run(allNasrCsvData, dtppData, previousDtpp: null, HarnessSettings.ProcedureSettings());
	}
}
