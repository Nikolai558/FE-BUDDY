using FEBuddyLibrary.Models.NASR.CSV;
using FEBuddyLibrary.Models.Services.Airac.Airways;
using FEBuddyLibrary.Models.Services.General;
using FEBuddyLibrary.Services.Airac.Airways;

using UnitTests.Services.Airac.Airways.Fixtures;

namespace UnitTests.Services.Airac.Airways;

public class AirwayBuilderTests
{
	private static AirwaySettings MinimalSettings(RegionOfInterest? roi = null) => new()
	{
		OutputDirectory = @"C:\Output",
		OutputBy = AirwayGeojsonOutputBy.HighLow,
		BufferAirwayWaypoints = false,
		IncludeFebCustomProperties = false,
		IncludeAirwayWaypointIds = false,
		GenerateAliasFile = true,
		SplitAtAntimeridian = true,
		IncludeCrcEramPropertyDefaults = false,
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
			fixes: new[] { ("MONPI", 21.0, 140.6), ("OATSS", 16.75, 142.16666666) },
			awyId: "A216",
			awyDesignation: "A",
			awyLocation: "C",
			segments: new[]
			{
				AirwayTestDataBuilder.Segment("A216", 10, "MONPI", "WP", "OATSS", maxAuthAlt: 45000)
			});

		AirwayBuildAllResult result = AirwayBuilder.BuildAll(data, MinimalSettings());

		Airway airway = Assert.Single(result.Airways);
		Assert.Equal("A216", airway.AwyId);
		Assert.Equal("A", airway.Designation);
		Assert.Equal("C", airway.AwyLocation);
		Assert.Equal(AirwayAltitudeClass.High, airway.AltitudeClass);
		Assert.Equal(45000, airway.MaxAuthAlt);
	}

	[Fact]
	public void build_all_resolves_ordered_deduplicated_waypoints_including_the_final_toPoint()
	{
		var data = AirwayTestDataBuilder.Build(
			fixes: new[] { ("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0), ("CCCCC", 42.0, -82.0) },
			awyId: "J1",
			segments: new[]
			{
				AirwayTestDataBuilder.Segment("J1", 10, "AAAAA", "WP", "BBBBB"),
				AirwayTestDataBuilder.Segment("J1", 20, "BBBBB", "WP", "CCCCC"),
			});

		AirwayBuildAllResult result = AirwayBuilder.BuildAll(data, MinimalSettings());

		Airway airway = Assert.Single(result.Airways);
		Assert.Equal(new[] { "AAAAA", "BBBBB", "CCCCC" }, airway.Points.Select(p => p.PointId));
	}

	[Fact]
	public void an_airway_entirely_outside_the_roi_is_excluded()
	{
		var data = AirwayTestDataBuilder.Build(
			fixes: new[] { ("AAAAA", 10.0, 10.0), ("BBBBB", 11.0, 11.0) }, // nowhere near the ROI below
			awyId: "J1",
			segments: new[] { AirwayTestDataBuilder.Segment("J1", 10, "AAAAA", "WP", "BBBBB") });

		RegionOfInterest roi = new(38.0, -85.0, 43.0, -78.0);

		AirwayBuildAllResult result = AirwayBuilder.BuildAll(data, MinimalSettings(roi));

		Assert.Empty(result.Airways);
	}

	[Fact]
	public void duplicate_awy_base_records_for_the_same_id_do_not_throw()
	{
		var data = AirwayTestDataBuilder.Build(
			fixes: new[] { ("AAAAA", 40.0, -80.0), ("BBBBB", 41.0, -81.0) },
			awyId: "J1",
			segments: new[] { AirwayTestDataBuilder.Segment("J1", 10, "AAAAA", "WP", "BBBBB") });

		// Add a second AWY_BASE record for the same AwyId, as duplicate NASR rows sometimes do.
		data.Awy!.AwyBase.Add(new AwyCsvDataModel.AwyBase { AwyId = "J1", AwyDesignation = "J", AwyLocation = "C" });

		AirwayBuildAllResult result = AirwayBuilder.BuildAll(data, MinimalSettings());

		Assert.Single(result.Airways);
	}
}
