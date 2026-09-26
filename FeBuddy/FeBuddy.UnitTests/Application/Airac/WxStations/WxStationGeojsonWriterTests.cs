using System.Text.Json;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.WxStations;
using FeBuddy.Core.Application.Airac.WxStations.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.WxStations.Models;

using FeBuddy.UnitTests.Application.Airac.WxStations.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.WxStations;

/// <summary>
/// Covers <see cref="WxStationGeojsonWriter"/>: the ROI filter, that a Symbols Feature carries no
/// properties at all, the Text Feature's two-line label, the Emit flags, CRC-ERAM defaults, and
/// vNAS folder routing.
/// </summary>
public sealed class WxStationGeojsonWriterTests : IDisposable
{
	private readonly string _outputDirectory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_WxStationGeojson_" + Guid.NewGuid().ToString("N"));

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

	private WxStationSettings Settings() => new() { OutputDirectory = _outputDirectory };

	private static List<JsonElement> FeaturesOf(string path)
	{
		using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
		return [.. document.RootElement.GetProperty("features").EnumerateArray().Select(feature => feature.Clone())];
	}

	private static string FileNamed(WxStationGeojsonGenerateResult result, string suffix) =>
		Assert.Single(result.Files.FilesWritten, f => f.EndsWith(suffix, StringComparison.Ordinal));

	// ---- ROI ----

	[Fact]
	public void filter_to_roi_keeps_only_stations_inside_the_box()
	{
		WxStation inside = WxStationTestData.Dtw(); // lat 42.212, lon -83.353
		WxStation outside = WxStationTestData.BuiltStation("KFAR", "", "", 60.0, 10.0);
		RegionOfInterest roi = new(40.0, -85.0, 44.0, -81.0);

		IReadOnlyList<WxStation> kept = WxStationGeojsonWriter.FilterToRoi([inside, outside], roi);

		Assert.Equal(["KDTW"], kept.Select(s => s.IcaoId));
	}

	[Fact]
	public void filter_to_roi_with_no_roi_keeps_every_station_and_returns_the_same_instance()
	{
		IReadOnlyList<WxStation> stations = [WxStationTestData.Dtw(), WxStationTestData.Phx()];

		IReadOnlyList<WxStation> kept = WxStationGeojsonWriter.FilterToRoi(stations, null);

		Assert.Same(stations, kept);
	}

	// ---- Generate: basic guards ----

	[Fact]
	public void generate_rejects_null_arguments()
	{
		Assert.Throws<ArgumentNullException>(() => WxStationGeojsonWriter.Generate(null!, Settings()));
		Assert.Throws<ArgumentNullException>(() => WxStationGeojsonWriter.Generate([], null!));
	}

	[Fact]
	public void generate_writes_nothing_when_there_are_no_stations() =>
		Assert.Empty(WxStationGeojsonWriter.Generate([], Settings()).Files.FilesWritten);

	// ---- Symbols / Text ----

	[Fact]
	public void generate_writes_one_symbols_file_and_one_text_file_for_every_station()
	{
		WxStationGeojsonGenerateResult result = WxStationGeojsonWriter.Generate(
			[WxStationTestData.Dtw(), WxStationTestData.Phx()], Settings());

		Assert.Equal(
			[
				Path.Combine(_outputDirectory, "Geojson", "Wx_Symbols.geojson"),
				Path.Combine(_outputDirectory, "Geojson", "Wx_Text.geojson"),
			],
			result.Files.FilesWritten);
		Assert.Equal(2, FeaturesOf(result.Files.FilesWritten[0]).Count);
	}

	[Fact]
	public void a_symbol_feature_carries_no_properties_at_all()
	{
		WxStationGeojsonGenerateResult result = WxStationGeojsonWriter.Generate([WxStationTestData.Dtw()], Settings());

		string symbolFile = FileNamed(result, "Wx_Symbols.geojson");
		JsonElement properties = Assert.Single(FeaturesOf(symbolFile)).GetProperty("properties");

		Assert.Empty(properties.EnumerateObject());
	}

	[Fact]
	public void a_text_features_text_array_is_the_icao_id_and_the_second_label_line()
	{
		WxStationGeojsonGenerateResult result = WxStationGeojsonWriter.Generate([WxStationTestData.Dtw()], Settings());

		string textFile = FileNamed(result, "Wx_Text.geojson");
		JsonElement properties = Assert.Single(FeaturesOf(textFile)).GetProperty("properties");

		Assert.Equal(
			["KDTW", "DTW_Detroit/Metro Wayne Cnty"],
			properties.GetProperty("text").EnumerateArray().Select(e => e.GetString()));
	}

