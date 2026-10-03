using FeBuddy.Core.Application.Conversions.EramToGeojson;
using FeBuddy.Core.Application.Conversions.EramToGeojson.Models;
using FeBuddy.Core.Domain.Crc;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Infrastructure.Eram.Models;

using NetTopologySuite.Features;

namespace FeBuddy.UnitTests.Application.Conversions.EramToGeojson;

/// <summary>
/// Covers <see cref="EramCrcProperties"/>: complete CRC defaults from ERAM's, what keeps defaults
/// from being complete, style names, the overrides an element sets, and taking out what CRC can't
/// draw.
/// </summary>
public sealed class EramCrcPropertiesTests
{
	private static readonly EramProperties Line = new() { Bcg = 1, Filters = [1], Style = "ShortDashed", Thickness = 2 };
	private static readonly EramProperties Symbol = new() { Bcg = 2, Filters = [2], Style = "RNAVOnlyWaypoint", Size = 3 };
	private static readonly EramProperties Text = new()
	{
		Bcg = 3,
		Filters = [3],
		Size = 1,
		Underline = true,
		XOffset = 4,
		YOffset = -4,
	};

	[Fact]
	public void complete_defaults_become_crc_defaults_in_crc_spelling()
	{
		CrcLineDefaults line = EramCrcProperties.LineDefaults(Line)!;
		CrcSymbolDefaults symbol = EramCrcProperties.SymbolDefaults(Symbol)!;
		CrcTextDefaults text = EramCrcProperties.TextDefaults(Text)!;

		Assert.Equal(("shortDashed", 2), (line.Style, line.Thickness));
		Assert.Equal(("rnavOnlyWaypoint", 3), (symbol.Style, symbol.Size));

		// ERAM text has no opaque background, so CRC's opaque is always off.
		Assert.Equal((true, false, 4, -4), (text.Underline, text.Opaque, text.XOffset, text.YOffset));
		Assert.Empty(EramCrcProperties.Gaps(EramElementKind.Line, Line));
		Assert.Empty(EramCrcProperties.Gaps(EramElementKind.Symbol, Symbol));
		Assert.Empty(EramCrcProperties.Gaps(EramElementKind.Text, Text));
	}

	[Fact]
	public void missing_or_incomplete_defaults_are_not_used_and_their_gaps_are_named()
	{
		Assert.Null(EramCrcProperties.LineDefaults(null));
		Assert.Equal(
			[new EramDefaultsGap("bcg", null), new EramDefaultsGap("filters", null), new EramDefaultsGap("style", null), new EramDefaultsGap("thickness", null)],
			EramCrcProperties.Gaps(EramElementKind.Line, null));

		Assert.Null(EramCrcProperties.SymbolDefaults(Symbol with { Style = null, Size = null }));
		Assert.Equal(
			[new EramDefaultsGap("style", null), new EramDefaultsGap("size", null)],
			EramCrcProperties.Gaps(EramElementKind.Symbol, Symbol with { Style = null, Size = null }));

		Assert.Null(EramCrcProperties.TextDefaults(Text with { YOffset = null }));
		Assert.Equal([new EramDefaultsGap("yOffset", null)], EramCrcProperties.Gaps(EramElementKind.Text, Text with { YOffset = null }));
	}

	[Fact]
	public void defaults_crc_cannot_draw_are_not_used_and_each_bad_value_is_named()
	{
		Assert.Null(EramCrcProperties.LineDefaults(Line with { Style = "Dotted" }));
		Assert.Equal([new EramDefaultsGap("style", "Dotted")], EramCrcProperties.Gaps(EramElementKind.Line, Line with { Style = "Dotted" }));

		Assert.Null(EramCrcProperties.SymbolDefaults(Symbol with { Style = "DME", Size = 9 }));
		Assert.Equal(
			[new EramDefaultsGap("style", "DME"), new EramDefaultsGap("size", "9")],
			EramCrcProperties.Gaps(EramElementKind.Symbol, Symbol with { Style = "DME", Size = 9 }));

		Assert.Null(EramCrcProperties.TextDefaults(Text with { Bcg = 99, Filters = [3, 41] }));
		Assert.Equal(
			[new EramDefaultsGap("bcg", "99"), new EramDefaultsGap("filters", "3,41")],
			EramCrcProperties.Gaps(EramElementKind.Text, Text with { Bcg = 99, Filters = [3, 41] }));
	}

