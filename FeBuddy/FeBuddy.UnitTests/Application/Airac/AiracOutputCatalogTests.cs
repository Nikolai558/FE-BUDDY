using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.UnitTests.Application.Airac;

/// <summary>
/// Covers <see cref="AiracOutputCatalog"/>: which cycle folders count, and which GeoJSON files it
/// lists from a cycle folder and in what order.
/// </summary>
public sealed class AiracOutputCatalogTests : IDisposable
{
	private readonly string _output = Path.Combine(Path.GetTempPath(), "FeBuddyTests_OutputCatalog_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_output))
		{
			Directory.Delete(_output, recursive: true);
		}
	}

	[Fact]
	public void a_missing_output_folder_has_no_cycles()
	{
		Assert.Empty(AiracOutputCatalog.FindCycleIds(_output, addFeBuddyOutputFolder: true));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void cycles_are_listed_newest_first_and_only_real_cycle_folders_count(bool addFeBuddyOutputFolder)
	{
		string root = addFeBuddyOutputFolder ? Path.Combine(_output, "FE-Buddy_Output") : _output;
		foreach (string name in new[] { "AIRAC_2609", "AIRAC_2611", "AIRAC_2610", "AIRAC_26", "AIRAC_abcd", "Other" })
		{
			Directory.CreateDirectory(Path.Combine(root, name));
		}

		Assert.Equal(["2611", "2610", "2609"], AiracOutputCatalog.FindCycleIds(_output, addFeBuddyOutputFolder));
	}

	[Fact]
	public void a_missing_cycle_folder_has_no_files()
	{
		Assert.Empty(AiracOutputCatalog.FindGeojsonFiles(Path.Combine(_output, "AIRAC_2610")));
	}

	[Fact]
	public void geojson_files_are_listed_ordinary_folder_first_each_in_name_order()
	{
		string cycle = Path.Combine(_output, "AIRAC_2610");
		Write(cycle, "Geojson", "Fixes_Symbols.geojson");
		Write(cycle, "Geojson", "Airways_High_Lines.geojson");
		Write(cycle, "Geojson", "notes.txt");
		Write(cycle, Path.Combine("Upload_to_vNAS", "Geojson"), "ARTCC_High_Lines.geojson");
		Write(cycle, string.Empty, "Airways.txt");

		IReadOnlyList<AiracOutputGeojsonFile> files = AiracOutputCatalog.FindGeojsonFiles(cycle);

		Assert.Equal(
			[
				Path.Combine("Geojson", "Airways_High_Lines.geojson"),
				Path.Combine("Geojson", "Fixes_Symbols.geojson"),
				Path.Combine("Upload_to_vNAS", "Geojson", "ARTCC_High_Lines.geojson"),
			],
			files.Select(f => f.RelativePath));
		Assert.Equal([false, false, true], files.Select(f => f.UploadToVnas));
	}

	[Fact]
	public void sub_folder_files_follow_the_top_level_ones_and_carry_their_folder()
	{
		string cycle = Path.Combine(_output, "AIRAC_2610");
		Write(cycle, Path.Combine("Geojson", "ZOB", "CLE"), "CLE_ALPHE_Lines.geojson");
		Write(cycle, Path.Combine("Geojson", "ZAB", "ABQ"), "ABQ_ADYOS_Lines.geojson");
		Write(cycle, "Geojson", "Fixes_Symbols.geojson");

		IReadOnlyList<AiracOutputGeojsonFile> files = AiracOutputCatalog.FindGeojsonFiles(cycle);

		Assert.Equal(["Fixes_Symbols", "ABQ_ADYOS_Lines", "CLE_ALPHE_Lines"], files.Select(f => f.Name));
		Assert.Equal([string.Empty, Path.Combine("ZAB", "ABQ"), Path.Combine("ZOB", "CLE")], files.Select(f => f.SubFolder));
		Assert.Equal(Path.Combine("Geojson", "ZAB", "ABQ", "ABQ_ADYOS_Lines.geojson"), files[1].RelativePath);
	}

	[Fact]
	public void a_file_carries_its_name_size_and_write_time()
	{
		string cycle = Path.Combine(_output, "AIRAC_2610");
		string path = Write(cycle, "Geojson", "Fixes_Text.geojson");

		AiracOutputGeojsonFile file = Assert.Single(AiracOutputCatalog.FindGeojsonFiles(cycle));

		Assert.Equal(path, file.FullPath);
		Assert.Equal("Fixes_Text", file.Name);
		Assert.Equal(new FileInfo(path).Length, file.SizeBytes);
		Assert.Equal(File.GetLastWriteTimeUtc(path), file.LastWriteUtc);
	}

	private static string Write(string cycle, string folder, string name)
	{
		string directory = Path.Combine(cycle, folder);
		Directory.CreateDirectory(directory);
		string path = Path.Combine(directory, name);
		File.WriteAllText(path, "{\"type\":\"FeatureCollection\",\"features\":[]}");
		return path;
	}
}
