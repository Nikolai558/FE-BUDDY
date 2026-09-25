using System.Text.Json;

using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Application.Conversions.VeramToGeojson;
using FeBuddy.Core.Application.Conversions.VeramToGeojson.Models;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Veram.Models;

namespace FeBuddy.UnitTests.Application.Conversions.VeramToGeojson;

/// <summary>
/// Runs the whole vERAM to GeoJSON conversion (<see cref="VeramToGeojsonService.Run"/>) against
/// small made-up GeoMaps files written to a temp folder, in both layouts and with each defaults
/// source, and checks what lands on disk and what is reported.
/// </summary>
public sealed class VeramToGeojsonServiceTests : IDisposable
{
	private const string LineDefaults = """<LineDefaults Bcg="1" Filters="1" Style="Solid" Thickness="1" />""";
	private const string SymbolDefaults = """<SymbolDefaults Bcg="2" Filters="2" Style="Vor" Size="1" />""";
	private const string TextDefaults = """<TextDefaults Bcg="3" Filters="3" Size="1" Underline="false" Opaque="false" XOffset="0" YOffset="0" />""";

	private const string LineAB = """<Element xsi:type="Line" Filters="" StartLat="40.0" StartLon="-100.0" EndLat="40.5" EndLon="-100.0" />""";
	private const string LineBC = """<Element xsi:type="Line" Filters="" StartLat="40.5" StartLon="-100.0" EndLat="40.5" EndLon="-99.5" />""";
	private const string SymbolA = """<Element xsi:type="Symbol" Filters="" Lat="40.0" Lon="-100.0" />""";
	private const string TextA = """<Element xsi:type="Text" Filters="" Lat="40.0" Lon="-100.0" Lines="ZXX" />""";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Veram_" + Guid.NewGuid().ToString("N"));

	public VeramToGeojsonServiceTests()
	{
		Directory.CreateDirectory(Source);
	}

	private string Source => Path.Combine(_root, "GeoMaps");

	private string Output => Path.Combine(_root, "Out");

	private string MapFolder(string file = "ZXX", string map = "CENTER") =>
		Path.Combine(Output, "FE-Buddy_Output", VeramGeojsonWriter.RootFolder, file, map);

	public void Dispose()
	{
		if (Directory.Exists(_root))
		{
			Directory.Delete(_root, recursive: true);
		}
	}

	private static string GeoMapObject(string description, string content, bool tdmOnly = false) =>
		$"""<GeoMapObject Description="{description}" TdmOnly="{(tdmOnly ? "true" : "false")}">{content}</GeoMapObject>""";

	private static string Elements(params string[] elements) => $"<Elements>{string.Concat(elements)}</Elements>";

	private void WriteGeoMaps(string fileName, params string[] objects) =>
		WriteGeoMapsWithMaps(fileName, $"""<GeoMap Name="CENTER"><Objects>{string.Concat(objects)}</Objects></GeoMap>""");

	private void WriteGeoMapsWithMaps(string fileName, string maps) =>
		File.WriteAllText(Path.Combine(Source, fileName), $"""
			<?xml version="1.0" encoding="utf-8"?>
			<GeoMapSet xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" DefaultMap="CENTER">
			  <GeoMaps>{maps}</GeoMaps>
			</GeoMapSet>
			""");

