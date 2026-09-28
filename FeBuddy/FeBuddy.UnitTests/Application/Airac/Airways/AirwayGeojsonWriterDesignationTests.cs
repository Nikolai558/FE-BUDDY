using System.Text.Json;

using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Airways.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Airways.Fixtures;

using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Application.Airac.Airways;

/// <summary>
/// Verifies that an <see cref="AirwayGeojsonOutputBy.Designation"/> run names files from the
/// designation derived from <c>AWY_ID</c>, so a <c>Q</c>/<c>T</c> RNAV
/// airway lands in <c>Airways_Q_*</c> / <c>Airways_T_*</c> and never <c>Airways_RN_*</c>; and that
/// each file goes to the GeoJSON or vNAS folder and gets CRC defaults only as chosen.
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

		AirwaySettings settings = Settings(VnasFileChoices.None);

		AirwayBuildAllResult built = AirwayBuilder.BuildAll(data, settings);
		GeojsonFileSet result = AirwayGeojsonWriter.Generate(built.Airways, settings);

		string[] fileNames = result.FilesWritten.Select(Path.GetFileName).ToArray()!;

		Assert.Contains("Airways_Q_Lines.geojson", fileNames);
		Assert.Contains("Airways_Q_Symbols.geojson", fileNames);
		Assert.Contains("Airways_Q_Text.geojson", fileNames);
		Assert.DoesNotContain(fileNames, name => name!.Contains("_RN_"));
		Assert.All(result.FilesWritten, path => Assert.Equal(Path.Combine(_outputDirectory, "Geojson"), Path.GetDirectoryName(path)));
	}

	[Fact]
	public void vnas_files_go_to_upload_to_vnas_and_only_the_chosen_ones_get_crc_defaults()
	{
		// J: two High airways and one Low - the High defaults are the file's, the Low airway
		// carries its own as an override. V: one Low airway, uploaded without defaults.
		Airway[] airways =
		[
			BuildAirway("J1", AirwayAltitudeClass.High, 40.0),
			BuildAirway("J2", AirwayAltitudeClass.High, 41.0),
			BuildAirway("J3", AirwayAltitudeClass.Low, 42.0),
			BuildAirway("V1", AirwayAltitudeClass.Low, 43.0),
		];

		AirwaySettings settings = Settings(new VnasFileChoices(
			uploadFiles: ["Airways_J_Lines", "Airways_V_Lines"],
			crcDefaultsFiles: ["Airways_J_Lines"])) with
		{
			LineDefaults = new Dictionary<AirwayAltitudeClass, CrcLineDefaults>
			{
				[AirwayAltitudeClass.High] = LineDefaults(bcg: 3),
				[AirwayAltitudeClass.Low] = LineDefaults(bcg: 2),
				[AirwayAltitudeClass.Other] = LineDefaults(bcg: 1),
			},
		};

		GeojsonFileSet result = AirwayGeojsonWriter.Generate(airways, settings);

		string vnas = Path.Combine(_outputDirectory, "Upload_to_vNAS", "Geojson");
		string geojson = Path.Combine(_outputDirectory, "Geojson");

		Assert.Contains(Path.Combine(vnas, "Airways_J_Lines.geojson"), result.FilesWritten);
		Assert.Contains(Path.Combine(vnas, "Airways_V_Lines.geojson"), result.FilesWritten);
		Assert.Contains(Path.Combine(geojson, "Airways_J_Symbols.geojson"), result.FilesWritten);
		Assert.False(File.Exists(Path.Combine(geojson, "Airways_J_Lines.geojson")));

		JsonElement[] jLines = Properties(Path.Combine(vnas, "Airways_J_Lines.geojson"));
		Assert.True(jLines[0].GetProperty("isLineDefaults").GetBoolean());
		Assert.Equal(3, jLines[0].GetProperty("bcg").GetInt32());
		Assert.Equal(2, jLines[3].GetProperty("bcg").GetInt32()); // J3, the Low airway

		// Uploaded without defaults: the one airway, and no isLineDefaults Feature before it.
		Assert.Single(Properties(Path.Combine(vnas, "Airways_V_Lines.geojson")));

		JsonElement[] jSymbols = Properties(Path.Combine(geojson, "Airways_J_Symbols.geojson"));
		Assert.DoesNotContain(jSymbols, properties => properties.TryGetProperty("isSymbolDefaults", out _));
	}

	private AirwaySettings Settings(VnasFileChoices vnas) => new()
	{
		OutputDirectory = _outputDirectory,
		OutputBy = AirwayGeojsonOutputBy.Designation,
		BufferAirwayWaypoints = false,
		IncludeFebCustomProperties = false,
		FebProperties = [],
		GenerateAliasFile = false,
		SplitAtAntimeridian = true,
		Vnas = vnas,
		Roi = null,
	};

	private static CrcLineDefaults LineDefaults(int bcg) => new()
	{
		Bcg = bcg,
		Filters = [bcg],
		Style = "solid",
		Thickness = 1,
	};

	/// <summary>A one-leg airway at the given latitude, its designation taken from its ID.</summary>
	private static Airway BuildAirway(string awyId, AirwayAltitudeClass altitudeClass, double latitude)
	{
		AirwayPoint from = new($"{awyId}AA", "WP", latitude, -80.0, "fix");
		AirwayPoint to = new($"{awyId}BB", "WP", latitude, -81.0, "fix");

		return new Airway
		{
			AwyId = awyId,
			Designation = awyId[..1],
			AwyLocation = "C",
			AltitudeClass = altitudeClass,
			Segments = [new AirwaySegment(from.PointId, to.PointId, IsGap: false, MaxAuthAlt: null)],
			Points = [from, to],
			Geometry = Wgs84.Factory.CreateLineString(
			[
				new Coordinate(from.Longitude, from.Latitude),
				new Coordinate(to.Longitude, to.Latitude),
			]),
		};
	}

	/// <summary>The <c>properties</c> of every Feature in a file, in order.</summary>
	private static JsonElement[] Properties(string path)
	{
		using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
		return [.. document.RootElement.GetProperty("features").EnumerateArray().Select(feature => feature.GetProperty("properties").Clone())];
	}
}
