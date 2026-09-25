using System.Text.Json;

using FeBuddy.Core.Application.Conversions.Models;
using FeBuddy.Core.Application.Conversions.SctToGeojson;
using FeBuddy.Core.Application.Conversions.SctToGeojson.Models;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Sct.Models;

namespace FeBuddy.UnitTests.Application.Conversions.SctToGeojson;

/// <summary>
/// Runs the whole SCT2 to GeoJSON conversion (<see cref="SctToGeojsonService.Run"/>) against small
/// sector files written to a temp folder, and checks what lands on disk and what is reported.
/// </summary>
public sealed class SctToGeojsonServiceTests : IDisposable
{
	private const string A = "N040.00.00.000 W075.00.00.000";
	private const string B = "N040.30.00.000 W075.00.00.000";
	private const string C = "N040.30.00.000 W074.30.00.000";
	private const string D = "N041.00.00.000 W074.30.00.000";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Sct_" + Guid.NewGuid().ToString("N"));

	public SctToGeojsonServiceTests()
	{
		Directory.CreateDirectory(Source);
	}

	private string Source => Path.Combine(_root, "Sectors");

	private string Output => Path.Combine(_root, "Out");

	private string Converted(string sectorName) =>
		Path.Combine(Output, "FE-Buddy_Output", SctGeojsonWriter.RootFolder, sectorName);

	public void Dispose()
	{
		if (Directory.Exists(_root))
		{
			Directory.Delete(_root, recursive: true);
		}
	}

	private string WriteSector(string fileName, params string[] records)
	{
		string path = Path.Combine(Source, fileName);
		File.WriteAllLines(path, records);
		return path;
	}

	/// <summary>A sector with something in every section FE-Buddy converts.</summary>
	private string WriteFullSector(string fileName = "ZXX.sct2") => WriteSector(fileName,
		"[ARTCC]",
		$"ZXX {A} {B}",
		$"ZXX {B} {C}",
		$"ZYY {C} {D}",
		"[ARTCC HIGH]",
		$"ZXX_HI {A} {B}",
		"[ARTCC LOW]",
		$"ZXX_LO {A} {B}",
		"[LOW AIRWAY]",
		$"V1 {A} {B}",
		"[HIGH AIRWAY]",
		$"J1 {A} {B}",
		"[GEO]",
		$"{A} {B} COAST",
		$"{A} {B} COAST",
		"[SID]",
		$"{"KXXX DEP",-26}{A} {B}",
		$"                          {B} {C}",
		"[STAR]",
		$"{"KXXX ARR",-26}{C} {D}",
		"[LABELS]",
		$"\"KXXX\" {A} WHITE",
		"[REGIONS]",
		$"GRASS {A}",
		$"  {B}",
		$"  {C}");

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

	private static JsonElement[] Features(string path) =>
		[.. JsonDocument.Parse(File.ReadAllText(path)).RootElement.GetProperty("features").EnumerateArray()];

	private static string GeometryType(JsonElement feature) =>
		feature.GetProperty("geometry").GetProperty("type").GetString()!;

	[Fact]
	public void converts_every_section_into_its_own_file_in_a_folder_per_sector_file()
	{
		WriteFullSector();

		SctToGeojsonServiceResult result = SctToGeojsonService.Run(Settings());
		string folder = Converted("ZXX");

		Assert.Equal(
			[
				Path.Combine(folder, "ARTCC.geojson"),
				Path.Combine(folder, "ARTCC-HIGH.geojson"),
				Path.Combine(folder, "ARTCC-LOW.geojson"),
				Path.Combine(folder, "LOW-AIRWAY.geojson"),
				Path.Combine(folder, "HIGH-AIRWAY.geojson"),
				Path.Combine(folder, "GEO.geojson"),
				Path.Combine(folder, "SID", "KXXX DEP.geojson"),
				Path.Combine(folder, "STAR", "KXXX ARR.geojson"),
				Path.Combine(folder, "LABELS.geojson"),
				Path.Combine(folder, "REGIONS.geojson"),
			],
			result.GeojsonFilesWritten.OrderBy(Order));

		Assert.Equal(1, result.SourceFileCount);
		Assert.Equal(0, result.FailedCount);
		Assert.Empty(result.Warnings);
		Assert.Equal(Path.Combine(Output, "FE-Buddy_Output", SctGeojsonWriter.RootFolder), result.OutputDirectory);

		SctFileConversion file = Assert.Single(result.Files);
		// Two ARTCC names, one feature in each other line file, one SID, one STAR, one label, one region.
		Assert.Equal(11, file.FeaturesWritten);
		Assert.Equal(0, file.RecordsSkipped);
	}

