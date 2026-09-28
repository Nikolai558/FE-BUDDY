using System.Globalization;

using FeBuddy.Core.Domain.Arrivals.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.UnitTests.Application.Airac.Arrivals.Fixtures;

/// <summary>
/// Builds small, synthetic <see cref="NasrCsvDataCollection"/> instances for Arrivals tests, so
/// tests never depend on real FAA CSV files. Holds the real BLAID2 STAR at LAS (ZLA) as NASR
/// publishes it, with invented but distinct coordinates for each of its points.
/// </summary>
internal static class ArrivalTestData
{
	/// <summary>The NASR subscription effective date every fixture row carries by default.</summary>
	public const string CycleDate = "2026/09/03";

	/// <summary>LAS's FAA identifier.</summary>
	public const string LasId = "LAS";

	/// <summary>LAS's reference-point latitude.</summary>
	public const double LasLatitude = 36.0840;

	/// <summary>LAS's reference-point longitude.</summary>
	public const double LasLongitude = -115.1537;

	/// <summary>BLAID2's published name.</summary>
	public const string BlaidName = "BLAID";

	/// <summary>BLAID2's ARTCC.</summary>
	public const string BlaidArtcc = "ZLA";

	/// <summary>BLAID2's computer code.</summary>
	public const string BlaidCode = "AALAN.BLAID2";

	/// <summary>The point type NASR publishes for a reporting point, trailing padding included.</summary>
	public const string PaddedFixType = "RP   ";

	/// <summary>BLAID2's fixes (FIX_BASE points) with invented, distinct SoCal/Utah-area coordinates.</summary>
	public static readonly IReadOnlyList<(string Id, double Lat, double Lon)> BlaidFixes =
	[
		("AALAN", 36.4200, -115.3500),
		("HOLDM", 36.6800, -115.5200),
		("BLAID", 36.1400, -115.0900),
	];

	/// <summary>BLAID2's navaids (NAV_BASE points) with invented, distinct coordinates.</summary>
	public static readonly IReadOnlyList<(string Id, double Lat, double Lon)> BlaidNavaids =
	[
		("BCE", 37.6900, -112.1400),
		("EHK", 37.7500, -112.9200),
		("PGA", 36.9300, -111.4500),
	];

	/// <summary>BLAID2's body: <c>AALAN-BLAID</c>, BODY_SEQ 1, assigned to LAS.</summary>
	public static readonly (string Name, (string Id, string PointType)[] Points) BlaidBody =
		("AALAN-BLAID", [("AALAN", PaddedFixType), ("BLAID", PaddedFixType)]);

	/// <summary>BLAID2's transitions as STAR_RTE lists them: route name, transition code, then points in sequence.</summary>
	public static readonly IReadOnlyList<(string Name, string Code, (string Id, string PointType)[] Points)> BlaidTransitions =
	[
		("BRYCE CANYON TRANSITION", "BCE.BLAID2", [("BCE", "VORTAC"), ("HOLDM", PaddedFixType), ("AALAN", PaddedFixType)]),
		("ENOCH TRANSITION", "EHK.BLAID2", [("EHK", "VOR/DME"), ("HOLDM", PaddedFixType), ("AALAN", PaddedFixType)]),
		("PAGE TRANSITION", "PGA.BLAID2", [("PGA", "VOR/DME"), ("HOLDM", PaddedFixType), ("AALAN", PaddedFixType)]),
	];

	/// <summary>
	/// Builds a <see cref="NasrCsvDataCollection"/> holding only the given STAR, FIX, NAV and
	/// APT rows. <c>Star</c> is always non-null.
	/// </summary>
	public static NasrCsvDataCollection Build(
		IEnumerable<StarCsvDataModel.StarBase>? bases = null,
		IEnumerable<StarCsvDataModel.StarApt>? apts = null,
		IEnumerable<StarCsvDataModel.StarRte>? routes = null,
		IEnumerable<(string Id, double Lat, double Lon)>? fixes = null,
		IEnumerable<(string Id, double Lat, double Lon)>? navaids = null,
		IEnumerable<AptCsvDataModel.AptBase>? airports = null)
	{
		StarCsvDataCollection starCollection = new();
		starCollection.StarBase.AddRange(bases ?? []);
		starCollection.StarApt.AddRange(apts ?? []);
		starCollection.StarRte.AddRange(routes ?? []);

		FixCsvDataCollection fixCollection = new();

		foreach ((string Id, double Lat, double Lon) in fixes ?? [])
		{
			fixCollection.FixBase.Add(new FixCsvDataModel.FixBase
			{
				FixId = Id,
				LatDecimal = Lat,
				LongDecimal = Lon
			});
		}

		NavCsvDataCollection navCollection = new();

		foreach ((string Id, double Lat, double Lon) in navaids ?? [])
		{
			navCollection.NavBase.Add(new NavCsvDataModel.NavBase
			{
				NavId = Id,
				LatDecimal = Lat,
				LongDecimal = Lon
			});
		}

		AptCsvDataCollection aptCollection = new();
		aptCollection.AptBase.AddRange(airports ?? []);

		return new NasrCsvDataCollection
		{
			Star = starCollection,
			Fix = fixCollection,
			Nav = navCollection,
			Apt = aptCollection
		};
	}

