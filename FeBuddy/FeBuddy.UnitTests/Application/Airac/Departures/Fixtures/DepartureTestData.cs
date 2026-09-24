using FeBuddy.Core.Domain.Airports.Models;
using FeBuddy.Core.Domain.Departures.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.UnitTests.Application.Airac.Departures.Fixtures;

/// <summary>
/// Builds small, synthetic <see cref="NasrCsvDataCollection"/> instances for Departures tests,
/// so tests never depend on real FAA CSV files. Holds the real DOTSS2 procedure at LAX (ZLA) as
/// NASR publishes it, with invented but distinct coordinates for each of its points.
/// </summary>
internal static class DepartureTestData
{
	/// <summary>The NASR subscription effective date every fixture row carries by default.</summary>
	public const string CycleDate = "2026/09/03";

	/// <summary>LAX's FAA identifier.</summary>
	public const string LaxId = "LAX";

	/// <summary>LAX's reference-point latitude.</summary>
	public const double LaxLatitude = 33.9425;

	/// <summary>LAX's reference-point longitude.</summary>
	public const double LaxLongitude = -118.408;

	/// <summary>DOTSS2's published name.</summary>
	public const string DotssName = "DOTSS";

	/// <summary>DOTSS2's ARTCC.</summary>
	public const string DotssArtcc = "ZLA";

	/// <summary>DOTSS2's computer code.</summary>
	public const string DotssCode = "DOTSS2.DOTSS";

	/// <summary>The point type NASR publishes for a waypoint, trailing padding included.</summary>
	public const string PaddedWaypointType = "WP   ";

	/// <summary>Every DOTSS2 point with an invented, distinct SoCal coordinate.</summary>
	public static readonly IReadOnlyList<(string Id, double Lat, double Lon)> DotssFixes = new[]
	{
		("DLREY", 33.9310, -118.5020),
		("DOCKR", 33.9120, -118.4950),
		("FABRA", 33.9380, -118.5110),
		("HIIPR", 33.9050, -118.4880),
		("ENNEY", 33.9010, -118.5620),
		("WEILR", 33.8770, -118.5410),
		("ADORE", 33.8640, -118.5230),
		("NAANC", 33.8610, -118.6250),
		("SHAEF", 33.8350, -118.6010),
		("HAYNK", 33.8120, -118.6870),
		("PEVEE", 33.7680, -118.7440),
		("HOLTZ", 33.7020, -118.8310),
		("DOTSS", 33.6150, -118.9420),
		("EYEDL", 33.7840, -119.1560),
		("HOMER", 33.9570, -119.3740),
		("CLEEE", 34.1330, -119.5980),
		("WIILD", 33.5210, -119.1880),
		("BLCKD", 33.4460, -119.4020),
		("CSTWY", 33.3690, -119.6350),
		("CNERY", 33.2920, -119.8610),
	};

	/// <summary>DOTSS2's bodies as DP_RTE lists them: route name then points in sequence.</summary>
	public static readonly IReadOnlyList<(string Name, string RunwayEnd, string[] Points)> DotssBodies = new[]
	{
		("DLREY-DOTSS", "24L", new[] { "DLREY", "ENNEY", "NAANC", "HAYNK", "PEVEE", "HOLTZ", "DOTSS" }),
		("DOCKR-DOTSS", "25R", new[] { "DOCKR", "WEILR", "SHAEF", "PEVEE", "HOLTZ", "DOTSS" }),
		("FABRA-DOTSS", "24R", new[] { "FABRA", "ENNEY", "NAANC", "HAYNK", "PEVEE", "HOLTZ", "DOTSS" }),
		("HIIPR-DOTSS", "25L", new[] { "HIIPR", "ADORE", "SHAEF", "PEVEE", "HOLTZ", "DOTSS" }),
	};

	/// <summary>DOTSS2's transitions as DP_RTE lists them: route name, transition code, then points in sequence.</summary>
	public static readonly IReadOnlyList<(string Name, string Code, string[] Points)> DotssTransitions = new[]
	{
		("CLEEE TRANSITION", "DOTSS2.CLEEE", new[] { "DOTSS", "EYEDL", "HOMER", "CLEEE" }),
		("CNERY TRANSITION", "DOTSS2.CNERY", new[] { "DOTSS", "WIILD", "BLCKD", "CSTWY", "CNERY" }),
	};

