using FeBuddy.Core.Application.Settings;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.UnitTests.Application.Settings;

/// <summary>
/// Covers reading CRC defaults out of a raw settings block: every property of every kind is
/// required and a missing one names its key, text offsets accept any integer, Y/N flags are
/// strict, and styles are matched case-insensitively to their canonical spelling.
/// </summary>
public sealed class CrcDefaultsReaderTests
{
	private const string LinePrefix = "Crc.High.Line";
	private const string SymbolPrefix = "Crc.High.Symbol";
	private const string TextPrefix = "Crc.High.Text";

	private static Dictionary<string, string> FullLine() => new()
	{
		{ $"{LinePrefix}.bcg", "3" },
		{ $"{LinePrefix}.filters", "3, 4" },
		{ $"{LinePrefix}.style", "solid" },
		{ $"{LinePrefix}.thickness", "2" }
	};

	private static Dictionary<string, string> FullSymbol() => new()
	{
		{ $"{SymbolPrefix}.bcg", "5" },
		{ $"{SymbolPrefix}.filters", "7" },
		{ $"{SymbolPrefix}.style", "vor" },
		{ $"{SymbolPrefix}.size", "4" }
	};

	private static Dictionary<string, string> FullText() => new()
	{
		{ $"{TextPrefix}.bcg", "6" },
		{ $"{TextPrefix}.filters", "8" },
		{ $"{TextPrefix}.size", "2" },
		{ $"{TextPrefix}.underline", "Y" },
		{ $"{TextPrefix}.opaque", "N" },
		{ $"{TextPrefix}.xOffset", "12" },
		{ $"{TextPrefix}.yOffset", "0" }
	};

	[Fact]
	public void read_line_reads_a_full_set()
	{
		CrcLineDefaults defaults = CrcDefaultsReader.ReadLine(FullLine(), LinePrefix);

		Assert.Equal(3, defaults.Bcg);
		Assert.Equal([3, 4], defaults.Filters);
		Assert.Equal("solid", defaults.Style);
		Assert.Equal(2, defaults.Thickness);
	}

	[Fact]
	public void read_symbol_reads_a_full_set()
	{
		CrcSymbolDefaults defaults = CrcDefaultsReader.ReadSymbol(FullSymbol(), SymbolPrefix);

		Assert.Equal(5, defaults.Bcg);
		Assert.Equal([7], defaults.Filters);
		Assert.Equal("vor", defaults.Style);
		Assert.Equal(4, defaults.Size);
	}

	[Fact]
	public void read_text_reads_a_full_set()
	{
		CrcTextDefaults defaults = CrcDefaultsReader.ReadText(FullText(), TextPrefix);

		Assert.Equal(6, defaults.Bcg);
		Assert.Equal([8], defaults.Filters);
		Assert.Equal(2, defaults.Size);
		Assert.True(defaults.Underline);
		Assert.False(defaults.Opaque);
		Assert.Equal(12, defaults.XOffset);
		Assert.Equal(0, defaults.YOffset);
	}

	[Theory]
	[InlineData("bcg")]
	[InlineData("filters")]
	[InlineData("style")]
	[InlineData("thickness")]
	public void read_line_throws_naming_a_missing_key(string property)
	{
		Dictionary<string, string> settings = FullLine();
		settings.Remove($"{LinePrefix}.{property}");

		ArgumentException ex = Assert.Throws<ArgumentException>(() => CrcDefaultsReader.ReadLine(settings, LinePrefix));

		Assert.Contains($"{LinePrefix}.{property}", ex.Message);
	}

	[Theory]
	[InlineData("bcg")]
	[InlineData("filters")]
	[InlineData("style")]
	[InlineData("size")]
	public void read_symbol_throws_naming_a_missing_key(string property)
	{
		Dictionary<string, string> settings = FullSymbol();
		settings.Remove($"{SymbolPrefix}.{property}");

		ArgumentException ex = Assert.Throws<ArgumentException>(() => CrcDefaultsReader.ReadSymbol(settings, SymbolPrefix));

		Assert.Contains($"{SymbolPrefix}.{property}", ex.Message);
	}