	[Fact]
	public void emit_text_off_skips_the_text_file_only()
	{
		WxStationSettings settings = Settings() with { EmitText = false };

		WxStationGeojsonGenerateResult result = WxStationGeojsonWriter.Generate([WxStationTestData.Dtw()], settings);

		string written = Assert.Single(result.Files.FilesWritten);
		Assert.EndsWith("Wx_Symbols.geojson", written, StringComparison.Ordinal);
	}

	[Fact]
	public void emit_symbols_off_skips_the_symbols_file_only()
	{
		WxStationSettings settings = Settings() with { EmitSymbols = false };

		WxStationGeojsonGenerateResult result = WxStationGeojsonWriter.Generate([WxStationTestData.Dtw()], settings);

		string written = Assert.Single(result.Files.FilesWritten);
		Assert.EndsWith("Wx_Text.geojson", written, StringComparison.Ordinal);
	}

	// ---- CRC defaults ----

	private static CrcSymbolDefaults SymbolDefaults() => new() { Bcg = 3, Filters = [3], Style = "otherWaypoints", Size = 1 };

	private static CrcTextDefaults TextDefaults() =>
		new() { Bcg = 3, Filters = [3], Size = 1, Underline = false, Opaque = false, XOffset = 0, YOffset = 0 };

	[Fact]
	public void is_symbol_defaults_feature_is_first_only_for_a_file_chosen_for_crc_defaults()
	{
		WxStationSettings settings = Settings() with
		{
			Vnas = new VnasFileChoices([WxStationOutputFiles.Symbols], [WxStationOutputFiles.Symbols]),
			SymbolDefaults = new Dictionary<string, CrcSymbolDefaults>(StringComparer.OrdinalIgnoreCase)
			{
				[WxStationOutputFiles.AllClass] = SymbolDefaults(),
			},
		};

		WxStationGeojsonGenerateResult result = WxStationGeojsonWriter.Generate([WxStationTestData.Dtw()], settings);

		List<JsonElement> symbolFeatures = FeaturesOf(FileNamed(result, "Wx_Symbols.geojson"));
		Assert.True(symbolFeatures[0].GetProperty("properties").GetProperty("isSymbolDefaults").GetBoolean());
		Assert.Equal(2, symbolFeatures.Count); // the defaults Feature plus the one real station

		List<JsonElement> textFeatures = FeaturesOf(FileNamed(result, "Wx_Text.geojson"));
		Assert.False(textFeatures[0].GetProperty("properties").TryGetProperty("isTextDefaults", out _));
		Assert.Single(textFeatures);
	}

	[Fact]
	public void is_text_defaults_feature_is_first_only_for_a_file_chosen_for_crc_defaults()
	{
		WxStationSettings settings = Settings() with
		{
			Vnas = new VnasFileChoices([WxStationOutputFiles.Text], [WxStationOutputFiles.Text]),
			TextDefaults = new Dictionary<string, CrcTextDefaults>(StringComparer.OrdinalIgnoreCase)
			{
				[WxStationOutputFiles.AllClass] = TextDefaults(),
			},
		};

		WxStationGeojsonGenerateResult result = WxStationGeojsonWriter.Generate([WxStationTestData.Dtw()], settings);

		List<JsonElement> textFeatures = FeaturesOf(FileNamed(result, "Wx_Text.geojson"));
		Assert.True(textFeatures[0].GetProperty("properties").GetProperty("isTextDefaults").GetBoolean());
		Assert.Equal(2, textFeatures.Count); // the defaults Feature plus the one real station

		List<JsonElement> symbolFeatures = FeaturesOf(FileNamed(result, "Wx_Symbols.geojson"));
		Assert.False(symbolFeatures[0].GetProperty("properties").TryGetProperty("isSymbolDefaults", out _));
		Assert.Single(symbolFeatures);
	}

	// ---- vNAS folder routing ----

	[Fact]
	public void a_file_marked_for_vnas_goes_under_upload_to_vnas_while_the_other_does_not()
	{
		WxStationSettings settings = Settings() with
		{
			Vnas = new VnasFileChoices([WxStationOutputFiles.Symbols], []),
		};

		WxStationGeojsonGenerateResult result = WxStationGeojsonWriter.Generate([WxStationTestData.Dtw()], settings);

		Assert.Contains(
			result.Files.FilesWritten,
			f => f.Contains("Upload_to_vNAS", StringComparison.Ordinal) && f.EndsWith("Wx_Symbols.geojson", StringComparison.Ordinal));
		Assert.Contains(
			result.Files.FilesWritten,
			f => !f.Contains("Upload_to_vNAS", StringComparison.Ordinal) && f.EndsWith("Wx_Text.geojson", StringComparison.Ordinal));
	}
}
