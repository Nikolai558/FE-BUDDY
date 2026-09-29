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
/// small made-up Geomaps files written to a temp folder - in the original ERAM_2_GEOJSON tool's
/// three layouts, named as it named them, and with each defaults source, plus the
/// <c>ConsoleCommandControl.txt</c> rundown - and checks what lands on disk and what is reported.
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
	private static readonly string LineAB = Line("ZOB3NM", "40000000N", "100000000W", "40300000N", "100000000W");

	/// <summary>40°30'N 100°W to 40°30'N 99°30'W: starts where <see cref="LineAB"/> ends.</summary>
	private static readonly string LineBC = Line("ZOB3NM", "40300000N", "100000000W", "40300000N", "099300000W");

	private static readonly string SymbolA = Symbol("ZXX", "40000000N", "100000000W");

	private static readonly string TextA = Text("40000000N", "100000000W", "ZXX");

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Eram_" + Guid.NewGuid().ToString("N"));

	public EramToGeojsonServiceTests()
	{
		Directory.CreateDirectory(Source);
	}

	private string Source => Path.Combine(_root, "Export");

	private string Output => Path.Combine(_root, "Out");

	private string RootFolder => Path.Combine(Output, "FE-Buddy_Output", EramGeojsonWriter.RootFolder);

	private string MapFolder => Path.Combine(RootFolder, "CENTER_CENTER-MAP");

	public void Dispose()
	{
		if (Directory.Exists(_root))
		{
			Directory.Delete(_root, recursive: true);
		}
	}

	// ================= made-up Geomaps content =================

	private static string Line(string id, string startLat, string startLon, string endLat, string endLon, string overrides = "") =>
		$"<GeoMapLine><LineObjectId>{id}</LineObjectId>{overrides}<StartLatitude>{startLat}</StartLatitude><StartLongitude>{startLon}</StartLongitude>" +
		$"<EndLatitude>{endLat}</EndLatitude><EndLongitude>{endLon}</EndLongitude></GeoMapLine>";

	private static string Symbol(string id, string lat, string lon, string overrides = "", string label = "") =>
		$"<GeoMapSymbol><SymbolId>{id}</SymbolId>{overrides}<Latitude>{lat}</Latitude><Longitude>{lon}</Longitude>{label}</GeoMapSymbol>";

	private static string Text(string lat, string lon, string text, string overrides = "") =>
		$"<GeoMapText><TextObjectId>ZXX</TextObjectId>{overrides}<Latitude>{lat}</Latitude><Longitude>{lon}</Longitude>" +
		$"<GeoTextStrings>{string.Concat(text.Split('|').Select(line => $"<TextLine>{line}</TextLine>"))}</GeoTextStrings></GeoMapText>";

	private static string Filters(string container, params int[] filters) =>
		$"<{container}>{string.Concat(filters.Select(filter => $"<FilterGroup>{filter}</FilterGroup>"))}</{container}>";

	private static string Object(string type, int group, string content) =>
		$"<GeoMapObjectType><MapObjectType>{type}</MapObjectType><MapGroupId>{group}</MapGroupId>{content}</GeoMapObjectType>";

	/// <summary>A map labelled <c>&lt;name&gt;</c> / <c>MAP</c>, so it is written as <c>&lt;name&gt;_&lt;name&gt;-MAP</c>.</summary>
	private static string Record(string name, params string[] objects) =>
		$"<GeoMapRecord><GeomapId>{name}</GeomapId><BCGMenuName>DEFAULT</BCGMenuName><LabelLine1>{name}</LabelLine1><LabelLine2>MAP</LabelLine2>" +
		$"{string.Concat(objects)}</GeoMapRecord>";

	private void WriteGeomaps(params string[] objects) =>
		WriteGeomapsWithRecords("Geomaps.xml", Record("CENTER", objects));

	private void WriteGeomapsWithRecords(string fileName, string records) =>
		File.WriteAllText(Path.Combine(Source, fileName), $"""
			<Geomaps_Records xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="Geomaps.xsd">
			{records}
			</Geomaps_Records>
			""");

	/// <summary>
	/// A ConsoleCommandControl file in the source folder: a brightness menu (named
	/// <paramref name="bcgMenu"/>, as <see cref="Record"/>'s maps use <c>DEFAULT</c>) with one button,
	/// and a filter menu no map uses.
	/// </summary>
	private void WriteConsoleCommandControl(string fileName = "ConsoleCommandControl.xml", string bcgMenu = "DEFAULT") =>
		File.WriteAllText(Path.Combine(Source, fileName), $"""
			<ConsoleCommandControl_Records>
			  <MapBrightnessMenu><BCGMenuName>{bcgMenu}</BCGMenuName>
			    <MapBCGButton><MenuPosition>1</MenuPosition><Label>AAV</Label>
			      <MapBCGGroups><MapBCGGroup>1</MapBCGGroup><MapBCGGroup>2</MapBCGGroup></MapBCGGroups></MapBCGButton>
			  </MapBrightnessMenu>
			  <MapFilterMenu><FilterMenuName>ZOB</FilterMenuName></MapFilterMenu>
			</ConsoleCommandControl_Records>
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

	private static string[] Names(JsonElement feature) => [.. Properties(feature).EnumerateObject().Select(p => p.Name)];

	// ================= By Attributes =================

	[Fact]
	public void by_attributes_names_each_look_as_the_original_tool_did_with_its_defaults_first()
	{
		WriteGeomaps(
			Object("AAV", 64, LineDefaults + LineAB + LineBC),
			Object("NAVAID", 7, SymbolDefaults + TextDefaults + Symbol("CLE", "40000000N", "100000000W",
				label: "<GeoMapText><GeoTextStrings><TextLine>CLE</TextLine></GeoTextStrings></GeoMapText>")));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings());

		string lines = Path.Combine(MapFolder, "BCG 01_Filters 01_Type AAV_Group 64_Object ZOB3NM_Style Solid_Thick 1_Lines.geojson");
		string symbols = Path.Combine(MapFolder, "BCG 02_Filters 02_Type NAVAID_Group 7_Style VOR_Font 1_Symbols.geojson");
		string text = Path.Combine(MapFolder, "BCG 03_Filters 03_Type NAVAID_Group 7_Font 1_Underline F_X 0_Y 0_Text.geojson");

		Assert.Equal([lines, symbols, text], result.GeojsonFilesWritten);
		Assert.Equal(3, result.FeaturesWritten);
		Assert.Empty(result.Warnings);

		// Everything in a file looks the same: the isDefaults Feature says it all.
		JsonElement[] lineFeatures = Features(lines);
		Assert.True(Properties(lineFeatures[0]).GetProperty("isLineDefaults").GetBoolean());
		Assert.Equal("solid", Properties(lineFeatures[0]).GetProperty("style").GetString());
		Assert.Empty(Names(lineFeatures[1]));

		// The two segments meet, so they are one three-point line.
		JsonElement coordinates = lineFeatures[1].GetProperty("geometry").GetProperty("coordinates");
		Assert.Equal(1, coordinates.GetArrayLength());
		Assert.Equal(3, coordinates[0].GetArrayLength());

		Assert.Equal("vor", Properties(Features(symbols)[0]).GetProperty("style").GetString());

		JsonElement[] textFeatures = Features(text);
		Assert.False(Properties(textFeatures[0]).GetProperty("opaque").GetBoolean());
		Assert.Equal(["text"], Names(textFeatures[1]));
		Assert.Equal("CLE", Properties(textFeatures[1]).GetProperty("text")[0].GetString());
	}

	/// <summary>A line object, a different look, or another object type or map group is a file of its own.</summary>
	[Fact]
	public void by_attributes_splits_by_look_type_group_and_line_object()
	{
		WriteGeomaps(
			Object("AAV", 64, LineDefaults + LineAB +
				Line("OTHER", "41000000N", "100000000W", "41300000N", "100000000W") +
				Line("ZOB3NM", "42000000N", "100000000W", "42300000N", "100000000W",
					"<LineStyle>ShortDashed</LineStyle>" + Filters("GeoLineFilters", 8, 2, 3))),
			Object("AAV", 65, LineDefaults + LineBC));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings());

		Assert.Equal(
			[
				"BCG 01_Filters 01_Type AAV_Group 64_Object ZOB3NM_Style Solid_Thick 1_Lines.geojson",
				"BCG 01_Filters 01_Type AAV_Group 64_Object OTHER_Style Solid_Thick 1_Lines.geojson",
				"BCG 01_Filters 02 03 08_Type AAV_Group 64_Object ZOB3NM_Style ShortDashed_Thick 1_Lines.geojson",
				"BCG 01_Filters 01_Type AAV_Group 65_Object ZOB3NM_Style Solid_Thick 1_Lines.geojson",
			],
			result.GeojsonFilesWritten.Select(Path.GetFileName));
	}

	/// <summary>A look missing a value CRC needs is named with <c>none</c>, has no isDefaults Feature, and its Features carry what they have.</summary>
	[Fact]
	public void by_attributes_a_look_missing_a_value_is_named_none_and_carries_what_it_has()
	{
		WriteGeomaps(Object("BARE", 1,
			Line("L1", "40000000N", "100000000W", "41000000N", "100000000W", "<BCGGroup>4</BCGGroup>" + Filters("GeoLineFilters", 4))));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings());
		string path = Assert.Single(result.GeojsonFilesWritten);

		Assert.Equal("BCG 04_Filters 04_Type BARE_Group 1_Object L1_Style none_Thick none_Lines.geojson", Path.GetFileName(path));
		JsonElement feature = Assert.Single(Features(path));
		Assert.Equal(["bcg", "filters"], Names(feature));
		Assert.Contains(result.Warnings, w => w.Contains("CENTER / BARE_1: it has no LineDefaults"));
	}

	// ================= By Filters =================

	[Fact]
	public void by_filters_is_a_folder_per_set_of_filters_with_a_file_per_kind()
	{
		WriteGeomaps(
			Object("ONE", 1, LineDefaults + SymbolDefaults + TextDefaults + LineAB + SymbolA + TextA),
			Object("MULTI", 1,
				"<DefaultLineProperties><LineStyle>Solid</LineStyle><BCGGroup>1</BCGGroup><Thickness>1</Thickness>" +
				Filters("GeoLineFilters", 8, 2, 3) + "</DefaultLineProperties>" + LineBC));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings(("OutputLayout", "ByFilters")));

		Assert.Equal(
			[
				Path.Combine(MapFolder, "Filter_01", "Filter_01_Lines.geojson"),
				Path.Combine(MapFolder, "Filter_02", "Filter_02_Symbols.geojson"),
				Path.Combine(MapFolder, "Filter_03", "Filter_03_Text.geojson"),
				Path.Combine(MapFolder, "Multi-Filter_02_03_08", "Multi-Filter_02_03_08_Lines.geojson"),
			],
			result.GeojsonFilesWritten);
	}

	/// <summary>
	/// Everything with the same filters shares a file whatever its look: the look most share is
	/// the isDefaults Feature, and a Feature that looks different carries only what differs.
	/// </summary>
	[Fact]
	public void by_filters_the_most_common_look_is_the_defaults_and_the_rest_say_what_differs()
	{
		WriteGeomaps(
			Object("ONE", 1, LineDefaults + LineAB),
			Object("TWO", 2, LineDefaults + LineBC +
				Line("ODD", "42000000N", "100000000W", "43000000N", "100000000W", "<LineStyle>LongDashed</LineStyle><BCGGroup>5</BCGGroup>")));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings(("OutputLayout", "ByFilters")));
		JsonElement[] features = Features(Assert.Single(result.GeojsonFilesWritten));

		Assert.Equal(3, features.Length);
		Assert.Equal("solid", Properties(features[0]).GetProperty("style").GetString());

		// ONE's and TWO's plain lines look alike, so they join into one three-point line.
		Assert.Empty(Names(features[1]));
		Assert.Equal(3, features[1].GetProperty("geometry").GetProperty("coordinates")[0].GetArrayLength());

		Assert.Equal(["bcg", "style"], Names(features[2]));
		Assert.Equal("longDashed", Properties(features[2]).GetProperty("style").GetString());
		Assert.Equal(5, Properties(features[2]).GetProperty("bcg").GetInt32());
	}

	/// <summary>With no filters from the XML or the element, a feature shows at every filter: filter 0, as ERAM does.</summary>
	[Fact]
	public void by_filters_no_filters_at_all_is_filter_0()
	{
		WriteGeomaps(Object("NOFILTER", 1,
			"<DefaultLineProperties><LineStyle>Solid</LineStyle><BCGGroup>1</BCGGroup><Thickness>1</Thickness></DefaultLineProperties>" + LineAB));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings(("OutputLayout", "ByFilters")));
		string path = Assert.Single(result.GeojsonFilesWritten);

		Assert.Equal(Path.Combine(MapFolder, "Filter_00", "Filter_00_Lines.geojson"), path);
		Assert.Equal(0, Properties(Features(path)[0]).GetProperty("filters")[0].GetInt32());
		Assert.Empty(result.Warnings);
	}

	// ================= Raw =================

	[Fact]
	public void raw_is_one_file_per_map_beside_the_folders_every_feature_carrying_its_look()
	{
		WriteGeomaps(Object("ALL", 1, LineDefaults + SymbolDefaults + TextDefaults + LineAB + LineBC + SymbolA +
			Text("40000000N", "100000000W", "24L|6R", "<FontSize>3</FontSize>")));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings(("OutputLayout", "Raw")));
		string path = Path.Combine(RootFolder, "CENTER_CENTER-MAP.geojson");

		Assert.Equal([path], result.GeojsonFilesWritten);

		// No isDefaults Feature, and lines are not joined: one Feature per element.
		JsonElement[] features = Features(path);
		Assert.Equal(4, features.Length);
		Assert.Equal("LineString", features[0].GetProperty("geometry").GetProperty("type").GetString());
		Assert.Equal(["bcg", "filters", "style", "thickness"], Names(features[0]));
		Assert.Equal(["bcg", "filters", "style", "size"], Names(features[2]));
		Assert.Equal(["text", "bcg", "filters", "size", "underline", "xOffset", "yOffset"], Names(features[3]));
		Assert.Equal(3, Properties(features[3]).GetProperty("size").GetInt32());
		Assert.Equal(["24L", "6R"], Properties(features[3]).GetProperty("text").EnumerateArray().Select(t => t.GetString()));
	}

	// ================= maps, text and properties =================

	[Fact]
	public void a_map_without_label_lines_is_ll1_ll2_and_names_lose_forbidden_characters()
	{
		WriteGeomapsWithRecords("Geomaps.xml",
			"<GeoMapRecord><GeomapId>A/B</GeomapId>" + Object("X", 1, LineDefaults + LineAB) + "</GeoMapRecord>" +
			"<GeoMapRecord><GeomapId>AB</GeomapId>" + Object("X", 1, LineDefaults + LineAB) + "</GeoMapRecord>");

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings(("OutputLayout", "Raw"), ("AddFeBuddyOutputFolder", "N")));
		string root = Path.Combine(Output, EramGeojsonWriter.RootFolder);

		// Taken out, not replaced, as the original tool did; the second map keeps its own file.
		Assert.Equal([Path.Combine(root, "AB_LL1-LL2.geojson"), Path.Combine(root, "AB_LL1-LL2 (2).geojson")], result.GeojsonFilesWritten);
	}

	[Fact]
	public void text_erams_keeps_hidden_is_left_out_and_counted()
	{
		WriteGeomaps(
			Object("SHOWN", 1, TextDefaults + TextA + Text("41000000N", "100000000W", "HIDE", "<DisplaySetting>false</DisplaySetting>")),
			Object("HIDDEN", 2, TextDefaults.Replace("<DisplaySetting>true", "<DisplaySetting>false", StringComparison.Ordinal) + TextA));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings(("OutputLayout", "Raw")));

		JsonElement feature = Assert.Single(Features(Assert.Single(result.GeojsonFilesWritten)));
		Assert.Equal("ZXX", Properties(feature).GetProperty("text")[0].GetString());
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Info && m.Text == "Geomaps.xml: 2 label(s) ERAM keeps hidden (DisplaySetting false) were left out.");
	}

	[Fact]
	public void the_chosen_feb_properties_go_on_every_feature_they_apply_to()
	{
		WriteGeomaps(
			Object("AAV", 64, LineDefaults + SymbolDefaults + LineAB + SymbolA),
			Object("SAA", 9, "<GeoMapSaa><SaaID>R0001</SaaID><GeoMapSaaBoundary><GeoSaaLinesSegments>" +
				"<GeoMapSaaLine><StartLatitude>40000000N</StartLatitude><StartLongitude>100000000W</StartLongitude>" +
				"<EndLatitude>40300000N</EndLatitude><EndLongitude>100000000W</EndLongitude></GeoMapSaaLine>" +
				"</GeoSaaLinesSegments></GeoMapSaaBoundary></GeoMapSaa>"));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings(
			("OutputLayout", "Raw"),
			("IncludeFebCustomProperties", "Y"), ("FebProperties", "mapObjectType,mapGroupId,lineObjectId,symbolId,saaId")));

		JsonElement[] features = Features(Assert.Single(result.GeojsonFilesWritten));
		Assert.Equal("ZOB3NM", Properties(features[0]).GetProperty("feb.lineObjectId").GetString());
		Assert.Equal(64, Properties(features[0]).GetProperty("feb.mapGroupId").GetInt32());
		Assert.False(Has(features[0], "feb.symbolId"));
		Assert.Equal("ZXX", Properties(features[1]).GetProperty("feb.symbolId").GetString());
		Assert.Equal("AAV", Properties(features[1]).GetProperty("feb.mapObjectType").GetString());
		Assert.Equal("R0001", Properties(features[2]).GetProperty("feb.saaId").GetString());
		Assert.False(Has(features[2], "feb.lineObjectId"));
	}

	[Fact]
	public void an_element_s_own_bad_value_is_left_out_with_a_warning()
	{
		WriteGeomaps(Object("RUNWAY", 2, LineDefaults + TextDefaults +
			Text("40000000N", "100000000W", "24L", "<FontSize>9</FontSize><XPixelOffset>9</XPixelOffset>")));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings(("OutputLayout", "Raw")));
		JsonElement feature = Assert.Single(Features(Assert.Single(result.GeojsonFilesWritten)));

		Assert.False(Has(feature, "size"));
		Assert.Equal(9, Properties(feature).GetProperty("xOffset").GetInt32());
		Assert.Contains(result.Warnings, w => w.Contains("a Text element's Size=\"9\" is not a value CRC can draw and was left out"));
	}

	// ================= defaults source =================

	[Fact]
	public void from_the_xml_an_object_whose_defaults_leave_something_out_is_warned_about()
	{
		WriteGeomaps(Object("PARTIAL", 1,
			"<DefaultLineProperties><BCGGroup>1</BCGGroup><Thickness>1</Thickness>" + Filters("GeoLineFilters", 1) +
			"</DefaultLineProperties>" + LineAB));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings());

		Assert.Contains(result.Warnings, w => w.Contains("Geomaps.xml: CENTER / PARTIAL_1: its LineDefaults have no Style"));
		Assert.Equal("BCG 01_Filters 01_Type PARTIAL_Group 1_Object ZOB3NM_Style none_Thick 1_Lines.geojson", Path.GetFileName(result.GeojsonFilesWritten[0]));
	}

	[Fact]
	public void from_the_xml_then_the_card_fills_only_the_gaps()
	{
		WriteGeomaps(
			Object("HAS", 1, LineDefaults + LineAB),
			Object("SAA", 4,
				"<DefaultLineProperties><LineStyle>ShortDashed</LineStyle><Color>White</Color><Thickness>1</Thickness></DefaultLineProperties>" +
				"<GeoMapSaa><SaaID>R0001</SaaID><GeoMapSaaBoundary><GeoSaaLinesSegments>" +
				"<GeoMapSaaLine><StartLatitude>40000000N</StartLatitude><StartLongitude>100000000W</StartLongitude>" +
				"<EndLatitude>40300000N</EndLatitude><EndLongitude>100000000W</EndLongitude></GeoMapSaaLine>" +
				"</GeoSaaLinesSegments></GeoMapSaaBoundary></GeoMapSaa>"));

		SourceFilesConversionResult result = EramToGeojsonService.Run(CardSettings("XmlThenCard"));

		// SAA defaults carry no BCG or filters: the card's 11 fills both, the XML keeps its style.
		Assert.Equal(
			[
				"BCG 01_Filters 01_Type HAS_Group 1_Object ZOB3NM_Style Solid_Thick 1_Lines.geojson",
				"BCG 11_Filters 11_Type SAA_Group 4_Object R0001_Style ShortDashed_Thick 1_Lines.geojson",
			],
			result.GeojsonFilesWritten.Select(Path.GetFileName));
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Info && m.Text.Contains("CENTER / SAA_4: its LineDefaults have no Bcg, Filters"));
		Assert.Empty(result.Warnings);
	}

	[Fact]
	public void from_the_card_only_the_xml_s_styling_is_ignored()
	{
		WriteGeomaps(Object("ALL", 1, LineDefaults + SymbolDefaults + TextDefaults +
			Line("L1", "40000000N", "100000000W", "41000000N", "100000000W", "<LineStyle>ShortDashed</LineStyle>") +
			SymbolA +
			Text("40000000N", "100000000W", "ZXX", "<FontSize>4</FontSize>")));

		SourceFilesConversionResult result = EramToGeojsonService.Run(CardSettings("Card"));

		Assert.Equal(
			[
				"BCG 11_Filters 11_Type ALL_Group 1_Object L1_Style LongDashed_Thick 3_Lines.geojson",
				"BCG 12_Filters 12_Type ALL_Group 1_Style Ndb_Font 2_Symbols.geojson",
				"BCG 13_Filters 13_Type ALL_Group 1_Font 2_Underline F_X 0_Y 0_Text.geojson",
			],
			result.GeojsonFilesWritten.Select(Path.GetFileName));
		Assert.Empty(result.Warnings);
	}

	[Fact]
	public void from_the_card_only_a_kind_without_card_defaults_is_warned_about_once()
	{
		WriteGeomaps(
			Object("ONE", 1, SymbolA),
			Object("TWO", 1, SymbolA));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings(("DefaultsSource", "Card")));

		string warning = Assert.Single(result.Warnings);
		Assert.Contains("no CRC Symbol defaults are set on the tab", warning);
	}

	// ================= the run =================

	/// <summary>As the original tool emptied its output folder: only this run's files are left.</summary>
	[Fact]
	public void each_run_empties_the_folder_first()
	{
		Directory.CreateDirectory(Path.Combine(RootFolder, "OLD_OLD-MAP"));
		File.WriteAllText(Path.Combine(RootFolder, "OLD_OLD-MAP", "stale.geojson"), "{}");
		File.WriteAllText(Path.Combine(RootFolder, "stale.geojson"), "{}");
		WriteGeomaps(Object("OK", 1, LineDefaults + LineAB));

		EramToGeojsonService.Run(Settings());

		Assert.Equal(["CENTER_CENTER-MAP"], Directory.EnumerateFileSystemEntries(RootFolder).Select(Path.GetFileName));
	}

	[Fact]
	public void what_cannot_be_emptied_is_said_and_the_run_goes_on()
	{
		Directory.CreateDirectory(RootFolder);
		string locked = Path.Combine(RootFolder, "open.geojson");
		WriteGeomaps(Object("OK", 1, LineDefaults + LineAB));

		SourceFilesConversionResult result;
		using (new FileStream(locked, FileMode.Create, FileAccess.Write, FileShare.None))
		{
			result = EramToGeojsonService.Run(Settings());
		}

		Assert.Single(result.GeojsonFilesWritten);
		Assert.Contains(result.Messages, m => m.IsAdvisory && m.Text.Contains("open.geojson could not be deleted"));
	}

	/// <summary>A file that fails leaves the last run's files alone: the folder is emptied only once the file has been read.</summary>
	[Fact]
	public void a_file_that_cannot_be_read_leaves_the_last_run_s_files()
	{
		Directory.CreateDirectory(RootFolder);
		File.WriteAllText(Path.Combine(RootFolder, "last.geojson"), "{}");
		File.WriteAllText(Path.Combine(Source, "Airport.xml"), "<Airport_Records />");
		List<ConversionProgress> reports = [];

		SourceFilesConversionResult result = EramToGeojsonService.Run(new Dictionary<string, string>
		{
			["OutputDirectory"] = Output,
			["SourceFiles"] = Path.Combine(Source, "Airport.xml"),
		}, new InlineProgress(reports.Add));

		string error = Assert.Single(result.Files).Error!;
		Assert.Contains("is not an ERAM Geomaps file", error);
		Assert.Equal(error, reports[^1].Message);
		Assert.True(File.Exists(Path.Combine(RootFolder, "last.geojson")));
	}

	[Fact]
	public void more_than_one_geomaps_file_is_refused()
	{
		WriteGeomapsWithRecords("A.xml", Record("CENTER"));
		WriteGeomapsWithRecords("B.xml", Record("CENTER"));

		ArgumentException folder = Assert.Throws<ArgumentException>(() => EramToGeojsonService.Run(Settings()));
		ArgumentException files = Assert.Throws<ArgumentException>(() => EramToGeojsonService.Run(new Dictionary<string, string>
		{
			["OutputDirectory"] = Output,
			["SourceFiles"] = $"{Path.Combine(Source, "A.xml")}|{Path.Combine(Source, "B.xml")}",
		}));

		Assert.Equal("ERAM to GeoJSON converts one Geomaps file per run, and 2 were given (A.xml, B.xml). Pick one.", folder.Message);
		Assert.Equal(folder.Message, files.Message);
	}

	/// <summary>The ConsoleCommandControl file is read, not left alone, so it is not listed with the rest.</summary>
	[Fact]
	public void a_folder_holding_a_whole_adaptation_export_converts_only_its_geomaps_file()
	{
		File.WriteAllText(Path.Combine(Source, "Airport.xml"), "<Airport_Records />");
		File.WriteAllText(Path.Combine(Source, "Broken.xml"), "not xml");
		File.WriteAllText(Path.Combine(Source, "Geomaps.xsd"), "<xsd:schema />");
		WriteConsoleCommandControl();
		WriteGeomaps(Object("OK", 1, LineDefaults + LineAB));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings());

		SourceFileConversion converted = Assert.Single(result.Files);
		Assert.Equal("Geomaps.xml", Path.GetFileName(converted.SourcePath));
		Assert.Equal(0, result.FailedCount);
		Assert.Equal(RootFolder, result.OutputDirectory);
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Info && m.Text.Contains("2 other file(s)") && m.Text.EndsWith("left alone: Airport.xml, Broken.xml"));
	}

	// ================= ConsoleCommandControl.txt =================

	/// <summary>As the original tool did, with the file beside Geomaps.xml: the menus are listed beside the maps.</summary>
	[Fact]
	public void the_console_command_control_file_beside_geomaps_is_listed_in_console_command_control_txt()
	{
		WriteConsoleCommandControl();
		WriteGeomaps(Object("OK", 1, LineDefaults + LineAB));
		List<ConversionProgress> reports = [];

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings(), new InlineProgress(reports.Add));

		string rundown = Path.Combine(RootFolder, "ConsoleCommandControl.txt");
		Assert.Equal([rundown], result.OtherFilesWritten);
		Assert.Single(result.GeojsonFilesWritten);
		Assert.Equal("1 GeoJSON file(s) written, plus ConsoleCommandControl.txt", reports[^1].Message);

		string text = File.ReadAllText(rundown);
		Assert.Contains("BCG Menu: DEFAULT\r\n\r\n\tUsed with:\tCENTER\r\n\r\n\tLabel:\t\tAAV\r\n\tPosition:\t1\r\n\tGroup:\t\t1, 2\r\n", text);
		Assert.Contains("FilterMenu: ZOB\r\n\r\n\tUsed with:\tNone\r\n", text);
	}

	/// <summary>Found by what it holds, as the Geomaps file is, so a renamed one is found too.</summary>
	[Fact]
	public void a_picked_geomaps_file_finds_the_console_command_control_file_beside_it()
	{
		WriteConsoleCommandControl("ZOB_CCC.xml");
		WriteGeomaps(Object("OK", 1, LineDefaults + LineAB));

		SourceFilesConversionResult result = EramToGeojsonService.Run(new Dictionary<string, string>
		{
			["OutputDirectory"] = Output,
			["SourceFiles"] = Path.Combine(Source, "Geomaps.xml"),
		});

		Assert.Equal("ConsoleCommandControl.txt", Path.GetFileName(Assert.Single(result.OtherFilesWritten)));
		Assert.Contains("\tUsed with:\tCENTER", File.ReadAllText(Assert.Single(result.OtherFilesWritten)));
	}

	[Fact]
	public void without_a_console_command_control_file_the_geojson_is_written_and_that_is_said()
	{
		WriteGeomaps(Object("OK", 1, LineDefaults + LineAB));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings());

		Assert.Single(result.GeojsonFilesWritten);
		Assert.Empty(result.OtherFilesWritten);
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Info && !m.IsAdvisory
			&& m.Text == "There is no ConsoleCommandControl.xml beside Geomaps.xml, so no ConsoleCommandControl.txt was written.");
	}

	[Fact]
	public void of_several_console_command_control_files_the_one_so_named_or_else_the_first_is_used()
	{
		WriteConsoleCommandControl("A.xml", bcgMenu: "FROM_A");
		WriteConsoleCommandControl("ConsoleCommandControl.xml", bcgMenu: "FROM_CCC");
		WriteGeomaps(Object("OK", 1, LineDefaults + LineAB));

		SourceFilesConversionResult named = EramToGeojsonService.Run(Settings());

		Assert.Contains("BCG Menu: FROM_CCC", File.ReadAllText(Assert.Single(named.OtherFilesWritten)));
		Assert.Contains(named.Messages, m => m.IsAdvisory
			&& m.Text == "2 ConsoleCommandControl files are beside Geomaps.xml (A.xml, ConsoleCommandControl.xml), so ConsoleCommandControl.xml was used.");

		File.Delete(Path.Combine(Source, "ConsoleCommandControl.xml"));
		WriteConsoleCommandControl("B.xml", bcgMenu: "FROM_B");

		SourceFilesConversionResult first = EramToGeojsonService.Run(Settings());

		Assert.Contains("BCG Menu: FROM_A", File.ReadAllText(Assert.Single(first.OtherFilesWritten)));
		Assert.Contains(first.Messages, m => m.IsAdvisory && m.Text.EndsWith("(A.xml, B.xml), so A.xml was used."));
	}

	[Fact]
	public void a_console_command_control_file_that_cannot_be_read_is_warned_about_and_the_geojson_stands()
	{
		File.WriteAllText(Path.Combine(Source, "ConsoleCommandControl.xml"), "<ConsoleCommandControl_Records><MapBrightnessMenu>");
		WriteGeomaps(Object("OK", 1, LineDefaults + LineAB));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings());

		Assert.Single(result.GeojsonFilesWritten);
		Assert.Empty(result.OtherFilesWritten);
		Assert.Equal(0, result.FailedCount);
		Assert.Contains(result.Messages, m => m.IsAdvisory
			&& m.Text.StartsWith("ConsoleCommandControl.xml could not be read, so no ConsoleCommandControl.txt was written: ConsoleCommandControl.xml is not well-formed XML"));
	}

	[Fact]
	public void a_rundown_that_cannot_be_written_is_warned_about_and_the_geojson_stands()
	{
		Directory.CreateDirectory(RootFolder);
		WriteConsoleCommandControl();
		WriteGeomaps(Object("OK", 1, LineDefaults + LineAB));

		SourceFilesConversionResult result;
		using (new FileStream(Path.Combine(RootFolder, "ConsoleCommandControl.txt"), FileMode.Create, FileAccess.Write, FileShare.None))
		{
			result = EramToGeojsonService.Run(Settings());
		}

		Assert.Single(result.GeojsonFilesWritten);
		Assert.Empty(result.OtherFilesWritten);
		Assert.Contains(result.Messages, m => m.IsAdvisory && m.Text.StartsWith("ConsoleCommandControl.txt could not be written (open elsewhere?): "));
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
	public void text_without_text_is_skipped_and_a_file_with_nothing_to_draw_says_so()
	{
		WriteGeomaps(Object("LABELS", 1, TextDefaults + Text("40000000N", "100000000W", " ")));
		List<ConversionProgress> reports = [];

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings(), new InlineProgress(reports.Add));

		Assert.Empty(result.GeojsonFilesWritten);
		Assert.Contains(result.Messages, m => m.Text.Contains("has no text and was skipped"));
		Assert.Contains(result.Messages, m => m.IsAdvisory && m.Text.Contains("Geomaps.xml has no elements to draw"));
		Assert.Equal("nothing to write", reports[^1].Message);
	}

	[Fact]
	public void reader_problems_are_warned_about_and_counted()
	{
		WriteGeomaps(Object("OK", 1, LineDefaults + LineAB + "<GeoMapSymbol><Latitude>40000000N</Latitude></GeoMapSymbol>"));

		SourceFilesConversionResult result = EramToGeojsonService.Run(Settings());

		Assert.Equal(1, result.Files[0].RecordsSkipped);
		Assert.Contains(result.Warnings, w => w.StartsWith("Geomaps.xml: Line"));
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
		Assert.Throws<ArgumentException>(() => EramGeojsonWriter.OutputDirectory(" ", addFeBuddyOutputFolder: true));
		Assert.Throws<ArgumentNullException>(() => EramGeojsonWriter.MapName(null!));
	}

	/// <summary>Reports straight away on the calling thread, so the order is exactly the order reported.</summary>
	private sealed class InlineProgress(Action<ConversionProgress> report) : IProgress<ConversionProgress>
	{
		public void Report(ConversionProgress value) => report(value);
	}
}
