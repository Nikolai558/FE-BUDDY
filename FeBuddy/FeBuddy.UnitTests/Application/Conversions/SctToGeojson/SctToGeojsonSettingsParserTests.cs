using FeBuddy.Core.Application.Conversions.SctToGeojson;
using FeBuddy.Core.Application.Conversions.SctToGeojson.Models;

namespace FeBuddy.UnitTests.Application.Conversions.SctToGeojson;

/// <summary>
/// Covers <see cref="SctToGeojsonSettingsParser"/>: the shared source and output keys, the CRC Line
/// and Text defaults, and unrecognized keys.
/// </summary>
public sealed class SctToGeojsonSettingsParserTests
{
	private static Dictionary<string, string> Settings(params (string Key, string Value)[] entries)
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = @"C:\Out",
			["SourceFolder"] = @"C:\Sectors",
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
		SctToGeojsonSettingsParseResult result = SctToGeojsonSettingsParser.Parse(Settings());
		SctToGeojsonSettings settings = result.Settings;

		Assert.Equal(@"C:\Out", settings.OutputDirectory);
		Assert.Equal(@"C:\Sectors", settings.SourceFolder);
		Assert.True(settings.AddFeBuddyOutputFolder);
		Assert.Equal(6, settings.CoordinatePrecision);
		Assert.False(settings.IncludeCrcLineDefaults);
		Assert.Null(settings.LineDefaults);
		Assert.False(settings.IncludeCrcTextDefaults);
		Assert.Null(settings.TextDefaults);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void included_line_and_text_defaults_are_read_and_required()
	{
		SctToGeojsonSettings settings = SctToGeojsonSettingsParser.Parse(Settings(
			("IncludeCrcLineDefaults", "Y"),
			("Crc.SectorFile.Line.bcg", "2"),
			("Crc.SectorFile.Line.filters", "1"),
			("Crc.SectorFile.Line.style", "solid"),
			("Crc.SectorFile.Line.thickness", "1"),
			("IncludeCrcTextDefaults", "Y"),
			("Crc.SectorFile.Text.bcg", "3"),
			("Crc.SectorFile.Text.filters", "2"),
			("Crc.SectorFile.Text.size", "1"),
			("Crc.SectorFile.Text.underline", "N"),
			("Crc.SectorFile.Text.opaque", "N"),
			("Crc.SectorFile.Text.xOffset", "0"),
			("Crc.SectorFile.Text.yOffset", "0"))).Settings;

		Assert.Equal(2, settings.LineDefaults!.Bcg);
		Assert.Equal(3, settings.TextDefaults!.Bcg);

		Assert.Throws<ArgumentException>(() => SctToGeojsonSettingsParser.Parse(Settings(("IncludeCrcLineDefaults", "Y"))));
		Assert.Throws<ArgumentException>(() => SctToGeojsonSettingsParser.Parse(Settings(("IncludeCrcTextDefaults", "Y"))));
	}

	[Fact]
	public void files_instead_of_a_folder_and_the_output_choices_are_read()
	{
		Dictionary<string, string> raw = Settings(
			("SourceFiles", @"C:\a.sct2|C:\b.sct"),
			("AddFeBuddyOutputFolder", "N"),
			("CoordinatePrecision", "5"));
		raw.Remove("SourceFolder");

		SctToGeojsonSettings settings = SctToGeojsonSettingsParser.Parse(raw).Settings;

		Assert.Null(settings.SourceFolder);
		Assert.Equal([@"C:\a.sct2", @"C:\b.sct"], settings.SourceFiles);
		Assert.False(settings.AddFeBuddyOutputFolder);
		Assert.Equal(5, settings.CoordinatePrecision);
	}

	[Fact]
	public void unknown_keys_and_symbol_defaults_are_warned_about_not_failed()
	{
		SctToGeojsonSettingsParseResult result = SctToGeojsonSettingsParser.Parse(Settings(
			("CroppingDistance", "40"),
			("Crc.SectorFile.Symbol.bcg", "1"),
			("Crc.SectorFile.Text.text", "X")));

		Assert.Equal(3, result.Messages.Count);
		Assert.Contains(result.Messages, m => m.Text.Contains("'CroppingDistance'"));
		Assert.Contains(result.Messages, m => m.Text.Contains("has no Symbol output"));
		Assert.Contains(result.Messages, m => m.Text.Contains("each label keeps its own text"));
	}

	[Fact]
	public void null_settings_are_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => SctToGeojsonSettingsParser.Parse(null!));
	}
}
