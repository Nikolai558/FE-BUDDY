namespace FeBuddy.UnitTests.Handlers;

/// <summary>
/// Covers the value parsers every NASR parser leans on: a required number that is blank or
/// malformed fails loudly naming the value, while the nullable forms quietly return null.
/// </summary>
public class FebCsvHelperTests
{
	[Theory]
	[InlineData("")]
	[InlineData("  ")]
	public void ParseInt_and_ParseDouble_reject_a_blank_required_value(string value)
	{
		Assert.Throws<ArgumentNullException>(() => FebCsvHelper.ParseInt(value));
		Assert.Throws<ArgumentNullException>(() => FebCsvHelper.ParseDouble(value));
	}

	[Fact]
	public void ParseInt_and_ParseDouble_name_a_malformed_value()
	{
		FormatException intError = Assert.Throws<FormatException>(() => FebCsvHelper.ParseInt("12a"));
		FormatException doubleError = Assert.Throws<FormatException>(() => FebCsvHelper.ParseDouble("N/A"));

		Assert.Equal("Invalid integer format: '12a'", intError.Message);
		Assert.Equal("Invalid double format: 'N/A'", doubleError.Message);
	}

	[Fact]
	public void the_nullable_parsers_return_null_for_anything_unparseable()
	{
		Assert.Null(FebCsvHelper.ParseNullableInt(""));
		Assert.Null(FebCsvHelper.ParseNullableInt("x"));
		Assert.Null(FebCsvHelper.ParseNullableDouble(""));
		Assert.Equal(7, FebCsvHelper.ParseNullableInt("7"));
	}

	[Fact]
	public void GetField_returns_empty_for_a_column_the_file_lacks()
	{
		Dictionary<string, string> fields = new() { ["PRESENT"] = "yes" };

		Assert.Equal("yes", FebCsvHelper.GetField(fields, "PRESENT"));
		Assert.Equal(string.Empty, FebCsvHelper.GetField(fields, "ABSENT"));
	}
}