	[Theory]
	[InlineData("bcg")]
	[InlineData("filters")]
	[InlineData("size")]
	[InlineData("underline")]
	[InlineData("opaque")]
	[InlineData("xOffset")]
	[InlineData("yOffset")]
	public void read_text_throws_naming_a_missing_key(string property)
	{
		Dictionary<string, string> settings = FullText();
		settings.Remove($"{TextPrefix}.{property}");

		ArgumentException ex = Assert.Throws<ArgumentException>(() => CrcDefaultsReader.ReadText(settings, TextPrefix));

		Assert.Contains($"{TextPrefix}.{property}", ex.Message);
	}

	[Fact]
	public void read_text_treats_a_blank_value_as_missing()
	{
		Dictionary<string, string> settings = FullText();
		settings[$"{TextPrefix}.opaque"] = "  ";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => CrcDefaultsReader.ReadText(settings, TextPrefix));

		Assert.Contains($"{TextPrefix}.opaque", ex.Message);
	}

	[Fact]
	public void read_text_accepts_negative_offsets()
	{
		Dictionary<string, string> settings = FullText();
		settings[$"{TextPrefix}.xOffset"] = "-10";
		settings[$"{TextPrefix}.yOffset"] = "-3";

		CrcTextDefaults defaults = CrcDefaultsReader.ReadText(settings, TextPrefix);

		Assert.Equal(-10, defaults.XOffset);
		Assert.Equal(-3, defaults.YOffset);
	}

	[Theory]
	[InlineData("Maybe")]
	[InlineData("true")]
	[InlineData("1")]
	public void read_text_throws_when_underline_is_not_y_or_n(string value)
	{
		Dictionary<string, string> settings = FullText();
		settings[$"{TextPrefix}.underline"] = value;

		ArgumentException ex = Assert.Throws<ArgumentException>(() => CrcDefaultsReader.ReadText(settings, TextPrefix));

		Assert.Contains($"{TextPrefix}.underline", ex.Message);
	}

	[Fact]
	public void read_text_throws_when_an_offset_is_not_an_integer()
	{
		Dictionary<string, string> settings = FullText();
		settings[$"{TextPrefix}.yOffset"] = "1.5";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => CrcDefaultsReader.ReadText(settings, TextPrefix));

		Assert.Contains($"{TextPrefix}.yOffset", ex.Message);
	}

	[Fact]
	public void read_line_normalises_style_case()
	{
		Dictionary<string, string> settings = FullLine();
		settings[$"{LinePrefix}.style"] = "SOLID";

		Assert.Equal("solid", CrcDefaultsReader.ReadLine(settings, LinePrefix).Style);

		settings[$"{LinePrefix}.style"] = "longdashshortdash";

		Assert.Equal("longDashShortDash", CrcDefaultsReader.ReadLine(settings, LinePrefix).Style);
	}

	[Fact]
	public void read_symbol_normalises_style_case()
	{
		Dictionary<string, string> settings = FullSymbol();
		settings[$"{SymbolPrefix}.style"] = "VOR";

		Assert.Equal("vor", CrcDefaultsReader.ReadSymbol(settings, SymbolPrefix).Style);
	}

	[Fact]
	public void an_unknown_style_throws_naming_the_prefix()
	{
		Dictionary<string, string> settings = FullLine();
		settings[$"{LinePrefix}.style"] = "wavy";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => CrcDefaultsReader.ReadLine(settings, LinePrefix));

		Assert.Contains(LinePrefix, ex.Message);
		Assert.Contains("wavy", ex.Message);
	}

	[Fact]
	public void an_out_of_range_value_throws_naming_the_prefix()
	{
		Dictionary<string, string> settings = FullText();
		settings[$"{TextPrefix}.size"] = "9";

		ArgumentException ex = Assert.Throws<ArgumentException>(() => CrcDefaultsReader.ReadText(settings, TextPrefix));

		Assert.Contains(TextPrefix, ex.Message);
		Assert.Contains("size", ex.Message);
	}
}
