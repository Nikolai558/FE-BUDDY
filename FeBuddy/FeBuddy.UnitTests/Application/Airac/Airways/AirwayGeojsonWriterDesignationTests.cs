using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Airways.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Airways;

/// <summary>
/// Verifies that an <see cref="AirwayGeojsonOutputBy.Designation"/> run names files from the
/// designation derived from <c>AWY_ID</c> (remediation plan 3.1), so a <c>Q</c>/<c>T</c> RNAV
/// airway lands in <c>Airways_Q_*</c> / <c>Airways_T_*</c> and never <c>Airways_RN_*</c>.
/// </summary>
public sealed class AirwayGeojsonWriterDesignationTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_AwyGeojson_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(_outputDirectory))
			{
				Directory.Delete(_outputDirectory, recursive: true);
			}
		}
		catch
		{
			// Best-effort.
		}
	}

	[Fact]
	public void designation_run_names_files_from_the_awy_id_not_awy_designation()
	{
		// A "Q" RNAV airway whose NASR AWY_DESIGNATION says "RN" - the file must still be Airways_Q_*.
		NasrCsvDataCollection data = AirwayTestDataBuilder.Build(
			fixes: [("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0), ("CCCCC", 42.0, -82.0)],
			awyId: "Q100",
			awyDesignation: "RN",
			segments:
			[
				AirwayTestDataBuilder.Segment("Q100", 10, "AAAAA", "WP", "BBBBB"),
				AirwayTestDataBuilder.Segment("Q100", 20, "BBBBB", "WP", "CCCCC"),
			]);

		AirwaySettings settings = new()
		{
			OutputDirectory = _outputDirectory,
			OutputBy = AirwayGeojsonOutputBy.Designation,
			BufferAirwayWaypoints = false,
			IncludeFebCustomProperties = false,
			FebProperties = [],
			GenerateAliasFile = false,
			SplitAtAntimeridian = true,
			IncludeCrcLineDefaults = false,
			IncludeCrcSymbolDefaults = false,
			IncludeCrcTextDefaults = false,
			Roi = null,
		};

		AirwayBuildAllResult built = AirwayBuilder.BuildAll(data, settings);
		GeojsonFileSet result = AirwayGeojsonWriter.Generate(built.Airways, settings);

		string[] fileNames = result.FilesWritten.Select(Path.GetFileName).ToArray()!;

		Assert.Contains("Airways_Q_Lines.geojson", fileNames);
		Assert.Contains("Airways_Q_Symbols.geojson", fileNames);
		Assert.Contains("Airways_Q_Text.geojson", fileNames);
		Assert.DoesNotContain(fileNames, name => name!.Contains("_RN_"));
	}
}
