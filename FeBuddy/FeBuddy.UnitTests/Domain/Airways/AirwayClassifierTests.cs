using FeBuddy.Core.Domain.Airways;
using FeBuddy.Core.Domain.Airways.Models;

namespace FeBuddy.UnitTests.Domain.Airways;

public class AirwayClassifierTests
{
	private static AirwaySegment Seg(int? maxAuthAlt) => new("A", "B", IsGap: false, maxAuthAlt);

	[Theory]
	[InlineData(18000, AirwayAltitudeClass.High)]
	[InlineData(45000, AirwayAltitudeClass.High)]
	[InlineData(17999, AirwayAltitudeClass.Low)]
	[InlineData(1, AirwayAltitudeClass.Low)]
	[InlineData(0, AirwayAltitudeClass.Other)]
	[InlineData(-100, AirwayAltitudeClass.Other)]
	public void single_segment_classification_boundaries_are_correct(int maxAuthAlt, AirwayAltitudeClass expected)
	{
		var (altitudeClass, _) = AirwayClassifier.Classify([Seg(maxAuthAlt)]);

		Assert.Equal(expected, altitudeClass);
	}

	[Fact]
	public void no_segment_with_a_value_classifies_as_other()
	{
		var (altitudeClass, maxAuthAlt) = AirwayClassifier.Classify([Seg(null), Seg(null)]);

		Assert.Equal(AirwayAltitudeClass.Other, altitudeClass);
		Assert.Null(maxAuthAlt);
	}

	[Fact]
	public void classification_uses_the_highest_segment_not_the_first_or_last()
	{
		var (altitudeClass, maxAuthAlt) = AirwayClassifier.Classify([Seg(5000), Seg(18000), Seg(9000)]);

		Assert.Equal(AirwayAltitudeClass.High, altitudeClass);
		Assert.Equal(18000, maxAuthAlt);
	}

	[Fact]
	public void an_airway_is_never_split_it_gets_exactly_one_classification()
	{
		// A mixed-altitude airway (some Low segments, one High segment) still classifies as a
		// single class - the highest one - rather than being split by altitude.
		var (altitudeClass, _) = AirwayClassifier.Classify([Seg(5000), Seg(5000), Seg(20000), Seg(5000)]);

		Assert.Equal(AirwayAltitudeClass.High, altitudeClass);
	}

	[Theory]
	[InlineData("J3", "J")]
	[InlineData("V23", "V")]
	[InlineData("AT1", "AT")]
	[InlineData("Q100", "Q")]
	[InlineData("T295", "T")]
	[InlineData("j146", "J")]     // lower-case ID -> upper-cased designation
	[InlineData(" V16 ", "V")]    // surrounding whitespace is trimmed
	public void designation_is_the_leading_letters_of_the_awy_id_upper_cased(string awyId, string expected)
	{
		Assert.Equal(expected, AirwayClassifier.DeriveDesignation(awyId));
	}

	[Theory]
	[InlineData("123")]
	[InlineData("")]
	[InlineData(null)]
	public void an_awy_id_with_no_leading_letters_is_grouped_under_unknown(string? awyId)
	{
		Assert.Equal(AirwayClassifier.UnknownDesignation, AirwayClassifier.DeriveDesignation(awyId));
	}
}
