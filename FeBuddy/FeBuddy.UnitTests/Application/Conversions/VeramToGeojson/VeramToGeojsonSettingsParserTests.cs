using FeBuddy.Core.Application.Conversions.VeramToGeojson;
using FeBuddy.Core.Application.Conversions.VeramToGeojson.Models;

namespace FeBuddy.UnitTests.Application.Conversions.VeramToGeojson;

/// <summary>
/// Covers <see cref="VeramToGeojsonSettingsParser"/>: the layout and defaults-source choices, and
/// the tab's CRC defaults, read only when they can be used.
/// </summary>
public sealed class VeramToGeojsonSettingsParserTests
{
	private static Dictionary<string, string> Settings(params (string Key, string Value)[] entries)
	{
		Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = @"C:\Out",
			["SourceFolder"] = @"C:\GeoMaps",
		};

		foreach ((string key, string value) in entries)
		{
			settings[key] = value;
		}

		return settings;
	}

	/// <summary>Every CRC default the tab can send, all Include boxes ticked.</summary>
	private static (string, string)[] AllCard(params (string, string)[] extra) =>
	[
		("IncludeCrcLineDefaults", "Y"), ("IncludeCrcSymbolDefaults", "Y"), ("IncludeCrcTextDefaults", "Y"),
		("Crc.GeoMap.Line.bcg", "1"), ("Crc.GeoMap.Line.filters", "1"), ("Crc.GeoMap.Line.style", "solid"), ("Crc.GeoMap.Line.thickness", "1"),
		("Crc.GeoMap.Symbol.bcg", "2"), ("Crc.GeoMap.Symbol.filters", "2"), ("Crc.GeoMap.Symbol.style", "vor"), ("Crc.GeoMap.Symbol.size", "1"),
		("Crc.GeoMap.Text.bcg", "3"), ("Crc.GeoMap.Text.filters", "3"), ("Crc.GeoMap.Text.size", "1"), ("Crc.GeoMap.Text.underline", "N"),
		("Crc.GeoMap.Text.opaque", "N"), ("Crc.GeoMap.Text.xOffset", "0"), ("Crc.GeoMap.Text.yOffset", "0"),
		.. extra,
	];

	[Fact]
	public void defaults_to_a_file_per_object_with_defaults_from_the_xml()
	{
		VeramToGeojsonSettingsParseResult result = VeramToGeojsonSettingsParser.Parse(Settings());

		Assert.Equal(VeramOutputLayout.ByObject, result.Settings.OutputLayout);
		Assert.Equal(VeramDefaultsSource.Xml, result.Settings.DefaultsSource);
		Assert.Equal(@"C:\GeoMaps", result.Settings.SourceFolder);
		Assert.Null(result.Settings.LineDefaults);
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void the_tab_s_defaults_are_ignored_while_the_xml_is_the_source()
	{
		VeramToGeojsonSettings settings = VeramToGeojsonSettingsParser.Parse(Settings(AllCard(("OutputLayout", "byfilter")))).Settings;

		Assert.Equal(VeramOutputLayout.ByFilter, settings.OutputLayout);
		Assert.Null(settings.LineDefaults);
		Assert.Null(settings.SymbolDefaults);
		Assert.Null(settings.TextDefaults);
	}

	[Theory]
	[InlineData("XmlThenCard")]
	[InlineData("Card")]
	public void the_tab_s_defaults_are_read_when_they_are_a_source(string source)
	{
		VeramToGeojsonSettings settings = VeramToGeojsonSettingsParser.Parse(Settings(AllCard(("DefaultsSource", source)))).Settings;

		Assert.Equal(1, settings.LineDefaults!.Bcg);
		Assert.Equal("vor", settings.SymbolDefaults!.Style);
		Assert.Equal(3, settings.TextDefaults!.Bcg);
	}

	[Fact]
	public void only_included_kinds_of_the_tab_s_defaults_are_read()
	{
		VeramToGeojsonSettings settings = VeramToGeojsonSettingsParser.Parse(Settings(
			("DefaultsSource", "Card"),
			("IncludeCrcLineDefaults", "Y"),
			("Crc.GeoMap.Line.bcg", "1"), ("Crc.GeoMap.Line.filters", "1"),
			("Crc.GeoMap.Line.style", "solid"), ("Crc.GeoMap.Line.thickness", "1"))).Settings;

		Assert.NotNull(settings.LineDefaults);
		Assert.Null(settings.SymbolDefaults);
		Assert.Null(settings.TextDefaults);

		Assert.Throws<ArgumentException>(() => VeramToGeojsonSettingsParser.Parse(Settings(
			("DefaultsSource", "Card"), ("IncludeCrcTextDefaults", "Y"))));
	}

	[Theory]
	[InlineData("OutputLayout", "Sideways")]
	[InlineData("DefaultsSource", "Somewhere")]
	public void unknown_choices_are_rejected(string key, string value)
	{
		ArgumentException error = Assert.Throws<ArgumentException>(() => VeramToGeojsonSettingsParser.Parse(Settings((key, value))));

		Assert.Contains($"'{key}'", error.Message);
	}

	[Fact]
	public void unknown_keys_are_warned_about_not_failed()
	{
		VeramToGeojsonSettingsParseResult result = VeramToGeojsonSettingsParser.Parse(Settings(
			("CroppingDistance", "30"),
			("Crc.GeoMap.Text.text", "X")));

		Assert.Equal(2, result.Messages.Count);
		Assert.Contains(result.Messages, m => m.Text.Contains("each text keeps its own text"));
	}

	[Fact]
	public void null_settings_are_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => VeramToGeojsonSettingsParser.Parse(null!));
	}
}
