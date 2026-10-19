using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Navaids;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.UnitTests.Application.Airac;

/// <summary>
/// Covers <see cref="AiracCycleOptionListBuilder"/>: the options each AIRAC Service tab offers for a
/// cycle, worked out in Core by the builders' own rules, kept once per cycle's data, and empty for a
/// table the data doesn't hold.
/// </summary>
public sealed class AiracCycleOptionListBuilderTests
{
	private static AiracCycleOptionLists Lists(Action<NasrCsvDataCollection> fill)
	{
		NasrCsvDataCollection data = new();
		fill(data);
		return AiracCycleOptionListBuilder.Build(data);
	}

	[Fact]
	public void with_no_tables_every_list_is_empty()
	{
		AiracCycleOptionLists lists = AiracCycleOptionListBuilder.Build(new NasrCsvDataCollection());

		Assert.Empty(lists.AirwayDesignations);
		Assert.Empty(lists.StarArtccs);
		Assert.Empty(lists.DepartureArtccs);
		Assert.Empty(lists.NavaidTypes);
		Assert.Empty(lists.BoundaryLocations);
		Assert.Empty(lists.BoundaryLocationAltitudes);
		Assert.Empty(lists.FixUseNames);
		Assert.Empty(lists.FixCharts);
		Assert.False(lists.HasFixOnNoChart);
		Assert.Empty(lists.AirportFacilities);
		Assert.Empty(lists.FacilityNames);
		Assert.Empty(lists.AirportFaaIdsByIdent);
		Assert.Empty(lists.AirportsByFaaId);
	}

	[Fact]
	public void airway_designations_are_each_airways_letters_once_and_sorted()
	{
		AiracCycleOptionLists lists = Lists(data =>
		{
			data.Awy = new AwyCsvDataCollection();
			foreach (string id in new[] { "V12", "J60", "j80", "Q1" })
			{
				data.Awy.AwyBase.Add(new AwyCsvDataModel.AwyBase { AwyId = id });
			}
		});

		Assert.Equal(["J", "Q", "V"], lists.AirwayDesignations);
	}

	/// <summary>A STAR two centres share names both in STAR_BASE.ARTCC; each is offered once.</summary>
	[Fact]
	public void star_artccs_split_a_shared_stars_centres()
	{
		AiracCycleOptionLists lists = Lists(data =>
		{
			data.Star = new StarCsvDataCollection();
			data.Star.StarBase.Add(new StarCsvDataModel.StarBase { Artcc = "ZNY ZDC" });
			data.Star.StarBase.Add(new StarCsvDataModel.StarBase { Artcc = " zob " });
			data.Star.StarBase.Add(new StarCsvDataModel.StarBase { Artcc = "ZDC" });
			data.Dp = new DpCsvDataCollection();
			data.Dp.DpBase.Add(new DpCsvDataModel.DpBase { Artcc = "zau" });
			data.Dp.DpBase.Add(new DpCsvDataModel.DpBase { Artcc = " " });
		});

		Assert.Equal(["ZDC", "ZNY", "ZOB"], lists.StarArtccs);
		Assert.Equal(["ZAU"], lists.DepartureArtccs);
	}

	/// <summary>Shut-down NAVAIDs are never drawn, so their types aren't offered; known types come first, in their usual order.</summary>
	[Fact]
	public void navaid_types_leave_out_shut_down_navaids_and_list_known_types_first()
	{
		AiracCycleOptionLists lists = Lists(data =>
		{
			data.Nav = new NavCsvDataCollection();
			data.Nav.NavBase.Add(new NavCsvDataModel.NavBase { NavType = "NDB", NavStatus = "OPERATIONAL IFR" });
			data.Nav.NavBase.Add(new NavCsvDataModel.NavBase { NavType = "ZZZ", NavStatus = "OPERATIONAL IFR" });
			data.Nav.NavBase.Add(new NavCsvDataModel.NavBase { NavType = " vor ", NavStatus = "OPERATIONAL IFR" });
			data.Nav.NavBase.Add(new NavCsvDataModel.NavBase { NavType = "TACAN", NavStatus = " shutdown " });
		});

		Assert.Equal(["VOR", "NDB", "ZZZ"], lists.NavaidTypes);
		Assert.Throws<ArgumentNullException>(() => NavaidBuilder.IsShutDown(null!));
	}

	[Fact]
	public void boundary_altitudes_are_by_location_then_high_low_unlimited_and_unknown_ones_are_left_out()
	{
		AiracCycleOptionLists lists = Lists(data =>
		{
			data.Arb = new ArbCsvDataCollection();
			foreach ((string location, string altitude) in new[] { ("ZOB", "UNLIMITED"), ("zau", "low"), ("ZOB", "HIGH"), ("ZOB", "HIGH"), ("ZAU", "HIGH"), ("ZNY", "FIR ONLY") })
			{
				data.Arb.ArbSeg.Add(new ArbCsvDataModel.ArbSeg { LocationId = location, Altitude = altitude });
			}
		});

		Assert.Equal(["ZAU", "ZNY", "ZOB"], lists.BoundaryLocations);
		Assert.Equal([("ZAU", "HIGH"), ("ZAU", "LOW"), ("ZOB", "HIGH"), ("ZOB", "UNLIMITED")], lists.BoundaryLocationAltitudes);
	}

