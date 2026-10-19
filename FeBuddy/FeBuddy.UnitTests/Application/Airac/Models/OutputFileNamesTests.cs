using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.UnitTests.Application.Airac.Models;

/// <summary>
/// Covers <see cref="OutputFileNames"/>: the name each file is written under, its extension, and
/// which new names can't be used.
/// </summary>
public sealed class OutputFileNamesTests
{
	[Theory]
	[InlineData("Airways_High_Lines", "Airways_High_Lines.geojson")]
	[InlineData("NAVAIDs_VOR-DMEs_Symbols", "NAVAIDs_VOR-DMEs_Symbols.geojson")]
	[InlineData("Airways.txt", "Airways.txt")]
	[InlineData("Procedure_Changes.md", "Procedure_Changes.md")]
	[InlineData("Procedures.json", "Procedures.json")]
	public void a_file_that_is_not_renamed_keeps_fe_buddys_name(string key, string expected)
	{
		Assert.Equal(expected, OutputFileNames.None.FileName(key));
		Assert.Equal(expected, OutputFileNames.DefaultFileName(key));
	}

	[Theory]
	[InlineData("Airways_High_Lines", ".geojson")]
	[InlineData("Airways.txt", ".txt")]
	[InlineData("AIRWAYS.TXT", ".txt")]
	[InlineData("Procedure_Changes.md", ".md")]
	[InlineData("Procedures.json", ".json")]
	public void a_file_keeps_its_own_extension(string key, string expected)
	{
		Assert.Equal(expected, OutputFileNames.ExtensionOf(key));
	}

	/// <summary>The new name takes the file's own extension, and the key matches ignoring case.</summary>
	[Fact]
	public void a_renamed_file_is_written_under_its_new_name_and_its_own_extension()
	{
		OutputFileNames names = new(new Dictionary<string, string>
		{
			["Airways_High_Lines"] = "ZOB High",
			["Airways.txt"] = "  ZOB Airways  ",
		});

		Assert.Equal("ZOB High.geojson", names.FileName("airways_high_lines"));
		Assert.Equal("ZOB Airways.txt", names.FileName("Airways.txt"));
		Assert.Equal("Airways_Low_Lines.geojson", names.FileName("Airways_Low_Lines"));
		Assert.Equal("ZOB Airways", names.NewNames["Airways.txt"]);
	}

	[Theory]
	[InlineData("ZOB High")]
	[InlineData("ZOB.High")]
	[InlineData("  ZOB  ")]
	[InlineData("CONSOLE")]
	[InlineData("COM10")]
	public void a_usable_name_has_no_problem(string name)
	{
		Assert.Null(OutputFileNames.Problem(name));
	}

	[Theory]
	[InlineData("", "Type a new name.")]
	[InlineData("   ", "Type a new name.")]
	[InlineData("ZOB/High", "can't contain")]
	[InlineData(@"ZOB\High", "can't contain")]
	[InlineData("ZOB:High", "can't contain")]
	[InlineData("ZOB*", "can't contain")]
	[InlineData("ZOB?", "can't contain")]
	[InlineData("\"ZOB\"", "can't contain")]
	[InlineData("<ZOB>", "can't contain")]
	[InlineData("ZOB|High", "can't contain")]
	[InlineData("ZOB\tHigh", "can't contain")]
	[InlineData("ZOB.", "can't end with a dot")]
	[InlineData("ZOB High.geojson", "Leave off .geojson")]
	[InlineData("ZOB High.GEOJSON", "Leave off .geojson")]
	[InlineData("ZOB.txt", "Leave off .txt")]
	[InlineData("ZOB.md", "Leave off .md")]
	[InlineData("ZOB.json", "Leave off .json")]
	[InlineData("CON", "Windows keeps CON")]
	[InlineData("nul", "Windows keeps NUL")]
	[InlineData("com1", "Windows keeps COM1")]
	[InlineData("LPT9.backup", "Windows keeps LPT9")]
	public void an_unusable_name_says_why(string name, string expected)
	{
		Assert.Contains(expected, OutputFileNames.Problem(name), StringComparison.Ordinal);
	}

	[Fact]
	public void a_name_may_be_at_most_a_hundred_characters()
	{
		Assert.Null(OutputFileNames.Problem(new string('a', OutputFileNames.MaxNameLength)));
		Assert.Equal("Keep it to 100 characters or fewer.", OutputFileNames.Problem(new string('a', OutputFileNames.MaxNameLength + 1)));
	}

