using System.Text.Json;

using FeBuddy.Core.Application.Conversions.DatToGeojson;
using FeBuddy.Core.Application.Conversions.DatToGeojson.Models;
using FeBuddy.Core.Infrastructure.Geojson;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.UnitTests.Application.Conversions.DatToGeojson;

/// <summary>
/// Runs the whole DAT to GeoJSON conversion (<see cref="DatToGeojsonService.Run"/>) against small
/// <c>.dat</c> files written to a temp folder, and checks what lands on disk and what is reported.
/// </summary>
public sealed class DatToGeojsonServiceTests : IDisposable
{
	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Dat_" + Guid.NewGuid().ToString("N"));

	public DatToGeojsonServiceTests()
	{
		Directory.CreateDirectory(Source);
	}

	private string Source => Path.Combine(_root, "Maps");

	private string Output => Path.Combine(_root, "Out");

	private string ConvertedFolder => Path.Combine(Output, "FE-Buddy_Output", DatGeojsonWriter.RootFolder);

	public void Dispose()
	{
		if (Directory.Exists(_root))
		{
			Directory.Delete(_root, recursive: true);
		}
	}

	/// <summary>
	/// A map centred on 40 N 75 W with two lines: one short one at the centre, and one running
	/// 120 NM due north from it.
	/// </summary>
	private string WriteMap(string name, bool withPointOfTangency = true)
	{
		List<string> records = ["Filename: " + name];

		if (withPointOfTangency)
		{
			records.Add("9900  40 00 00.00 N 075 00 00.00 W");
		}

		records.AddRange(
		[
			"LINE 01",
			" 40 00 00.00 N 075 00 00.00 W",
			" 40 06 00.00 N 075 00 00.00 W",
			"LINE 02",
			" 40 00 00.00 N 075 00 00.00 W",
			" 42 00 00.00 N 075 00 00.00 W",
		]);

		string path = Path.Combine(Source, name);
		File.WriteAllLines(path, records);
		return path;
	}

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

	private static JsonElement Features(string path) =>
		JsonDocument.Parse(File.ReadAllText(path)).RootElement.GetProperty("features");

	[Fact]
	public void converts_every_dat_in_the_folder_to_its_own_geojson()
	{
		WriteMap("B.dat");
		WriteMap("A.dat");
		File.WriteAllText(Path.Combine(Source, "notes.txt"), "not a map");

		DatToGeojsonServiceResult result = DatToGeojsonService.Run(Settings());

		Assert.Equal([Path.Combine(Source, "A.dat"), Path.Combine(Source, "B.dat")], result.Files.Select(f => f.SourcePath));
		Assert.Equal(
			[Path.Combine(ConvertedFolder, "A.geojson"), Path.Combine(ConvertedFolder, "B.geojson")],
			result.GeojsonFilesWritten);
		Assert.Equal(ConvertedFolder, result.OutputDirectory);
		Assert.Equal(0, result.FailedCount);
		Assert.Empty(result.Warnings);

		DatFileConversion a = result.Files[0];
		Assert.Equal(2, a.LinesRead);
		Assert.Equal(2, a.LinesWritten);
		Assert.Equal(0, a.RecordsSkipped);
		Assert.Null(a.Error);

		JsonElement feature = Assert.Single(Features(a.OutputPath!).EnumerateArray());
		Assert.Equal("MultiLineString", feature.GetProperty("geometry").GetProperty("type").GetString());
		Assert.Equal(2, feature.GetProperty("geometry").GetProperty("coordinates").GetArrayLength());
	}

	[Fact]
	public void line_defaults_go_first_when_included()
	{
		WriteMap("A.dat");

		DatToGeojsonServiceResult result = DatToGeojsonService.Run(Settings(
			("IncludeCrcLineDefaults", "Y"),
			("Crc.VideoMap.Line.bcg", "4"),
			("Crc.VideoMap.Line.filters", "7"),
			("Crc.VideoMap.Line.style", "solid"),
			("Crc.VideoMap.Line.thickness", "2")));

		JsonElement[] features = [.. Features(result.GeojsonFilesWritten[0]).EnumerateArray()];

		Assert.Equal(2, features.Length);
		JsonElement defaults = features[0].GetProperty("properties");
		Assert.True(defaults.GetProperty("isLineDefaults").GetBoolean());
		Assert.Equal(4, defaults.GetProperty("bcg").GetInt32());
		Assert.Equal(2, defaults.GetProperty("thickness").GetInt32());
	}

