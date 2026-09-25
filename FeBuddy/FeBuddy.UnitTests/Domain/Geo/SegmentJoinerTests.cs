using FeBuddy.Core.Domain.Geo;

using NetTopologySuite.Geometries;

namespace FeBuddy.UnitTests.Domain.Geo;

/// <summary>
/// Covers <see cref="SegmentJoiner"/>: joining two-point segments back into lines, including
/// segments written backwards, drawn twice, of no length, or across the antimeridian.
/// </summary>
public sealed class SegmentJoinerTests
{
	private static readonly Coordinate A = new(-100.0, 40.0);
	private static readonly Coordinate B = new(-100.0, 40.5);
	private static readonly Coordinate C = new(-99.5, 40.5);
	private static readonly Coordinate D = new(-99.5, 41.0);

	[Fact]
	public void consecutive_segments_that_meet_become_one_line()
	{
		LineString line = Assert.Single(SegmentJoiner.Join([(A, B), (B, C), (C, D)]));

		Assert.Equal([A, B, C, D], line.Coordinates);
	}

	[Fact]
	public void a_segment_written_backwards_still_continues_the_line()
	{
		LineString line = Assert.Single(SegmentJoiner.Join([(A, B), (C, B), (C, D)]));

		Assert.Equal([A, B, C, D], line.Coordinates);
	}

	[Fact]
	public void segments_that_do_not_meet_stay_separate_lines()
	{
		Assert.Equal(2, SegmentJoiner.Join([(A, B), (C, D)]).Count);
	}

	[Fact]
	public void a_segment_drawn_twice_is_drawn_once()
	{
		LineString line = Assert.Single(SegmentJoiner.Join([(A, B), (A, B), (B, A)]));

		Assert.Equal(2, line.NumPoints);
	}

	[Fact]
	public void zero_length_segments_draw_nothing()
	{
		Assert.Empty(SegmentJoiner.Join([(A, A), (B, B)]));
		Assert.Empty(SegmentJoiner.Join([]));
	}

	[Fact]
	public void a_line_across_the_antimeridian_is_split()
	{
		IReadOnlyList<LineString> lines = SegmentJoiner.Join([(new Coordinate(179.5, 13.5), new Coordinate(-179.5, 13.5))]);

		Assert.Equal(2, lines.Count);
	}

	[Fact]
	public void null_segments_are_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => SegmentJoiner.Join(null!));
	}
}
