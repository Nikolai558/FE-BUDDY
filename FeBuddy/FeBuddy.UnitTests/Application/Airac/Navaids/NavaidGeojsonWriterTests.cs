using System.Text.Json;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Navaids.Models;

using FeBuddy.UnitTests.Application.Airac.Navaids.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Navaids;

/// <summary>
/// Covers <see cref="NavaidGeojsonWriter"/>: All-mode vs. Type-mode file grouping, the ROI filter,
/// which <c>feb.*</c> properties land on the Symbols file vs. the Text file, when a per-Feature
/// <c>style</c> is written (only a merged All-mode Symbols file with CRC defaults and
/// <c>SymbolStyleBy=Type</c>), and vNAS folder routing.
/// </summary>
public sealed class NavaidGeojsonWriterTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_NavGeojson_" + Guid.NewGuid().ToString("N"));

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

	private NavaidSettings Settings() => new()
	{
		OutputDirectory = _outputDirectory,
		GenerateGeojson = true,
		GenerateAliasFile = false,
		IncludeFebCustomProperties = false,
	};

	private static CrcSymbolDefaults SymbolDefaults(string? style) =>
		new() { Bcg = 3, Filters = [3], Style = style, Size = 1 };

	private static List<JsonElement> FeaturesOf(string path)
	{
		using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
		return [.. document.RootElement.GetProperty("features").EnumerateArray().Select(feature => feature.Clone())];
	}

	/// <summary>The Symbols file's Features, picked out from every file the run wrote.</summary>
	private static List<JsonElement> SymbolFeatures(NavaidGeojsonGenerateResult result) =>
		FeaturesOf(Assert.Single(result.Files.FilesWritten, f => f.Contains("_Symbols.geojson", StringComparison.Ordinal)));

	// ---- All mode vs. Type mode ----

	[Fact]
	public void all_mode_writes_one_symbols_file_and_one_text_file_for_every_type_mixed_together()
	{
		NavaidGeojsonGenerateResult result = NavaidGeojsonWriter.Generate(
			[NavaidTestData.Cgt(), NavaidTestData.FanMarker()], Settings());

		Assert.Equal(
			[
				Path.Combine(_outputDirectory, "Geojson", "NAVAIDs_Symbols.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "NAVAIDs_Text.geojson"),
			],
			result.Files.FilesWritten);
		Assert.Equal(2, FeaturesOf(result.Files.FilesWritten[0]).Count);
	}

	[Fact]
	public void type_mode_writes_one_symbols_and_text_pair_per_type_present()
	{
		NavaidSettings settings = Settings() with { OutputBy = NavaidOutputBy.Type };

		NavaidGeojsonGenerateResult result = NavaidGeojsonWriter.Generate(
			[NavaidTestData.Cgt(), NavaidTestData.AaCedar()], settings);

		Assert.Equal(
			[
				Path.Combine(_outputDirectory, "Geojson", "NAVAIDs_NDBs_Symbols.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "NAVAIDs_NDBs_Text.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "NAVAIDs_VORTACs_Symbols.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "NAVAIDs_VORTACs_Text.geojson"),
			],
			result.Files.FilesWritten);
	}

	[Fact]
	public void emit_symbols_or_emit_text_off_skips_that_files_kind_only()
	{
		NavaidSettings settings = Settings() with { EmitText = false };

		NavaidGeojsonGenerateResult result = NavaidGeojsonWriter.Generate([NavaidTestData.Cgt()], settings);

		string written = Assert.Single(result.Files.FilesWritten);
		Assert.EndsWith("NAVAIDs_Symbols.geojson", written);
	}

	[Fact]
	public void generate_writes_nothing_when_geojson_is_off_or_there_are_no_navaids()
	{
		NavaidGeojsonGenerateResult offResult = NavaidGeojsonWriter.Generate(
			[NavaidTestData.Cgt()], Settings() with { GenerateGeojson = false });
		Assert.Empty(offResult.Files.FilesWritten);

		NavaidGeojsonGenerateResult emptyResult = NavaidGeojsonWriter.Generate([], Settings());
		Assert.Empty(emptyResult.Files.FilesWritten);
	}

	[Fact]
	public void generate_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => NavaidGeojsonWriter.Generate(null!, Settings()));
		Assert.Throws<ArgumentNullException>(() => NavaidGeojsonWriter.Generate([], null!));
	}

	// ---- ROI ----

	[Fact]
	public void filter_to_roi_keeps_only_navaids_inside_the_box()
	{
		Navaid chicagoArea = NavaidTestData.Cgt();
		Navaid nevadaArea = NavaidTestData.Ely();
		RegionOfInterest chicagoRoi = new(40.0, -89.0, 43.0, -86.0);

		IReadOnlyList<Navaid> kept = NavaidGeojsonWriter.FilterToRoi([chicagoArea, nevadaArea], chicagoRoi);

		Assert.Equal(["CGT"], kept.Select(n => n.NavId));
	}

	[Fact]
	public void filter_to_roi_with_no_roi_keeps_every_navaid()
	{
		IReadOnlyList<Navaid> navaids = [NavaidTestData.Cgt(), NavaidTestData.Ely()];

		IReadOnlyList<Navaid> kept = NavaidGeojsonWriter.FilterToRoi(navaids, null);

		Assert.Same(navaids, kept);
	}

	// ---- feb.* properties ----

	private static readonly NavaidFebProperty[] AllFebProperties =
	[
		NavaidFebProperty.NavId, NavaidFebProperty.NavType, NavaidFebProperty.Name,
		NavaidFebProperty.Freq, NavaidFebProperty.LowAltArtccId, NavaidFebProperty.HighAltArtccId,
	];

	private NavaidSettings SettingsWithAllFebProperties() => Settings() with
	{
		IncludeFebCustomProperties = true,
		FebProperties = AllFebProperties,
	};

	[Fact]
	public void the_text_files_feature_carries_the_id_and_name_plus_type_and_never_navid_navtype_or_name_as_feb_properties()
	{
		NavaidGeojsonGenerateResult result = NavaidGeojsonWriter.Generate([NavaidTestData.Cgt()], SettingsWithAllFebProperties());

		string textFile = Assert.Single(result.Files.FilesWritten, f => f.Contains("_Text.geojson", StringComparison.Ordinal));
		JsonElement properties = Assert.Single(FeaturesOf(textFile)).GetProperty("properties");

		Assert.Equal(
			["CGT", "CHICAGO HEIGHTS VORTAC"],
			properties.GetProperty("text").EnumerateArray().Select(e => e.GetString()));
		Assert.False(properties.TryGetProperty("feb.navId", out _));
		Assert.False(properties.TryGetProperty("feb.navType", out _));
		Assert.False(properties.TryGetProperty("feb.name", out _));
	}

	[Fact]
	public void the_text_files_feature_still_carries_freq_and_artccs_when_selected()
	{
		NavaidGeojsonGenerateResult result = NavaidGeojsonWriter.Generate([NavaidTestData.Cgt()], SettingsWithAllFebProperties());

		string textFile = Assert.Single(result.Files.FilesWritten, f => f.Contains("_Text.geojson", StringComparison.Ordinal));
		JsonElement properties = Assert.Single(FeaturesOf(textFile)).GetProperty("properties");

		Assert.Equal(114.2, properties.GetProperty("feb.freq").GetDouble());
		Assert.Equal("ZAU", properties.GetProperty("feb.lowAltArtccId").GetString());
		Assert.Equal("ZAU", properties.GetProperty("feb.highAltArtccId").GetString());
	}

	[Fact]
	public void the_symbol_files_feature_carries_every_selected_feb_property_including_navid_navtype_and_name()
	{
		NavaidGeojsonGenerateResult result = NavaidGeojsonWriter.Generate([NavaidTestData.Cgt()], SettingsWithAllFebProperties());

		string symbolFile = Assert.Single(result.Files.FilesWritten, f => f.Contains("_Symbols.geojson", StringComparison.Ordinal));
		JsonElement properties = Assert.Single(FeaturesOf(symbolFile)).GetProperty("properties");

		Assert.Equal("CGT", properties.GetProperty("feb.navId").GetString());
		Assert.Equal("VORTAC", properties.GetProperty("feb.navType").GetString());
		Assert.Equal("CHICAGO HEIGHTS", properties.GetProperty("feb.name").GetString());
		Assert.Equal(114.2, properties.GetProperty("feb.freq").GetDouble());
		Assert.Equal(JsonValueKind.Number, properties.GetProperty("feb.freq").ValueKind);
	}

	[Fact]
	public void a_blank_artcc_is_omitted_rather_than_written_as_an_empty_string()
	{
		NavaidGeojsonGenerateResult result = NavaidGeojsonWriter.Generate([NavaidTestData.AbqVot()], SettingsWithAllFebProperties());

		string symbolFile = Assert.Single(result.Files.FilesWritten, f => f.Contains("_Symbols.geojson", StringComparison.Ordinal));
		JsonElement properties = Assert.Single(FeaturesOf(symbolFile)).GetProperty("properties");

		Assert.False(properties.TryGetProperty("feb.lowAltArtccId", out _));
		Assert.False(properties.TryGetProperty("feb.highAltArtccId", out _));
	}

	[Fact]
	public void no_feb_properties_are_written_when_feb_properties_are_off()
	{
		NavaidGeojsonGenerateResult result = NavaidGeojsonWriter.Generate([NavaidTestData.Cgt()], Settings());

		foreach (string file in result.Files.FilesWritten)
		{
			Assert.All(FeaturesOf(file), feature =>
				Assert.DoesNotContain(
					feature.GetProperty("properties").EnumerateObject(),
					property => property.Name.StartsWith("feb.", StringComparison.Ordinal)));
		}
	}

	// ---- per-Feature style ----

	private NavaidSettings AllModeSymbolsWithCrcDefaults(string? classStyle, NavaidSymbolStyleBy styleBy, string? fanMarkerStyle = "otherWaypoints") =>
		Settings() with
		{
			SymbolStyleBy = styleBy,
			FanMarkerStyle = fanMarkerStyle,
			Vnas = new VnasFileChoices([NavaidOutputFiles.Symbols], [NavaidOutputFiles.Symbols]),
			SymbolDefaults = new Dictionary<string, CrcSymbolDefaults>(StringComparer.OrdinalIgnoreCase)
			{
				[NavaidOutputFiles.AllClass] = SymbolDefaults(classStyle),
			},
		};

	[Fact]
	public void a_merged_symbols_file_with_style_by_type_gives_each_feature_its_own_mapped_style()
	{
		NavaidSettings settings = AllModeSymbolsWithCrcDefaults(classStyle: null, NavaidSymbolStyleBy.Type);

		NavaidGeojsonGenerateResult result = NavaidGeojsonWriter.Generate(
			[NavaidTestData.Cgt(), NavaidTestData.Elo(), NavaidTestData.FanMarker()], settings);

		List<JsonElement> features = SymbolFeatures(result);
		JsonElement defaultsFeature = features[0];

		Assert.True(defaultsFeature.GetProperty("properties").GetProperty("isSymbolDefaults").GetBoolean());
		Assert.False(defaultsFeature.GetProperty("properties").TryGetProperty("style", out _));

		JsonElement cgtSymbol = features[1].GetProperty("properties");
		JsonElement eloSymbol = features[2].GetProperty("properties");
		JsonElement fanSymbol = features[3].GetProperty("properties");

		Assert.Equal("vor", cgtSymbol.GetProperty("style").GetString());
		Assert.Equal("tacan", eloSymbol.GetProperty("style").GetString());
		Assert.Equal("otherWaypoints", fanSymbol.GetProperty("style").GetString());
	}

	[Fact]
	public void a_consolan_or_unknown_type_gets_no_style_and_exactly_one_warning_per_type()
	{
		NavaidSettings settings = AllModeSymbolsWithCrcDefaults(classStyle: null, NavaidSymbolStyleBy.Type);

		NavaidGeojsonGenerateResult result = NavaidGeojsonWriter.Generate(
			[NavaidTestData.Consolan("NY1"), NavaidTestData.Consolan("NY2")], settings);

		List<JsonElement> features = SymbolFeatures(result);
		Assert.All(features.Skip(1), f => Assert.False(f.GetProperty("properties").TryGetProperty("style", out _)));

		string warning = Assert.Single(result.Messages).Text;
		Assert.Contains("CONSOLAN", warning);
	}

	[Fact]
	public void a_fan_marker_with_no_chosen_style_gets_none_and_one_fan_marker_warning()
	{
		NavaidSettings settings = AllModeSymbolsWithCrcDefaults(classStyle: null, NavaidSymbolStyleBy.Type, fanMarkerStyle: null);

		NavaidGeojsonGenerateResult result = NavaidGeojsonWriter.Generate(
			[NavaidTestData.Cgt(), NavaidTestData.FanMarker()], settings);

		List<JsonElement> features = SymbolFeatures(result);
		Assert.Equal("vor", features[1].GetProperty("properties").GetProperty("style").GetString());
		Assert.False(features[2].GetProperty("properties").TryGetProperty("style", out _));

		string warning = Assert.Single(result.Messages).Text;
		Assert.Contains("fan marker", warning, StringComparison.OrdinalIgnoreCase);
	}

	[Fact]
	public void style_is_never_written_per_feature_in_type_mode_even_with_crc_defaults()
	{
		NavaidSettings settings = Settings() with
		{
			OutputBy = NavaidOutputBy.Type,
			Vnas = new VnasFileChoices(["NAVAIDs_VORTACs_Symbols"], ["NAVAIDs_VORTACs_Symbols"]),
			SymbolDefaults = new Dictionary<string, CrcSymbolDefaults>(StringComparer.OrdinalIgnoreCase)
			{
				["VORTAC"] = SymbolDefaults("vor"),
			},
		};

		NavaidGeojsonGenerateResult result = NavaidGeojsonWriter.Generate([NavaidTestData.Cgt()], settings);

		List<JsonElement> features = SymbolFeatures(result);
		Assert.Equal("vor", features[0].GetProperty("properties").GetProperty("style").GetString());
		Assert.False(features[1].GetProperty("properties").TryGetProperty("style", out _));
	}

	[Fact]
	public void style_is_never_written_per_feature_when_symbol_style_is_by_file()
	{
		NavaidSettings settings = AllModeSymbolsWithCrcDefaults(classStyle: "vor", NavaidSymbolStyleBy.File, fanMarkerStyle: null);

		NavaidGeojsonGenerateResult result = NavaidGeojsonWriter.Generate([NavaidTestData.Cgt(), NavaidTestData.Elo()], settings);

		List<JsonElement> features = SymbolFeatures(result);
		Assert.Equal("vor", features[0].GetProperty("properties").GetProperty("style").GetString());
		Assert.All(features.Skip(1), f => Assert.False(f.GetProperty("properties").TryGetProperty("style", out _)));
	}

	[Fact]
	public void style_is_never_written_per_feature_when_the_file_has_no_crc_defaults()
	{
		NavaidGeojsonGenerateResult result = NavaidGeojsonWriter.Generate([NavaidTestData.Cgt()], Settings());

		List<JsonElement> features = SymbolFeatures(result);
		Assert.False(features[0].GetProperty("properties").TryGetProperty("isSymbolDefaults", out _));
		Assert.False(features[0].GetProperty("properties").TryGetProperty("style", out _));
	}

	// ---- vNAS folder routing ----

	[Fact]
	public void a_file_marked_for_vnas_goes_under_upload_to_vnas_while_others_do_not()
	{
		NavaidSettings settings = Settings() with
		{
			Vnas = new VnasFileChoices(["NAVAIDs_Symbols"], []),
		};

		NavaidGeojsonGenerateResult result = NavaidGeojsonWriter.Generate([NavaidTestData.Cgt()], settings);

		Assert.Contains(result.Files.FilesWritten, f => f.Contains("Upload_to_vNAS", StringComparison.Ordinal) && f.EndsWith("NAVAIDs_Symbols.geojson", StringComparison.Ordinal));
		Assert.Contains(result.Files.FilesWritten, f => !f.Contains("Upload_to_vNAS", StringComparison.Ordinal) && f.EndsWith("NAVAIDs_Text.geojson", StringComparison.Ordinal));
	}

	[Fact]
	public void type_mode_routes_each_types_files_to_vnas_independently()
	{
		NavaidSettings settings = Settings() with
		{
			OutputBy = NavaidOutputBy.Type,
			Vnas = new VnasFileChoices(["NAVAIDs_VORTACs_Symbols"], []),
		};

		NavaidGeojsonGenerateResult result = NavaidGeojsonWriter.Generate(
			[NavaidTestData.Cgt(), NavaidTestData.AaCedar()], settings);

		Assert.Contains(result.Files.FilesWritten, f => f.Contains("Upload_to_vNAS", StringComparison.Ordinal) && f.EndsWith("NAVAIDs_VORTACs_Symbols.geojson", StringComparison.Ordinal));
		Assert.Contains(result.Files.FilesWritten, f => !f.Contains("Upload_to_vNAS", StringComparison.Ordinal) && f.EndsWith("NAVAIDs_NDBs_Symbols.geojson", StringComparison.Ordinal));
	}
}