	/// <summary>
	/// Builds a <see cref="NasrCsvDataCollection"/> holding only the given DP, FIX, NAV and
	/// APT rows. <c>Dp</c> is always non-null.
	/// </summary>
	public static NasrCsvDataCollection Build(
		IEnumerable<DpCsvDataModel.DpBase>? bases = null,
		IEnumerable<DpCsvDataModel.DpApt>? apts = null,
		IEnumerable<DpCsvDataModel.DpRte>? routes = null,
		IEnumerable<(string Id, double Lat, double Lon)>? fixes = null,
		IEnumerable<(string Id, double Lat, double Lon)>? navaids = null,
		IEnumerable<AptCsvDataModel.AptBase>? airports = null)
	{
		DpCsvDataCollection dpCollection = new();
		dpCollection.DpBase.AddRange(bases ?? Enumerable.Empty<DpCsvDataModel.DpBase>());
		dpCollection.DpApt.AddRange(apts ?? Enumerable.Empty<DpCsvDataModel.DpApt>());
		dpCollection.DpRte.AddRange(routes ?? Enumerable.Empty<DpCsvDataModel.DpRte>());

		FixCsvDataCollection fixCollection = new();

		foreach ((string Id, double Lat, double Lon) fix in fixes ?? Enumerable.Empty<(string, double, double)>())
		{
			fixCollection.FixBase.Add(new FixCsvDataModel.FixBase
			{
				FixId = fix.Id,
				LatDecimal = fix.Lat,
				LongDecimal = fix.Lon
			});
		}

		NavCsvDataCollection navCollection = new();

		foreach ((string Id, double Lat, double Lon) navaid in navaids ?? Enumerable.Empty<(string, double, double)>())
		{
			navCollection.NavBase.Add(new NavCsvDataModel.NavBase
			{
				NavId = navaid.Id,
				LatDecimal = navaid.Lat,
				LongDecimal = navaid.Lon
			});
		}

		AptCsvDataCollection aptCollection = new();
		aptCollection.AptBase.AddRange(airports ?? Enumerable.Empty<AptCsvDataModel.AptBase>());

		return new NasrCsvDataCollection
		{
			Dp = dpCollection,
			Fix = fixCollection,
			Nav = navCollection,
			Apt = aptCollection
		};
	}

	/// <summary>
	/// Builds the DOTSS2 procedure at LAX - DP_BASE, DP_APT and DP_RTE rows - with a FIX_BASE
	/// row for every point and an APT_BASE row for LAX.
	/// </summary>
	public static NasrCsvDataCollection Dotss() =>
		Build(
			bases: new[] { DotssBase() },
			apts: DotssApts(),
			routes: DotssRoutes(),
			fixes: DotssFixes,
			airports: new[] { Airport(LaxId, LaxLatitude, LaxLongitude, "KLAX") });

	/// <summary>DOTSS2's DP_BASE row.</summary>
	public static DpCsvDataModel.DpBase DotssBase() =>
		Base(DotssName, DotssArtcc, DotssCode, amendmentNo: "TWO", amendEffDate: "2017/08/17", servedArpt: LaxId);

	/// <summary>DOTSS2's DP_APT rows, one per body.</summary>
	public static IEnumerable<DpCsvDataModel.DpApt> DotssApts() =>
		DotssBodies.Select(body => Apt(DotssName, DotssArtcc, DotssCode, body.Name, LaxId, body.RunwayEnd)).ToList();

	/// <summary>DOTSS2's DP_RTE rows: every body, then every transition.</summary>
	public static IEnumerable<DpCsvDataModel.DpRte> DotssRoutes()
	{
		List<DpCsvDataModel.DpRte> rows = new();

		foreach ((string Name, string RunwayEnd, string[] Points) body in DotssBodies)
		{
			rows.AddRange(Body(DotssName, DotssArtcc, DotssCode, body.Name, body.Points, $"{LaxId}/{body.RunwayEnd}"));
		}

		foreach ((string Name, string Code, string[] Points) transition in DotssTransitions)
		{
			rows.AddRange(Transition(DotssName, DotssArtcc, DotssCode, transition.Name, transition.Code, transition.Points));
		}

		return rows;
	}

	/// <summary>Builds one DP_BASE row. <paramref name="graphicalDpType"/> of <c>OBSTACLE</c> makes an obstacle departure.</summary>
	public static DpCsvDataModel.DpBase Base(
		string dpName,
		string artcc,
		string computerCode,
		string amendmentNo = "ONE",
		string amendEffDate = CycleDate,
		string graphicalDpType = "SID",
		string servedArpt = "",
		string effDate = CycleDate,
		string rnavFlag = "Y") =>
		new()
		{
			EffDate = effDate,
			DpName = dpName,
			Artcc = artcc,
			DpComputerCode = computerCode,
			AmendmentNo = amendmentNo,
			DpAmendEffDate = amendEffDate,
			RnavFlag = rnavFlag,
			GraphicalDpType = graphicalDpType,
			ServedArpt = servedArpt
		};

	/// <summary>Builds one DP_APT row assigning <paramref name="bodyName"/> to <paramref name="arptId"/>.</summary>
	public static DpCsvDataModel.DpApt Apt(
		string dpName,
		string artcc,
		string computerCode,
		string bodyName,
		string arptId,
		string? rwyEndId = null) =>
		new()
		{
			EffDate = CycleDate,
			DpName = dpName,
			Artcc = artcc,
			DpComputerCode = computerCode,
			BodyName = bodyName,
			AptBodySeq = 1,
			ArptId = arptId,
			RwyEndId = rwyEndId
		};

