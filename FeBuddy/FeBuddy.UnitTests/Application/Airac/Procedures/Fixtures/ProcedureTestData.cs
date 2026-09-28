using FeBuddy.Core.Domain.Procedures.Models;
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

	/// <summary>Builds an already-assembled <see cref="Procedure"/>, bypassing d-TPP Metafile classification.</summary>
	public static Procedure BuiltProcedure(
		string name,
		string chartCode = "IAP",
		int chartSeq = 50750,
		ProcedureChange change = ProcedureChange.None,
		string? reportedName = null,
		string reportedPdfName = "00100TEST.PDF",
		string? currentPdfName = "00100TEST.PDF",
		string? previousPdfName = null,
		bool isRemovedFromAirportOnly = false,
		int? procUid = null,
		string? amdtNum = null,
		DateOnly? amdtDate = null,
		string? computerCode = null,
		string? civil = null,
		IReadOnlyList<ProcedurePage>? pages = null) =>
		new()
		{
			Name = name,
			ChartCode = chartCode,
			ChartSeq = chartSeq,
			Pages = pages ?? [new ProcedurePage(reportedName ?? name, reportedPdfName, null)],
			Change = change,
			ReportedName = reportedName ?? name,
			ReportedPdfName = reportedPdfName,
			CurrentPdfName = currentPdfName,
			PreviousPdfName = previousPdfName,
			IsRemovedFromAirportOnly = isRemovedFromAirportOnly,
			ProcUid = procUid,
			AmdtNum = amdtNum,
			AmdtDate = amdtDate,
			ComputerCode = computerCode,
			Civil = civil,
		};
}
