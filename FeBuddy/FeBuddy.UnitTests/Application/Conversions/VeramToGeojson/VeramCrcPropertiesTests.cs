using FeBuddy.Core.Application.Conversions.VeramToGeojson;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Infrastructure.Veram.Models;

using NetTopologySuite.Features;

namespace FeBuddy.UnitTests.Application.Conversions.VeramToGeojson;

/// <summary>
/// Covers <see cref="VeramCrcProperties"/>: complete CRC defaults from vERAM's, style names, and
/// the overrides an element sets.
/// </summary>
public sealed class VeramCrcPropertiesTests
{
	private static readonly VeramProperties Line = new() { Bcg = 1, Filters = [1], Style = "ShortDashed", Thickness = 2 };
	private static readonly VeramProperties Symbol = new() { Bcg = 2, Filters = [2], Style = "Vor", Size = 3 };
	private static readonly VeramProperties Text = new()
	{
		Bcg = 3,
		Filters = [3],
		Size = 1,
		Underline = true,
		Opaque = false,
		XOffset = 4,
		YOffset = -4,
	};

	[Fact]
	public void complete_defaults_become_crc_defaults_in_crc_spelling()
	{
		CrcLineDefaults line = VeramCrcProperties.LineDefaults(Line, out string? lineProblem)!;
		CrcSymbolDefaults symbol = VeramCrcProperties.SymbolDefaults(Symbol, out string? symbolProblem)!;
		CrcTextDefaults text = VeramCrcProperties.TextDefaults(Text, out string? textProblem)!;

		Assert.Equal(("shortDashed", 2), (line.Style, line.Thickness));
		Assert.Equal(("vor", 3), (symbol.Style, symbol.Size));
		Assert.Equal((true, false, 4, -4), (text.Underline, text.Opaque, text.XOffset, text.YOffset));
		Assert.Null(lineProblem);
		Assert.Null(symbolProblem);
		Assert.Null(textProblem);
	}

	[Fact]
	public void missing_or_incomplete_defaults_are_not_used_and_say_why()
	{
		Assert.Null(VeramCrcProperties.LineDefaults(null, out string? missing));
		Assert.Equal("it has no LineDefaults", missing);

		Assert.Null(VeramCrcProperties.SymbolDefaults(Symbol with { Style = null, Size = null }, out string? incomplete));
		Assert.Equal("its SymbolDefaults have no Style, Size", incomplete);

		Assert.Null(VeramCrcProperties.TextDefaults(Text with { YOffset = null }, out string? text));
		Assert.Contains("YOffset", text);
	}

	[Fact]
	public void defaults_crc_cannot_draw_are_not_used_and_say_why()
	{
		Assert.Null(VeramCrcProperties.LineDefaults(Line with { Style = "Dotted" }, out string? badStyle));
		Assert.Contains("not values CRC can draw", badStyle);

		Assert.Null(VeramCrcProperties.SymbolDefaults(Symbol with { Size = 9 }, out string? badSize));
		Assert.Contains("'size'", badSize);

		Assert.Null(VeramCrcProperties.TextDefaults(Text with { Bcg = 99 }, out string? badBcg));
		Assert.Contains("'bcg'", badBcg);
	}

	[Fact]
	public void crc_defaults_turn_back_into_veram_properties()
	{
		CrcLineDefaults line = VeramCrcProperties.LineDefaults(Line, out _)!;
		CrcSymbolDefaults symbol = VeramCrcProperties.SymbolDefaults(Symbol, out _)!;
		CrcTextDefaults text = VeramCrcProperties.TextDefaults(Text, out _)!;

		Assert.Equal(Line with { Style = "shortDashed" }, VeramCrcProperties.AsVeram(line));
		Assert.Equal(Symbol with { Style = "vor" }, VeramCrcProperties.AsVeram(symbol));
		Assert.Equal(Text, VeramCrcProperties.AsVeram(text));
		Assert.Same(VeramProperties.None, VeramCrcProperties.AsVeram(null));
	}

	[Fact]
	public void an_element_s_own_values_win_over_its_defaults()
	{
		VeramProperties drawn = VeramCrcProperties.Over(Line, new VeramProperties { Thickness = 3, Filters = [9] });

		Assert.Equal(Line with { Thickness = 3, Filters = [9] }, drawn);
		Assert.Equal(Text, VeramCrcProperties.Over(Text, VeramProperties.None));
		Assert.Equal(Text, VeramCrcProperties.Over(VeramProperties.None, Text));
	}

	[Fact]
	public void overrides_carry_only_what_the_element_sets_for_its_kind()
	{
		List<string> dropped = [];

		AttributesTable line = VeramCrcProperties.Overrides(VeramElementKind.Line, Line with { Size = 2 }, dropped.Add);
		AttributesTable symbol = VeramCrcProperties.Overrides(VeramElementKind.Symbol, Symbol, dropped.Add);
		AttributesTable text = VeramCrcProperties.Overrides(VeramElementKind.Text, Text, dropped.Add);

		Assert.Equal(["bcg", "filters", "style", "thickness"], line.GetNames());
		Assert.Equal("shortDashed", line["style"]);
		Assert.Equal(["bcg", "filters", "style", "size"], symbol.GetNames());
		Assert.Equal(["bcg", "filters", "size", "underline", "opaque", "xOffset", "yOffset"], text.GetNames());
		Assert.Empty(VeramCrcProperties.Overrides(VeramElementKind.Line, VeramProperties.None, dropped.Add).GetNames());
		Assert.Empty(dropped);
	}

	[Fact]
	public void override_values_crc_cannot_draw_are_left_out_and_reported()
	{
		List<string> dropped = [];

		AttributesTable line = VeramCrcProperties.Overrides(VeramElementKind.Line,
			new VeramProperties { Bcg = 0, Filters = [1, 99], Style = "Dotted", Thickness = 5 }, dropped.Add);
		AttributesTable symbol = VeramCrcProperties.Overrides(VeramElementKind.Symbol,
			new VeramProperties { Style = "Castle", Size = 0 }, dropped.Add);
		AttributesTable text = VeramCrcProperties.Overrides(VeramElementKind.Text,
			new VeramProperties { Size = 6, XOffset = 1 }, dropped.Add);
		AttributesTable below = VeramCrcProperties.Overrides(VeramElementKind.Line,
			new VeramProperties { Filters = [-1], Thickness = 0 }, dropped.Add);
		AttributesTable belowText = VeramCrcProperties.Overrides(VeramElementKind.Text,
			new VeramProperties { Size = -1 }, dropped.Add);

		Assert.Empty(line.GetNames());
		Assert.Empty(symbol.GetNames());
		Assert.Equal(["xOffset"], text.GetNames());
		Assert.Empty(below.GetNames());
		Assert.Empty(belowText.GetNames());
		Assert.Equal(
			[
				"Bcg=\"0\"", "Filters=\"1,99\"", "Style=\"Dotted\"", "Thickness=\"5\"", "Style=\"Castle\"", "Size=\"0\"", "Size=\"6\"",
				"Filters=\"-1\"", "Thickness=\"0\"", "Size=\"-1\"",
			],
			dropped);
	}
}
