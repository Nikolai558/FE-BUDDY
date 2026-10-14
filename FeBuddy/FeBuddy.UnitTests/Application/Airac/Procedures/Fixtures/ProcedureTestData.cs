using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Domain.Procedures.Models;
using FeBuddy.Core.Infrastructure.Dtpp;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Nasr.Parsers;

namespace FeBuddy.UnitTests.Application.Airac.Procedures.Fixtures;

/// <summary>
/// Builds small, real-looking <see cref="NasrCsvDataCollection"/> and
/// <see cref="DtppMetafileDataCollection"/> instances - and the already-built
/// <see cref="ProcedureAirport"/>/<see cref="Procedure"/> objects the selection, ordering and
/// writer tests need - for Procedures tests, so tests never depend on real FAA files.
/// </summary>
internal static class ProcedureTestData
{
	// ---- NASR (APT_BASE / CLS_ARSP) ----

	/// <summary>Builds a <see cref="NasrCsvDataCollection"/> holding only the given APT_BASE and CLS_ARSP rows.</summary>
	public static NasrCsvDataCollection Nasr(
		IEnumerable<AptCsvDataModel.AptBase>? apt = null,
		IEnumerable<ClsArspCsvDataModel.ClsArsp>? clsArsp = null)
	{
		AptCsvDataCollection aptCollection = new();
		aptCollection.AptBase.AddRange(apt ?? []);

		ClsArspCsvDataCollection clsArspCollection = new();
		clsArspCollection.ClsArsp.AddRange(clsArsp ?? []);

		return new NasrCsvDataCollection { Apt = aptCollection, ClsArsp = clsArspCollection };
	}

	/// <summary>Builds one APT_BASE row.</summary>
	public static AptCsvDataModel.AptBase AptBaseRow(
		string arptId,
		string? icaoId = null,
		string? respArtccId = "ZOB",
		double latitude = 40.0,
		double longitude = -80.0) =>
		new()
		{
			ArptId = arptId,
			IcaoId = icaoId,
			RespArtccId = respArtccId!,
			BaseLatDecimal = latitude,
			BaseLongDecimal = longitude,
		};

	/// <summary>Builds one CLS_ARSP row; pass <c>"Y"</c> for each class the airport underlies.</summary>
	public static ClsArspCsvDataModel.ClsArsp ClassAirspaceRow(
		string arptId,
		string? classB = null,
		string? classC = null,
		string? classD = null,
		string? classE = null) =>
		new()
		{
			ArptId = arptId,
			ClassBAirspace = classB,
			ClassCAirspace = classC,
			ClassDAirspace = classD,
			ClassEAirspace = classE,
		};

	// ---- NASR routes: DP, STAR, FIX and NAV (Feature #367) ----

	/// <summary>
	/// Builds a <see cref="NasrCsvDataCollection"/> with every group the procedures-with-a-fix-in-my-facility
	/// rule reads: the routes (<paramref name="departures"/>, <paramref name="arrivals"/>), the fixes and
	/// NAVAIDs their points are, and the airports (also what the documents need).
	/// </summary>
	public static NasrCsvDataCollection RouteNasr(
		IEnumerable<AptCsvDataModel.AptBase>? apt = null,
		IEnumerable<(DpCsvDataModel.DpBase Base, IEnumerable<DpCsvDataModel.DpRte> Points)>? departures = null,
		IEnumerable<(StarCsvDataModel.StarBase Base, IEnumerable<StarCsvDataModel.StarRte> Points)>? arrivals = null,
		IEnumerable<FixCsvDataModel.FixBase>? fixes = null,
		IEnumerable<NavCsvDataModel.NavBase>? navaids = null)
	{
		NasrCsvDataCollection nasr = Nasr(apt);

		DpCsvDataCollection dp = new();
		foreach ((DpCsvDataModel.DpBase dpBase, IEnumerable<DpCsvDataModel.DpRte> points) in departures ?? [])
		{
			dp.DpBase.Add(dpBase);
			dp.DpRte.AddRange(points);
		}

		StarCsvDataCollection star = new();
		foreach ((StarCsvDataModel.StarBase starBase, IEnumerable<StarCsvDataModel.StarRte> points) in arrivals ?? [])
		{
			star.StarBase.Add(starBase);
			star.StarRte.AddRange(points);
		}

		FixCsvDataCollection fix = new();
		fix.FixBase.AddRange(fixes ?? []);

		NavCsvDataCollection nav = new();
		nav.NavBase.AddRange(navaids ?? []);

		nasr.Dp = dp;
		nasr.Star = star;
		nasr.Fix = fix;
		nasr.Nav = nav;
		return nasr;
	}