	[Fact]
	public void cropping_cuts_lines_at_the_distance_from_the_point_of_tangency()
	{
		WriteMap("A.dat");

		DatToGeojsonServiceResult result = DatToGeojsonService.Run(Settings(("CroppingDistance", "60")));

		JsonElement lines = Assert.Single(Features(result.GeojsonFilesWritten[0]).EnumerateArray())
			.GetProperty("geometry").GetProperty("coordinates");

		// The 120 NM line now stops 60 NM north: about one degree of latitude.
		JsonElement longLine = lines[1];
		double endLatitude = longLine[longLine.GetArrayLength() - 1][1].GetDouble();
		Assert.Equal(41.0, endLatitude, 1);
		Assert.Equal(2, result.Files[0].LinesWritten);
	}

	[Fact]
	public void cropping_that_leaves_nothing_writes_nothing_and_says_so()
	{
		string path = Path.Combine(Source, "FAR.dat");
		File.WriteAllLines(path,
		[
			"9900  40 00 00.00 N 075 00 00.00 W",
			"LINE 01",
			" 45 00 00.00 N 075 00 00.00 W",
			" 45 30 00.00 N 075 00 00.00 W",
		]);

		DatToGeojsonServiceResult result = DatToGeojsonService.Run(Settings(("CroppingDistance", "10")));

		DatFileConversion far = Assert.Single(result.Files);
		Assert.Null(far.OutputPath);
		Assert.Null(far.Error);
		Assert.Equal(0, far.LinesWritten);
		Assert.Contains(result.Messages, m => m.IsAdvisory && m.Text.Contains("within 10 NM"));
		Assert.False(Directory.Exists(ConvertedFolder));
	}

	[Fact]
	public void cropping_a_map_without_a_point_of_tangency_fails_that_map_only()
	{
		WriteMap("NOPOT.dat", withPointOfTangency: false);
		WriteMap("OK.dat");

		DatToGeojsonServiceResult result = DatToGeojsonService.Run(Settings(("CroppingDistance", "60")));

		Assert.Equal(1, result.FailedCount);
		Assert.Contains("no point of tangency", result.Files[0].Error);
		Assert.Single(result.GeojsonFilesWritten);
		Assert.Contains(result.Messages, m => m.Level == LogLevel.Error && m.Text.Contains("NOPOT.dat"));
	}

	[Fact]
	public void a_missing_file_fails_on_its_own_and_the_rest_convert()
	{
		string present = WriteMap("A.dat");
		string missing = Path.Combine(Source, "GONE.dat");

		Dictionary<string, string> settings = Settings(("SourceFiles", $"{missing}|{present}"));
		settings.Remove("SourceFolder");

		DatToGeojsonServiceResult result = DatToGeojsonService.Run(settings);

		Assert.Equal(2, result.Files.Count);
		Assert.Contains("GONE.dat could not be converted", result.Files[0].Error);
		Assert.Equal(Path.Combine(ConvertedFolder, "A.geojson"), result.Files[1].OutputPath);
	}

	[Fact]
	public void a_map_with_no_lines_writes_nothing_and_its_skipped_records_are_warned_about()
	{
		File.WriteAllLines(Path.Combine(Source, "EMPTY.dat"), ["LINE 01", " not a point"]);

		DatToGeojsonServiceResult result = DatToGeojsonService.Run(Settings());

		DatFileConversion empty = Assert.Single(result.Files);
		Assert.Null(empty.OutputPath);
		Assert.Equal(2, empty.RecordsSkipped);
		Assert.Contains(result.Messages, m => m.IsAdvisory && m.Text.Contains("has no lines"));
		Assert.Contains(result.Warnings, w => w.StartsWith("EMPTY.dat: Line 2:"));
	}

