using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Domain.Geo.Models;

/// <summary>
/// Covers <see cref="RegionOfInterest"/>: whether it crosses the antimeridian (AM), and the one or two
/// boxes within -180..180 it clips with.
/// </summary>
public sealed class RegionOfInterestTests
{
	[Theory]
	[InlineData(-206.0, -124.0, true)]
	[InlineData(154.0, 236.0, true)]
	[InlineData(170.0, 190.0, true)]
	[InlineData(-85.0, -78.0, false)]
	[InlineData(-200.0, -190.0, false)] // 160 … 170, written past -180
	[InlineData(170.0, 180.0, false)] // up to the AM, not across it
	[InlineData(180.0, 200.0, false)] // -180 … -160, written from the AM
	public void it_knows_whether_it_crosses_the_antimeridian(double swLon, double neLon, bool crosses)
	{
		Assert.Equal(crosses, new RegionOfInterest(44, swLon, 90, neLon).CrossesAntimeridian);
	}

	[Fact]
	public void across_the_antimeridian_it_is_two_boxes_either_side_of_it()
	{
		IReadOnlyList<Envelope> boxes = new RegionOfInterest(44, -206, 90, -124).ToEnvelopes();

		Assert.Equal([new Envelope(154, 180, 44, 90), new Envelope(-180, -124, 44, 90)], boxes);
	}

	[Theory]
	[InlineData(-85.0, -78.0, -85.0, -78.0)]
	[InlineData(-200.0, -190.0, 160.0, 170.0)]
	[InlineData(180.0, 200.0, -180.0, -160.0)]
	public void otherwise_it_is_one_box_within_180(double swLon, double neLon, double west, double east)
	{
		Envelope box = Assert.Single(new RegionOfInterest(44, swLon, 90, neLon).ToEnvelopes());

		Assert.Equal(new Envelope(west, east, 44, 90), box);
	}
}