	[Fact]
	public void fix_uses_list_known_ones_first_and_charts_carry_their_tokens()
	{
		AiracCycleOptionLists lists = Lists(data =>
		{
			data.Fix = new FixCsvDataCollection();
			data.Fix.FixBase.Add(new FixCsvDataModel.FixBase { FixUseCode = "WP", Charts = "ENROUTE LOW,IAP" });
			data.Fix.FixBase.Add(new FixCsvDataModel.FixBase { FixUseCode = "ZZ", Charts = "iap, ENROUTE HIGH" });
			data.Fix.FixBase.Add(new FixCsvDataModel.FixBase { FixUseCode = "RP", Charts = null });
		});

		Assert.Equal(["RPRTNG-PNT", "WYPNT", "ZZ"], lists.FixUseNames);
		Assert.Equal([("ENROUTE-HIGH", "ENROUTE HIGH"), ("ENROUTE-LOW", "ENROUTE LOW"), ("IAP", "IAP")], lists.FixCharts);
		Assert.True(lists.HasFixOnNoChart);
	}

	[Fact]
	public void airports_give_their_facilities_and_are_found_by_faa_or_icao_id()
	{
		AptCsvDataModel.AptBase cle = new() { ArptId = "CLE", IcaoId = "KCLE", RespArtccId = "ZOB" };

		AiracCycleOptionLists lists = Lists(data =>
		{
			data.Apt = new AptCsvDataCollection();
			data.Apt.AptBase.Add(cle);
			data.Apt.AptBase.Add(new AptCsvDataModel.AptBase { ArptId = "ORD", RespArtccId = "zau" });
			data.Apt.AptBase.Add(new AptCsvDataModel.AptBase { ArptId = " ", IcaoId = "XXXX", RespArtccId = "ZNY" });
		});

		Assert.Equal(["ZAU", "ZNY", "ZOB"], lists.AirportFacilities);
		Assert.Equal("CLE", lists.AirportFaaIdsByIdent["kcle"]);
		Assert.Equal("ORD", lists.AirportFaaIdsByIdent["ORD"]);
		Assert.False(lists.AirportFaaIdsByIdent.ContainsKey("XXXX"));
		Assert.Same(cle, lists.AirportsByFaaId["cle"]);
	}

	/// <summary>
	/// The Settings page's facility list: each responsible ARTCC once, as the first airport spells it, with
	/// its name or, without one, its ID; sorted by "name (ID)".
	/// </summary>
	[Fact]
	public void the_facilities_are_named_once_each_and_sorted_by_name()
	{
		AiracCycleOptionLists lists = Lists(data =>
		{
			data.Apt = new AptCsvDataCollection();
			data.Apt.AptBase.Add(new AptCsvDataModel.AptBase { ArptId = "CLE", RespArtccId = " ZOB ", ArtccName = " Cleveland " });
			data.Apt.AptBase.Add(new AptCsvDataModel.AptBase { ArptId = "CAK", RespArtccId = "zob", ArtccName = "Other" });
			data.Apt.AptBase.Add(new AptCsvDataModel.AptBase { ArptId = "ORD", RespArtccId = "ZAU", ArtccName = "Chicago" });
			data.Apt.AptBase.Add(new AptCsvDataModel.AptBase { ArptId = "XYZ", RespArtccId = "ZZZ", ArtccName = " " });
			data.Apt.AptBase.Add(new AptCsvDataModel.AptBase { ArptId = "NNN", RespArtccId = " " });
		});

		Assert.Equal([("ZAU", "Chicago"), ("ZOB", "Cleveland"), ("ZZZ", "ZZZ")], lists.FacilityNames);
	}

	/// <summary>A cycle's data never changes, so its lists are built once and kept with it.</summary>
	[Fact]
	public void the_lists_are_built_once_per_cycles_data()
	{
		NasrCsvDataCollection data = new();
		NasrCsvDataCollection other = new();

		Assert.Same(AiracCycleOptionListBuilder.For(data), AiracCycleOptionListBuilder.For(data));
		Assert.NotSame(AiracCycleOptionListBuilder.For(data), AiracCycleOptionListBuilder.For(other));
		Assert.Throws<ArgumentNullException>(() => AiracCycleOptionListBuilder.For(null!));
		Assert.Throws<ArgumentNullException>(() => AiracCycleOptionListBuilder.Build(null!));
	}
}
