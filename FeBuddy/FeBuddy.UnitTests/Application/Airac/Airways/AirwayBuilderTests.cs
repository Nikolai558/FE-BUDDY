using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Domain.Airways.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using FeBuddy.UnitTests.Application.Airac.Airways.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Airways;

/// <summary>
/// Covers <see cref="AirwayBuilder.BuildAll"/>: identity and classification, waypoint
/// resolution, the ROI flags, excluded designations and airways, and border-terminating airways.
/// </summary>
public sealed class AirwayBuilderTests
{
	private static AirwaySettings MinimalSettings(RegionOfInterest? roi = null) => new()
	{
		OutputDirectory = @"C:\Output",
		OutputBy = AirwayGeojsonOutputBy.HighLow,
		BufferAirwayWaypoints = false,
		IncludeFebCustomProperties = false,
		FebProperties = [],
		GenerateAliasFile = true,
		SplitAtAntimeridian = true,
		IncludeCrcLineDefaults = false,
		IncludeCrcSymbolDefaults = false,
		IncludeCrcTextDefaults = false,
		Roi = roi
	};

	[Fact]
	public void build_all_throws_when_awy_data_was_never_parsed()
	{
		NasrCsvDataCollection data = new(); // Awy is null

		Assert.Throws<InvalidOperationException>(() => AirwayBuilder.BuildAll(data, MinimalSettings()));
	}

	[Fact]
	public void build_all_produces_one_airway_with_expected_identity_and_classification()
	{
		var data = AirwayTestDataBuilder.Build(
			fixes: [("MONPI", 21.0, 140.6), ("OATSS", 16.75, 142.16666666)],
			awyId: "A216",
			awyDesignation: "A",
			awyLocation: "C",
			segments:
			[
				AirwayTestDataBuilder.Segment("A216", 10, "MONPI", "WP", "OATSS", maxAuthAlt: 45000)
			]);

		AirwayBuildAllResult result = AirwayBuilder.BuildAll(data, MinimalSettings());

		Airway airway = Assert.Single(result.Airways);
		Assert.Equal("A216", airway.AwyId);
		Assert.Equal("A", airway.Designation);
		Assert.Equal("C", airway.AwyLocation);
		Assert.Equal(AirwayAltitudeClass.High, airway.AltitudeClass);
		Assert.Equal(45000, airway.MaxAuthAlt);
	}

	[Fact]
	public void build_all_resolves_ordered_deduplicated_waypoints_including_the_final_to_point()
	{
		var data = AirwayTestDataBuilder.Build(
			fixes: [("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0), ("CCCCC", 42.0, -82.0)],
			awyId: "J1",
			segments:
			[
				AirwayTestDataBuilder.Segment("J1", 10, "AAAAA", "WP", "BBBBB"),
				AirwayTestDataBuilder.Segment("J1", 20, "BBBBB", "WP", "CCCCC"),
			]);

		AirwayBuildAllResult result = AirwayBuilder.BuildAll(data, MinimalSettings());

		Airway airway = Assert.Single(result.Airways);
		Assert.Equal(["AAAAA", "BBBBB", "CCCCC"], airway.Points.Select(p => p.PointId));
	}

	[Fact]
	public void an_airway_entirely_outside_the_roi_is_kept_but_marked_as_outside_it()
	{
		var data = AirwayTestDataBuilder.Build(
			fixes: [("AAAAA", 10.0, 10.0), ("BBBBB", 11.0, 11.0)], // nowhere near the ROI below
			awyId: "J1",
			segments: [AirwayTestDataBuilder.Segment("J1", 10, "AAAAA", "WP", "BBBBB")]);

		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);

		AirwayBuildAllResult result = AirwayBuilder.BuildAll(data, MinimalSettings(roi));

		// Kept for the alias file's "All" scope; the service keeps it out of the GeoJSON.
		Airway airway = Assert.Single(result.Airways);
		Assert.False(airway.CrossesRoi);
	}

	[Fact]
	public void an_airway_crossing_the_roi_is_marked_as_crossing_it()
	{
		var data = AirwayTestDataBuilder.Build(
			fixes: [("AAAAA", 40.0, -84.0), ("BBBBB", 41.0, -80.0)], // both inside the ROI below
			awyId: "J1",
			segments: [AirwayTestDataBuilder.Segment("J1", 10, "AAAAA", "WP", "BBBBB")]);

		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);

		AirwayBuildAllResult result = AirwayBuilder.BuildAll(data, MinimalSettings(roi));

		Airway airway = Assert.Single(result.Airways);
		Assert.True(airway.CrossesRoi);
	}

