using System.Text.Json;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Departures;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Domain.Departures.Models;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Departures.Fixtures;
using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Application.Airac.Departures;

/// <summary>
/// Runs the whole Departures pipeline (<see cref="DepartureService.Run"/>) against the DOTSS2
/// fixture and checks what lands on disk: the three GeoJSON files per airport + procedure, the
/// alias file, and the warnings when there is nothing to write.
/// </summary>
public sealed class DepartureServiceTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_Departures_" + Guid.NewGuid().ToString("N"));

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

	private string ProcedureDirectory(bool feBuddyOutputFolder = true) =>
		feBuddyOutputFolder
			? Path.Combine(_outputDirectory, "FE-Buddy_Output", "Departure Procedures", "ZLA", "LAX")
			: Path.Combine(_outputDirectory, "Departure Procedures", "ZLA", "LAX");

	[Fact]
	public void run_writes_lines_symbols_and_text_for_each_airport_procedure_plus_the_alias_file()
	{
		DepartureServiceResult result = DepartureService.Run(DepartureTestData.Dotss(), Settings());

		Assert.Equal(1, result.ProcedureCount);
		Assert.Equal(1, result.ProceduresInScopeCount);
		Assert.Equal(1, result.AirportProcedureCount);
		Assert.Equal(0, result.SkippedForMissingPointsCount);
		Assert.Empty(result.Warnings);

		string lines = Path.Combine(ProcedureDirectory(), "LAX_DOTSS_Lines.geojson");
		string symbols = Path.Combine(ProcedureDirectory(), "LAX_DOTSS_Symbols.geojson");
		string text = Path.Combine(ProcedureDirectory(), "LAX_DOTSS_Text.geojson");

		Assert.Equal([lines, symbols, text], result.GeojsonFilesWritten);
		Assert.Equal(1, result.GeojsonFeatureCountsByFile[lines]);
		Assert.Equal(DepartureTestData.DotssFixes.Count, result.GeojsonFeatureCountsByFile[symbols]);
		Assert.Equal(DepartureTestData.DotssFixes.Count, result.GeojsonFeatureCountsByFile[text]);

		Assert.Equal(1, result.AliasCommandCount);
		Assert.Equal(
			Path.Combine(_outputDirectory, "FE-Buddy_Output", "Departure Procedures", "Alias", "Departures.txt"),
			result.AliasFilePath);
		Assert.StartsWith(".laxDOTSSf .FF DLREY ", File.ReadAllText(result.AliasFilePath!));
	}

	[Fact]
	public void run_puts_crc_defaults_first_and_feb_properties_on_every_feature_when_asked()
	{
		DepartureServiceResult result = DepartureService.Run(DepartureTestData.Dotss(), Settings(
			("IncludeCrcLineDefaults", "Y"),
			("IncludeCrcSymbolDefaults", "Y"),
			("IncludeCrcTextDefaults", "Y"),
			("Crc.Departures.Line.bcg", "3"), ("Crc.Departures.Line.filters", "3"),
			("Crc.Departures.Line.style", "solid"), ("Crc.Departures.Line.thickness", "1"),
			("Crc.Departures.Symbol.bcg", "3"), ("Crc.Departures.Symbol.filters", "3"),
			("Crc.Departures.Symbol.style", "vor"), ("Crc.Departures.Symbol.size", "1"),
			("Crc.Departures.Text.bcg", "3"), ("Crc.Departures.Text.filters", "3"),
			("Crc.Departures.Text.size", "1"), ("Crc.Departures.Text.underline", "N"),
			("Crc.Departures.Text.opaque", "N"), ("Crc.Departures.Text.xOffset", "0"),
			("Crc.Departures.Text.yOffset", "0"),
			("IncludeFebCustomProperties", "Y"),
			("FebProperties", "dpName,pointId,arptId,artcc,amendmentNo,amendEffDate,waypoints"),
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
			Assert.Equal("DOTSS", properties.GetProperty("feb.dpName").GetString());
			Assert.Equal("LAX", properties.GetProperty("feb.arptId").GetString());
			Assert.Equal("ZLA", properties.GetProperty("feb.artcc").GetString());
			Assert.Equal("TWO", properties.GetProperty("feb.amendmentNo").GetString());
			Assert.Equal("2017/08/17", properties.GetProperty("feb.amendEffDate").GetString());
		}
	}

	[Fact]
	public void run_without_the_fe_buddy_output_folder_writes_straight_under_the_output_directory()
	{
		DepartureServiceResult result = DepartureService.Run(DepartureTestData.Dotss(), Settings(
			("AddFeBuddyOutputFolder", "N"),
			("EmitSymbols", "N"),
			("EmitText", "N")));

		Assert.Equal([Path.Combine(ProcedureDirectory(feBuddyOutputFolder: false), "LAX_DOTSS_Lines.geojson")], result.GeojsonFilesWritten);
		Assert.Equal(Path.Combine(_outputDirectory, "Departure Procedures", "Alias", "Departures.txt"), result.AliasFilePath);
	}

	[Fact]
	public void run_with_geojson_off_writes_only_the_alias_file()
	{
		DepartureServiceResult result = DepartureService.Run(DepartureTestData.Dotss(), Settings(("GenerateGeojson", "N")));

		Assert.Equal(1, result.AirportProcedureCount);
		Assert.Empty(result.GeojsonFilesWritten);
		Assert.Empty(result.GeojsonFeatureCountsByFile);
		Assert.Equal(1, result.AliasCommandCount);
		Assert.False(Directory.Exists(ProcedureDirectory()));
	}

	[Fact]
	public void run_whose_filters_leave_nothing_says_why_nothing_was_written()
	{
		DepartureServiceResult result = DepartureService.Run(DepartureTestData.Dotss(), Settings(("ArtccFilter", "ZNY")));

		Assert.Equal(1, result.ProcedureCount);
		Assert.Equal(0, result.ProceduresInScopeCount);
		Assert.Equal(0, result.AirportProcedureCount);
		Assert.Empty(result.GeojsonFilesWritten);
		Assert.Null(result.AliasFilePath);
		Assert.Contains(result.Messages, m => m.IsAdvisory && m.Text.Contains("No departure procedures matched your filters", StringComparison.Ordinal));
	}

	[Fact]
	public void run_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => DepartureService.Run(null!, Settings()));
		Assert.Throws<ArgumentNullException>(() => DepartureService.Run(new NasrCsvDataCollection(), null!));
	}

	[Fact]
	public void a_single_point_procedure_gets_symbols_and_text_but_no_lines_file()
	{
		DeparturePoint only = new("PAVYL", "WP", 42.2, -83.3);
		DepartureAirportProcedure airportProcedure = DepartureTestData.AirportProcedure(DepartureTestData.Procedure(), "DTW", only);
		DepartureSettings settings = DepartureSettingsParser.Parse(Settings()).Settings;

		GeojsonFileSet result = DepartureGeojsonWriter.Generate([airportProcedure], settings);

		Assert.Equal(["DTW_ABC_Symbols.geojson", "DTW_ABC_Text.geojson"], result.FilesWritten.Select(Path.GetFileName));
	}

	[Fact]
	public void geojson_generate_rejects_null_arguments()
	{
		DepartureSettings settings = DepartureSettingsParser.Parse(Settings()).Settings;

		Assert.Throws<ArgumentNullException>(() => DepartureGeojsonWriter.Generate(null!, settings));
		Assert.Throws<ArgumentNullException>(() => DepartureGeojsonWriter.Generate([], null!));
	}

	[Theory]
	[InlineData(DepartureFebProperty.DpName, "dpName")]
	[InlineData(DepartureFebProperty.PointId, "pointId")]
	[InlineData(DepartureFebProperty.ArptId, "arptId")]
	[InlineData(DepartureFebProperty.Artcc, "artcc")]
	[InlineData(DepartureFebProperty.AmendmentNo, "amendmentNo")]
	[InlineData(DepartureFebProperty.AmendEffDate, "amendEffDate")]
	[InlineData(DepartureFebProperty.Waypoints, "waypoints")]
	[InlineData((DepartureFebProperty)999, "999")]
	public void feb_property_names_are_camel_case(DepartureFebProperty property, string expected)
	{
		Assert.Equal(expected, FebProperties.Name(property));
	}

	[Fact]
	public void alias_skips_a_procedure_with_no_points_and_warns_on_a_duplicate_command()
	{
		DeparturePoint point = new("PAVYL", "WP", 42.2, -83.3);
		DepartureProcedure procedure = DepartureTestData.Procedure();
		DepartureSettings settings = DepartureSettingsParser.Parse(Settings()).Settings;

		DepartureAliasGenerateResult result = DepartureAliasWriter.Generate(
			[
				DepartureTestData.AirportProcedure(procedure, "DTW"),
				DepartureTestData.AirportProcedure(procedure, "DTW", point),
				DepartureTestData.AirportProcedure(procedure, "DTW", point),
			],
			settings);

		Assert.Equal(1, result.CommandCount);
		Assert.Equal(".dtwABCf .FF PAVYL" + Environment.NewLine, File.ReadAllText(result.FilePath!));
		Assert.Contains("'.dtwABCf' was already written", Assert.Single(result.Messages).Text, StringComparison.Ordinal);
	}

	[Fact]
	public void alias_with_nothing_to_write_writes_no_file()
	{
		DepartureSettings settings = DepartureSettingsParser.Parse(Settings()).Settings;

		DepartureAliasGenerateResult result = DepartureAliasWriter.Generate(
			[DepartureTestData.AirportProcedure(DepartureTestData.Procedure(), "DTW")], settings);

		Assert.Null(result.FilePath);
		Assert.Equal(0, result.CommandCount);
		Assert.Throws<ArgumentNullException>(() => DepartureAliasWriter.Generate(null!, settings));
		Assert.Throws<ArgumentNullException>(() => DepartureAliasWriter.Generate([], null!));
	}

	[Fact]
	public void a_transition_that_does_not_continue_any_body_is_drawn_as_its_own_line()
	{
		DeparturePoint alpha = new("ALPHA", "WP", 42.0, -83.0);
		DeparturePoint bravo = new("BRAVO", "WP", 42.1, -83.1);
		DeparturePoint xray = new("XRAYY", "WP", 43.0, -84.0);
		DeparturePoint yank = new("YANKE", "WP", 43.1, -84.1);

		DepartureAirportProcedure airportProcedure = new()
		{
			Procedure = DepartureTestData.Procedure(),
			AirportId = "DTW",
			Routes =
			[
				new DepartureRoute("BODY", DepartureRouteKind.Body, [alpha, bravo]),
				new DepartureRoute("XRAYY TRANSITION", DepartureRouteKind.Transition, [xray, yank]),
			],
			Points = [alpha, bravo, xray, yank],
		};

		MultiLineString? geometry = DepartureGeometryBuilder.Build(airportProcedure);

		Assert.Equal(2, geometry!.NumGeometries);
	}
}
