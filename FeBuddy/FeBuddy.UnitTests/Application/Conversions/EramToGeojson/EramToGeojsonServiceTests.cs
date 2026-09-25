using System.Text.Json;

using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Application.Conversions.EramToGeojson;
using FeBuddy.Core.Application.Conversions.EramToGeojson.Models;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Eram.Models;

namespace FeBuddy.UnitTests.Application.Conversions.EramToGeojson;

/// <summary>
/// Runs the whole ERAM to GeoJSON conversion (<see cref="EramToGeojsonService.Run"/>) against
/// small made-up Geomaps files written to a temp folder, in both layouts and with each defaults
/// source, and checks what lands on disk and what is reported.
/// </summary>
public sealed class EramToGeojsonServiceTests : IDisposable
{
	private const string LineDefaults =
		"<DefaultLineProperties><LineStyle>Solid</LineStyle><BCGGroup>1</BCGGroup><Color>White</Color><Thickness>1</Thickness>" +
		"<GeoLineFilters><FilterGroup>1</FilterGroup></GeoLineFilters></DefaultLineProperties>";

	private const string SymbolDefaults =
		"<DefaultSymbolProperties><SymbolStyle>VOR</SymbolStyle><BCGGroup>2</BCGGroup><Color>White</Color><FontSize>1</FontSize>" +
		"<GeoSymbolFilters><FilterGroup>2</FilterGroup></GeoSymbolFilters></DefaultSymbolProperties>";

	private const string TextDefaults =
		"<TextDefaultProperties><BCGGroup>3</BCGGroup><Color>White</Color><FontSize>1</FontSize><Underline>false</Underline>" +
		"<DisplaySetting>true</DisplaySetting><XPixelOffset>0</XPixelOffset><YPixelOffset>0</YPixelOffset>" +
		"<GeoTextFilters><FilterGroup>3</FilterGroup></GeoTextFilters></TextDefaultProperties>";

	/// <summary>40°N 100°W to 40°30'N 100°W.</summary>
	private static readonly string LineAB = Line("40000000N", "100000000W", "40300000N", "100000000W");

	/// <summary>40°30'N 100°W to 40°30'N 99°30'W: starts where <see cref="LineAB"/> ends.</summary>
	private static readonly string LineBC = Line("40300000N", "100000000W", "40300000N", "099300000W");

	private static readonly string SymbolA = Symbol("40000000N", "100000000W");

	private static readonly string TextA = Text("40000000N", "100000000W", "ZXX");

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Eram_" + Guid.NewGuid().ToString("N"));

	public EramToGeojsonServiceTests()
	{
		Directory.CreateDirectory(Source);
	}

	private string Source => Path.Combine(_root, "Export");

	private string Output => Path.Combine(_root, "Out");

	private string MapFolder(string file = "ZXX", string map = "CENTER") =>
		Path.Combine(Output, "FE-Buddy_Output", EramGeojsonWriter.RootFolder, file, map);

	public void Dispose()
	{
		if (Directory.Exists(_root))
		{
			Directory.Delete(_root, recursive: true);
		}
	}

	// ================= made-up Geomaps content =================

	private static string Line(string startLat, string startLon, string endLat, string endLon, string overrides = "") =>
		$"<GeoMapLine><LineObjectId>ZXX</LineObjectId>{overrides}<StartLatitude>{startLat}</StartLatitude><StartLongitude>{startLon}</StartLongitude>" +
		$"<EndLatitude>{endLat}</EndLatitude><EndLongitude>{endLon}</EndLongitude></GeoMapLine>";

	private static string Symbol(string lat, string lon, string overrides = "", string label = "") =>
		$"<GeoMapSymbol><SymbolId>ZXX</SymbolId>{overrides}<Latitude>{lat}</Latitude><Longitude>{lon}</Longitude>{label}</GeoMapSymbol>";

	private static string Text(string lat, string lon, string text, string overrides = "") =>
		$"<GeoMapText><TextObjectId>ZXX</TextObjectId>{overrides}<Latitude>{lat}</Latitude><Longitude>{lon}</Longitude>" +
		$"<GeoTextStrings>{string.Concat(text.Split('|').Select(line => $"<TextLine>{line}</TextLine>"))}</GeoTextStrings></GeoMapText>";

