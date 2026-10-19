using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.UnitTests.Wpf.ViewModels.Models;

/// <summary>
/// Covers what a CRC ERAM defaults row marks: a blank box only while the row is required, and a value the
/// run would refuse - out of range, not a listed style, a malformed filter list - always, counted as not filled in.
/// </summary>
public sealed class EramClassDefaultTests
{
	private static EramClassDefault Filled(EramFieldKind kind)
	{
		EramClassDefault row = new("High", kind, () => { })
		{
			Bcg = "5",
			Filters = "2,7",
			Style = kind == EramFieldKind.Symbol ? "vor" : "Solid",
			Thickness = "1",
			Size = "1",
			Underline = "N",
			Opaque = "Y",
			XOffset = "0",
			YOffset = "-4",
		};
		return row;
	}

	/// <summary>A row with every value the run accepts has nothing missing and nothing marked.</summary>
	[Theory]
	[InlineData(EramFieldKind.Line)]
	[InlineData(EramFieldKind.Symbol)]
	[InlineData(EramFieldKind.Text)]
	public void a_row_of_accepted_values_is_complete(EramFieldKind kind)
	{
		EramClassDefault row = Filled(kind);
		row.IsRequired = true;

		Assert.False(row.HasMissingValues);
		Assert.All(
			new[] { row.BcgError, row.FiltersError, row.StyleError, row.ThicknessError, row.SizeError, row.UnderlineError, row.OpaqueError, row.XOffsetError, row.YOffsetError },
			Assert.Null);
	}

	/// <summary>Values from a hand-edited file or an import that the run would refuse are marked, required or not.</summary>
	[Fact]
	public void a_value_the_run_would_refuse_is_marked_and_counts_as_missing()
	{
		EramClassDefault line = Filled(EramFieldKind.Line);
		line.Bcg = "50";
		line.Style = "Dotted";
		line.Thickness = "9";
		line.Filters = "2;7";

		Assert.True(line.HasMissingValues);
		Assert.Equal("Pick one of the listed values.", line.BcgError);
		Assert.Equal("Pick one of the listed values.", line.StyleError);
		Assert.Equal("Pick one of the listed values.", line.ThicknessError);
		Assert.Equal("Must be whole numbers from 0 to 40, separated by commas, e.g. 2,7.", line.FiltersError);

		EramClassDefault text = Filled(EramFieldKind.Text);
		text.Size = "6";
		text.Underline = "Maybe";
		text.XOffset = "1.5";

		Assert.True(text.HasMissingValues);
		Assert.Equal("Pick one of the listed values.", text.SizeError);
		Assert.Equal("Choose Yes or No.", text.UnderlineError);
		Assert.Equal("Must be a whole number, e.g. 0, 12 or -4.", text.XOffsetError);
	}

	/// <summary>A blank box is marked only while the row is required; a box the kind doesn't show never is.</summary>
	[Fact]
	public void a_blank_box_is_marked_only_while_required()
	{
		EramClassDefault row = Filled(EramFieldKind.Line);
		row.Bcg = " ";
		row.Size = "99";

		Assert.True(row.HasMissingValues);
		Assert.Null(row.BcgError);
		Assert.Null(row.SizeError);

		row.IsRequired = true;
		Assert.Equal("Required while CRC ERAM defaults are on.", row.BcgError);
		Assert.Null(row.SizeError);
	}

	/// <summary>A NAVAIDs Symbols row styled by type asks for no style, so its style isn't checked.</summary>
	[Fact]
	public void a_style_the_features_bring_is_not_checked()
	{
		EramClassDefault row = Filled(EramFieldKind.Symbol);
		row.Style = "Dotted";
		Assert.True(row.HasMissingValues);

		row.StyleFromFeatures = true;
		Assert.False(row.HasMissingValues);
		Assert.Null(row.StyleError);
	}
}