	/// <summary>
	/// Builds the BLAID2 procedure at LAS - STAR_BASE, STAR_APT and STAR_RTE rows - with a
	/// FIX_BASE row for every fix, a NAV_BASE row for every navaid, and an APT_BASE row for LAS.
	/// </summary>
	public static NasrCsvDataCollection Blaid() =>
		Build(
			bases: [BlaidBase()],
			apts: BlaidApts(),
			routes: BlaidRoutes(),
			fixes: BlaidFixes,
			navaids: BlaidNavaids,
			airports: [Airport(LasId, LasLatitude, LasLongitude, "KLAS")]);

	/// <summary>BLAID2's STAR_BASE row.</summary>
	public static StarCsvDataModel.StarBase BlaidBase() =>
		Base(BlaidName, BlaidArtcc, BlaidCode, amendmentNo: "TWO", amendEffDate: "2024/01/25", servedArpt: LasId);

	/// <summary>BLAID2's STAR_APT rows: its one body, assigned to LAS.</summary>
	public static IEnumerable<StarCsvDataModel.StarApt> BlaidApts() =>
		[Apt(BlaidArtcc, BlaidCode, BlaidBody.Name, LasId, bodySeq: 1, rwyEndId: "ALL")];

	/// <summary>BLAID2's STAR_RTE rows: the body, then every transition.</summary>
	public static IEnumerable<StarCsvDataModel.StarRte> BlaidRoutes()
	{
		List<StarCsvDataModel.StarRte> rows = [.. Body(BlaidArtcc, BlaidCode, BlaidBody.Name, BlaidBody.Points, arptRwyAssoc: LasId)];

		foreach ((string Name, string Code, (string Id, string PointType)[] Points) in BlaidTransitions)
		{
			rows.AddRange(Transition(BlaidArtcc, BlaidCode, Name, Code, Points));
		}

		return rows;
	}

	/// <summary>Builds one STAR_BASE row.</summary>
	public static StarCsvDataModel.StarBase Base(
		string arrivalName,
		string artcc,
		string computerCode,
		string amendmentNo = "ONE",
		string amendEffDate = CycleDate,
		string servedArpt = "",
		string effDate = CycleDate,
		string rnavFlag = "N") =>
		new()
		{
			EffDate = effDate,
			ArrivalName = arrivalName,
			Artcc = artcc,
			StarComputerCode = computerCode,
			AmendmentNo = amendmentNo,
			StarAmendEffDate = amendEffDate,
			RnavFlag = rnavFlag,
			ServedArpt = servedArpt
		};

	/// <summary>
	/// Builds one STAR_APT row assigning <paramref name="bodyName"/> (BODY_SEQ
	/// <paramref name="bodySeq"/>) to <paramref name="arptId"/>. Unlike DP_APT, STAR_APT carries no
	/// arrival name - only the computer code and ARTCC identify which procedure it belongs to.
	/// </summary>
	public static StarCsvDataModel.StarApt Apt(
		string artcc,
		string computerCode,
		string bodyName,
		string arptId,
		int bodySeq = 1,
		string? rwyEndId = null) =>
		new()
		{
			EffDate = CycleDate,
			Artcc = artcc,
			StarComputerCode = computerCode,
			BodyName = bodyName,
			AptBodySeq = bodySeq,
			ArptId = arptId,
			RwyEndId = rwyEndId
		};

	/// <summary>Builds the STAR_RTE rows of one body, every point the same type, POINT_SEQ 10, 20, 30...</summary>
	public static List<StarCsvDataModel.StarRte> Body(
		string artcc,
		string computerCode,
		string routeName,
		IReadOnlyList<string> points,
		int bodySeq = 1,
		string? arptRwyAssoc = null,
		string pointType = PaddedFixType) =>
		Route(artcc, computerCode, "BODY", routeName, "", bodySeq, [.. points.Select(p => (p, pointType))], arptRwyAssoc);

	/// <summary>Builds the STAR_RTE rows of one body, each point carrying its own type, POINT_SEQ 10, 20, 30...</summary>
	public static List<StarCsvDataModel.StarRte> Body(
		string artcc,
		string computerCode,
		string routeName,
		IReadOnlyList<(string Id, string PointType)> points,
		int bodySeq = 1,
		string? arptRwyAssoc = null) =>
		Route(artcc, computerCode, "BODY", routeName, "", bodySeq, points, arptRwyAssoc);

	/// <summary>Builds the STAR_RTE rows of one transition, every point the same type, POINT_SEQ 10, 20, 30...</summary>
	public static List<StarCsvDataModel.StarRte> Transition(
		string artcc,
		string computerCode,
		string routeName,
		string transitionCode,
		IReadOnlyList<string> points,
		int bodySeq = 1,
		string pointType = PaddedFixType) =>
		Route(artcc, computerCode, "TRANSITION", routeName, transitionCode, bodySeq, [.. points.Select(p => (p, pointType))], null);