	private static string Filters(string container, params int[] filters) =>
		$"<{container}>{string.Concat(filters.Select(filter => $"<FilterGroup>{filter}</FilterGroup>"))}</{container}>";

	private static string Object(string type, int group, string content) =>
		$"<GeoMapObjectType><MapObjectType>{type}</MapObjectType><MapGroupId>{group}</MapGroupId>{content}</GeoMapObjectType>";

	private static string Record(string name, params string[] objects) =>
		$"<GeoMapRecord><GeomapId>{name}</GeomapId><BCGMenuName>DEFAULT</BCGMenuName>{string.Concat(objects)}</GeoMapRecord>";

	private void WriteGeomaps(string fileName, params string[] objects) =>
		WriteGeomapsWithRecords(fileName, Record("CENTER", objects));

	private void WriteGeomapsWithRecords(string fileName, string records) =>
		File.WriteAllText(Path.Combine(Source, fileName), $"""
			<Geomaps_Records xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="Geomaps.xsd">
			{records}
			</Geomaps_Records>
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

	// ================= Object Type and Map Group =================

	[Fact]
	public void each_object_is_a_file_with_its_defaults_first_then_its_features()
	{
		WriteGeomaps("ZXX.xml", Object("BOUNDARY", 1,
			LineDefaults + SymbolDefaults + TextDefaults + LineAB + LineBC + SymbolA + TextA));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings());
		string path = Path.Combine(MapFolder(), "BOUNDARY_1.geojson");

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

		// ERAM text has no opaque background.
		Assert.False(Properties(features[2]).GetProperty("opaque").GetBoolean());

		// The two segments meet, so they are one three-point line.
		JsonElement lines = features[3].GetProperty("geometry").GetProperty("coordinates");
		Assert.Equal(1, lines.GetArrayLength());
		Assert.Equal(3, lines[0].GetArrayLength());
		Assert.Equal(-99.5, lines[0][2][0].GetDouble(), 6);
		Assert.Equal("Point", features[4].GetProperty("geometry").GetProperty("type").GetString());
		Assert.Equal("ZXX", Properties(features[5]).GetProperty("text")[0].GetString());
	}

	[Fact]
	public void element_overrides_are_carried_over_and_lines_with_different_looks_stay_apart()
	{
		WriteGeomaps("ZXX.xml", Object("RUNWAY", 2, LineDefaults + TextDefaults +
			LineAB +
			Line("41000000N", "100000000W", "41300000N", "100000000W",
				"<LineStyle>LongDashed</LineStyle>" + Filters("GeoLineFilters", 7)) +
			Text("40000000N", "100000000W", "24L|6R", "<FontSize>3</FontSize><XPixelOffset>9</XPixelOffset>")));

		EramToGeojsonService.Run(Settings());
		JsonElement[] features = Features(Path.Combine(MapFolder(), "RUNWAY_2.geojson"));

		Assert.Equal(5, features.Length);
		Assert.False(Has(features[2], "style"));
		Assert.Equal("longDashed", Properties(features[3]).GetProperty("style").GetString());
		Assert.Equal(7, Properties(features[3]).GetProperty("filters")[0].GetInt32());
		Assert.Equal(3, Properties(features[4]).GetProperty("size").GetInt32());
		Assert.Equal(9, Properties(features[4]).GetProperty("xOffset").GetInt32());

		// Each ERAM TextLine is a line of CRC text.
		Assert.Equal(["24L", "6R"], Properties(features[4]).GetProperty("text").EnumerateArray().Select(t => t.GetString()));
	}

	[Fact]
	public void a_symbol_s_own_label_and_an_saa_s_boundary_and_label_are_converted()
	{
		WriteGeomaps("ZXX.xml",
			Object("SupplementalSymbol", 3, SymbolDefaults + TextDefaults +
				Symbol("40000000N", "100000000W", label: "<GeoMapText><GeoTextStrings><TextLine>RADR</TextLine></GeoTextStrings></GeoMapText>")),
			Object("SAA", 4,
				"<DefaultLineProperties><LineStyle>Solid</LineStyle><Color>White</Color><Thickness>1</Thickness></DefaultLineProperties>" +
				"<TextDefaultProperties><Color>White</Color><FontSize>1</FontSize><Underline>false</Underline>" +
				"<XPixelOffset>0</XPixelOffset><YPixelOffset>0</YPixelOffset></TextDefaultProperties>" +
				"<GeoMapSaa><SaaID>R0001</SaaID><Owning_Facility>ZXX</Owning_Facility>" +
				"<GeoMapSaaLabel><SaaLabel>R0001</SaaLabel><Latitude>40150000N</Latitude><Longitude>100150000W</Longitude></GeoMapSaaLabel>" +
				"<GeoMapSaaBoundary><GeoSaaLinesSegments>" +
				"<GeoMapSaaLine><ModuleID>A</ModuleID><Seq_num>1</Seq_num><StartLatitude>40000000N</StartLatitude><StartLongitude>100000000W</StartLongitude>" +
				"<EndLatitude>40300000N</EndLatitude><EndLongitude>100000000W</EndLongitude></GeoMapSaaLine>" +
				"<GeoMapSaaLine><ModuleID>A</ModuleID><Seq_num>2</Seq_num><StartLatitude>40300000N</StartLatitude><StartLongitude>100000000W</StartLongitude>" +
				"<EndLatitude>40300000N</EndLatitude><EndLongitude>100300000W</EndLongitude></GeoMapSaaLine>" +
				"</GeoSaaLinesSegments></GeoMapSaaBoundary></GeoMapSaa>"));

		// SAA defaults carry no BCG or filters, so the card fills them in.
		SourceFilesConversionResult result = EramToGeojsonService.Run(CardSettings("XmlThenCard"));

		JsonElement[] symbols = Features(Path.Combine(MapFolder(), "SupplementalSymbol_3.geojson"));
		Assert.Equal("RADR", Properties(symbols[^1]).GetProperty("text")[0].GetString());
		Assert.Equal(symbols[^2].GetProperty("geometry").ToString(), symbols[^1].GetProperty("geometry").ToString());

		JsonElement[] saa = Features(Path.Combine(MapFolder(), "SAA_4.geojson"));
		Assert.Equal(11, Properties(saa[0]).GetProperty("bcg").GetInt32());
		Assert.Equal(13, Properties(saa[1]).GetProperty("bcg").GetInt32());
		Assert.Equal(3, saa[2].GetProperty("geometry").GetProperty("coordinates")[0].GetArrayLength());
		Assert.Equal("R0001", Properties(saa[3]).GetProperty("text")[0].GetString());
		Assert.Contains(result.Messages, m => m.Text.Contains("CENTER / SAA_4: its LineDefaults have no Bcg, Filters"));
		Assert.Empty(result.Warnings);
	}

	[Fact]
	public void objects_sharing_a_name_share_a_file_only_when_their_defaults_agree()
	{
		WriteGeomaps("ZXX.xml",
			Object("VIDEO", 1, LineDefaults + LineAB),
			Object("video", 1, LineDefaults + TextDefaults + LineBC + TextA),
			Object("VIDEO", 1,
				"<DefaultLineProperties><LineStyle>Solid</LineStyle><BCGGroup>5</BCGGroup><Thickness>1</Thickness>" +
				Filters("GeoLineFilters", 5) + "</DefaultLineProperties>" + LineAB),
			Object("EMPTY", 1, LineDefaults));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings());

		Assert.Equal(
			[Path.Combine(MapFolder(), "VIDEO_1 (2).geojson"), Path.Combine(MapFolder(), "VIDEO_1.geojson")],
			result.GeojsonFilesWritten.Order(StringComparer.Ordinal));

		// The first two agree on their line defaults, so the text defaults join them and the
		// lines join up; the third's differ.
		JsonElement[] shared = Features(Path.Combine(MapFolder(), "VIDEO_1.geojson"));
		Assert.Equal(4, shared.Length);
		Assert.Equal(3, shared[2].GetProperty("geometry").GetProperty("coordinates")[0].GetArrayLength());
	}

	[Fact]
	public void objects_without_defaults_share_a_file_and_bad_overrides_are_left_out()
	{
		WriteGeomaps("ZXX.xml",
			Object("BARE", 1, TextA),
			Object("BARE", 1, Text("41000000N", "100000000W", "OTHER", "<FontSize>9</FontSize>")));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings());

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
		WriteGeomapsWithRecords("ZXX.xml",
			Record("CENTER", Object("A/B", 1, LineDefaults + LineAB)) +
			Record("center", Object("A/B", 1, LineDefaults + LineAB)));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings(("AddFeBuddyOutputFolder", "N")));
		string root = Path.Combine(Output, EramGeojsonWriter.RootFolder, "ZXX");

		Assert.Equal(
			[Path.Combine(root, "CENTER", "A-B_1.geojson"), Path.Combine(root, "center (2)", "A-B_1.geojson")],
			result.GeojsonFilesWritten);
	}

	// ================= Filter Index and Similar Attributes =================

	[Fact]
	public void by_filter_files_each_look_once_with_its_defaults_and_no_overrides()
	{
		WriteGeomaps("ZXX.xml",
			Object("ONE", 1, LineDefaults + SymbolDefaults + TextDefaults + LineAB + SymbolA +
				Text("40000000N", "100000000W", "ZXX", "<Underline>true</Underline>")),
			Object("TWO", 1, LineDefaults + TextDefaults + LineBC + TextA),
			Object("MULTI", 1,
				"<DefaultLineProperties><LineStyle>Solid</LineStyle><BCGGroup>1</BCGGroup><Thickness>1</Thickness>" +
				Filters("GeoLineFilters", 2, 1) + "</DefaultLineProperties>" + LineAB),
			Object("OVER", 1, LineDefaults +
				Line("40000000N", "100000000W", "41000000N", "100000000W", Filters("GeoLineFilters", 5))));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings(("OutputLayout", "ByFilter")));
		string map = MapFolder();

		string oneAndTwo = Path.Combine(map, "FILTER 01", "FILTER 01__Line__BCG 1__Style solid__Thickness 1.geojson");
		Assert.Equal(
			[
				oneAndTwo,
				Path.Combine(map, "FILTER 02", "FILTER 02__Symbol__BCG 2__Style vor__Size 1.geojson"),
				Path.Combine(map, "FILTER 03", "FILTER 03__Text__BCG 3__Size 1__Underline T__XOffset 0__YOffset 0.geojson"),
				Path.Combine(map, "FILTER 03", "FILTER 03__Text__BCG 3__Size 1__Underline F__XOffset 0__YOffset 0.geojson"),
				Path.Combine(map, "MULTI FILTERS", "FILTER 02 01", "FILTER 02 01__Line__BCG 1__Style solid__Thickness 1.geojson"),
				Path.Combine(map, "FILTER 05", "FILTER 05__Line__BCG 1__Style solid__Thickness 1.geojson"),
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
		WriteGeomaps("ZXX.xml", Object("BARE", 1,
			Line("40000000N", "100000000W", "41000000N", "100000000W", Filters("GeoLineFilters", 4)) +
			Line("42000000N", "100000000W", "43000000N", "100000000W", "<BCGGroup>99</BCGGroup>" + Filters("GeoLineFilters", 4))));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings(("OutputLayout", "ByFilter")));
		string path = Path.Combine(MapFolder(), "MISSING DEFAULTS", "Line.geojson");

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
		WriteGeomaps("ZXX.xml", Object("PARTIAL", 1,
			"<DefaultLineProperties><BCGGroup>1</BCGGroup><Thickness>1</Thickness>" + Filters("GeoLineFilters", 1) +
			"</DefaultLineProperties>" + LineAB));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings());

		Assert.Contains(result.Warnings, w => w.Contains("ZXX.xml: CENTER / PARTIAL_1: its LineDefaults have no Style"));
		Assert.Single(Features(result.GeojsonFilesWritten[0]));
	}

	[Fact]
	public void from_the_xml_then_the_card_fills_only_the_gaps()
	{
		WriteGeomaps("ZXX.xml",
			Object("HAS", 1, LineDefaults + LineAB),
			Object("LACKS", 1, LineBC +
				Line("42000000N", "100000000W", "43000000N", "100000000W", "<LineStyle>ShortDashed</LineStyle>")));

		SourceFilesConversionResult result = EramToGeojsonService.Run(CardSettings("XmlThenCard"));

		Assert.Equal(1, Properties(Features(Path.Combine(MapFolder(), "HAS_1.geojson"))[0]).GetProperty("bcg").GetInt32());

		JsonElement[] lacks = Features(Path.Combine(MapFolder(), "LACKS_1.geojson"));
		Assert.Equal(11, Properties(lacks[0]).GetProperty("bcg").GetInt32());
		Assert.Equal("shortDashed", Properties(lacks[2]).GetProperty("style").GetString());
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Info && m.Text.Contains("use the CRC Line defaults set on the tab"));
		Assert.Empty(result.Warnings);
	}

	[Fact]
	public void from_the_card_only_the_xml_s_styling_is_ignored()
	{
		WriteGeomaps("ZXX.xml", Object("ALL", 1, LineDefaults + SymbolDefaults + TextDefaults +
			Line("40000000N", "100000000W", "41000000N", "100000000W", "<LineStyle>ShortDashed</LineStyle>") +
			SymbolA +
			Text("40000000N", "100000000W", "ZXX", "<FontSize>4</FontSize>")));

		SourceFilesConversionResult result = EramToGeojsonService.Run(CardSettings("Card"));
		JsonElement[] features = Features(result.GeojsonFilesWritten[0]);

		Assert.Equal([11, 12, 13], features[..3].Select(f => Properties(f).GetProperty("bcg").GetInt32()));
		Assert.False(Has(features[3], "style"));
		Assert.Equal(["text"], Properties(features[5]).EnumerateObject().Select(p => p.Name));
		Assert.Empty(result.Warnings);
	}

	[Fact]
	public void by_filter_from_the_card_only_files_by_the_card_s_look_and_ignores_the_xml()
	{
		WriteGeomaps("ZXX.xml", Object("ALL", 1, LineDefaults +
			Line("40000000N", "100000000W", "41000000N", "100000000W", Filters("GeoLineFilters", 7)) +
			Symbol("40000000N", "100000000W", "<FontSize>4</FontSize>" + Filters("GeoSymbolFilters", 7))));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings(
			("OutputLayout", "ByFilter"), ("DefaultsSource", "Card"),
			("IncludeCrcLineDefaults", "Y"),
			("Crc.GeoMap.Line.bcg", "11"), ("Crc.GeoMap.Line.filters", "11"),
			("Crc.GeoMap.Line.style", "solid"), ("Crc.GeoMap.Line.thickness", "1")));

		// The line takes the card's look (filter 11, not its own 7); the symbol has no card
		// defaults and none of its own may be used, so it is kept apart with nothing set.
		Assert.Equal(
			[
				Path.Combine(MapFolder(), "FILTER 11", "FILTER 11__Line__BCG 11__Style solid__Thickness 1.geojson"),
				Path.Combine(MapFolder(), "MISSING DEFAULTS", "Symbol.geojson"),
			],
			result.GeojsonFilesWritten);
		Assert.Empty(Properties(Features(result.GeojsonFilesWritten[1])[0]).EnumerateObject());
	}

	[Fact]
	public void from_the_card_only_a_kind_without_card_defaults_is_warned_about_once()
	{
		WriteGeomaps("ZXX.xml",
			Object("ONE", 1, SymbolA),
			Object("TWO", 1, SymbolA));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings(("DefaultsSource", "Card")));

		string warning = Assert.Single(result.Warnings);
		Assert.Contains("no CRC Symbol defaults are set on the tab", warning);
	}

	// ================= files =================

	[Fact]
	public void text_without_text_is_skipped_and_a_file_with_nothing_to_draw_says_so()
	{
		WriteGeomaps("BLANK.xml", Object("LABELS", 1, TextDefaults + Text("40000000N", "100000000W", " ")));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings());

		Assert.Empty(result.GeojsonFilesWritten);
		Assert.Contains(result.Messages, m => m.Text.Contains("has no text and was skipped"));
		Assert.Contains(result.Messages, m => m.IsAdvisory && m.Text.Contains("BLANK.xml has no elements to draw"));
	}

	[Fact]
	public void a_folder_holding_a_whole_adaptation_export_converts_only_its_geomaps_file()
	{
		File.WriteAllText(Path.Combine(Source, "Airport.xml"), "<Airport_Records />");
		File.WriteAllText(Path.Combine(Source, "Broken.xml"), "not xml");
		File.WriteAllText(Path.Combine(Source, "Geomaps.xsd"), "<xsd:schema />");
		WriteGeomaps("Geomaps.xml", Object("OK", 1, LineDefaults + LineAB));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings());

		SourceFileConversion converted = Assert.Single(result.Files);
		Assert.Equal("Geomaps.xml", Path.GetFileName(converted.SourcePath));
		Assert.Equal(0, result.FailedCount);
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Info && m.Text.Contains("2 other file(s)") && m.Text.Contains("Airport.xml, Broken.xml"));
	}

	[Fact]
	public void a_folder_with_no_geomaps_file_says_so()
	{
		File.WriteAllText(Path.Combine(Source, "Airport.xml"), "<Airport_Records />");

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings());

		Assert.Empty(result.Files);
		Assert.Contains(result.Messages, m => m.IsAdvisory && m.Text.Contains("that this conversion reads, so nothing was converted"));
	}

	[Fact]
	public void a_picked_file_that_is_not_a_geomaps_file_fails_on_its_own()
	{
		File.WriteAllText(Path.Combine(Source, "Airport.xml"), "<Airport_Records />");
		File.WriteAllText(Path.Combine(Source, "Broken.xml"), "<Geomaps_Records><GeoMapRecord>");
		WriteGeomaps("Geomaps.xml", Object("OK", 1, LineDefaults + LineAB));
		List<ConversionProgress> reports = [];

		SourceFilesConversionResult result = EramToGeojsonService.Run(
			new Dictionary<string, string>
			{
				["OutputDirectory"] = Output,
				["SourceFiles"] = string.Join('|', new[] { "Airport.xml", "Broken.xml", "Geomaps.xml" }.Select(name => Path.Combine(Source, name))),
			},
			new InlineProgress(reports.Add));

		Assert.Equal(2, result.FailedCount);
		Assert.Contains("is not an ERAM Geomaps file", result.Files[0].Error);
		Assert.Contains("not well-formed XML", result.Files[1].Error);
		Assert.Single(result.GeojsonFilesWritten);
		Assert.Equal("1 GeoJSON file(s) written", reports[^1].Message);
	}

	[Fact]
	public void reader_problems_are_warned_about_and_counted()
	{
		WriteGeomaps("ZXX.xml", Object("OK", 1, LineDefaults + LineAB + "<GeoMapSymbol><Latitude>40000000N</Latitude></GeoMapSymbol>"));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings());

		Assert.Equal(1, result.Files[0].RecordsSkipped);
		Assert.Contains(result.Warnings, w => w.StartsWith("ZXX.xml: Line"));
	}

	[Fact]
	public void progress_says_when_a_file_had_nothing_to_write()
	{
		WriteGeomapsWithRecords("EMPTY.xml", Record("CENTER"));
		List<ConversionProgress> reports = [];

		EramToGeojsonService.Run(Settings(), new InlineProgress(reports.Add));

		Assert.Equal("nothing to write", reports[^1].Message);
	}

	[Fact]
	public void bad_arguments_are_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => EramToGeojsonService.Run(null!));

		EramGeoMapFile empty = new("x.xml", [], []);
		EramToGeojsonSettings settings = new() { OutputDirectory = Output };
		GeojsonFileSet files = new(6);

		Assert.Throws<ArgumentNullException>(() => EramGeojsonWriter.Write(null!, settings, files, []));
		Assert.Throws<ArgumentNullException>(() => EramGeojsonWriter.Write(empty, null!, files, []));
		Assert.Throws<ArgumentNullException>(() => EramGeojsonWriter.Write(empty, settings, null!, []));
		Assert.Throws<ArgumentNullException>(() => EramGeojsonWriter.Write(empty, settings, files, null!));
		Assert.Throws<ArgumentNullException>(() => EramGeojsonWriter.OutputDirectory(null!));
	}

	/// <summary>Reports straight away on the calling thread, so the order is exactly the order reported.</summary>
	private sealed class InlineProgress(Action<ConversionProgress> report) : IProgress<ConversionProgress>
	{
		public void Report(ConversionProgress value) => report(value);
	}
}
