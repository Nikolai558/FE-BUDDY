using FeBuddy.Core.Application.Conversions.EramToGeojson;
using FeBuddy.Core.Application.Conversions.EramToGeojson.Models;

namespace FeBuddy.UnitTests.Application.Conversions.EramToGeojson;

/// <summary>
/// Covers <see cref="EramToGeojsonSettingsParser"/>: the layout and defaults-source choices, the
/// retired layouts read as their nearest, the <c>feb.*</c> properties, and the tab's CRC
/// defaults, read only when they can be used.
/// </summary>
public sealed class EramToGeojsonSettingsParserTests
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
	public void defaults_to_by_attributes_with_defaults_from_the_xml_and_no_feb_properties()
	{
		EramToGeojsonSettingsParseResult result = EramToGeojsonSettingsParser.Parse(Settings());

		Assert.Equal(EramOutputLayout.ByAttributes, result.Settings.OutputLayout);
		Assert.Equal(EramDefaultsSource.Xml, result.Settings.DefaultsSource);
		Assert.Equal(@"C:\GeoMaps", result.Settings.SourceFolder);
		Assert.Null(result.Settings.LineDefaults);
		Assert.False(result.Settings.IncludeFebProperties);
		Assert.Empty(result.Settings.FebProperties);
		Assert.Empty(result.Messages);
	}

	[Theory]
	[InlineData("ByFilters", EramOutputLayout.ByFilters)]
	[InlineData("byattributes", EramOutputLayout.ByAttributes)]
	[InlineData("Raw", EramOutputLayout.Raw)]
	[InlineData("RawPlus", EramOutputLayout.RawPlus)]
	public void each_layout_is_read(string value, EramOutputLayout expected)
	{
		EramToGeojsonSettingsParseResult result = EramToGeojsonSettingsParser.Parse(Settings(("OutputLayout", value)));

		Assert.Equal(expected, result.Settings.OutputLayout);
		Assert.Empty(result.Messages);
	}

	/// <summary>A layout saved before these three is read as its nearest, with a note, so an old setup still runs.</summary>
	[Theory]
	[InlineData("ByFilter", EramOutputLayout.ByFilters)]
	[InlineData(" byobject ", EramOutputLayout.ByAttributes)]
	public void a_retired_layout_is_read_as_its_nearest(string value, EramOutputLayout expected)
	{
		EramToGeojsonSettingsParseResult result = EramToGeojsonSettingsParser.Parse(Settings(("OutputLayout", value)));

		Assert.Equal(expected, result.Settings.OutputLayout);
		Assert.Contains("is no longer offered", Assert.Single(result.Messages).Text);
	}

	[Fact]
	public void the_chosen_feb_properties_are_read()
	{
		EramToGeojsonSettings settings = EramToGeojsonSettingsParser.Parse(Settings(
			("IncludeFebCustomProperties", "Y"), ("FebProperties", "lineObjectId,MapObjectType,saaId"))).Settings;

		Assert.True(settings.IncludeFebProperties);
		Assert.Equal([EramFebProperty.LineObjectId, EramFebProperty.MapObjectType, EramFebProperty.SaaId], settings.FebProperties);
		Assert.Throws<ArgumentException>(() => EramToGeojsonSettingsParser.Parse(Settings(("IncludeFebCustomProperties", "Y"))));
	}

	[Fact]
	public void the_tab_s_defaults_are_ignored_while_the_xml_is_the_source()
	{
		EramToGeojsonSettings settings = EramToGeojsonSettingsParser.Parse(Settings(AllCard(("OutputLayout", "Raw")))).Settings;

		Assert.Equal(EramOutputLayout.Raw, settings.OutputLayout);
		Assert.Null(settings.LineDefaults);
		Assert.Null(settings.SymbolDefaults);
		Assert.Null(settings.TextDefaults);
	}

	[Theory]
	[InlineData("XmlThenCard")]
	[InlineData("Card")]
	public void the_tab_s_defaults_are_read_when_they_are_a_source(string source)
	{
		EramToGeojsonSettings settings = EramToGeojsonSettingsParser.Parse(Settings(AllCard(("DefaultsSource", source)))).Settings;

		Assert.Equal(1, settings.LineDefaults!.Bcg);
		Assert.Equal("vor", settings.SymbolDefaults!.Style);
		Assert.Equal(3, settings.TextDefaults!.Bcg);
	}

	[Fact]
	public void only_included_kinds_of_the_tab_s_defaults_are_read()
	{
		EramToGeojsonSettings settings = EramToGeojsonSettingsParser.Parse(Settings(
			("DefaultsSource", "Card"),
			("IncludeCrcLineDefaults", "Y"),
			("Crc.GeoMap.Line.bcg", "1"), ("Crc.GeoMap.Line.filters", "1"),
			("Crc.GeoMap.Line.style", "solid"), ("Crc.GeoMap.Line.thickness", "1"))).Settings;

		Assert.NotNull(settings.LineDefaults);
		Assert.Null(settings.SymbolDefaults);
		Assert.Null(settings.TextDefaults);

		Assert.Throws<ArgumentException>(() => EramToGeojsonSettingsParser.Parse(Settings(
			("DefaultsSource", "Card"), ("IncludeCrcTextDefaults", "Y"))));
	}

	[Theory]
	[InlineData("OutputLayout", "Sideways")]
	[InlineData("DefaultsSource", "Somewhere")]
	public void unknown_choices_are_rejected(string key, string value)
	{
		ArgumentException error = Assert.Throws<ArgumentException>(() => EramToGeojsonSettingsParser.Parse(Settings((key, value))));

		Assert.Contains($"'{key}'", error.Message);
	}

	[Fact]
	public void unknown_keys_are_warned_about_not_failed()
	{
		EramToGeojsonSettingsParseResult result = EramToGeojsonSettingsParser.Parse(Settings(
			("CroppingDistance", "30"),
			("Crc.GeoMap.Text.text", "X")));

		Assert.Equal(2, result.Messages.Count);
		Assert.Contains(result.Messages, m => m.Text.Contains("each text keeps its own text"));
	}

	[Fact]
	public void null_settings_are_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => EramToGeojsonSettingsParser.Parse(null!));
	}
}
