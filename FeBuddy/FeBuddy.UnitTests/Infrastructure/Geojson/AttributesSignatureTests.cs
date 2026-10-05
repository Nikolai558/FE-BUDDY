using FeBuddy.Core.Infrastructure.Geojson;

using NetTopologySuite.Features;

namespace FeBuddy.UnitTests.Infrastructure.Geojson;

/// <summary>
/// Covers <see cref="AttributesSignature"/>: tables that would write the same properties get the
/// same key, and tables that would not never do.
/// </summary>
public sealed class AttributesSignatureTests
{
	private static string Key(params (string Name, object? Value)[] properties)
	{
		AttributesTable table = [];
		foreach ((string name, object? value) in properties)
		{
			table.Add(name, value);
		}

		return AttributesSignature.Of(table);
	}

	[Fact]
	public void the_same_properties_in_another_order_give_the_same_key() =>
		Assert.Equal(Key(("bcg", 1), ("style", "vor")), Key(("style", "vor"), ("bcg", 1)));

	[Fact]
	public void arrays_compare_by_their_elements()
	{
		Assert.Equal(Key(("text", new[] { "A", "B" })), Key(("text", new[] { "A", "B" })));
		Assert.NotEqual(Key(("text", new[] { "A", "B" })), Key(("text", new[] { "B", "A" })));
		Assert.NotEqual(Key(("text", new[] { "AB" })), Key(("text", new[] { "A", "B" })));
		Assert.Equal(Key(("filters", new[] { 1, 2 })), Key(("filters", new List<int> { 1, 2 })));
	}

	[Fact]
	public void numbers_compare_by_value_whatever_their_type() =>
		Assert.Equal(Key(("size", 1)), Key(("size", 1.0)));

	[Theory]
	[InlineData("1")]
	[InlineData("true")]
	[InlineData(null)]
	public void a_string_or_nothing_never_matches_a_number_or_flag_that_reads_the_same(string? value)
	{
		Assert.NotEqual(Key(("v", value)), Key(("v", 1)));
		Assert.NotEqual(Key(("v", value)), Key(("v", true)));
	}

	[Fact]
	public void names_and_values_cannot_run_into_each_other() =>
		Assert.NotEqual(Key(("a", "b;c=d")), Key(("a", "b"), ("c", "d")));

	[Fact]
	public void no_properties_and_no_table_give_the_same_empty_key()
	{
		Assert.Equal(string.Empty, AttributesSignature.Of(null));
		Assert.Equal(string.Empty, Key());
	}

	[Fact]
	public void any_other_value_is_keyed_by_its_text() =>
		Assert.Equal(Key(("when", new DateOnly(2026, 10, 4))), Key(("when", new DateOnly(2026, 10, 4))));
}
