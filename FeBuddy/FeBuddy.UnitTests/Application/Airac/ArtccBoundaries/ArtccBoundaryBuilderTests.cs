using FeBuddy.Core.Application.Airac.ArtccBoundaries;
using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.ArtccBoundaries.Models;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.ArtccBoundaries.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.ArtccBoundaries;

/// <summary>
/// Covers <see cref="ArtccBoundaryBuilder.Read"/>: ring splitting by <c>POINT_SEQ</c> (never by
/// sorting), closing a ring exactly once, skipping a degenerate ring, an unrecognized
/// <c>ALTITUDE</c>, a LocationId with no <c>ARB_BASE</c> row, field trimming/casing, and the final
/// ordering.
/// </summary>
public sealed class ArtccBoundaryBuilderTests
{
	[Fact]
	public void read_rejects_a_null_argument() =>
		Assert.Throws<ArgumentNullException>(() => ArtccBoundaryBuilder.Read(null!));

	[Fact]
	public void read_throws_when_arb_data_was_never_parsed()
	{
		NasrCsvDataCollection data = new(); // Arb is null

		Assert.Throws<InvalidOperationException>(() => ArtccBoundaryBuilder.Read(data));
	}

	[Fact]
	public void zobs_high_and_low_rows_build_one_ring_each()
	{
		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.ZobBaseRow()],
			[.. ArtccBoundaryTestData.ZobHighRows(), .. ArtccBoundaryTestData.ZobLowRows()]);

		ArtccBoundaryBuildAllResult result = ArtccBoundaryBuilder.Read(data);

		Assert.Equal(["ZOB", "ZOB"], result.Rings.Select(r => r.Location.LocationId));
		Assert.Equal([ArtccBoundaryAltitude.High, ArtccBoundaryAltitude.Low], result.Rings.Select(r => r.Altitude));
		Assert.Empty(result.Messages);
	}

	[Fact]
	public void the_zak_unlimited_group_splits_into_a_cta_ring_then_a_fir_ring_where_point_seq_resets()
	{
		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.ZakBaseRow()],
			[.. ArtccBoundaryTestData.ZakCtaRows(), .. ArtccBoundaryTestData.ZakFirRows()]);

		ArtccBoundaryBuildAllResult result = ArtccBoundaryBuilder.Read(data);

		Assert.Equal(2, result.Rings.Count);
		Assert.All(result.Rings, r => Assert.Equal(ArtccBoundaryAltitude.Unlimited, r.Altitude));
		Assert.Equal(["CTA", "FIR"], result.Rings.Select(r => r.Type));
	}

	[Fact]
	public void rings_are_split_where_point_seq_does_not_increase_never_by_sorting_the_rows()
	{
		// The sequence numbers themselves are out of numeric order after the split (30 then 15):
		// if the builder sorted by POINT_SEQ instead of walking file order, the rings built here
		// would come out differently.
		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.ZobBaseRow()],
			[
				ArtccBoundaryTestData.SegRow("ZOB", "HIGH", "ARTCC", 10, 40.0, -81.0),
				ArtccBoundaryTestData.SegRow("ZOB", "HIGH", "ARTCC", 30, 41.0, -82.0),
				ArtccBoundaryTestData.SegRow("ZOB", "HIGH", "ARTCC", 15, 42.0, -83.0), // does not increase -> new ring
				ArtccBoundaryTestData.SegRow("ZOB", "HIGH", "ARTCC", 50, 43.0, -84.0),
			]);

		ArtccBoundaryBuildAllResult result = ArtccBoundaryBuilder.Read(data);

		Assert.Equal(2, result.Rings.Count);
		Assert.Equal([40.0, 41.0], result.Rings[0].Points.Take(2).Select(p => p.Latitude));
		Assert.Equal([42.0, 43.0], result.Rings[1].Points.Take(2).Select(p => p.Latitude));
	}

	[Fact]
	public void a_ring_is_closed_by_appending_its_first_point_when_nasr_does_not_repeat_it()
	{
		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.ZobBaseRow()],
			ArtccBoundaryTestData.ZobLowRows()); // 3 rows, not closed by NASR

		ArtccBoundaryRing ring = Assert.Single(ArtccBoundaryBuilder.Read(data).Rings);

		Assert.Equal(4, ring.Points.Count);
		Assert.Equal(ring.Points[0], ring.Points[^1]);
	}

	[Fact]
	public void a_ring_nasr_already_closes_is_not_closed_a_second_time()
	{
		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.ZobBaseRow()],
			ArtccBoundaryTestData.ZobHighRows()); // 4 rows, NASR already repeats the first point

		ArtccBoundaryRing ring = Assert.Single(ArtccBoundaryBuilder.Read(data).Rings);

		Assert.Equal(4, ring.Points.Count);
		Assert.Equal(ring.Points[0], ring.Points[^1]);
	}

	[Fact]
	public void a_ring_with_fewer_than_two_distinct_points_is_skipped_with_an_info_message()
	{
		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.ZobBaseRow()],
			[
				ArtccBoundaryTestData.SegRow("ZOB", "HIGH", "ARTCC", 10, 40.0, -81.0),
				ArtccBoundaryTestData.SegRow("ZOB", "HIGH", "ARTCC", 20, 40.0, -81.0), // the very same point
			]);

		ArtccBoundaryBuildAllResult result = ArtccBoundaryBuilder.Read(data);

		Assert.Empty(result.Rings);
		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Info, message.Level);
		Assert.Contains("ZOB", message.Text);
		Assert.Contains("HIGH", message.Text);
	}

	[Fact]
	public void an_unknown_altitude_warns_once_per_value_and_skips_its_rows()
	{
		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.ZobBaseRow()],
			[
				ArtccBoundaryTestData.SegRow("ZOB", "MEDIUM", "ARTCC", 10, 40.0, -81.0),
				ArtccBoundaryTestData.SegRow("ZOB", "MEDIUM", "ARTCC", 20, 41.0, -82.0),
				ArtccBoundaryTestData.SegRow("ZOB", "MEDIUM", "ARTCC", 30, 42.0, -83.0),
			]);

		ArtccBoundaryBuildAllResult result = ArtccBoundaryBuilder.Read(data);

		Assert.Empty(result.Rings);
		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Warning, message.Level);
		Assert.Contains("MEDIUM", message.Text);
	}

	[Fact]
	public void two_different_unknown_altitude_values_each_get_their_own_warning()
	{
		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.ZobBaseRow()],
			[
				ArtccBoundaryTestData.SegRow("ZOB", "MEDIUM", "ARTCC", 10, 40.0, -81.0),
				ArtccBoundaryTestData.SegRow("ZOB", "SUPERHIGH", "ARTCC", 10, 40.0, -81.0),
			]);

		ArtccBoundaryBuildAllResult result = ArtccBoundaryBuilder.Read(data);

		Assert.Equal(2, result.Messages.Count);
		Assert.Contains(result.Messages, m => m.Text.Contains("MEDIUM"));
		Assert.Contains(result.Messages, m => m.Text.Contains("SUPERHIGH"));
	}

	[Fact]
	public void a_location_id_with_segments_but_no_arb_base_row_warns_once_and_uses_the_segments_location_name()
	{
		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(baseRows: null, segRows: ArtccBoundaryTestData.ZzzRows());

		ArtccBoundaryBuildAllResult result = ArtccBoundaryBuilder.Read(data);

		ArtccBoundaryRing ring = Assert.Single(result.Rings);
		Assert.Equal("ZZZ", ring.Location.LocationId);
		Assert.Equal("MYSTERY CENTER", ring.Location.LocationName);
		Assert.Equal(string.Empty, ring.Location.ComputerId);
		Assert.Equal(string.Empty, ring.Location.IcaoId);
		Assert.Equal(string.Empty, ring.Location.LocationType);

		ServiceMessage warning = Assert.Single(result.Messages, m => m.Level == LogLevel.Warning);
		Assert.Contains("ZZZ", warning.Text);
		Assert.Contains("no ARB_BASE row", warning.Text);
	}

	[Fact]
	public void a_location_id_with_no_arb_base_row_warns_only_once_even_across_multiple_rings()
	{
		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			baseRows: null,
			segRows:
			[
				.. ArtccBoundaryTestData.ZzzRows(),
				ArtccBoundaryTestData.SegRow("ZZZ", "LOW", "ARTCC", 10, 20.0, -20.0, locationName: "MYSTERY CENTER"),
				ArtccBoundaryTestData.SegRow("ZZZ", "LOW", "ARTCC", 20, 21.0, -21.0, locationName: "MYSTERY CENTER"),
				ArtccBoundaryTestData.SegRow("ZZZ", "LOW", "ARTCC", 30, 20.0, -22.0, locationName: "MYSTERY CENTER"),
			]);

		ArtccBoundaryBuildAllResult result = ArtccBoundaryBuilder.Read(data);

		Assert.Equal(2, result.Rings.Count);
		Assert.Single(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("ZZZ"));
	}

	[Fact]
	public void base_fields_are_trimmed_and_only_the_location_id_is_upper_cased()
	{
		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.BaseRow(
				" zob ", "  Cleveland  ", locationType: " artcc ", computerId: " zob ",
				icaoId: " kzob ", city: " Cleveland ", countryCode: " us ")],
			ArtccBoundaryTestData.ZobHighRows());

		ArtccBoundaryRing ring = Assert.Single(ArtccBoundaryBuilder.Read(data).Rings);

		Assert.Equal("ZOB", ring.Location.LocationId); // trimmed AND upper-cased
		Assert.Equal("Cleveland", ring.Location.LocationName); // trimmed only
		Assert.Equal("artcc", ring.Location.LocationType); // trimmed only, case kept
		Assert.Equal("zob", ring.Location.ComputerId);
		Assert.Equal("kzob", ring.Location.IcaoId);
		Assert.Equal("Cleveland", ring.Location.City);
		Assert.Equal("us", ring.Location.CountryCode);
	}

	[Fact]
	public void a_rings_type_is_trimmed_from_its_first_rows_type()
	{
		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.ZobBaseRow()],
			[
				ArtccBoundaryTestData.SegRow("ZOB", "HIGH", "  ARTCC  ", 10, 40.0, -81.0),
				ArtccBoundaryTestData.SegRow("ZOB", "HIGH", "ARTCC", 20, 41.0, -82.0),
				ArtccBoundaryTestData.SegRow("ZOB", "HIGH", "ARTCC", 30, 42.0, -83.0),
			]);

		ArtccBoundaryRing ring = Assert.Single(ArtccBoundaryBuilder.Read(data).Rings);

		Assert.Equal("ARTCC", ring.Type);
	}

	[Fact]
	public void rings_are_ordered_by_location_id_then_altitude_then_ring_order()
	{
		// ZAK before ZOB, and ZOB's LOW rows before its HIGH rows: neither reflects the expected
		// final order, which must come from the sort, not from file order.
		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			ArtccBoundaryTestData.AllBaseRows(),
			[
				.. ArtccBoundaryTestData.ZakCtaRows(), .. ArtccBoundaryTestData.ZakFirRows(),
				.. ArtccBoundaryTestData.ZobLowRows(), .. ArtccBoundaryTestData.ZobHighRows(),
			]);

		ArtccBoundaryBuildAllResult result = ArtccBoundaryBuilder.Read(data);

		Assert.Equal(
			[
				("ZAK", ArtccBoundaryAltitude.Unlimited, "CTA"),
				("ZAK", ArtccBoundaryAltitude.Unlimited, "FIR"),
				("ZOB", ArtccBoundaryAltitude.High, "ARTCC"),
				("ZOB", ArtccBoundaryAltitude.Low, "ARTCC"),
			],
			result.Rings.Select(r => (r.Location.LocationId, r.Altitude, r.Type)));
	}

	[Fact]
	public void blank_location_id_rows_are_skipped_in_both_files()
	{
		NasrCsvDataCollection data = ArtccBoundaryTestData.Build(
			[ArtccBoundaryTestData.BaseRow(" ", "BLANK"), ArtccBoundaryTestData.ZobBaseRow()],
			[
				ArtccBoundaryTestData.SegRow(" ", "HIGH", "ARTCC", 10, 1.0, 1.0),
				.. ArtccBoundaryTestData.ZobHighRows(),
			]);

		ArtccBoundaryBuildAllResult result = ArtccBoundaryBuilder.Read(data);

		ArtccBoundaryRing ring = Assert.Single(result.Rings);
		Assert.Equal("ZOB", ring.Location.LocationId);
	}
}
