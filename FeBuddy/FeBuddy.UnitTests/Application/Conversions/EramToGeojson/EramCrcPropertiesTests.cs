using FeBuddy.Core.Application.Conversions.EramToGeojson;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Infrastructure.Eram.Models;

using NetTopologySuite.Features;

namespace FeBuddy.UnitTests.Application.Conversions.EramToGeojson;

/// <summary>
/// Covers <see cref="EramCrcProperties"/>: complete CRC defaults from ERAM's, style names, and
/// the overrides an element sets.
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
		CrcLineDefaults line = EramCrcProperties.LineDefaults(Line, out string? lineProblem)!;
		CrcSymbolDefaults symbol = EramCrcProperties.SymbolDefaults(Symbol, out string? symbolProblem)!;
		CrcTextDefaults text = EramCrcProperties.TextDefaults(Text, out string? textProblem)!;

		Assert.Equal(("shortDashed", 2), (line.Style, line.Thickness));
		Assert.Equal(("rnavOnlyWaypoint", 3), (symbol.Style, symbol.Size));

		// ERAM text has no opaque background, so CRC's opaque is always off.
		Assert.Equal((true, false, 4, -4), (text.Underline, text.Opaque, text.XOffset, text.YOffset));
		Assert.Null(lineProblem);
		Assert.Null(symbolProblem);
		Assert.Null(textProblem);
	}

	[Fact]
	public void missing_or_incomplete_defaults_are_not_used_and_say_why()
	{
		Assert.Null(EramCrcProperties.LineDefaults(null, out string? missing));
		Assert.Equal("it has no LineDefaults", missing);

		Assert.Null(EramCrcProperties.SymbolDefaults(Symbol with { Style = null, Size = null }, out string? incomplete));
		Assert.Equal("its SymbolDefaults have no Style, Size", incomplete);

		Assert.Null(EramCrcProperties.TextDefaults(Text with { YOffset = null }, out string? text));
		Assert.Contains("YOffset", text);
	}

	[Fact]
	public void defaults_crc_cannot_draw_are_not_used_and_say_why()
	{
		Assert.Null(EramCrcProperties.LineDefaults(Line with { Style = "Dotted" }, out string? badStyle));
		Assert.Contains("not values CRC can draw", badStyle);

		Assert.Null(EramCrcProperties.SymbolDefaults(Symbol with { Size = 9 }, out string? badSize));
		Assert.Contains("'size'", badSize);

		Assert.Null(EramCrcProperties.TextDefaults(Text with { Bcg = 99 }, out string? badBcg));
		Assert.Contains("'bcg'", badBcg);
	}

	[Fact]
	public void crc_defaults_turn_back_into_eram_properties()
	{
		CrcLineDefaults line = EramCrcProperties.LineDefaults(Line, out _)!;
		CrcSymbolDefaults symbol = EramCrcProperties.SymbolDefaults(Symbol, out _)!;
		CrcTextDefaults text = EramCrcProperties.TextDefaults(Text, out _)!;

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
		List<string> dropped = [];

		AttributesTable line = EramCrcProperties.Overrides(EramElementKind.Line, Line with { Size = 2 }, dropped.Add);
		AttributesTable symbol = EramCrcProperties.Overrides(EramElementKind.Symbol, Symbol, dropped.Add);
		AttributesTable text = EramCrcProperties.Overrides(EramElementKind.Text, Text, dropped.Add);

		Assert.Equal(["bcg", "filters", "style", "thickness"], line.GetNames());
		Assert.Equal("shortDashed", line["style"]);
		Assert.Equal(["bcg", "filters", "style", "size"], symbol.GetNames());
		Assert.Equal(["bcg", "filters", "size", "underline", "xOffset", "yOffset"], text.GetNames());
		Assert.Empty(EramCrcProperties.Overrides(EramElementKind.Line, EramProperties.None, dropped.Add).GetNames());
		Assert.Empty(dropped);
	}

	[Fact]
	public void override_values_crc_cannot_draw_are_left_out_and_reported()
	{
		List<string> dropped = [];

		AttributesTable line = EramCrcProperties.Overrides(EramElementKind.Line,
			new EramProperties { Bcg = 0, Filters = [1, 99], Style = "Dotted", Thickness = 5 }, dropped.Add);
		AttributesTable symbol = EramCrcProperties.Overrides(EramElementKind.Symbol,
			new EramProperties { Style = "DME", Size = 0 }, dropped.Add);
		AttributesTable text = EramCrcProperties.Overrides(EramElementKind.Text,
			new EramProperties { Size = 6, XOffset = 1 }, dropped.Add);
		AttributesTable below = EramCrcProperties.Overrides(EramElementKind.Line,
			new EramProperties { Filters = [-1], Thickness = 0 }, dropped.Add);
		AttributesTable belowText = EramCrcProperties.Overrides(EramElementKind.Text,
			new EramProperties { Size = -1 }, dropped.Add);

		Assert.Empty(line.GetNames());
		Assert.Empty(symbol.GetNames());
		Assert.Equal(["xOffset"], text.GetNames());
		Assert.Empty(below.GetNames());
		Assert.Empty(belowText.GetNames());
		Assert.Equal(
			[
				"Bcg=\"0\"", "Filters=\"1,99\"", "Style=\"Dotted\"", "Thickness=\"5\"", "Style=\"DME\"", "Size=\"0\"", "Size=\"6\"",
				"Filters=\"-1\"", "Thickness=\"0\"", "Size=\"-1\"",
			],
			dropped);
	}
}