	[Fact]
	public void each_boundary_name_is_one_feature_and_touching_segments_join_into_one_line()
	{
		WriteFullSector();

		SctToGeojsonService.Run(Settings());
		JsonElement[] artcc = Features(Path.Combine(Converted("ZXX"), "ARTCC.geojson"));

		Assert.Equal(2, artcc.Length);

		// ZXX's two segments meet at B, so they are one three-point line - not joined onto ZYY,
		// which starts where ZXX ends.
		JsonElement zxx = artcc[0].GetProperty("geometry").GetProperty("coordinates");
		Assert.Equal(1, zxx.GetArrayLength());
		Assert.Equal(3, zxx[0].GetArrayLength());
	}

	[Fact]
	public void a_segment_drawn_twice_is_written_once()
	{
		WriteFullSector();

		SctToGeojsonService.Run(Settings());
		JsonElement geo = Assert.Single(Features(Path.Combine(Converted("ZXX"), "GEO.geojson")));

		JsonElement lines = geo.GetProperty("geometry").GetProperty("coordinates");
		Assert.Equal(1, lines.GetArrayLength());
		Assert.Equal(2, lines[0].GetArrayLength());
	}

	[Fact]
	public void a_diagram_s_continuation_lines_join_its_first_segment()
	{
		WriteFullSector();

		SctToGeojsonService.Run(Settings());
		JsonElement sid = Assert.Single(Features(Path.Combine(Converted("ZXX"), "SID", "KXXX DEP.geojson")));

		Assert.Equal("MultiLineString", GeometryType(sid));
		Assert.Equal(3, sid.GetProperty("geometry").GetProperty("coordinates")[0].GetArrayLength());
	}

	[Fact]
	public void labels_are_text_points_and_regions_are_closed_polygons()
	{
		WriteFullSector();

		SctToGeojsonService.Run(Settings());
		string folder = Converted("ZXX");

		JsonElement label = Assert.Single(Features(Path.Combine(folder, "LABELS.geojson")));
		Assert.Equal("Point", GeometryType(label));
		Assert.Equal("KXXX", label.GetProperty("properties").GetProperty("text")[0].GetString());

		JsonElement region = Assert.Single(Features(Path.Combine(folder, "REGIONS.geojson")));
		Assert.Equal("Polygon", GeometryType(region));
		JsonElement ring = region.GetProperty("geometry").GetProperty("coordinates")[0];
		Assert.Equal(4, ring.GetArrayLength());
		Assert.Equal(ring[0].ToString(), ring[3].ToString());
	}

	[Fact]
	public void a_region_already_closed_keeps_its_ring_as_written()
	{
		WriteSector("CLOSED.sct2", "[REGIONS]", $"GRASS {A}", $"  {B}", $"  {C}", $"  {A}");

		SctToGeojsonService.Run(Settings());
		JsonElement region = Assert.Single(Features(Path.Combine(Converted("CLOSED"), "REGIONS.geojson")));

		Assert.Equal(4, region.GetProperty("geometry").GetProperty("coordinates")[0].GetArrayLength());
	}