	[Fact]
	public void an_unusable_name_is_refused_naming_the_file()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => new OutputFileNames(new Dictionary<string, string>
		{
			["Airways.txt"] = "ZOB.txt",
		}));

		Assert.StartsWith("The new name for Airways.txt, 'ZOB.txt', can't be used. Leave off .txt", ex.Message, StringComparison.Ordinal);
	}

	/// <summary>Two files given one name would be written over each other - whatever the case.</summary>
	[Fact]
	public void two_files_may_not_share_a_new_name()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => new OutputFileNames(new Dictionary<string, string>
		{
			["Airways_High_Lines"] = "ZOB",
			["Airports_Symbols"] = "zob",
		}));

		Assert.StartsWith("The new name for ", ex.Message, StringComparison.Ordinal);
		Assert.Contains(" too. Give each file its own name.", ex.Message, StringComparison.Ordinal);
	}

	/// <summary>Each file's folder inside the cycle folder: two files clash only in one.</summary>
	[Theory]
	[InlineData("Airways_High_Lines", "Geojson")]
	[InlineData("Airways.txt", "Aliases")]
	[InlineData("Combined_Alias.txt", "Aliases")]
	[InlineData("Duplicate_Alias_Commands.txt", "")]
	[InlineData("Procedure_Changes.md", "Publication_Docs")]
	[InlineData("Procedures.json", "Publication_Docs")]
	public void each_file_has_its_folder(string key, string folder) => Assert.Equal(folder, OutputFileNames.FolderOf(key));

	/// <summary>
	/// A new name clashes with another renamed file's, or with one that keeps FE-Buddy's name, in the same folder;
	/// the same name in another folder, or with another extension, writes over nothing.
	/// </summary>
	[Fact]
	public void a_new_name_clashes_only_in_its_own_folder()
	{
		Dictionary<string, string> newNames = new()
		{
			["Airways.txt"] = "Airports",
			["Duplicate_Alias_Commands.txt"] = "Airways",
			["Airways_High_Lines"] = "Airports",
		};

		IReadOnlyDictionary<string, string> clashes = OutputFileNames.Clashes(newNames, ["Airports.txt", "Airways.txt", "Airports"]);

		Assert.Equal("Airports.txt is already the name of another file. Give this file a name of its own.", Assert.Single(clashes, c => c.Key == "Airways.txt").Value);
		Assert.Equal(2, clashes.Count);
		Assert.Contains("Airways_High_Lines", clashes.Keys);
		Assert.Empty(OutputFileNames.Clashes(new Dictionary<string, string> { ["Airways.txt"] = "Airports" }, []));
		Assert.Throws<ArgumentNullException>(() => OutputFileNames.Clashes(null!, []));
		Assert.Throws<ArgumentNullException>(() => OutputFileNames.Clashes(newNames, null!));
		Assert.Throws<ArgumentNullException>(() => OutputFileNames.FolderOf(null!));
	}

	/// <summary>A run refuses a new name that would write over a file keeping FE-Buddy's name.</summary>
	[Fact]
	public void a_run_refuses_a_name_another_file_keeps()
	{
		ArgumentException ex = Assert.Throws<ArgumentException>(() => OutputFileNamesParser.Parse(new Dictionary<string, string> { ["Airways.txt"] = "Airports" }));

		Assert.Equal("The new name for Airways.txt, 'Airports', can't be used. Airports.txt is already the name of another file. Give this file a name of its own. (Parameter 'fileNames')", ex.Message);
		Assert.Empty(OutputFileNamesParser.Parse(new Dictionary<string, string> { ["Duplicate_Alias_Commands.txt"] = "Airways" }).Messages);
	}

	/// <summary>The extension is part of the name, so an alias file and a GeoJSON file can share a new name.</summary>
	[Fact]
	public void files_with_different_extensions_may_share_a_new_name()
	{
		OutputFileNames names = new(new Dictionary<string, string>
		{
			["Airways_High_Lines"] = "ZOB",
			["Airways.txt"] = "ZOB",
		});

		Assert.Equal("ZOB.geojson", names.FileName("Airways_High_Lines"));
		Assert.Equal("ZOB.txt", names.FileName("Airways.txt"));
	}

	[Fact]
	public void nothing_may_be_null()
	{
		Assert.Throws<ArgumentNullException>(() => new OutputFileNames(null!));
		Assert.Throws<ArgumentNullException>(() => OutputFileNames.None.FileName(null!));
		Assert.Throws<ArgumentNullException>(() => OutputFileNames.ExtensionOf(null!));
		Assert.Throws<ArgumentNullException>(() => OutputFileNames.DefaultFileName(null!));
		Assert.Throws<ArgumentNullException>(() => OutputFileNames.Problem(null!));
	}
}
