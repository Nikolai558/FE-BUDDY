using FeBuddy.Core.Application.Airac;

namespace FeBuddy.UnitTests.Application.Airac;

/// <summary>Covers <see cref="OutputFileFilter"/>: the Map output picker's filter, split into words once.</summary>
public sealed class OutputFileFilterTests
{
	/// <summary>Every word must be in the file's name or its folder, ignoring case; extra spaces are no words.</summary>
	[Theory]
	[InlineData("abq lines", "ABQ_Lines.geojson", "GeoJSON", true)]
	[InlineData("ABQ  departures", "ABQ_Lines.geojson", "GeoJSON  ·  Departures", true)]
	[InlineData("abq symbols", "ABQ_Lines.geojson", "GeoJSON", false)]
	[InlineData("", "anything.geojson", "GeoJSON", true)]
	[InlineData("   ", "anything.geojson", "GeoJSON", true)]
	public void every_word_must_be_in_the_name_or_the_folder(string text, string name, string folder, bool matches) =>
		Assert.Equal(matches, new OutputFileFilter(text).Matches(name, folder));

	/// <summary>The words are split once, in the order typed; no text is no words.</summary>
	[Fact]
	public void the_words_are_split_once()
	{
		Assert.Equal(["ABQ", "Lines"], new OutputFileFilter("  ABQ   Lines ").Words);
		Assert.True(new OutputFileFilter(null).IsEmpty);
		Assert.True(OutputFileFilter.None.IsEmpty);
		Assert.False(new OutputFileFilter("x").IsEmpty);
		Assert.Throws<ArgumentNullException>(() => OutputFileFilter.None.Matches(null!, "f"));
		Assert.Throws<ArgumentNullException>(() => OutputFileFilter.None.Matches("n", null!));
	}
}