	[Fact]
	public void an_excluded_designation_is_dropped_before_any_geometry_work()
	{
		var data = AirwayTestDataBuilder.Build(
			fixes: [("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0)],
			awyId: "V16",
			segments: [AirwayTestDataBuilder.Segment("V16", 10, "AAAAA", "WP", "BBBBB")]);

		AirwaySettings settings = MinimalSettings() with { ExcludedDesignations = ["V"] };

		AirwayBuildAllResult result = AirwayBuilder.BuildAll(data, settings);

		Assert.Empty(result.Airways);
	}

	[Fact]
	public void an_airway_with_a_genuine_mid_route_unresolvable_waypoint_is_excluded_entirely()
	{
		// AAAAA and CCCCC resolve; the middle waypoint MISNG does not, and a resolvable
		// segment lies after it, so this is a real data fault, not a border crossing.
		var data = AirwayTestDataBuilder.Build(
			fixes: [("AAAAA", 40.0, -80.0), ("CCCCC", 42.0, -82.0), ("DDDDD", 43.0, -83.0)],
			awyId: "J146",
			segments:
			[
				AirwayTestDataBuilder.Segment("J146", 10, "AAAAA", "WP", "MISNG"),
				AirwayTestDataBuilder.Segment("J146", 20, "CCCCC", "WP", "DDDDD"),
			]);

		AirwayBuildAllResult result = AirwayBuilder.BuildAll(data, MinimalSettings());

		Assert.Empty(result.Airways);
		Assert.Contains("J146", result.ExcludedAirwayIds);
		Assert.Contains(result.Messages.WarningTexts(), w => w.Contains("J146") && w.Contains("excluded from all output"));
	}

	[Fact]
	public void a_border_terminating_airway_is_built_cleanly_ending_at_its_last_real_waypoint()
	{
		// J5 pattern: ... -> CFDCT -> U.S. CANADIAN BORDER-4, closed by a blank-ToPoint
		// terminator row. The airway must build, ending at CFDCT, with no warning.
		var data = AirwayTestDataBuilder.Build(
			fixes: [("CFJCC", 44.0, -83.0), ("CFDCT", 44.5, -82.5)],
			awyId: "J5",
			segments:
			[
				AirwayTestDataBuilder.Segment("J5", 200, "CFJCC", "CN", "CFDCT"),
				AirwayTestDataBuilder.Segment("J5", 210, "CFDCT", "CN", "U.S. CANADIAN BORDER-4"),
				AirwayTestDataBuilder.Segment("J5", 220, "U.S. CANADIAN BORDER-4", null, ""),
			]);

		AirwayBuildAllResult result = AirwayBuilder.BuildAll(data, MinimalSettings());

		Airway airway = Assert.Single(result.Airways);
		Assert.Equal("J5", airway.AwyId);
		Assert.Empty(result.ExcludedAirwayIds);
		Assert.Empty(result.Messages.WarningTexts());
		Assert.Equal(["CFJCC", "CFDCT"], airway.Points.Select(p => p.PointId));
	}

	[Fact]
	public void duplicate_awy_base_records_for_the_same_id_do_not_throw()
	{
		var data = AirwayTestDataBuilder.Build(
			fixes: [("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0)],
			awyId: "J1",
			segments: [AirwayTestDataBuilder.Segment("J1", 10, "AAAAA", "WP", "BBBBB")]);

		// Add a second AWY_BASE record for the same AwyId, as duplicate NASR rows sometimes do.
		data.Awy!.AwyBase.Add(new AwyCsvDataModel.AwyBase { AwyId = "J1", AwyDesignation = "J", AwyLocation = "C" });

		AirwayBuildAllResult result = AirwayBuilder.BuildAll(data, MinimalSettings());

		Assert.Single(result.Airways);
	}
}
