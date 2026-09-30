using FeBuddy.Core.Application.Settings;

namespace FeBuddy.UnitTests.Application.Settings;

/// <summary>
/// Covers the <see cref="SettingsValueReader"/> paths the sub-service parsers do not reach:
/// a present-but-out-of-range integer, malformed integer lists, positive decimals, and style
/// normalization.
/// </summary>
public sealed class SettingsValueReaderTests
{
	[Fact]
	public void int_in_range_uses_the_default_when_absent_and_enforces_the_range_when_present()
	{
		Dictionary<string, string> settings = new() { ["Precision"] = "20" };

		Assert.Equal(6, SettingsValueReader.IntInRange(new Dictionary<string, string>(), "Precision", 6, 0, 15));
		Assert.Throws<ArgumentException>(() => SettingsValueReader.IntInRange(settings, "Precision", 6, 0, 15));

		settings["Precision"] = "8";
		Assert.Equal(8, SettingsValueReader.IntInRange(settings, "Precision", 6, 0, 15));
	}

	[Theory]
	[InlineData(null, null)]
	[InlineData("  ", null)]
	[InlineData("106", 106d)]
	[InlineData(" 12.5 ", 12.5d)]
	[InlineData("1000", 1000d)]
	public void optional_positive_decimal_reads_a_number_or_nothing(string? value, double? expected)
	{
		Dictionary<string, string> settings = value is null ? [] : new() { ["Distance"] = value };

		Assert.Equal(expected, SettingsValueReader.OptionalPositiveDecimal(settings, "Distance", maximum: 1000));
	}

	[Theory]
	[InlineData("0")]
	[InlineData("-5")]
	[InlineData("1000.1")]
	[InlineData("ten")]
	[InlineData("1,000")]
	public void optional_positive_decimal_rejects_zero_negative_too_large_and_non_numbers(string value)
	{
		Dictionary<string, string> settings = new() { ["Distance"] = value };

		ArgumentException error = Assert.Throws<ArgumentException>(() =>
			SettingsValueReader.OptionalPositiveDecimal(settings, "Distance", maximum: 1000));

		Assert.Contains("'Distance'", error.Message);
	}

	[Theory]
	[InlineData(null, 2.5d)]
	[InlineData("  ", 2.5d)]
	[InlineData("0", 0d)]
	[InlineData(" 7.25 ", 7.25d)]
	[InlineData("10", 10d)]
	public void decimal_in_range_reads_a_number_or_the_default(string? value, double expected)
	{
		Dictionary<string, string> settings = value is null ? [] : new() { ["Distance"] = value };

		Assert.Equal(expected, SettingsValueReader.DecimalInRange(settings, "Distance", defaultValue: 2.5, minimum: 0, maximum: 10));
	}

	[Theory]
	[InlineData("-0.5")]
	[InlineData("10.01")]
	[InlineData("ten")]
	[InlineData("1,5")]
	[InlineData("NaN")]
	[InlineData("Infinity")]
	public void decimal_in_range_rejects_out_of_range_and_non_numbers(string value)
	{
		Dictionary<string, string> settings = new() { ["Distance"] = value };

		ArgumentException error = Assert.Throws<ArgumentException>(() =>
			SettingsValueReader.DecimalInRange(settings, "Distance", defaultValue: 2.5, minimum: 0, maximum: 10));

		Assert.Contains("'Distance'", error.Message);
		Assert.Contains("from 0 to 10", error.Message);
	}

	[Fact]
	public void required_int_list_reads_a_comma_list_and_rejects_an_empty_or_non_numeric_one()
	{
		Assert.Equal([1, 2, 3], SettingsValueReader.RequiredIntList(new Dictionary<string, string> { ["Filters"] = "1, 2 ,3" }, "Filters"));

		ArgumentException empty = Assert.Throws<ArgumentException>(() =>
			SettingsValueReader.RequiredIntList(new Dictionary<string, string> { ["Filters"] = ", ," }, "Filters"));
		ArgumentException bad = Assert.Throws<ArgumentException>(() =>
			SettingsValueReader.RequiredIntList(new Dictionary<string, string> { ["Filters"] = "1,two" }, "Filters"));

		Assert.Contains("at least one comma-separated integer", empty.Message);
		Assert.Contains("entry 'two' is not a valid integer", bad.Message);
	}

	[Fact]
	public void normalize_style_returns_the_canonical_spelling_or_the_value_unchanged()
	{
		string[] valid = ["solid", "shortDashed"];

		Assert.Null(SettingsValueReader.NormalizeStyle(null, valid));
		Assert.Equal("shortDashed", SettingsValueReader.NormalizeStyle("SHORTDASHED", valid));
		Assert.Equal("zigzag", SettingsValueReader.NormalizeStyle("zigzag", valid));
	}
}
