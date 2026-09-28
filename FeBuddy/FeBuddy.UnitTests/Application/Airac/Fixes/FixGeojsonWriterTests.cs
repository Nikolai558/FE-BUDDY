using System.Text.Json;

using FeBuddy.Core.Application.Airac.Fixes;
using FeBuddy.Core.Application.Airac.Fixes.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Fixes.Models;

using FeBuddy.UnitTests.Application.Airac.Fixes.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Fixes;

/// <summary>
/// Covers <see cref="FixGeojsonWriter"/>: the ROI filter, how each <see cref="FixOutputBy"/> value
/// groups fixes into files, which <c>feb.*</c> properties land on the Symbols file vs. the Text
/// file, that no per-Feature <c>style</c> is ever written, CRC-ERAM defaults, and vNAS folder
/// routing.
/// </summary>
public sealed class FixGeojsonWriterTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_FixGeojson_" + Guid.NewGuid().ToString("N"));

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

	private FixSettings Settings() => new()
	{
		OutputDirectory = _outputDirectory,
		IncludeFebCustomProperties = false,
	};

	private static List<JsonElement> FeaturesOf(string path)
	{
		using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
		return [.. document.RootElement.GetProperty("features").EnumerateArray().Select(feature => feature.Clone())];
	}

	private static string FileNamed(FixGeojsonGenerateResult result, string suffix) =>
		Assert.Single(result.Files.FilesWritten, f => f.EndsWith(suffix, StringComparison.Ordinal));

	// ---- ROI ----

	[Fact]
	public void filter_to_roi_keeps_only_fixes_inside_the_box()
	{
		Fix inside = FixTestData.Acme(); // lat 40, lon -100
		Fix outside = FixTestData.BuiltFix("FAR", 60.0, 10.0, "WP", "WYPNT");
		RegionOfInterest roi = new(38.0, -102.0, 42.0, -98.0);

		IReadOnlyList<Fix> kept = FixGeojsonWriter.FilterToRoi([inside, outside], roi);

		Assert.Equal(["ACME"], kept.Select(f => f.FixId));
	}

	[Fact]
	public void filter_to_roi_with_no_roi_keeps_every_fix_and_returns_the_same_instance()
	{
		IReadOnlyList<Fix> fixes = [FixTestData.Acme(), FixTestData.Bravo()];

		IReadOnlyList<Fix> kept = FixGeojsonWriter.FilterToRoi(fixes, null);

		Assert.Same(fixes, kept);
	}

	// ---- Generate: basic guards ----

	[Fact]
	public void generate_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => FixGeojsonWriter.Generate(null!, Settings()));
		Assert.Throws<ArgumentNullException>(() => FixGeojsonWriter.Generate([], null!));
	}

	[Fact]
	public void generate_writes_nothing_when_there_are_no_fixes() =>
		Assert.Empty(FixGeojsonWriter.Generate([], Settings()).Files.FilesWritten);

	// ---- All layout ----

	[Fact]
	public void all_layout_writes_one_symbols_file_and_one_text_file_for_every_included_fix()
	{
		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme(), FixTestData.Bravo()], Settings());

		Assert.Equal(
			[
				Path.Combine(_outputDirectory, "Geojson", "Fix_Symbols.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "Fix_Text.geojson"),
			],
			result.Files.FilesWritten);
		Assert.Equal(2, FeaturesOf(result.Files.FilesWritten[0]).Count);
	}

	[Fact]
	public void emit_symbols_or_emit_text_off_skips_that_kind_only()
	{
		FixSettings settings = Settings() with { EmitText = false };

		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme()], settings);

		string written = Assert.Single(result.Files.FilesWritten);
		Assert.EndsWith("Fix_Symbols.geojson", written);
	}

	// ---- feb.* properties ----

	private static readonly FixFebProperty[] AllFebProperties = [FixFebProperty.FixId, FixFebProperty.FixUseCode, FixFebProperty.Charts];

	private FixSettings SettingsWithAllFebProperties() => Settings() with
	{
		IncludeFebCustomProperties = true,
		FebProperties = AllFebProperties,
	};

	[Fact]
	public void the_text_files_feature_carries_the_fix_id_in_its_text_array_and_never_as_a_feb_property()
	{
		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme()], SettingsWithAllFebProperties());

		string textFile = FileNamed(result, "Fix_Text.geojson");
		JsonElement properties = Assert.Single(FeaturesOf(textFile)).GetProperty("properties");

		Assert.Equal(["ACME"], properties.GetProperty("text").EnumerateArray().Select(e => e.GetString()));
		Assert.False(properties.TryGetProperty("feb.fixId", out _));
	}

	[Fact]
	public void the_text_files_feature_still_carries_fix_use_code_and_charts_when_selected()
	{
		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme()], SettingsWithAllFebProperties());

		string textFile = FileNamed(result, "Fix_Text.geojson");
		JsonElement properties = Assert.Single(FeaturesOf(textFile)).GetProperty("properties");

		Assert.Equal("WYPNT", properties.GetProperty("feb.fixUseCode").GetString());
		Assert.Equal(["ENROUTE LOW", "ENROUTE HIGH"], properties.GetProperty("feb.charts").EnumerateArray().Select(e => e.GetString()));
	}

	[Fact]
	public void the_symbol_files_feature_carries_every_selected_feb_property_including_fix_id()
	{
		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme()], SettingsWithAllFebProperties());

		string symbolFile = FileNamed(result, "Fix_Symbols.geojson");
		JsonElement properties = Assert.Single(FeaturesOf(symbolFile)).GetProperty("properties");

		Assert.Equal("ACME", properties.GetProperty("feb.fixId").GetString());
		Assert.Equal("WYPNT", properties.GetProperty("feb.fixUseCode").GetString());
		Assert.Equal(["ENROUTE LOW", "ENROUTE HIGH"], properties.GetProperty("feb.charts").EnumerateArray().Select(e => e.GetString()));
	}

	[Fact]
	public void charts_is_omitted_for_a_fix_with_no_charts()
	{
		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Bravo()], SettingsWithAllFebProperties());

		string symbolFile = FileNamed(result, "Fix_Symbols.geojson");
		JsonElement properties = Assert.Single(FeaturesOf(symbolFile)).GetProperty("properties");

		Assert.False(properties.TryGetProperty("feb.charts", out _));
	}

	[Fact]
	public void no_feb_properties_are_written_when_they_are_off()
	{
		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme()], Settings());

		foreach (string file in result.Files.FilesWritten)
		{
			Assert.All(FeaturesOf(file), feature =>
				Assert.DoesNotContain(
					feature.GetProperty("properties").EnumerateObject(),
					property => property.Name.StartsWith("feb.", StringComparison.Ordinal)));
		}
	}

	[Fact]
	public void an_unrecognized_feb_property_value_contributes_nothing()
	{
		FixSettings settings = Settings() with
		{
			IncludeFebCustomProperties = true,
			FebProperties = [(FixFebProperty)99],
		};

		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme()], settings);

		string symbolFile = FileNamed(result, "Fix_Symbols.geojson");
		JsonElement properties = Assert.Single(FeaturesOf(symbolFile)).GetProperty("properties");

		Assert.DoesNotContain(
			properties.EnumerateObject(),
			property => property.Name.StartsWith("feb.", StringComparison.Ordinal));
	}

	[Fact]
	public void a_symbol_feature_never_carries_its_own_style()
	{
		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme()], SettingsWithAllFebProperties());

		string symbolFile = FileNamed(result, "Fix_Symbols.geojson");
		JsonElement properties = Assert.Single(FeaturesOf(symbolFile)).GetProperty("properties");

		Assert.False(properties.TryGetProperty("style", out _));
	}

	// ---- FixUse layout ----

	[Fact]
	public void fix_use_layout_writes_a_pair_per_fix_use_present_in_all_order()
	{
		FixSettings settings = Settings() with { OutputBy = FixOutputBy.FixUse };

		// WYPNT (index 7), COMPUTER-NAV (index 0), RPRTNG-PNT (index 5): expect COMPUTER-NAV, RPRTNG-PNT, WYPNT.
		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate(
			[FixTestData.Acme(), FixTestData.Bravo(), FixTestData.Charlie()], settings);

		Assert.Equal(
			[
				Path.Combine(_outputDirectory, "Geojson", "Fix_COMPUTER-NAV_Symbols.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "Fix_COMPUTER-NAV_Text.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "Fix_RPRTNG-PNT_Symbols.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "Fix_RPRTNG-PNT_Text.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "Fix_WYPNT_Symbols.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "Fix_WYPNT_Text.geojson"),
			],
			result.Files.FilesWritten);
	}

	[Fact]
	public void fix_use_layout_skips_excluded_fix_uses()
	{
		FixSettings settings = Settings() with { OutputBy = FixOutputBy.FixUse, ExcludedFixUses = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "COMPUTER-NAV" } };

		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme(), FixTestData.Bravo()], settings);

		Assert.DoesNotContain(result.Files.FilesWritten, f => f.Contains("COMPUTER-NAV", StringComparison.Ordinal));
		Assert.Contains(result.Files.FilesWritten, f => f.EndsWith("Fix_WYPNT_Symbols.geojson", StringComparison.Ordinal));
	}

	[Fact]
	public void fix_use_layout_sorts_an_unrecognized_fix_use_group_after_every_known_one()
	{
		FixSettings settings = Settings() with { OutputBy = FixOutputBy.FixUse };

		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme(), FixTestData.Mystery()], settings);

		Assert.Equal(
			[
				Path.Combine(_outputDirectory, "Geojson", "Fix_WYPNT_Symbols.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "Fix_WYPNT_Text.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "Fix_ZQ_Symbols.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "Fix_ZQ_Text.geojson"),
			],
			result.Files.FilesWritten);
	}

	// ---- Chart layout ----

	[Fact]
	public void chart_layout_puts_a_multi_chart_fix_into_every_one_of_its_chart_files()
	{
		FixSettings settings = Settings() with { OutputBy = FixOutputBy.Chart };

		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme()], settings);

		Assert.Equal(
			[
				Path.Combine(_outputDirectory, "Geojson", "Fix_ENROUTE-HIGH_Symbols.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "Fix_ENROUTE-HIGH_Text.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "Fix_ENROUTE-LOW_Symbols.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "Fix_ENROUTE-LOW_Text.geojson"),
			],
			result.Files.FilesWritten);
		Assert.Single(FeaturesOf(FileNamed(result, "Fix_ENROUTE-HIGH_Symbols.geojson")));
		Assert.Single(FeaturesOf(FileNamed(result, "Fix_ENROUTE-LOW_Symbols.geojson")));
	}

	[Fact]
	public void chart_layout_puts_chart_less_fixes_into_the_no_chart_files()
	{
		FixSettings settings = Settings() with { OutputBy = FixOutputBy.Chart };

		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Bravo()], settings);

		Assert.Equal(
			[
				Path.Combine(_outputDirectory, "Geojson", "Fix_NO-CHART_Symbols.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "Fix_NO-CHART_Text.geojson"),
			],
			result.Files.FilesWritten);
	}

	[Fact]
	public void chart_layout_skips_excluded_charts()
	{
		FixSettings settings = Settings() with
		{
			OutputBy = FixOutputBy.Chart,
			ExcludedCharts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ENROUTE-LOW" },
		};

		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme()], settings);

		Assert.DoesNotContain(result.Files.FilesWritten, f => f.Contains("ENROUTE-LOW", StringComparison.Ordinal));
		Assert.Contains(result.Files.FilesWritten, f => f.EndsWith("Fix_ENROUTE-HIGH_Symbols.geojson", StringComparison.Ordinal));
	}

	// ---- ChartAndFixUse layout ----

	[Fact]
	public void chart_and_fix_use_layout_writes_one_pair_per_combination_with_the_and_match()
	{
		FixSettings settings = Settings() with
		{
			OutputBy = FixOutputBy.ChartAndFixUse,
			Combinations = [new FixCombination("ENROUTE-LOW", "WYPNT")],
		};

		// Charlie is also on ENROUTE LOW, but as a RPRTNG-PNT: the AND match must exclude it. Bravo
		// has no chart at all, so it fails the chart side before the fix use side is even checked.
		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate(
			[FixTestData.Acme(), FixTestData.Charlie(), FixTestData.Bravo()], settings);

		string symbolsFile = FileNamed(result, "Fix_ENROUTE-LOW-WYPNT_Symbols.geojson");
		JsonElement feature = Assert.Single(FeaturesOf(symbolsFile));
		Assert.Equal("Point", feature.GetProperty("geometry").GetProperty("type").GetString());
	}

	[Fact]
	public void the_no_chart_combination_matches_chart_less_fixes()
	{
		FixSettings settings = Settings() with
		{
			OutputBy = FixOutputBy.ChartAndFixUse,
			Combinations = [new FixCombination("NO-CHART", "COMPUTER-NAV")],
		};

		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Bravo()], settings);

		Assert.Single(FeaturesOf(FileNamed(result, "Fix_NO-CHART-COMPUTER-NAV_Symbols.geojson")));
	}

	[Fact]
	public void a_combination_matching_nothing_writes_no_file()
	{
		FixSettings settings = Settings() with
		{
			OutputBy = FixOutputBy.ChartAndFixUse,
			Combinations = [new FixCombination("ENROUTE-LOW", "RADAR")],
		};

		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme()], settings);

		Assert.Empty(result.Files.FilesWritten);
	}

	// ---- CRC defaults ----

	private static CrcSymbolDefaults SymbolDefaults(string? style) => new() { Bcg = 3, Filters = [3], Style = style, Size = 1 };

	private static CrcTextDefaults TextDefaults() =>
		new() { Bcg = 3, Filters = [3], Size = 1, Underline = false, Opaque = false, XOffset = 0, YOffset = 0 };

	[Fact]
	public void is_text_defaults_feature_is_first_only_for_a_file_chosen_for_crc_defaults()
	{
		FixSettings settings = Settings() with
		{
			Vnas = new VnasFileChoices([FixOutputFiles.Text], [FixOutputFiles.Text]),
			TextDefaults = new Dictionary<string, CrcTextDefaults>(StringComparer.OrdinalIgnoreCase)
			{
				[FixOutputFiles.AllClass] = TextDefaults(),
			},
		};

		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme()], settings);

		List<JsonElement> textFeatures = FeaturesOf(FileNamed(result, "Fix_Text.geojson"));
		Assert.True(textFeatures[0].GetProperty("properties").GetProperty("isTextDefaults").GetBoolean());
		Assert.Equal(2, textFeatures.Count); // the defaults Feature plus the one real fix

		List<JsonElement> symbolFeatures = FeaturesOf(FileNamed(result, "Fix_Symbols.geojson"));
		Assert.False(symbolFeatures[0].GetProperty("properties").TryGetProperty("isSymbolDefaults", out _));
		Assert.Single(symbolFeatures);
	}

	[Fact]
	public void is_symbol_defaults_feature_is_first_only_for_a_file_chosen_for_crc_defaults()
	{
		FixSettings settings = Settings() with
		{
			Vnas = new VnasFileChoices([FixOutputFiles.Symbols], [FixOutputFiles.Symbols]),
			SymbolDefaults = new Dictionary<string, CrcSymbolDefaults>(StringComparer.OrdinalIgnoreCase)
			{
				[FixOutputFiles.AllClass] = SymbolDefaults("otherWaypoints"),
			},
		};

		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme()], settings);

		List<JsonElement> symbolFeatures = FeaturesOf(FileNamed(result, "Fix_Symbols.geojson"));
		Assert.True(symbolFeatures[0].GetProperty("properties").GetProperty("isSymbolDefaults").GetBoolean());
		Assert.Equal(2, symbolFeatures.Count); // the defaults Feature plus the one real fix

		List<JsonElement> textFeatures = FeaturesOf(FileNamed(result, "Fix_Text.geojson"));
		Assert.False(textFeatures[0].GetProperty("properties").TryGetProperty("isTextDefaults", out _));
		Assert.Single(textFeatures);
	}

	[Fact]
	public void a_file_with_crc_defaults_but_no_matching_fixes_is_still_not_written()
	{
		// A combination that matches nothing still reaches WriteGroup, with an empty fix list; the
		// isSymbolDefaults Feature alone must not be enough to write the file.
		const string group = "ENROUTE-LOW-RADAR";
		FixSettings settings = Settings() with
		{
			OutputBy = FixOutputBy.ChartAndFixUse,
			Combinations = [new FixCombination("ENROUTE-LOW", "RADAR")],
			Vnas = new VnasFileChoices([$"Fix_{group}_Symbols"], [$"Fix_{group}_Symbols"]),
			SymbolDefaults = new Dictionary<string, CrcSymbolDefaults>(StringComparer.OrdinalIgnoreCase)
			{
				[group] = SymbolDefaults("otherWaypoints"),
			},
		};

		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme()], settings);

		Assert.Empty(result.Files.FilesWritten);
	}

	// ---- vNAS folder routing ----

	[Fact]
	public void a_file_marked_for_vnas_goes_under_upload_to_vnas_while_others_do_not()
	{
		FixSettings settings = Settings() with
		{
			Vnas = new VnasFileChoices([FixOutputFiles.Symbols], []),
		};

		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme()], settings);

		Assert.Contains(result.Files.FilesWritten, f => f.Contains("Upload_to_vNAS", StringComparison.Ordinal) && f.EndsWith("Fix_Symbols.geojson", StringComparison.Ordinal));
		Assert.Contains(result.Files.FilesWritten, f => !f.Contains("Upload_to_vNAS", StringComparison.Ordinal) && f.EndsWith("Fix_Text.geojson", StringComparison.Ordinal));
	}

	[Fact]
	public void non_all_layout_routes_each_groups_files_to_vnas_independently()
	{
		FixSettings settings = Settings() with
		{
			OutputBy = FixOutputBy.FixUse,
			Vnas = new VnasFileChoices(["Fix_WYPNT_Symbols"], []),
		};

		FixGeojsonGenerateResult result = FixGeojsonWriter.Generate([FixTestData.Acme(), FixTestData.Bravo()], settings);

		Assert.Contains(result.Files.FilesWritten, f => f.Contains("Upload_to_vNAS", StringComparison.Ordinal) && f.EndsWith("Fix_WYPNT_Symbols.geojson", StringComparison.Ordinal));
		Assert.Contains(result.Files.FilesWritten, f => !f.Contains("Upload_to_vNAS", StringComparison.Ordinal) && f.EndsWith("Fix_COMPUTER-NAV_Symbols.geojson", StringComparison.Ordinal));
	}
}