	/// <summary>One departure (<c>DP_BASE</c> row) and its route, every point on one body.</summary>
	public static (DpCsvDataModel.DpBase Base, IEnumerable<DpCsvDataModel.DpRte> Points) Departure(
		string code, string name, string servedAirports, params (string Id, string Type, string? Region)[] points) =>
		(new DpCsvDataModel.DpBase { DpComputerCode = code, DpName = name, ServedArpt = servedAirports, GraphicalDpType = "SID" },
		[.. points.Select((p, i) => new DpCsvDataModel.DpRte
		{
			DpComputerCode = code, DpName = name, RoutePortionType = "BODY", RouteName = name, PointSeq = (i + 1) * 10,
			Point = p.Id, PointType = p.Type, IcaoRegionCode = p.Region,
		})]);

	/// <summary>One arrival (<c>STAR_BASE</c> row) and its route, every point on one body.</summary>
	public static (StarCsvDataModel.StarBase Base, IEnumerable<StarCsvDataModel.StarRte> Points) Arrival(
		string code, string name, string servedAirports, params (string Id, string Type, string? Region)[] points) =>
		(new StarCsvDataModel.StarBase { StarComputerCode = code, ArrivalName = name, ServedArpt = servedAirports },
		[.. points.Select((p, i) => new StarCsvDataModel.StarRte
		{
			StarComputerCode = code, RoutePortionType = "BODY", RouteName = name, PointSeq = (i + 1) * 10,
			Point = p.Id, PointType = p.Type, IcaoRegionCode = p.Region,
		})]);

	/// <summary>One <c>FIX_BASE</c> row.</summary>
	public static FixCsvDataModel.FixBase FixRow(string id, string? high, string? low, string region = "K6", string useCode = "WP") =>
		new() { FixId = id, IcaoRegionCode = region, FixUseCode = useCode, ArtccIdHigh = high, ArtccIdLow = low!, CountryCode = "US" };

	/// <summary>One <c>NAV_BASE</c> row.</summary>
	public static NavCsvDataModel.NavBase NavRow(string id, string type, string? high, string? low) =>
		new() { NavId = id, NavType = type, HighAltArtccId = high, LowAltArtccId = low, CountryCode = "US" };

	// ---- FAA d-TPP Metafile ----

	/// <summary>Builds a <see cref="DtppMetafileDataCollection"/> holding only the given airports and records.</summary>
	public static DtppMetafileDataCollection Dtpp(
		string cycle,
		IEnumerable<DtppMetafileXmlDataModel.Airport>? airports = null,
		IEnumerable<DtppMetafileXmlDataModel.Record>? records = null,
		DateTime? fromEffectiveUtc = null) =>
		new()
		{
			Cycle = cycle,
			FromEffectiveUtc = fromEffectiveUtc,
			Airports = [.. airports ?? []],
			Records = [.. records ?? []],
		};

	/// <summary>Builds one <c>airport_name</c> row.</summary>
	public static DtppMetafileXmlDataModel.Airport AirportRow(
		string aptIdent,
		string? icaoIdent = null,
		string name = "TEST AIRPORT",
		string city = "TEST CITY",
		string state = "OH",
		string military = "N",
		int alnum = 1) =>
		new()
		{
			AptIdent = aptIdent,
			IcaoIdent = icaoIdent,
			AirportName = name,
			CityName = city,
			StateCode = state,
			StateFullName = string.Empty,
			Volume = string.Empty,
			Military = military,
			Alnum = alnum,
		};