	[Fact]
	public void crc_defaults_head_the_line_files_and_the_labels_file_when_included()
	{
		WriteFullSector();

		SctToGeojsonService.Run(Settings(
			("IncludeCrcLineDefaults", "Y"),
			("Crc.SectorFile.Line.bcg", "4"),
			("Crc.SectorFile.Line.filters", "1"),
			("Crc.SectorFile.Line.style", "solid"),
			("Crc.SectorFile.Line.thickness", "1"),
			("IncludeCrcTextDefaults", "Y"),
			("Crc.SectorFile.Text.bcg", "5"),
			("Crc.SectorFile.Text.filters", "2"),
			("Crc.SectorFile.Text.size", "1"),
			("Crc.SectorFile.Text.underline", "N"),
			("Crc.SectorFile.Text.opaque", "N"),
			("Crc.SectorFile.Text.xOffset", "0"),
			("Crc.SectorFile.Text.yOffset", "0")));

		string folder = Converted("ZXX");

		Assert.True(Features(Path.Combine(folder, "GEO.geojson"))[0].GetProperty("properties").GetProperty("isLineDefaults").GetBoolean());
		Assert.True(Features(Path.Combine(folder, "SID", "KXXX DEP.geojson"))[0].GetProperty("properties").GetProperty("isLineDefaults").GetBoolean());
		Assert.True(Features(Path.Combine(folder, "LABELS.geojson"))[0].GetProperty("properties").GetProperty("isTextDefaults").GetBoolean());

		// Regions are filled areas, which CRC ERAM defaults do not describe.
		Assert.Single(Features(Path.Combine(folder, "REGIONS.geojson")));
	}

	[Fact]
	public void diagram_names_are_made_file_safe_without_two_diagrams_sharing_a_file()
	{
		WriteSector("NAMES.sct2",
			"[SID]",
			$"{"KXXX A/B",-26}{A} {B}",
			$"{"KXXX A?B",-26}{B} {C}",
			$"{"kxxx a/b",-26}{C} {D}");

		SctToGeojsonServiceResult result = SctToGeojsonService.Run(Settings());
		string sid = Path.Combine(Converted("NAMES"), "SID");

		// Names differing only in case are one diagram; names differing only in characters a
		// file name cannot hold get a numbered file.
		Assert.Equal(
			[Path.Combine(sid, "KXXX A-B (2).geojson"), Path.Combine(sid, "KXXX A-B.geojson")],
			result.GeojsonFilesWritten.Order(StringComparer.Ordinal));
		Assert.Equal(2, Features(Path.Combine(sid, "KXXX A-B.geojson"))[0].GetProperty("geometry").GetProperty("coordinates").GetArrayLength());
	}

	[Fact]
	public void a_line_across_the_antimeridian_is_split_in_two()
	{
		WriteSector("GUAM.sct", "[GEO]", "N013.30.00.000 E179.30.00.000 N013.30.00.000 W179.30.00.000");

		SctToGeojsonService.Run(Settings());
		JsonElement geo = Assert.Single(Features(Path.Combine(Converted("GUAM"), "GEO.geojson")));

		Assert.Equal(2, geo.GetProperty("geometry").GetProperty("coordinates").GetArrayLength());
	}

	[Fact]
	public void a_sector_file_with_nothing_to_convert_writes_nothing_and_says_so()
	{
		WriteSector("EMPTY.sct2",
			"[VOR]",
			"DJB 116.400 N041.21.28.000 W082.09.43.000",
			"[GEO]",
			$"{A} {A} ZERO_LENGTH",
			"not a record");

		SctToGeojsonServiceResult result = SctToGeojsonService.Run(Settings());

		SctFileConversion empty = Assert.Single(result.Files);
		Assert.Empty(empty.OutputPaths);
		Assert.Equal(1, empty.RecordsSkipped);
		Assert.Contains(result.Messages, m => m.IsAdvisory && m.Text.Contains("has nothing FE-Buddy converts"));
		Assert.Contains(result.Warnings, w => w.StartsWith("EMPTY.sct2: Line 5:"));
	}

