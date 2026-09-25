using System.Globalization;

using FeBuddy.Core.Infrastructure.Nasr;

namespace FeBuddy.UnitTests.Infrastructure.Nasr;

/// <summary>
/// Covers the value parsers every NASR parser leans on: a required number that is blank or
/// malformed fails loudly naming the value, the nullable forms quietly return null, and every
/// number reads the same whatever the PC's regional format.
/// </summary>
public sealed class NasrCsvReaderTests
{
	[Fact]
	public void coordinates_read_the_same_under_a_german_regional_format()
	{
		// German writes 40,123456 and uses the period as a thousands separator, so a
		// culture-sensitive parse would read NASR's 40.123456 as 40123456.
		CultureInfo original = CultureInfo.CurrentCulture;
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

		try
		{
			Assert.Equal(40.123456, NasrCsvReader.ParseDouble("40.123456"));
			Assert.Equal(-80.5, NasrCsvReader.ParseNullableDouble("-80.5"));
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}

	[Theory]
	[InlineData("")]
	[InlineData("  ")]
	public void parse_int_and_parse_double_reject_a_blank_required_value(string value)
	{
		Assert.Throws<ArgumentNullException>(() => NasrCsvReader.ParseInt(value));
		Assert.Throws<ArgumentNullException>(() => NasrCsvReader.ParseDouble(value));
	}

	[Fact]
	public void parse_int_and_parse_double_name_a_malformed_value()
	{
		FormatException intError = Assert.Throws<FormatException>(() => NasrCsvReader.ParseInt("12a"));
		FormatException doubleError = Assert.Throws<FormatException>(() => NasrCsvReader.ParseDouble("N/A"));

		Assert.Equal("Invalid integer format: '12a'", intError.Message);
		Assert.Equal("Invalid double format: 'N/A'", doubleError.Message);
	}

	[Fact]
	public void the_nullable_parsers_return_null_for_anything_unparseable()
	{
		Assert.Null(NasrCsvReader.ParseNullableInt(""));
		Assert.Null(NasrCsvReader.ParseNullableInt("x"));
		Assert.Null(NasrCsvReader.ParseNullableDouble(""));
		Assert.Equal(7, NasrCsvReader.ParseNullableInt("7"));
	}

	[Fact]
	public void get_field_returns_empty_for_a_column_the_file_lacks()
	{
		Dictionary<string, string> fields = new() { ["PRESENT"] = "yes" };

		Assert.Equal("yes", NasrCsvReader.GetField(fields, "PRESENT"));
		Assert.Equal(string.Empty, NasrCsvReader.GetField(fields, "ABSENT"));
	}
}
