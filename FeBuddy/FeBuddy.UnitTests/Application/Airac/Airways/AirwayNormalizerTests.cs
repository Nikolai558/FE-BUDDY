using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Domain.Airways.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.UnitTests.Application.Airac.Airways;

public class AirwayNormalizerTests
{
	private static AwyCsvDataModel.AwySegAlt Seg(
		int seq, string fromPoint, string? fromPtType, string? toPoint,
		string gapFlag = "N", int? maxAuthAlt = null) =>
		new()
		{
			PointSeq = seq,
			FromPoint = fromPoint,
			FromPtType = fromPtType,
			ToPoint = toPoint,
			AwySegGapFlag = gapFlag,
			MaxAuthAlt = maxAuthAlt
		};

	/// <summary>
	/// The exact TIJ / U.S. MEXICAN BORDER-2 / TEYON example from the build plan: a
	/// reference-only middle point is collapsed out.
	/// </summary>
	[Fact]
	public void reference_only_points_are_collapsed_into_a_direct_segment()
	{
		List<AwyCsvDataModel.AwySegAlt> raw = new()
		{
			Seg(10, "TIJ", "VOR", "U.S. MEXICAN BORDER-2"),
			Seg(20, "U.S. MEXICAN BORDER-2", null, "TEYON"),
		};

		List<AirwaySegment> result = AirwayNormalizer.Normalize(raw);

		var segment = Assert.Single(result);
		Assert.Equal("TIJ", segment.StartWptId);
		Assert.Equal("TEYON", segment.EndWptId);
	}

	[Fact]
	public void gap_flag_on_a_collapsed_record_is_preserved()
	{
		List<AwyCsvDataModel.AwySegAlt> raw = new()
		{
			Seg(10, "AAA", "WP", "BORDER-X"),
			Seg(20, "BORDER-X", null, "BBB", gapFlag: "Y"),
		};

		var result = AirwayNormalizer.Normalize(raw);

		Assert.True(Assert.Single(result).IsGap);
	}

	[Fact]
	public void a_reference_only_leading_record_does_not_produce_its_own_segment()
	{
		List<AwyCsvDataModel.AwySegAlt> raw = new()
		{
			Seg(10, "BORDER-X", null, "AAA"),
		};

		var result = AirwayNormalizer.Normalize(raw);

		Assert.Empty(result);
	}

	[Fact]
	public void highest_max_auth_alt_among_collapsed_records_is_carried_through()
	{
		List<AwyCsvDataModel.AwySegAlt> raw = new()
		{
			Seg(10, "AAA", "WP", "BORDER-X", maxAuthAlt: 5000),
			Seg(20, "BORDER-X", null, "BBB", maxAuthAlt: 18000),
		};

		var result = AirwayNormalizer.Normalize(raw);

		Assert.Equal(18000, Assert.Single(result).MaxAuthAlt);
	}

	/// <summary>
	/// The J5 terminator-row case from the plan: NASR closes a border-terminating airway with
	/// a row that has a blank TO_POINT. The segment left ending at the border marker
	/// (CFDCT -&gt; U.S. CANADIAN BORDER-4) is dropped, so the airway ends at CFDCT with no
	/// warning (remediation plan 3.2b).
	/// </summary>
	[Fact]
	public void a_border_terminator_row_does_not_leave_a_segment_ending_at_the_border_marker()
	{
		List<AwyCsvDataModel.AwySegAlt> raw = new()
		{
			Seg(200, "CFJCC", "CN", "CFDCT"),
			Seg(210, "CFDCT", "CN", "U.S. CANADIAN BORDER-4"),
			Seg(220, "U.S. CANADIAN BORDER-4", null, ""), // terminator: blank ToPoint, blank type
		};

		var result = AirwayNormalizer.Normalize(raw);

		var segment = Assert.Single(result);
		Assert.Equal("CFJCC", segment.StartWptId);
		Assert.Equal("CFDCT", segment.EndWptId);
		Assert.DoesNotContain(result, s => s.EndWptId.Contains("BORDER"));
	}

	[Fact]
	public void unrelated_segments_are_kept_separate_not_merged()
	{
		List<AwyCsvDataModel.AwySegAlt> raw = new()
		{
			Seg(10, "AAA", "WP", "BBB"),
			Seg(20, "BBB", "WP", "CCC"),
		};

		var result = AirwayNormalizer.Normalize(raw);

		Assert.Equal(2, result.Count);
	}
}
