using System.Text.Json;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Arrivals;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Domain.Arrivals.Models;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Arrivals.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Arrivals;

/// <summary>
/// Runs the whole Arrivals pipeline (<see cref="ArrivalService.Run"/>) against the BLAID2 fixture
/// and checks what lands on disk, and where: the three GeoJSON files per airport + procedure, the
/// alias file, the vNAS folder, and the warnings when there is nothing to write.
/// </summary>
public sealed class ArrivalServiceTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_Arrivals_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_outputDirectory))
		{
			Directory.Delete(_outputDirectory, recursive: true);
		}
	}

	private Dictionary<string, string> Settings(params (string Key, string Value)[] overrides)
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = _outputDirectory,
		};

		foreach ((string key, string value) in overrides)
		{
			settings[key] = value;
		}

		return settings;
	}

	private string ProcedureDirectory(bool uploadToVnas = false) =>
		uploadToVnas
			? Path.Combine(_outputDirectory, "Upload_to_vNAS", "Geojson", "ZLA", "LAS")
			: Path.Combine(_outputDirectory, "Geojson", "ZLA", "LAS");

	[Fact]
	public void run_writes_lines_symbols_and_text_for_each_airport_procedure_plus_the_alias_file()
	{
		ArrivalServiceResult result = ArrivalService.Run(ArrivalTestData.Blaid(), Settings());

		Assert.Equal(1, result.ProcedureCount);
		Assert.Equal(1, result.ProceduresInScopeCount);
		Assert.Equal(1, result.AirportProcedureCount);
		Assert.Equal(0, result.SkippedForMissingPointsCount);
		Assert.Empty(result.Warnings);

		string lines = Path.Combine(ProcedureDirectory(), "LAS_BLAID_STAR_Lines.geojson");
		string symbols = Path.Combine(ProcedureDirectory(), "LAS_BLAID_STAR_Symbols.geojson");
		string text = Path.Combine(ProcedureDirectory(), "LAS_BLAID_STAR_Text.geojson");

		Assert.Equal([lines, symbols, text], result.GeojsonFilesWritten);
		int pointCount = ArrivalTestData.BlaidFixes.Count + ArrivalTestData.BlaidNavaids.Count;
		Assert.Equal(1, result.GeojsonFeatureCountsByFile[lines]);
		Assert.Equal(pointCount, result.GeojsonFeatureCountsByFile[symbols]);
		Assert.Equal(pointCount, result.GeojsonFeatureCountsByFile[text]);

		Assert.Equal(1, result.AliasCommandCount);
		Assert.Equal(Path.Combine(_outputDirectory, "Arrivals.txt"), result.AliasFilePath);
		Assert.StartsWith(".lasBLAIDf .FF BCE ", File.ReadAllText(result.AliasFilePath!));
	}

	[Fact]
	public void run_puts_crc_defaults_first_and_feb_properties_on_every_feature_when_asked()
	{
		ArrivalServiceResult result = ArrivalService.Run(ArrivalTestData.Blaid(), Settings(
			("UploadToVnas", "Arrivals_Lines,Arrivals_Symbols,Arrivals_Text"),
			("CrcDefaultsFor", "Arrivals_Lines,Arrivals_Symbols,Arrivals_Text"),
			("Crc.Arrivals.Line.bcg", "3"), ("Crc.Arrivals.Line.filters", "3"),
			("Crc.Arrivals.Line.style", "solid"), ("Crc.Arrivals.Line.thickness", "1"),
			("Crc.Arrivals.Symbol.bcg", "3"), ("Crc.Arrivals.Symbol.filters", "3"),
			("Crc.Arrivals.Symbol.style", "vor"), ("Crc.Arrivals.Symbol.size", "1"),
			("Crc.Arrivals.Text.bcg", "3"), ("Crc.Arrivals.Text.filters", "3"),
			("Crc.Arrivals.Text.size", "1"), ("Crc.Arrivals.Text.underline", "N"),
			("Crc.Arrivals.Text.opaque", "N"), ("Crc.Arrivals.Text.xOffset", "0"),
			("Crc.Arrivals.Text.yOffset", "0"),
			("IncludeFebCustomProperties", "Y"),
			("FebProperties", "arrivalName,pointId,arptId,artcc,amendmentNo,amendEffDate,waypoints"),
			("GenerateAliasFile", "N")));

		Assert.Null(result.AliasFilePath);

		foreach (string path in result.GeojsonFilesWritten)
		{
			using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
			JsonElement[] features = [.. document.RootElement.GetProperty("features").EnumerateArray()];

			string defaultsFlag = path.EndsWith("_Lines.geojson", StringComparison.Ordinal) ? "isLineDefaults"
				: path.EndsWith("_Symbols.geojson", StringComparison.Ordinal) ? "isSymbolDefaults"
				: "isTextDefaults";
			Assert.True(features[0].GetProperty("properties").GetProperty(defaultsFlag).GetBoolean(), path);

			JsonElement properties = features[1].GetProperty("properties");
			Assert.Equal("BLAID", properties.GetProperty("feb.arrivalName").GetString());
			Assert.Equal("LAS", properties.GetProperty("feb.arptId").GetString());
			Assert.Equal("ZLA", properties.GetProperty("feb.artcc").GetString());
			Assert.Equal("TWO", properties.GetProperty("feb.amendmentNo").GetString());
			Assert.Equal("2024/01/25", properties.GetProperty("feb.amendEffDate").GetString());
		}
	}

	[Fact]
	public void a_kind_marked_for_vnas_goes_under_upload_to_vnas_in_its_artcc_and_airport_folders()
	{
		ArrivalServiceResult result = ArrivalService.Run(ArrivalTestData.Blaid(), Settings(
			("UploadToVnas", "Arrivals_Lines,Arrivals.txt"),
			("EmitText", "N")));

		Assert.Equal(
			[
				Path.Combine(ProcedureDirectory(uploadToVnas: true), "LAS_BLAID_STAR_Lines.geojson"),
				Path.Combine(ProcedureDirectory(), "LAS_BLAID_STAR_Symbols.geojson"),
			],
			result.GeojsonFilesWritten);
		Assert.Equal(Path.Combine(_outputDirectory, "Upload_to_vNAS", "Arrivals.txt"), result.AliasFilePath);

		// Uploaded without defaults: the procedure's one Feature, and no isLineDefaults before it.
		using JsonDocument lines = JsonDocument.Parse(File.ReadAllText(result.GeojsonFilesWritten[0]));
		Assert.Single(lines.RootElement.GetProperty("features").EnumerateArray());
	}

	[Fact]
	public void run_with_geojson_off_writes_only_the_alias_file()
	{
		ArrivalServiceResult result = ArrivalService.Run(ArrivalTestData.Blaid(), Settings(("GenerateGeojson", "N")));

		Assert.Equal(1, result.AirportProcedureCount);
		Assert.Empty(result.GeojsonFilesWritten);
		Assert.Empty(result.GeojsonFeatureCountsByFile);
		Assert.Equal(1, result.AliasCommandCount);
		Assert.False(Directory.Exists(ProcedureDirectory()));
	}

	[Fact]
	public void run_whose_filters_leave_nothing_says_why_nothing_was_written()
	{
		ArrivalServiceResult result = ArrivalService.Run(ArrivalTestData.Blaid(), Settings(("ArtccFilter", "ZNY")));

		Assert.Equal(1, result.ProcedureCount);
		Assert.Equal(0, result.ProceduresInScopeCount);
		Assert.Equal(0, result.AirportProcedureCount);
		Assert.Empty(result.GeojsonFilesWritten);
		Assert.Null(result.AliasFilePath);
		Assert.Contains(result.Messages, m => m.IsAdvisory && m.Text.Contains("No arrival procedures matched your filters", StringComparison.Ordinal));
	}

	[Fact]
	public void run_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => ArrivalService.Run(null!, Settings()));
		Assert.Throws<ArgumentNullException>(() => ArrivalService.Run(new NasrCsvDataCollection(), null!));
	}

	[Fact]
	public void invalid_settings_errors_propagate_from_run()
	{
		Assert.Throws<ArgumentException>(() => ArrivalService.Run(ArrivalTestData.Blaid(), Settings(("RoiMode", "Sideways"))));
	}

	[Fact]
	public void a_single_point_procedure_gets_symbols_and_text_but_no_lines_file()
	{
		ArrivalPoint only = new("PAVYL", "RP", 42.2, -83.3);
		ArrivalAirportProcedure airportProcedure = ArrivalTestData.AirportProcedure(ArrivalTestData.Procedure(), "DTW", only);
		ArrivalSettings settings = ArrivalSettingsParser.Parse(Settings()).Settings;

		GeojsonFileSet result = ArrivalGeojsonWriter.Generate([airportProcedure], settings);

		Assert.Equal(["DTW_ABC_STAR_Symbols.geojson", "DTW_ABC_STAR_Text.geojson"], result.FilesWritten.Select(Path.GetFileName));
	}

	[Fact]
	public void geojson_generate_rejects_null_arguments()
	{
		ArrivalSettings settings = ArrivalSettingsParser.Parse(Settings()).Settings;

		Assert.Throws<ArgumentNullException>(() => ArrivalGeojsonWriter.Generate(null!, settings));
		Assert.Throws<ArgumentNullException>(() => ArrivalGeojsonWriter.Generate([], null!));
	}

	[Theory]
	[InlineData(ArrivalFebProperty.ArrivalName, "arrivalName")]
	[InlineData(ArrivalFebProperty.PointId, "pointId")]
	[InlineData(ArrivalFebProperty.ArptId, "arptId")]
	[InlineData(ArrivalFebProperty.Artcc, "artcc")]
	[InlineData(ArrivalFebProperty.AmendmentNo, "amendmentNo")]
	[InlineData(ArrivalFebProperty.AmendEffDate, "amendEffDate")]
	[InlineData(ArrivalFebProperty.Waypoints, "waypoints")]
	[InlineData((ArrivalFebProperty)999, "999")]
	public void feb_property_names_are_camel_case(ArrivalFebProperty property, string expected)
	{
		Assert.Equal(expected, FebProperties.Name(property));
	}

	[Fact]
	public void alias_skips_a_procedure_with_no_points_and_warns_on_a_duplicate_command()
	{
		ArrivalPoint point = new("PAVYL", "RP", 42.2, -83.3);
		ArrivalProcedure procedure = ArrivalTestData.Procedure();
		ArrivalSettings settings = ArrivalSettingsParser.Parse(Settings()).Settings;

		ArrivalAliasGenerateResult result = ArrivalAliasWriter.Generate(
			[
				ArrivalTestData.AirportProcedure(procedure, "DTW"),
				ArrivalTestData.AirportProcedure(procedure, "DTW", point),
				ArrivalTestData.AirportProcedure(procedure, "DTW", point),
			],
			settings);

		Assert.Equal(1, result.CommandCount);
		Assert.Equal(".dtwABCf .FF PAVYL" + Environment.NewLine, File.ReadAllText(result.FilePath!));
		Assert.Contains("'.dtwABCf' was already written", Assert.Single(result.Messages).Text, StringComparison.Ordinal);
	}

	[Fact]
	public void alias_with_nothing_to_write_writes_no_file()
	{
		ArrivalSettings settings = ArrivalSettingsParser.Parse(Settings()).Settings;

		ArrivalAliasGenerateResult result = ArrivalAliasWriter.Generate(
			[ArrivalTestData.AirportProcedure(ArrivalTestData.Procedure(), "DTW")], settings);

		Assert.Null(result.FilePath);
		Assert.Equal(0, result.CommandCount);
		Assert.Throws<ArgumentNullException>(() => ArrivalAliasWriter.Generate(null!, settings));
		Assert.Throws<ArgumentNullException>(() => ArrivalAliasWriter.Generate([], null!));
	}
}