	/// <summary>
	/// The checks value by value must agree with CRC's validator, which checks defaults whole:
	/// defaults with no gaps are exactly the ones it accepts.
	/// </summary>
	[Theory]
	[InlineData(1, 0, "solid", 1)]
	[InlineData(40, 40, "longDashShortDash", 3)]
	[InlineData(0, 1, "solid", 1)]
	[InlineData(41, 1, "solid", 1)]
	[InlineData(1, -1, "solid", 1)]
	[InlineData(1, 41, "solid", 1)]
	[InlineData(1, 1, "dotted", 1)]
	[InlineData(1, 1, "solid", 0)]
	[InlineData(1, 1, "solid", 4)]
	public void line_gaps_agree_with_the_crc_validator(int bcg, int filter, string style, int thickness)
	{
		bool valid = CrcPropertyValidator.ValidateLineDefaults(new CrcLineDefaults { Bcg = bcg, Filters = [filter], Style = style, Thickness = thickness }).IsValid;

		Assert.Equal(valid, EramCrcProperties.Gaps(EramElementKind.Line, new EramProperties { Bcg = bcg, Filters = [filter], Style = style, Thickness = thickness }).Count == 0);
	}

	[Theory]
	[InlineData("vor", 1)]
	[InlineData("tacan", 4)]
	[InlineData("dme", 1)]
	[InlineData("vor", 0)]
	[InlineData("vor", 5)]
	public void symbol_gaps_agree_with_the_crc_validator(string style, int size)
	{
		bool valid = CrcPropertyValidator.ValidateSymbolDefaults(new CrcSymbolDefaults { Bcg = 1, Filters = [1], Style = style, Size = size }).IsValid;

		Assert.Equal(valid, EramCrcProperties.Gaps(EramElementKind.Symbol, new EramProperties { Bcg = 1, Filters = [1], Style = style, Size = size }).Count == 0);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(5)]
	[InlineData(-1)]
	[InlineData(6)]
	public void text_gaps_agree_with_the_crc_validator(int size)
	{
		bool valid = CrcPropertyValidator.ValidateTextDefaults(new CrcTextDefaults
		{
			Bcg = 1,
			Filters = [1],
			Size = size,
			Underline = false,
			Opaque = false,
			XOffset = -9,
			YOffset = 9,
		}).IsValid;

		Assert.Equal(valid, EramCrcProperties.Gaps(EramElementKind.Text, Text with { Size = size, XOffset = -9, YOffset = 9 }).Count == 0);
	}

	[Fact]
	public void usable_takes_out_only_what_crc_cannot_draw()
	{
		EramProperties mixed = new() { Bcg = 0, Filters = [41], Style = "DME", Size = 2, Thickness = 9, XOffset = 3, Display = true };

		// Only what applies to the kind is checked: a symbol's thickness, or a line's size, stays.
		Assert.Equal(new EramProperties { Size = 2, Thickness = 9, XOffset = 3, Display = true }, EramCrcProperties.Usable(EramElementKind.Symbol, mixed));
		Assert.Equal(
			new EramProperties { Style = "Solid", Size = 2, XOffset = 3, Display = true },
			EramCrcProperties.Usable(EramElementKind.Line, mixed with { Style = "Solid" }));
		Assert.Same(Line, EramCrcProperties.Usable(EramElementKind.Line, Line));
	}

	[Theory]
	[InlineData(EramElementKind.Line, "style", "solid")]
	[InlineData(EramElementKind.Line, "thickness", "1")]
	[InlineData(EramElementKind.Symbol, "style", "vor")]
	[InlineData(EramElementKind.Symbol, "bcg", "1")]
	[InlineData(EramElementKind.Text, "underline", "false")]
	[InlineData(EramElementKind.Text, "yOffset", "0")]
	[InlineData(EramElementKind.Line, "filters", null)]
	[InlineData(EramElementKind.Text, "filters", null)]
	public void crc_assigns_its_own_value_to_everything_but_filters(EramElementKind kind, string property, string? expected) =>
		Assert.Equal(expected, EramCrcProperties.AutoAssigned(kind, property));

