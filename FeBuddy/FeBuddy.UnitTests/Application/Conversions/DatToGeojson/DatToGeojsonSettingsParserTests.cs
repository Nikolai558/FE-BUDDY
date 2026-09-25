using FeBuddy.Core.Application.Conversions.DatToGeojson;
using FeBuddy.Core.Application.Conversions.DatToGeojson.Models;

namespace FeBuddy.UnitTests.Application.Conversions.DatToGeojson;

/// <summary>
/// Covers <see cref="DatToGeojsonSettingsParser"/>: the source (a folder or files, never both),
/// the cropping distance, the CRC Line defaults, and unrecognized keys.
/// </summary>
public sealed class DatToGeojsonSettingsParserTests
{
	private static Dictionary<string, string> Settings(params (string Key, string Value)[] entries)
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = @"C:\Out",
			["SourceFolder"] = @"C:\Maps",
		};

		foreach ((string key, string value) in entries)
		{
			settings[key] = value;
		}

		return settings;
	}

	[Fact]
	public void a_folder_source_with_nothing_else_uses_the_defaults()
	{
		DatToGeojsonSettingsParseResult result = DatToGeojsonSettingsParser.Parse(Settings());
		DatToGeojsonSettings settings = result.Settings;

		Assert.Equal(@"C:\Out", settings.OutputDirectory);
		Assert.Equal(@"C:\Maps", settings.SourceFolder);
		Assert.Empty(settings.SourceFiles);
		Assert.True(settings.AddFeBuddyOutputFolder);
		Assert.Equal(6, settings.CoordinatePrecision);
		Assert.Null(settings.CroppingDistanceNm);
		Assert.False(settings.IncludeCrcLineDefaults);
		Assert.Null(settings.LineDefaults);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void source_files_are_separated_by_a_bar_so_commas_in_paths_survive()
	{
		Dictionary<string, string> raw = Settings(("SourceFiles", @"C:\A, B\one.dat | C:\two.dat |"));
		raw.Remove("SourceFolder");

		DatToGeojsonSettings settings = DatToGeojsonSettingsParser.Parse(raw).Settings;

		Assert.Null(settings.SourceFolder);
		Assert.Equal([@"C:\A, B\one.dat", @"C:\two.dat"], settings.SourceFiles);
	}

	[Fact]
	public void a_folder_and_files_together_are_rejected()
	{
		ArgumentException error = Assert.Throws<ArgumentException>(() =>
			DatToGeojsonSettingsParser.Parse(Settings(("SourceFiles", @"C:\one.dat"))));

		Assert.Contains("not both", error.Message);
	}

	[Fact]
	public void no_source_at_all_is_rejected()
	{
		Dictionary<string, string> raw = Settings();
		raw.Remove("SourceFolder");

		Assert.Throws<ArgumentException>(() => DatToGeojsonSettingsParser.Parse(raw));
	}

	[Fact]
	public void the_output_directory_is_required()
	{
		Dictionary<string, string> raw = Settings();
		raw.Remove("OutputDirectory");

		Assert.Throws<ArgumentException>(() => DatToGeojsonSettingsParser.Parse(raw));
	}

	[Fact]
	public void reads_the_cropping_distance_precision_and_output_folder_choice()
	{
		DatToGeojsonSettings settings = DatToGeojsonSettingsParser.Parse(Settings(
			("CroppingDistance", "106.5"),
			("CoordinatePrecision", "7"),
			("AddFeBuddyOutputFolder", "N"))).Settings;

		Assert.Equal(106.5, settings.CroppingDistanceNm);
		Assert.Equal(7, settings.CoordinatePrecision);
		Assert.False(settings.AddFeBuddyOutputFolder);
	}

	[Theory]
	[InlineData("0")]
	[InlineData("1001")]
	[InlineData("far")]
	public void an_invalid_cropping_distance_is_rejected(string distance)
	{
		Assert.Throws<ArgumentException>(() => DatToGeojsonSettingsParser.Parse(Settings(("CroppingDistance", distance))));
	}

	[Fact]
	public void included_line_defaults_are_read_and_required()
	{
		DatToGeojsonSettings settings = DatToGeojsonSettingsParser.Parse(Settings(
			("IncludeCrcLineDefaults", "Y"),
			("Crc.VideoMap.Line.bcg", "3"),
			("Crc.VideoMap.Line.filters", "1,2"),
			("Crc.VideoMap.Line.style", "SOLID"),
			("Crc.VideoMap.Line.thickness", "1"))).Settings;

		Assert.True(settings.IncludeCrcLineDefaults);
		Assert.NotNull(settings.LineDefaults);
		Assert.Equal(3, settings.LineDefaults.Bcg);
		Assert.Equal([1, 2], settings.LineDefaults.Filters);
		Assert.Equal("solid", settings.LineDefaults.Style);
		Assert.Equal(1, settings.LineDefaults.Thickness);

		Assert.Throws<ArgumentException>(() => DatToGeojsonSettingsParser.Parse(Settings(("IncludeCrcLineDefaults", "Y"))));
	}

	[Fact]
	public void unknown_keys_and_other_crc_kinds_are_warned_about_not_failed()
	{
		DatToGeojsonSettingsParseResult result = DatToGeojsonSettingsParser.Parse(Settings(
			("SourceFolderr", "typo"),
			("Crc.VideoMap.Symbol.bcg", "1")));

		Assert.Equal(2, result.Messages.Count);
		Assert.Contains(result.Messages, m => m.Text.Contains("'SourceFolderr'"));
		Assert.Contains(result.Messages, m => m.Text.Contains("has no Symbol output"));
	}

	[Fact]
	public void null_settings_are_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => DatToGeojsonSettingsParser.Parse(null!));
	}
}
