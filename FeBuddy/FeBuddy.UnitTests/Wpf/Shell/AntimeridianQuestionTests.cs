using FeBuddy.Wpf.Behaviors;
using FeBuddy.Wpf.Shell;

namespace FeBuddy.UnitTests.Wpf.Shell;

/// <summary>
/// Covers <see cref="AntimeridianQuestion"/>: the question puts the longitudes in as typed, and links to what the
/// antimeridian (AM) is, with its own tooltip.
/// </summary>
public sealed class AntimeridianQuestionTests
{
	[Fact]
	public void the_question_puts_the_longitudes_in_as_typed_and_links_the_antimeridian()
	{
		string message = AntimeridianQuestion.Message("154", "-124");

		Assert.Equal(
			[
				("The Southwest Longitude (154) is east of the Northeast Longitude (-124). That's only right if the ROI crosses the ", null, null),
				("antimeridian", Links.Antimeridian, AntimeridianQuestion.LinkToolTip),
				(" (AM), the 180° meridian.", null, null),
			],
			InlineCode.SplitLinks(message));
	}
}