	[Fact]
	public void an_empty_folder_converts_nothing_and_says_so()
	{
		DatToGeojsonServiceResult result = DatToGeojsonService.Run(Settings());

		Assert.Empty(result.Files);
		Assert.Contains(result.Messages, m => m.IsAdvisory && m.Text.Contains("no .dat files"));
	}

	[Fact]
	public void a_missing_folder_is_a_bad_setting()
	{
		Assert.Throws<ArgumentException>(() => DatToGeojsonService.Run(Settings(("SourceFolder", Path.Combine(_root, "Nope")))));
	}

	[Fact]
	public void output_can_skip_the_fe_buddy_output_folder()
	{
		WriteMap("A.dat");

		DatToGeojsonServiceResult result = DatToGeojsonService.Run(Settings(("AddFeBuddyOutputFolder", "N")));

		Assert.Equal(Path.Combine(Output, DatGeojsonWriter.RootFolder, "A.geojson"), result.GeojsonFilesWritten[0]);
	}

	[Fact]
	public void a_line_across_the_antimeridian_is_split_in_two()
	{
		File.WriteAllLines(Path.Combine(Source, "GUAM.dat"),
		[
			"LINE 01",
			" 13 30 00.00 N 179 30 00.00 E",
			" 13 30 00.00 N 179 30 00.00 W",
		]);

		DatToGeojsonServiceResult result = DatToGeojsonService.Run(Settings());

		Assert.Equal(2, result.Files[0].LinesWritten);
		JsonElement lines = Assert.Single(Features(result.GeojsonFilesWritten[0]).EnumerateArray())
			.GetProperty("geometry").GetProperty("coordinates");
		Assert.Equal(180, lines[0][1][0].GetDouble());
		Assert.Equal(-180, lines[1][0][0].GetDouble());
	}

	[Fact]
	public void progress_is_reported_as_each_file_starts_and_finishes()
	{
		WriteMap("A.dat");
		WriteMap("NOPOT.dat", withPointOfTangency: false);
		List<DatToGeojsonProgress> reports = [];

		DatToGeojsonService.Run(Settings(("CroppingDistance", "60")), new InlineProgress(reports.Add));

		Assert.Equal(4, reports.Count);
		Assert.Equal(("A.dat", false), (reports[0].FileName, reports[0].IsComplete));
		Assert.Equal(("A.dat", "2 line(s) written", true), (reports[1].FileName, reports[1].Message, reports[1].IsComplete));
		Assert.True(reports[3].IsComplete);
		Assert.Contains("no point of tangency", reports[3].Message);
	}

	[Fact]
	public void progress_says_when_a_file_had_nothing_to_write()
	{
		File.WriteAllLines(Path.Combine(Source, "EMPTY.dat"), ["LINE 01"]);
		List<DatToGeojsonProgress> reports = [];

		DatToGeojsonService.Run(Settings(), new InlineProgress(reports.Add));

		Assert.Equal("nothing to write", reports[^1].Message);
	}

	[Fact]
	public void null_settings_are_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => DatToGeojsonService.Run(null!));
	}

	[Fact]
	public void the_writer_writes_nothing_for_no_lines_and_rejects_bad_arguments()
	{
		DatToGeojsonSettings settings = new() { OutputDirectory = Output };
		GeojsonFileSet files = new(6);

		Assert.Null(DatGeojsonWriter.Write([], "A.dat", settings, files));
		Assert.Empty(files.FilesWritten);

		Assert.Throws<ArgumentNullException>(() => DatGeojsonWriter.Write(null!, "A.dat", settings, files));
		Assert.Throws<ArgumentException>(() => DatGeojsonWriter.Write([], " ", settings, files));
		Assert.Throws<ArgumentNullException>(() => DatGeojsonWriter.Write([], "A.dat", null!, files));
		Assert.Throws<ArgumentNullException>(() => DatGeojsonWriter.Write([], "A.dat", settings, null!));
		Assert.Throws<ArgumentNullException>(() => DatGeojsonWriter.OutputDirectory(null!));
	}

	/// <summary>Reports straight away on the calling thread, so the order is exactly the order reported.</summary>
	private sealed class InlineProgress(Action<DatToGeojsonProgress> report) : IProgress<DatToGeojsonProgress>
	{
		public void Report(DatToGeojsonProgress value) => report(value);
	}
}
