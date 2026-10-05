using System.Security.AccessControl;
using System.Security.Principal;

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

	/// <summary>An output folder that cannot be listed has no cycles, rather than failing.</summary>
	[Fact]
	public void an_output_folder_that_cannot_be_read_has_no_cycles()
	{
		Directory.CreateDirectory(Path.Combine(_output, "AIRAC_2610"));

		using (DenyListing(_output))
		{
			Assert.Empty(AiracOutputCatalog.FindCycleIds(_output, addFeBuddyOutputFolder: false));
		}

		Assert.Equal(["2610"], AiracOutputCatalog.FindCycleIds(_output, addFeBuddyOutputFolder: false));
	}

	/// <summary>A GeoJSON folder that cannot be listed gives nothing, rather than failing.</summary>
	[Fact]
	public void a_geojson_folder_that_cannot_be_read_has_no_files()
	{
		string cycle = Path.Combine(_output, "AIRAC_2610");
		Write(cycle, "Geojson", "Fixes_Symbols.geojson");

		using (DenyListing(Path.Combine(cycle, "Geojson")))
		{
			Assert.Empty(AiracOutputCatalog.FindGeojsonFiles(cycle));
		}

		Assert.Equal(["Fixes_Symbols"], AiracOutputCatalog.FindGeojsonFiles(cycle).Select(f => f.Name));
	}

	[Fact]
	public void a_missing_cycle_folder_has_no_files()
	{
		Assert.Empty(AiracOutputCatalog.FindGeojsonFiles(Path.Combine(_output, "AIRAC_2610")));
	}

	[Fact]
	public void geojson_files_are_listed_in_name_order()
	{
		string cycle = Path.Combine(_output, "AIRAC_2610");
		Write(cycle, "Geojson", "Fixes_Symbols.geojson");
		Write(cycle, "Geojson", "Airways_High_Lines.geojson");
		Write(cycle, "Geojson", "notes.txt");
		Write(cycle, string.Empty, "Airways.txt");

		IReadOnlyList<AiracOutputGeojsonFile> files = AiracOutputCatalog.FindGeojsonFiles(cycle);

		Assert.Equal(
			[
				Path.Combine("Geojson", "Airways_High_Lines.geojson"),
				Path.Combine("Geojson", "Fixes_Symbols.geojson"),
			],
			files.Select(f => f.RelativePath));
	}

	/// <summary>Only the cycle's <c>Geojson</c> folder is listed: a GeoJSON file anywhere else in the cycle folder is not a run's.</summary>
	[Fact]
	public void geojson_files_outside_the_geojson_folder_are_not_listed()
	{
		string cycle = Path.Combine(_output, "AIRAC_2610");
		Write(cycle, "Geojson", "Fixes_Symbols.geojson");
		Write(cycle, Path.Combine("Aliases", "Geojson"), "ARTCC_High_Lines.geojson");
		Write(cycle, string.Empty, "Stray.geojson");

		Assert.Equal(["Fixes_Symbols"], AiracOutputCatalog.FindGeojsonFiles(cycle).Select(f => f.Name));
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

	/// <summary>A sub-folder that cannot be read is skipped; every other file is still listed.</summary>
	[Fact]
	public void a_sub_folder_that_cannot_be_read_does_not_hide_the_rest()
	{
		string cycle = Path.Combine(_output, "AIRAC_2610");
		Write(cycle, "Geojson", "Fixes_Symbols.geojson");
		Write(cycle, Path.Combine("Geojson", "ZAB", "ABQ"), "ABQ_ADYOS_Lines.geojson");
		string locked = Path.GetDirectoryName(Write(cycle, Path.Combine("Geojson", "ZOB", "CLE"), "CLE_ALPHE_Lines.geojson"))!;

		using (DenyListing(locked))
		{
			IReadOnlyList<AiracOutputGeojsonFile> files = AiracOutputCatalog.FindGeojsonFiles(cycle);

			Assert.Equal(["Fixes_Symbols", "ABQ_ADYOS_Lines"], files.Select(f => f.Name));
		}
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

	/// <summary>Stops the current user listing <paramref name="folder"/> until disposed, as a folder another account owns would.</summary>
	private static IDisposable DenyListing(string folder) => new ListingDenied(new DirectoryInfo(folder));

	private sealed class ListingDenied : IDisposable
	{
		private readonly DirectoryInfo _folder;
		private readonly DirectorySecurity _security;
		private readonly FileSystemAccessRule _deny;

		public ListingDenied(DirectoryInfo folder)
		{
			_folder = folder;
			_security = folder.GetAccessControl();
			_deny = new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!, FileSystemRights.ListDirectory, AccessControlType.Deny);
			_security.AddAccessRule(_deny);
			folder.SetAccessControl(_security);
		}

		public void Dispose()
		{
			_security.RemoveAccessRule(_deny);
			_folder.SetAccessControl(_security);
		}
	}
}