	[Fact]
	public void crc_defaults_turn_back_into_eram_properties()
	{
		CrcLineDefaults line = EramCrcProperties.LineDefaults(Line)!;
		CrcSymbolDefaults symbol = EramCrcProperties.SymbolDefaults(Symbol)!;
		CrcTextDefaults text = EramCrcProperties.TextDefaults(Text)!;

		Assert.Equal(Line with { Style = "shortDashed" }, EramCrcProperties.AsEram(line));
		Assert.Equal(Symbol with { Style = "rnavOnlyWaypoint" }, EramCrcProperties.AsEram(symbol));
		Assert.Equal(Text, EramCrcProperties.AsEram(text));
		Assert.Same(EramProperties.None, EramCrcProperties.AsEram(null));
	}

	[Fact]
	public void an_element_s_own_values_win_over_its_defaults()
	{
		EramProperties drawn = EramCrcProperties.Over(Line, new EramProperties { Thickness = 3, Filters = [9] });

		Assert.Equal(Line with { Thickness = 3, Filters = [9] }, drawn);
		Assert.Equal(Text, EramCrcProperties.Over(Text, EramProperties.None));
		Assert.Equal(Text, EramCrcProperties.Over(EramProperties.None, Text));
	}

	[Fact]
	public void overrides_carry_only_what_the_element_sets_for_its_kind()
	{
		List<(string, string)> dropped = [];

		AttributesTable line = EramCrcProperties.Overrides(EramElementKind.Line, Line with { Size = 2 }, (property, value) => dropped.Add((property, value)));
		AttributesTable symbol = EramCrcProperties.Overrides(EramElementKind.Symbol, Symbol, (property, value) => dropped.Add((property, value)));
		AttributesTable text = EramCrcProperties.Overrides(EramElementKind.Text, Text, (property, value) => dropped.Add((property, value)));

		Assert.Equal(["bcg", "filters", "style", "thickness"], line.GetNames());
		Assert.Equal("shortDashed", line["style"]);
		Assert.Equal(["bcg", "filters", "style", "size"], symbol.GetNames());
		Assert.Equal(["bcg", "filters", "size", "underline", "xOffset", "yOffset"], text.GetNames());
		Assert.Empty(EramCrcProperties.Overrides(EramElementKind.Line, EramProperties.None, (property, value) => dropped.Add((property, value))).GetNames());
		Assert.Empty(dropped);
	}

	[Fact]
	public void override_values_crc_cannot_draw_are_left_out_and_reported()
	{
		List<(string, string)> dropped = [];
		void Drop(string property, string value) => dropped.Add((property, value));

		AttributesTable line = EramCrcProperties.Overrides(EramElementKind.Line,
			new EramProperties { Bcg = 0, Filters = [1, 99], Style = "Dotted", Thickness = 5 }, Drop);
		AttributesTable symbol = EramCrcProperties.Overrides(EramElementKind.Symbol,
			new EramProperties { Style = "DME", Size = 0 }, Drop);
		AttributesTable text = EramCrcProperties.Overrides(EramElementKind.Text,
			new EramProperties { Size = 6, XOffset = 1 }, Drop);
		AttributesTable below = EramCrcProperties.Overrides(EramElementKind.Line,
			new EramProperties { Filters = [-1], Thickness = 0 }, Drop);
		AttributesTable belowText = EramCrcProperties.Overrides(EramElementKind.Text,
			new EramProperties { Size = -1 }, Drop);

		Assert.Empty(line.GetNames());
		Assert.Empty(symbol.GetNames());
		Assert.Equal(["xOffset"], text.GetNames());
		Assert.Empty(below.GetNames());
		Assert.Empty(belowText.GetNames());
		Assert.Equal(
			[
				("bcg", "0"), ("filters", "1,99"), ("style", "Dotted"), ("thickness", "5"), ("style", "DME"), ("size", "0"), ("size", "6"),
				("filters", "-1"), ("thickness", "0"), ("size", "-1"),
			],
			dropped);
	}
}