	private Dictionary<string, string> Settings(params (string Key, string Value)[] entries)
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = Output,
			["SourceFolder"] = Source,
		};

		foreach ((string key, string value) in entries)
		{
			settings[key] = value;
		}

		return settings;
	}

	/// <summary>The tab's CRC defaults, all three kinds, for the card-based sources.</summary>
	private Dictionary<string, string> CardSettings(string source, params (string Key, string Value)[] entries) => Settings(
	[
		("DefaultsSource", source),
		("IncludeCrcLineDefaults", "Y"), ("IncludeCrcSymbolDefaults", "Y"), ("IncludeCrcTextDefaults", "Y"),
		("Crc.GeoMap.Line.bcg", "11"), ("Crc.GeoMap.Line.filters", "11"), ("Crc.GeoMap.Line.style", "longDashed"), ("Crc.GeoMap.Line.thickness", "3"),
		("Crc.GeoMap.Symbol.bcg", "12"), ("Crc.GeoMap.Symbol.filters", "12"), ("Crc.GeoMap.Symbol.style", "ndb"), ("Crc.GeoMap.Symbol.size", "2"),
		("Crc.GeoMap.Text.bcg", "13"), ("Crc.GeoMap.Text.filters", "13"), ("Crc.GeoMap.Text.size", "2"), ("Crc.GeoMap.Text.underline", "N"),
		("Crc.GeoMap.Text.opaque", "N"), ("Crc.GeoMap.Text.xOffset", "0"), ("Crc.GeoMap.Text.yOffset", "0"),
		.. entries,
	]);

	private static JsonElement[] Features(string path) =>
		[.. JsonDocument.Parse(File.ReadAllText(path)).RootElement.GetProperty("features").EnumerateArray()];

	private static JsonElement Properties(JsonElement feature) => feature.GetProperty("properties");

	private static bool Has(JsonElement feature, string property) => Properties(feature).TryGetProperty(property, out _);

	// ================= GeoMapObject Description =================

	[Fact]
	public void each_object_is_a_file_with_its_defaults_first_then_its_features()
	{
		WriteGeoMaps("ZXX.xml", GeoMapObject("ZXX BOUNDARY",
			LineDefaults + SymbolDefaults + TextDefaults + Elements(LineAB, LineBC, SymbolA, TextA)));

		SourceFilesConversionResult result = VeramToGeojsonService.Run(Settings());
		string path = Path.Combine(MapFolder(), "ZXX BOUNDARY.geojson");

		Assert.Equal([path], result.GeojsonFilesWritten);
		Assert.Equal(3, result.FeaturesWritten);
		Assert.Empty(result.Warnings);

		JsonElement[] features = Features(path);
		Assert.Equal(6, features.Length);
		Assert.True(Properties(features[0]).GetProperty("isLineDefaults").GetBoolean());
		Assert.Equal("solid", Properties(features[0]).GetProperty("style").GetString());
		Assert.True(Properties(features[1]).GetProperty("isSymbolDefaults").GetBoolean());
		Assert.Equal("vor", Properties(features[1]).GetProperty("style").GetString());
		Assert.True(Properties(features[2]).GetProperty("isTextDefaults").GetBoolean());

		// The two segments meet, so they are one three-point line.
		JsonElement lines = features[3].GetProperty("geometry").GetProperty("coordinates");
		Assert.Equal(1, lines.GetArrayLength());
		Assert.Equal(3, lines[0].GetArrayLength());
		Assert.Equal("Point", features[4].GetProperty("geometry").GetProperty("type").GetString());
		Assert.Equal("ZXX", Properties(features[5]).GetProperty("text")[0].GetString());
	}

	[Fact]
	public void element_overrides_are_carried_over_and_lines_with_different_looks_stay_apart()
	{
		WriteGeoMaps("ZXX.xml", GeoMapObject("RWYS", LineDefaults + TextDefaults + Elements(
			LineAB,
			"""<Element xsi:type="Line" Filters="7" Style="LongDashed" StartLat="41" StartLon="-100" EndLat="41.5" EndLon="-100" />""",
			"""<Element xsi:type="Text" Filters="" Size="3" XOffset="9" Lat="40" Lon="-100" Lines="24L" />""")));

		VeramToGeojsonService.Run(Settings());
		JsonElement[] features = Features(Path.Combine(MapFolder(), "RWYS.geojson"));

		Assert.Equal(5, features.Length);
		Assert.False(Has(features[2], "style"));
		Assert.Equal("longDashed", Properties(features[3]).GetProperty("style").GetString());
		Assert.Equal(7, Properties(features[3]).GetProperty("filters")[0].GetInt32());
		Assert.Equal(3, Properties(features[4]).GetProperty("size").GetInt32());
		Assert.Equal(9, Properties(features[4]).GetProperty("xOffset").GetInt32());
	}

	[Fact]
	public void objects_sharing_a_description_share_a_file_only_when_their_defaults_agree()
	{
		WriteGeoMaps("ZXX.xml",
			GeoMapObject("VIDEO", LineDefaults + Elements(LineAB)),
			GeoMapObject("video", LineDefaults + TextDefaults + Elements(LineBC, TextA)),
			GeoMapObject("VIDEO", """<LineDefaults Bcg="5" Filters="5" Style="Solid" Thickness="1" />""" + Elements(LineAB)),
			GeoMapObject("EMPTY", LineDefaults));

		SourceFilesConversionResult result = VeramToGeojsonService.Run(Settings());

		Assert.Equal(
			[Path.Combine(MapFolder(), "VIDEO (2).geojson"), Path.Combine(MapFolder(), "VIDEO.geojson")],
			result.GeojsonFilesWritten.Order(StringComparer.Ordinal));

		// The first two agree on their line defaults, so the text defaults join them and the
		// lines join up; the third's differ.
		JsonElement[] shared = Features(Path.Combine(MapFolder(), "VIDEO.geojson"));
		Assert.Equal(4, shared.Length);
		Assert.Equal(3, shared[2].GetProperty("geometry").GetProperty("coordinates")[0].GetArrayLength());
	}

	[Fact]
	public void objects_without_defaults_share_a_file_and_bad_overrides_are_left_out()
	{
		WriteGeoMaps("ZXX.xml",
			GeoMapObject("BARE", Elements(TextA)),
			GeoMapObject("BARE", Elements(
				"""<Element xsi:type="Text" Size="9" Lat="41" Lon="-100" Lines="OTHER" />""")));

		SourceFilesConversionResult result = VeramToGeojsonService.Run(Settings());

		// Neither has text defaults, so they agree and share a file; the bad size is dropped.
		JsonElement[] features = Features(Assert.Single(result.GeojsonFilesWritten));
		Assert.Equal(2, features.Length);
		Assert.False(Has(features[1], "size"));
		Assert.Contains(result.Warnings, w => w.Contains("its text have no defaults"));
		Assert.Contains(result.Warnings, w => w.Contains("Size=\"9\" is not a value CRC can draw"));
	}

	[Fact]
	public void maps_get_their_own_folders_and_names_are_made_file_safe()
	{
		WriteGeoMapsWithMaps("ZXX.xml",
			$"""<GeoMap Name="CENTER"><Objects>{GeoMapObject("A/B", LineDefaults + Elements(LineAB))}</Objects></GeoMap>""" +
			$"""<GeoMap Name="center"><Objects>{GeoMapObject("A/B", LineDefaults + Elements(LineAB))}</Objects></GeoMap>""");

		SourceFilesConversionResult result = VeramToGeojsonService.Run(Settings(("AddFeBuddyOutputFolder", "N")));
		string root = Path.Combine(Output, VeramGeojsonWriter.RootFolder, "ZXX");

		Assert.Equal(
			[Path.Combine(root, "CENTER", "A-B.geojson"), Path.Combine(root, "center (2)", "A-B.geojson")],
			result.GeojsonFilesWritten);
	}

	// ================= Filter Index and Similar Attributes =================

	[Fact]
	public void by_filter_files_each_look_once_with_its_defaults_and_no_overrides()
	{
		WriteGeoMaps("ZXX.xml",
			GeoMapObject("ONE", LineDefaults + SymbolDefaults + TextDefaults + Elements(LineAB, SymbolA, TextA)),
			GeoMapObject("TWO", LineDefaults + Elements(LineBC)),
			GeoMapObject("TDM", LineDefaults + Elements(LineAB), tdmOnly: true),
			GeoMapObject("MULTI", """<LineDefaults Bcg="1" Filters="2,1" Style="Solid" Thickness="1" />""" + Elements(LineAB)),
			GeoMapObject("OVER", LineDefaults + Elements(
				"""<Element xsi:type="Line" Filters="5" StartLat="40" StartLon="-100" EndLat="41" EndLon="-100" />""")));

		SourceFilesConversionResult result = VeramToGeojsonService.Run(Settings(("OutputLayout", "ByFilter")));
		string map = MapFolder();

		string oneAndTwo = Path.Combine(map, "FILTER 01", "FILTER 01__TDM F__Line__BCG 1__Style solid__Thickness 1.geojson");
		Assert.Equal(
			[
				oneAndTwo,
				Path.Combine(map, "FILTER 02", "FILTER 02__TDM F__Symbol__BCG 2__Style vor__Size 1.geojson"),
				Path.Combine(map, "FILTER 03", "FILTER 03__TDM F__Text__BCG 3__Size 1__Underline F__Opaque F__XOffset 0__YOffset 0.geojson"),
				Path.Combine(map, "FILTER 01", "FILTER 01__TDM T__Line__BCG 1__Style solid__Thickness 1.geojson"),
				Path.Combine(map, "MULTI FILTERS", "FILTER 02 01", "FILTER 02 01__TDM F__Line__BCG 1__Style solid__Thickness 1.geojson"),
				Path.Combine(map, "FILTER 05", "FILTER 05__TDM F__Line__BCG 1__Style solid__Thickness 1.geojson"),
			],
			result.GeojsonFilesWritten);

		// ONE's and TWO's lines draw the same way, so they share a file and join into one line.
		JsonElement[] shared = Features(oneAndTwo);
		Assert.Equal(2, shared.Length);
		Assert.Equal(3, shared[1].GetProperty("geometry").GetProperty("coordinates")[0].GetArrayLength());
		Assert.Empty(Properties(shared[1]).EnumerateObject());
	}

	[Fact]
	public void by_filter_elements_that_cannot_be_fully_described_go_under_missing_defaults()
	{
		WriteGeoMaps("ZXX.xml", GeoMapObject("BARE", Elements(
			"""<Element xsi:type="Line" Filters="4" StartLat="40" StartLon="-100" EndLat="41" EndLon="-100" />""",
			"""<Element xsi:type="Line" Filters="4" Bcg="99" StartLat="42" StartLon="-100" EndLat="43" EndLon="-100" />""")));

		SourceFilesConversionResult result = VeramToGeojsonService.Run(Settings(("OutputLayout", "ByFilter")));
		string path = Path.Combine(MapFolder(), "MISSING DEFAULTS", "TDM F__Line.geojson");

		Assert.Equal([path], result.GeojsonFilesWritten);

		// No defaults Feature; each line keeps what it sets itself, less what CRC cannot draw.
		JsonElement[] features = Features(path);
		Assert.Single(features);
		Assert.Equal(4, Properties(features[0]).GetProperty("filters")[0].GetInt32());
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("Bcg=\"99\""));
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("it has no LineDefaults"));
	}

	// ================= defaults source =================

	[Fact]
	public void from_the_xml_an_object_without_usable_defaults_is_warned_about()
	{
		WriteGeoMaps("ZXX.xml", GeoMapObject("PARTIAL",
			"""<LineDefaults Bcg="1" Filters="1" Thickness="1" />""" + Elements(LineAB)));

		SourceFilesConversionResult result = VeramToGeojsonService.Run(Settings());

		Assert.Contains(result.Warnings, w => w.Contains("ZXX.xml: CENTER / PARTIAL: its LineDefaults have no Style"));
		Assert.Single(Features(result.GeojsonFilesWritten[0]));
	}

	[Fact]
	public void from_the_xml_then_the_card_fills_only_the_gaps()
	{
		WriteGeoMaps("ZXX.xml",
			GeoMapObject("HAS", LineDefaults + Elements(LineAB)),
			GeoMapObject("LACKS", Elements(LineBC,
				"""<Element xsi:type="Line" Style="ShortDashed" StartLat="42" StartLon="-100" EndLat="43" EndLon="-100" />""")));

		SourceFilesConversionResult result = VeramToGeojsonService.Run(CardSettings("XmlThenCard"));

		Assert.Equal(1, Properties(Features(Path.Combine(MapFolder(), "HAS.geojson"))[0]).GetProperty("bcg").GetInt32());

		JsonElement[] lacks = Features(Path.Combine(MapFolder(), "LACKS.geojson"));
		Assert.Equal(11, Properties(lacks[0]).GetProperty("bcg").GetInt32());
		Assert.Equal("shortDashed", Properties(lacks[2]).GetProperty("style").GetString());
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Info && m.Text.Contains("use the CRC Line defaults set on the tab"));
		Assert.Empty(result.Warnings);
	}

	[Fact]
	public void from_the_card_only_the_xml_s_styling_is_ignored()
	{
		WriteGeoMaps("ZXX.xml", GeoMapObject("ALL", LineDefaults + SymbolDefaults + TextDefaults + Elements(
			"""<Element xsi:type="Line" Style="ShortDashed" StartLat="40" StartLon="-100" EndLat="41" EndLon="-100" />""",
			SymbolA,
			"""<Element xsi:type="Text" Size="4" Lat="40" Lon="-100" Lines="ZXX" />""")));

		SourceFilesConversionResult result = VeramToGeojsonService.Run(CardSettings("Card"));
		JsonElement[] features = Features(result.GeojsonFilesWritten[0]);

		Assert.Equal([11, 12, 13], features[..3].Select(f => Properties(f).GetProperty("bcg").GetInt32()));
		Assert.False(Has(features[3], "style"));
		Assert.Equal(["text"], Properties(features[5]).EnumerateObject().Select(p => p.Name));
		Assert.Empty(result.Warnings);
	}

	[Fact]
	public void by_filter_from_the_card_only_files_by_the_card_s_look_and_ignores_the_xml()
	{
		WriteGeoMaps("ZXX.xml", GeoMapObject("ALL", LineDefaults + Elements(
			"""<Element xsi:type="Line" Filters="7" StartLat="40" StartLon="-100" EndLat="41" EndLon="-100" />""",
			"""<Element xsi:type="Symbol" Filters="7" Size="4" Lat="40" Lon="-100" />""")));

		SourceFilesConversionResult result = VeramToGeojsonService.Run(Settings(
			("OutputLayout", "ByFilter"), ("DefaultsSource", "Card"),
			("IncludeCrcLineDefaults", "Y"),
			("Crc.GeoMap.Line.bcg", "11"), ("Crc.GeoMap.Line.filters", "11"),
			("Crc.GeoMap.Line.style", "solid"), ("Crc.GeoMap.Line.thickness", "1")));

		// The line takes the card's look (filter 11, not its own 7); the symbol has no card
		// defaults and none of its own may be used, so it is kept apart with nothing set.
		Assert.Equal(
			[
				Path.Combine(MapFolder(), "FILTER 11", "FILTER 11__TDM F__Line__BCG 11__Style solid__Thickness 1.geojson"),
				Path.Combine(MapFolder(), "MISSING DEFAULTS", "TDM F__Symbol.geojson"),
			],
			result.GeojsonFilesWritten);
		Assert.Empty(Properties(Features(result.GeojsonFilesWritten[1])[0]).EnumerateObject());
	}

	[Fact]
	public void from_the_card_only_a_kind_without_card_defaults_is_warned_about_once()
	{
		WriteGeoMaps("ZXX.xml",
			GeoMapObject("ONE", Elements(SymbolA)),
			GeoMapObject("TWO", Elements(SymbolA)));

		SourceFilesConversionResult result = VeramToGeojsonService.Run(Settings(("DefaultsSource", "Card")));

		string warning = Assert.Single(result.Warnings);
		Assert.Contains("no CRC Symbol defaults are set on the tab", warning);
	}

	// ================= files =================

	[Fact]
	public void text_without_text_is_skipped_and_a_file_with_nothing_to_draw_says_so()
	{
		WriteGeoMaps("BLANK.xml", GeoMapObject("LABELS", TextDefaults + Elements(
			"""<Element xsi:type="Text" Lat="40" Lon="-100" Lines=" " />""")));

		SourceFilesConversionResult result = VeramToGeojsonService.Run(Settings());

		Assert.Empty(result.GeojsonFilesWritten);
		Assert.Contains(result.Messages, m => m.Text.Contains("has no text and was skipped"));
		Assert.Contains(result.Messages, m => m.IsAdvisory && m.Text.Contains("BLANK.xml has no elements to draw"));
	}

	[Fact]
	public void a_file_that_is_not_a_geomaps_file_fails_on_its_own()
	{
		File.WriteAllText(Path.Combine(Source, "AAA.xml"), "<Geomaps_Records />");
		File.WriteAllText(Path.Combine(Source, "BBB.xml"), "<GeoMapSet><GeoMaps>");
		WriteGeoMaps("ZXX.xml", GeoMapObject("OK", LineDefaults + Elements(LineAB)));
		List<ConversionProgress> reports = [];

		SourceFilesConversionResult result = VeramToGeojsonService.Run(Settings(), new InlineProgress(reports.Add));

		Assert.Equal(2, result.FailedCount);
		Assert.Contains("not a vERAM GeoMaps file", result.Files[0].Error);
		Assert.Contains("not well-formed XML", result.Files[1].Error);
		Assert.Single(result.GeojsonFilesWritten);
		Assert.Equal("1 GeoJSON file(s) written", reports[^1].Message);
	}

	[Fact]
	public void reader_problems_are_warned_about_and_counted()
	{
		WriteGeoMaps("ZXX.xml", GeoMapObject("OK", LineDefaults + Elements(LineAB,
			"""<Element xsi:type="Polygon" Lat="40" Lon="-100" />""")));
		List<ConversionProgress> reports = [];

		SourceFilesConversionResult result = VeramToGeojsonService.Run(Settings(), new InlineProgress(reports.Add));

		Assert.Equal(1, result.Files[0].RecordsSkipped);
		Assert.Contains(result.Warnings, w => w.StartsWith("ZXX.xml: Line"));
	}

	[Fact]
	public void progress_says_when_a_file_had_nothing_to_write()
	{
		WriteGeoMapsWithMaps("EMPTY.xml", """<GeoMap Name="CENTER" />""");
		List<ConversionProgress> reports = [];

		VeramToGeojsonService.Run(Settings(), new InlineProgress(reports.Add));

		Assert.Equal("nothing to write", reports[^1].Message);
	}

	[Fact]
	public void bad_arguments_are_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => VeramToGeojsonService.Run(null!));

		VeramGeoMapFile empty = new("x.xml", [], []);
		VeramToGeojsonSettings settings = new() { OutputDirectory = Output };
		GeojsonFileSet files = new(6);

		Assert.Throws<ArgumentNullException>(() => VeramGeojsonWriter.Write(null!, settings, files, []));
		Assert.Throws<ArgumentNullException>(() => VeramGeojsonWriter.Write(empty, null!, files, []));
		Assert.Throws<ArgumentNullException>(() => VeramGeojsonWriter.Write(empty, settings, null!, []));
		Assert.Throws<ArgumentNullException>(() => VeramGeojsonWriter.Write(empty, settings, files, null!));
		Assert.Throws<ArgumentNullException>(() => VeramGeojsonWriter.OutputDirectory(null!));
	}

	/// <summary>Reports straight away on the calling thread, so the order is exactly the order reported.</summary>
	private sealed class InlineProgress(Action<ConversionProgress> report) : IProgress<ConversionProgress>
	{
		public void Report(ConversionProgress value) => report(value);
	}
}