	/// <summary>Builds the DP_RTE rows of one body, POINT_SEQ 10, 20, 30...</summary>
	public static List<DpCsvDataModel.DpRte> Body(
		string dpName,
		string artcc,
		string computerCode,
		string routeName,
		IReadOnlyList<string> points,
		string? arptRwyAssoc = null,
		string pointType = PaddedWaypointType) =>
		Route(dpName, artcc, computerCode, "BODY", routeName, "", points, arptRwyAssoc, pointType);

	/// <summary>Builds the DP_RTE rows of one transition, POINT_SEQ 10, 20, 30...</summary>
	public static List<DpCsvDataModel.DpRte> Transition(
		string dpName,
		string artcc,
		string computerCode,
		string routeName,
		string transitionCode,
		IReadOnlyList<string> points,
		string pointType = PaddedWaypointType) =>
		Route(dpName, artcc, computerCode, "TRANSITION", routeName, transitionCode, points, null, pointType);

	/// <summary>Builds one APT_BASE row.</summary>
	public static AptCsvDataModel.AptBase Airport(string arptId, double latitude, double longitude, string? icaoId = null) =>
		new()
		{
			ArptId = arptId,
			IcaoId = icaoId,
			BaseLatDecimal = latitude,
			BaseLongDecimal = longitude
		};

	/// <summary>
	/// Invents a distinct coordinate for each identifier, for synthetic procedures whose
	/// geometry does not matter.
	/// </summary>
	public static IReadOnlyList<(string Id, double Lat, double Lon)> SyntheticFixes(params string[] ids) =>
		ids.Select((id, index) => (id, 40.0 + (index * 0.1), -100.0 - (index * 0.1))).ToList();

	/// <summary>
	/// Builds a <see cref="DepartureProcedure"/> directly, for tests that start after
	/// <c>DepartureBuilder.ReadProcedures</c>. It has one body so <c>HasRoutes</c> is true.
	/// </summary>
	public static DepartureProcedure Procedure(
		string codeId = "ABC",
		string artcc = "ZZZ",
		bool isObstacleDeparture = false,
		DateOnly? amendmentEffectiveDate = null,
		string? amendmentEffectiveDateText = null,
		DateOnly? cycleEffectiveDate = null,
		string dpName = "ABC") =>
		new()
		{
			DpName = dpName,
			Artcc = artcc,
			ComputerCode = codeId + "1." + codeId,
			CodeId = codeId,
			AmendmentNo = "ONE",
			AmendmentEffectiveDateText = amendmentEffectiveDateText
				?? amendmentEffectiveDate?.ToString("yyyy/MM/dd", System.Globalization.CultureInfo.InvariantCulture)
				?? string.Empty,
			AmendmentEffectiveDate = amendmentEffectiveDate,
			CycleEffectiveDate = cycleEffectiveDate,
			IsObstacleDeparture = isObstacleDeparture,
			ServedAirports = new[] { "AAA" },
			Bodies = new[]
			{
				new DepartureRawRoute("BODY", DepartureRouteKind.Body, null, new[] { new DepartureRawPoint("ALPHA", "WP") })
			},
			Transitions = Array.Empty<DepartureRawRoute>(),
			BodyNamesByAirport = new Dictionary<string, IReadOnlyList<string>>()
		};

	/// <summary>
	/// Builds a located <see cref="DepartureAirportProcedure"/> directly: one body route through
	/// <paramref name="points"/>.
	/// </summary>
	public static DepartureAirportProcedure AirportProcedure(
		DepartureProcedure procedure,
		string airportId,
		params DeparturePoint[] points) =>
		new()
		{
			Procedure = procedure,
			AirportId = airportId,
			Routes = new[] { new DepartureRoute("BODY", DepartureRouteKind.Body, points) },
			Points = points
		};

	private static List<DpCsvDataModel.DpRte> Route(
		string dpName,
		string artcc,
		string computerCode,
		string portion,
		string routeName,
		string transitionCode,
		IReadOnlyList<string> points,
		string? arptRwyAssoc,
		string pointType)
	{
		List<DpCsvDataModel.DpRte> rows = new(points.Count);

		for (int i = 0; i < points.Count; i++)
		{
			rows.Add(new DpCsvDataModel.DpRte
			{
				EffDate = CycleDate,
				DpName = dpName,
				Artcc = artcc,
				DpComputerCode = computerCode,
				RoutePortionType = portion,
				RouteName = routeName,
				RteBodySeq = 1,
				TransitionComputerCode = transitionCode,
				PointSeq = (i + 1) * 10,
				Point = points[i],
				PointType = pointType,
				NextPoint = i + 1 < points.Count ? points[i + 1] : null,
				ArptRwyAssoc = arptRwyAssoc
			});
		}

		return rows;
	}
}