	/// <summary>Builds one <c>record</c> row (a chart page); <paramref name="airportName"/> is the airport it is listed under.</summary>
	public static DtppMetafileXmlDataModel.Record RecordRow(
		string aptIdent,
		int chartSeq,
		string chartCode,
		string chartName,
		string pdfName,
		string? userAction = null,
		int? procUid = null,
		string? civil = null,
		string? amdtNum = null,
		string? amdtDate = null,
		string? faanfd18 = null,
		string airportName = "") =>
		new()
		{
			AptIdent = aptIdent,
			ChartSeq = chartSeq,
			ChartCode = chartCode,
			ChartName = chartName,
			PdfName = pdfName,
			UserAction = userAction,
			ProcUid = procUid,
			Civil = civil,
			AmdtNum = amdtNum,
			AmdtDate = amdtDate,
			Faanfd18 = faanfd18,
			CityName = string.Empty,
			StateCode = string.Empty,
			StateFullName = string.Empty,
			Volume = string.Empty,
			AirportName = airportName,
			Military = "N",
			CnFlg = "N",
			TwoColored = "N",
		};

	// ---- already-built ProcedureAirport / Procedure, for selection/ordering/writer tests ----

	/// <summary>Builds an already-assembled <see cref="ProcedureAirport"/>, bypassing NASR/metafile parsing.</summary>
	public static ProcedureAirport BuiltAirport(
		string aptIdent,
		string? icaoIdent = null,
		string name = "TEST AIRPORT",
		string city = "TEST CITY",
		string state = "OH",
		bool isMilitary = false,
		int alnum = 1,
		string? responsibleArtcc = null,
		ProcedureAirspaceClass airspaceClass = ProcedureAirspaceClass.None,
		double? latitude = null,
		double? longitude = null,
		IReadOnlyList<Procedure>? procedures = null) =>
		new()
		{
			AptIdent = aptIdent,
			IcaoIdent = icaoIdent,
			Name = name,
			City = city,
			State = state,
			IsMilitary = isMilitary,
			Alnum = alnum,
			ResponsibleArtcc = responsibleArtcc,
			AirspaceClass = airspaceClass,
			Latitude = latitude,
			Longitude = longitude,
			Procedures = procedures ?? [],
		};

	/// <summary>
	/// Builds an already-assembled <see cref="Procedure"/>, bypassing d-TPP Metafile classification.
	/// Unless <paramref name="pages"/> is given, it has one page, <paramref name="pdfName"/>, marked
	/// as <paramref name="change"/> says (a re-added one has a deleted and an added first page), and
	/// its current PDF is its first page's. Unless <paramref name="previousPages"/> is given, its
	/// previous pages are <paramref name="previousPdfName"/>'s, if any.
	/// </summary>
	public static Procedure BuiltProcedure(
		string name,
		string chartCode = "IAP",
		int chartSeq = 50750,
		ProcedureChange change = ProcedureChange.None,
		string pdfName = "00100TEST.PDF",
		string? previousPdfName = null,
		bool isRemovedFromAirportOnly = false,
		int? procUid = null,
		string? amdtNum = null,
		DateOnly? amdtDate = null,
		string? computerCode = null,
		string? civil = null,
		IReadOnlyList<ProcedurePage>? pages = null,
		IReadOnlyList<ProcedurePage>? previousPages = null,
		FacilityFixMatch? includedForFacilityFixes = null)
	{
		pages ??= change switch
		{
			ProcedureChange.Changed => [new ProcedurePage(name, pdfName, "C")],
			ProcedureChange.New => [new ProcedurePage(name, pdfName, "A")],
			ProcedureChange.Deleted => [new ProcedurePage(name, DtppFiles.DeletedChartPdfName, "D")],
			ProcedureChange.ReAdded => [new ProcedurePage(name, DtppFiles.DeletedChartPdfName, "D"), new ProcedurePage(name, pdfName, "A")],
			_ => [new ProcedurePage(name, pdfName, null)],
		};

		return new()
		{
			Name = name,
			ChartCode = chartCode,
			ChartSeq = chartSeq,
			Pages = pages,
			Change = change,
			CurrentPdfName = change == ProcedureChange.Deleted
				? null
				: pages.LastOrDefault(page => !ProcedureNaming.IsContinuation(page.ChartName) && page.UserAction != "D")?.PdfName,
			PreviousPdfName = previousPdfName,
			PreviousPages = previousPages ?? (previousPdfName is null ? [] : [new ProcedurePage(name, previousPdfName, null)]),
			IsRemovedFromAirportOnly = isRemovedFromAirportOnly,
			ProcUid = procUid,
			AmdtNum = amdtNum,
			AmdtDate = amdtDate,
			ComputerCode = computerCode,
			Civil = civil,
			IncludedForFacilityFixes = includedForFacilityFixes,
		};
	}
}