	[Fact]
	public void both_extensions_convert_from_a_folder_and_other_files_are_left_alone()
	{
		WriteFullSector("ONE.sct2");
		WriteFullSector("TWO.SCT");
		File.WriteAllText(Path.Combine(Source, "notes.sct2.bak"), "[GEO]");

		SctToGeojsonServiceResult result = SctToGeojsonService.Run(Settings());

		Assert.Equal(["ONE.sct2", "TWO.SCT"], result.Files.Select(f => Path.GetFileName(f.SourcePath)));
	}

	[Fact]
	public void a_missing_file_fails_on_its_own_and_progress_follows_each_file()
	{
		string present = WriteFullSector();
		string missing = Path.Combine(Source, "GONE.sct2");
		List<ConversionProgress> reports = [];

		Dictionary<string, string> settings = Settings(("SourceFiles", $"{missing}|{present}"), ("AddFeBuddyOutputFolder", "N"));
		settings.Remove("SourceFolder");

		SctToGeojsonServiceResult result = SctToGeojsonService.Run(settings, new InlineProgress(reports.Add));

		Assert.Equal(1, result.FailedCount);
		Assert.Contains("GONE.sct2 could not be converted", result.Files[0].Error);
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Error);
		Assert.Equal(Path.Combine(Output, SctGeojsonWriter.RootFolder), result.OutputDirectory);

		Assert.Equal(4, reports.Count);
		Assert.Contains("could not be converted", reports[1].Message);
		Assert.Equal("10 GeoJSON file(s) written", reports[3].Message);
	}

	[Fact]
	public void progress_says_when_a_file_had_nothing_to_write()
	{
		WriteSector("EMPTY.sct2", "[INFO]", "nothing");
		List<ConversionProgress> reports = [];

		SctToGeojsonService.Run(Settings(), new InlineProgress(reports.Add));

		Assert.Equal("nothing to write", reports[^1].Message);
	}

	[Fact]
	public void bad_arguments_are_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => SctToGeojsonService.Run(null!));

		SctFile empty = new("x.sct2", new Dictionary<SctLineSection, IReadOnlyList<SctSegment>>(), [], [], [], [], []);
		SctToGeojsonSettings settings = new() { OutputDirectory = Output };
		GeojsonFileSet files = new(6);

		Assert.Throws<ArgumentNullException>(() => SctGeojsonWriter.Write(null!, settings, files));
		Assert.Throws<ArgumentNullException>(() => SctGeojsonWriter.Write(empty, null!, files));
		Assert.Throws<ArgumentNullException>(() => SctGeojsonWriter.Write(empty, settings, null!));
		Assert.Throws<ArgumentNullException>(() => SctGeojsonWriter.OutputDirectory(null!));
		Assert.Equal("Unnamed", SctGeojsonWriter.SafeFileName("  "));
	}

	/// <summary>The written files in the order the writer produces them: sections, then SID, STAR, labels, regions.</summary>
	private static int Order(string path) => Path.GetFileName(path) switch
	{
		"ARTCC.geojson" => 0,
		"ARTCC-HIGH.geojson" => 1,
		"ARTCC-LOW.geojson" => 2,
		"LOW-AIRWAY.geojson" => 3,
		"HIGH-AIRWAY.geojson" => 4,
		"GEO.geojson" => 5,
		"KXXX DEP.geojson" => 6,
		"KXXX ARR.geojson" => 7,
		"LABELS.geojson" => 8,
		_ => 9,
	};

	/// <summary>Reports straight away on the calling thread, so the order is exactly the order reported.</summary>
	private sealed class InlineProgress(Action<ConversionProgress> report) : IProgress<ConversionProgress>
	{
		public void Report(ConversionProgress value) => report(value);
	}
}
