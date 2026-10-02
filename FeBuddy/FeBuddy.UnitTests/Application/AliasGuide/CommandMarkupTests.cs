using FeBuddy.Core.Application.AliasGuide;
using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.UnitTests.Application.AliasGuide;

/// <summary>
/// Covers <see cref="CommandMarkup"/>: typed text, a placeholder (<c>[a:airport ID]</c>), an optional
/// placeholder (<c>[p?:page]</c>) and a real value (<c>{i:DTW}</c>); what each kind letter means; the
/// parts in the order written; the flattened command; and every way the markup can be malformed.
/// </summary>
public sealed class CommandMarkupTests
{
	private static CommandPart Typed(string text) => new(text, CommandPartKind.Typed, IsPlaceholder: false, IsOptional: false);

	private static CommandPart Value(string text, CommandPartKind kind) => new(text, kind, IsPlaceholder: false, IsOptional: false);

	private static CommandPart Placeholder(string text, CommandPartKind kind, bool isOptional = false) => new(text, kind, IsPlaceholder: true, IsOptional: isOptional);

	[Fact]
	public void text_outside_brackets_is_one_typed_part()
	{
		Assert.Equal([Typed(".apt")], CommandMarkup.Parse(".apt"));
	}

	[Fact]
	public void no_markup_has_no_parts()
	{
		Assert.Empty(CommandMarkup.Parse(string.Empty));
	}

	[Fact]
	public void square_brackets_are_a_placeholder()
	{
		Assert.Equal([Placeholder("airport ID", CommandPartKind.Airport)], CommandMarkup.Parse("[a:airport ID]"));
	}

	[Fact]
	public void a_question_mark_after_the_kind_makes_a_placeholder_optional()
	{
		Assert.Equal([Placeholder("page", CommandPartKind.Page, isOptional: true)], CommandMarkup.Parse("[p?:page]"));
	}

	[Fact]
	public void curly_brackets_are_a_real_value()
	{
		Assert.Equal([Value("DTW", CommandPartKind.Identifier)], CommandMarkup.Parse("{i:DTW}"));
	}

	[Theory]
	[InlineData("a", "Airport")]
	[InlineData("i", "Identifier")]
	[InlineData("t", "ApproachType")]
	[InlineData("v", "Variant")]
	[InlineData("r", "Runway")]
	[InlineData("p", "Page")]
	public void each_kind_letter_is_its_part_kind(string letter, string kindName)
	{
		CommandPartKind kind = Enum.Parse<CommandPartKind>(kindName);

		Assert.Equal([Placeholder("text", kind)], CommandMarkup.Parse("[" + letter + ":text]"));
		Assert.Equal([Value("text", kind)], CommandMarkup.Parse("{" + letter + ":text}"));
	}

	[Fact]
	public void a_value_can_be_optional_without_being_a_placeholder()
	{
		CommandPart part = Assert.Single(CommandMarkup.Parse("{v?:Z}"));

		Assert.Equal(new CommandPart("Z", CommandPartKind.Variant, IsPlaceholder: false, IsOptional: true), part);
	}

	[Fact]
	public void typed_text_and_brackets_are_parts_in_the_order_written()
	{
		Assert.Equal(
			[
				Typed("."),
				Value("dtw", CommandPartKind.Airport),
				Value("I", CommandPartKind.ApproachType),
				Value("Z", CommandPartKind.Variant),
				Value("04L", CommandPartKind.Runway),
				Typed("c"),
			],
			CommandMarkup.Parse(".{a:dtw}{t:I}{v:Z}{r:04L}c"));
	}

	[Fact]
	public void a_syntax_pattern_mixes_typed_text_placeholders_and_an_optional_one()
	{
		Assert.Equal(
			[
				Typed("."),
				Placeholder("airport ID", CommandPartKind.Airport),
				Placeholder("chart code", CommandPartKind.Identifier),
				Typed("c"),
				Placeholder("page", CommandPartKind.Page, isOptional: true),
			],
			CommandMarkup.Parse(".[a:airport ID][i:chart code]c[p?:page]"));
	}

	[Fact]
	public void typed_text_after_a_bracket_is_its_own_part()
	{
		Assert.Equal(
			[Typed(".apt"), Placeholder("FAA or ICAO airport ID", CommandPartKind.Airport)],
			CommandMarkup.Parse(".apt[a:FAA or ICAO airport ID]"));
	}

	[Fact]
	public void flatten_joins_every_parts_text_into_the_command_as_typed()
	{
		string command = CommandMarkup.Flatten(CommandMarkup.Parse(".{a:dtw}{t:I}{v:Z}{r:04L}c"));

		Assert.Equal(".dtwIZ04Lc", command);
	}

	[Fact]
	public void flatten_of_nothing_is_empty()
	{
		Assert.Equal(string.Empty, CommandMarkup.Flatten([]));
	}

	[Theory]
	[InlineData("[a:x")]
	[InlineData("{a:x")]
	[InlineData("{")]
	[InlineData("[a:x}")]
	[InlineData("[q:x]")]
	[InlineData("[ax]")]
	[InlineData("[a:]")]
	[InlineData("{a:}")]
	[InlineData("[?:x]")]
	public void malformed_markup_is_a_format_exception(string markup)
	{
		Assert.Throws<FormatException>(() => CommandMarkup.Parse(markup));
	}

	[Fact]
	public void null_markup_is_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => CommandMarkup.Parse(null!));
	}
}