	/// <summary>Builds the STAR_RTE rows of one transition, each point carrying its own type, POINT_SEQ 10, 20, 30...</summary>
	public static List<StarCsvDataModel.StarRte> Transition(
		string artcc,
		string computerCode,
		string routeName,
		string transitionCode,
		IReadOnlyList<(string Id, string PointType)> points,
		int bodySeq = 1) =>
		Route(artcc, computerCode, "TRANSITION", routeName, transitionCode, bodySeq, points, null);

	/// <summary>Builds one APT_BASE row. <paramref name="respArtccId"/> is read for a multi-ARTCC procedure's <c>ArtccByAirport</c>.</summary>
	public static AptCsvDataModel.AptBase Airport(
		string arptId,
		double latitude,
		double longitude,
		string? icaoId = null,
		string? respArtccId = null) =>
		new()
		{
			ArptId = arptId,
			IcaoId = icaoId,
			BaseLatDecimal = latitude,
			BaseLongDecimal = longitude,
			RespArtccId = respArtccId ?? string.Empty
		};

	/// <summary>
	/// Invents a distinct coordinate for each identifier, for synthetic procedures whose
	/// geometry does not matter.
	/// </summary>
	public static IReadOnlyList<(string Id, double Lat, double Lon)> SyntheticFixes(params string[] ids) =>
		ids.Select((id, index) => (id, 40.0 + (index * 0.1), -100.0 - (index * 0.1))).ToList();

	/// <summary>
	/// Builds an <see cref="ArrivalProcedure"/> directly, for tests that start after
	/// <c>ArrivalBuilder.ReadProcedures</c>. It has one body so <c>HasRoutes</c> is true.
	/// </summary>
	public static ArrivalProcedure Procedure(
		string codeId = "ABC",
		string artcc = "ZZZ",
		DateOnly? amendmentEffectiveDate = null,
		string? amendmentEffectiveDateText = null,
		DateOnly? cycleEffectiveDate = null,
		string arrivalName = "ABC") =>
		new()
		{
			ArrivalName = arrivalName,
			ArtccText = artcc,
			Artccs = artcc.Length > 0 ? [artcc] : [],
			ArtccByAirport = new Dictionary<string, string>(),
			ComputerCode = "TRANS." + codeId + "1",
			CodeId = codeId,
			AmendmentNo = "ONE",
			AmendmentEffectiveDateText = amendmentEffectiveDateText
				?? amendmentEffectiveDate?.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)
				?? string.Empty,
			AmendmentEffectiveDate = amendmentEffectiveDate,
			CycleEffectiveDate = cycleEffectiveDate,
			ServedAirports = ["AAA"],
			Bodies =
			[
				new ArrivalRawRoute("BODY", 1, ArrivalRouteKind.Body, null, [new ArrivalRawPoint("ALPHA", "RP")])
			],
			Transitions = [],
			BodiesByAirport = new Dictionary<string, IReadOnlyList<(string Name, int Sequence)>>()
		};

	/// <summary>
	/// Builds a located <see cref="ArrivalAirportProcedure"/> directly: one body route through
	/// <paramref name="points"/>. <see cref="ArrivalAirportProcedure.Artcc"/> is derived from
	/// <paramref name="procedure"/> via <c>ArtccFor</c>, as <c>ArrivalBuilder.Locate</c> would set it.
	/// </summary>
	public static ArrivalAirportProcedure AirportProcedure(
		ArrivalProcedure procedure,
		string airportId,
		params ArrivalPoint[] points) =>
		new()
		{
			Procedure = procedure,
			AirportId = airportId,
			Artcc = procedure.ArtccFor(airportId),
			Routes = [new ArrivalRoute("BODY", ArrivalRouteKind.Body, points)],
			Points = points
		};

	private static List<StarCsvDataModel.StarRte> Route(
		string artcc,
		string computerCode,
		string portion,
		string routeName,
		string transitionCode,
		int bodySeq,
		IReadOnlyList<(string Id, string PointType)> points,
		string? arptRwyAssoc)
	{
		List<StarCsvDataModel.StarRte> rows = new(points.Count);

		for (int i = 0; i < points.Count; i++)
		{
			rows.Add(new StarCsvDataModel.StarRte
			{
				EffDate = CycleDate,
				Artcc = artcc,
				StarComputerCode = computerCode,
				RoutePortionType = portion,
				RouteName = routeName,
				RteBodySeq = bodySeq,
				TransitionComputerCode = transitionCode,
				PointSeq = (i + 1) * 10,
				Point = points[i].Id,
				PointType = points[i].PointType,
				NextPoint = i + 1 < points.Count ? points[i + 1].Id : null,
				ArptRwyAssoc = arptRwyAssoc
			});
		}

		return rows;
	}
}
