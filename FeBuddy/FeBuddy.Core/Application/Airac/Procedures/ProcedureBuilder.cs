using System.Globalization;

using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Domain.Procedures.Models;
using FeBuddy.Core.Infrastructure.Dtpp;
using FeBuddy.Core.Infrastructure.Dtpp.Models;
using FeBuddy.Core.Infrastructure.Nasr;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using DtppAirport = FeBuddy.Core.Infrastructure.Dtpp.Models.DtppMetafileXmlDataModel.Airport;
using DtppRecord = FeBuddy.Core.Infrastructure.Dtpp.Models.DtppMetafileXmlDataModel.Record;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// Assembles every <see cref="ProcedureAirport"/> the Procedures sub-service works with: one per
/// airport the FAA d-TPP Metafile lists, joined to NASR <c>APT_BASE</c>/<c>CLS_ARSP</c>, with its
/// chart records folded into <see cref="Procedure"/>s and classified against the previous cycle's
/// metafile.
/// </summary>
/// <remarks>
/// Everything is built once, for the whole metafile, before <c>ProcedureSelection</c> applies the
/// user's facility/ROI/airport/procedure picks - the same "build everything, filter downstream"
/// shape every other AIRAC sub-service builder follows.
/// </remarks>
public static class ProcedureBuilder
{
	/// <summary>
	/// Builds every airport and procedure the metafile lists.
	/// </summary>
	/// <param name="nasr">All parsed NASR CSV data. <c>Apt</c> and <c>ClsArsp</c> must not be null.</param>
	/// <param name="dtpp">The selected cycle's parsed FAA d-TPP Metafile.</param>
	/// <param name="previousDtpp">
	/// The previous cycle's parsed FAA d-TPP Metafile, or <see langword="null"/> when it is not
	/// available - a deleted or re-added procedure's <see cref="Procedure.PreviousPdfName"/> is then
	/// always <see langword="null"/>, and every procedure's <see cref="Procedure.PreviousPages"/> empty.
	/// </param>
	/// <returns>Every airport, in metafile file order.</returns>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="nasr"/>.Apt or .ClsArsp has not been parsed.</exception>
	public static ProcedureBuildResult Build(NasrCsvDataCollection nasr, DtppMetafileDataCollection dtpp, DtppMetafileDataCollection? previousDtpp)
	{
		ArgumentNullException.ThrowIfNull(nasr);
		ArgumentNullException.ThrowIfNull(dtpp);

		if (nasr.Apt is null)
		{
			throw NasrCsvGroups.NotParsed(NasrCsvGroup.Apt, "The Procedure Charts sub-service cannot run without it.");
		}

		if (nasr.ClsArsp is null)
		{
			throw NasrCsvGroups.NotParsed(NasrCsvGroup.ClsArsp, "The Procedure Charts sub-service cannot run without it.");
		}

		// NASR keys APT_BASE on SITE_NO, not ARPT_ID - a duplicated identifier is possible in
		// principle. AirportBuilder already reports that for the Airports sub-service; Procedures
		// just keeps the first, the same tie-break.
		Dictionary<string, AptCsvDataModel.AptBase> aptByIdent = new(StringComparer.OrdinalIgnoreCase);

		foreach (AptCsvDataModel.AptBase row in nasr.Apt.AptBase)
		{
			string id = row.ArptId?.Trim() ?? string.Empty;

			if (id.Length > 0)
			{
				aptByIdent.TryAdd(id, row);
			}
		}

		ILookup<string, ClsArspCsvDataModel.ClsArsp> airspaceByAirport = nasr.ClsArsp.ClsArsp
			.ToLookup(row => row.ArptId?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase);

		ILookup<string, DtppRecord> recordsByAirport = dtpp.Records
			.ToLookup(record => record.AptIdent, StringComparer.OrdinalIgnoreCase);

		ILookup<string, DtppRecord>? previousRecordsByAirport = previousDtpp?.Records
			.ToLookup(record => record.AptIdent, StringComparer.OrdinalIgnoreCase);

		List<ProcedureAirport> airports = new(dtpp.Airports.Count);

		foreach (DtppAirport airportRow in dtpp.Airports)
		{
			aptByIdent.TryGetValue(airportRow.AptIdent, out AptCsvDataModel.AptBase? aptBase);

			ProcedureAirspaceClass airspaceClass = ComputeAirspaceClass(airspaceByAirport[airportRow.AptIdent]);

			IReadOnlyList<DtppRecord> ownRecords = [.. recordsByAirport[airportRow.AptIdent]];

			IReadOnlyList<DtppRecord>? previousOwnRecords = previousRecordsByAirport is null
				? null
				: [.. previousRecordsByAirport[airportRow.AptIdent]];

			airports.Add(new ProcedureAirport
			{
				AptIdent = airportRow.AptIdent,
				IcaoIdent = Normalize(airportRow.IcaoIdent) ?? Normalize(aptBase?.IcaoId),
				Name = airportRow.AirportName,
				City = airportRow.CityName,
				State = airportRow.StateCode,
				IsMilitary = string.Equals(airportRow.Military, "M", StringComparison.OrdinalIgnoreCase),
				Alnum = airportRow.Alnum,
				ResponsibleArtcc = Normalize(aptBase?.RespArtccId),
				AirspaceClass = airspaceClass,
				Latitude = aptBase?.BaseLatDecimal,
				Longitude = aptBase?.BaseLongDecimal,
				Procedures = BuildProcedures(ownRecords, previousOwnRecords),
			});
		}

		return new ProcedureBuildResult(airports);
	}

	/// <summary>
	/// Groups one airport's records by normalized base name, joins each orphan continuation page to
	/// its procedure, and classifies each group.
	/// </summary>
	private static List<Procedure> BuildProcedures(IReadOnlyList<DtppRecord> records, IReadOnlyList<DtppRecord>? previousRecords)
	{
		Dictionary<string, List<DtppRecord>> groupsByName = new(StringComparer.OrdinalIgnoreCase);
		List<string> order = [];

		foreach (DtppRecord record in records)
		{
			string baseName = ProcedureNaming.BaseName(record.ChartName);

			if (!groupsByName.TryGetValue(baseName, out List<DtppRecord>? group))
			{
				group = [];
				groupsByName[baseName] = group;
				order.Add(baseName);
			}

			group.Add(record);
		}

		JoinOrphanPages(groupsByName, order);

		return [.. order.Select(baseName => BuildProcedure(baseName, groupsByName[baseName], previousRecords))];
	}

	/// <summary>
	/// Moves each orphan - a group of continuation pages whose main page is not listed this cycle -
	/// into the group whose main page has the same <c>procuid</c> under another name. The FAA does
	/// this when it renames a procedure and removes a page in the same cycle: MSP's 2611 metafile
	/// lists <c>MINNEAPOLIS NINE, CONT.1</c> as deleted beside <c>MINNEAPOLIS ONE</c> as changed,
	/// both with procuid 1300. An orphan that matches nothing stays its own procedure.
	/// </summary>
	private static void JoinOrphanPages(Dictionary<string, List<DtppRecord>> groupsByName, List<string> order)
	{
		string[] names = [.. order];

		foreach (string orphanName in names)
		{
			List<DtppRecord> orphan = groupsByName[orphanName];

			if (orphan.Any(record => !ProcedureNaming.IsContinuation(record.ChartName)))
			{
				continue;
			}

			string? ownerName = order.FirstOrDefault(name => groupsByName[name].Any(record =>
				!ProcedureNaming.IsContinuation(record.ChartName)
				&& record.ProcUid is { } procUid
				&& orphan.Any(page => page.ProcUid == procUid)));

			if (ownerName is not null)
			{
				groupsByName[ownerName].AddRange(orphan);
				groupsByName.Remove(orphanName);
				order.Remove(orphanName);
			}
		}
	}

	/// <summary>Classifies one group's pages into a single <see cref="Procedure"/>.</summary>
	private static Procedure BuildProcedure(string baseName, List<DtppRecord> group, IReadOnlyList<DtppRecord>? previousRecords)
	{
		// Page order, whatever the metafile's: OrderBy is stable, so a re-added page keeps its
		// records in file order.
		List<DtppRecord> pages = [.. group.OrderBy(record => ProcedureNaming.PageNumber(record.ChartName))];
		List<DtppRecord> mainPages = [.. pages.Where(record => !ProcedureNaming.IsContinuation(record.ChartName))];
		DtppRecord? mainPage = mainPages.Count > 0 ? mainPages[0] : null;

		DtppRecord? changedContinuation = pages.FirstOrDefault(record =>
			ProcedureNaming.IsContinuation(record.ChartName) && !string.IsNullOrEmpty(record.UserAction));

		ProcedureChange change;
		DtppRecord? currentRecord;
		DtppRecord? deletedRecord = null;

		if (mainPages.Any(record => HasAction(record, "D")) && mainPages.Any(record => HasAction(record, "A")))
		{
			// Deleted then re-added within this same metafile: the "A" main page is the chart going
			// forward.
			change = ProcedureChange.ReAdded;
			currentRecord = mainPages.First(record => HasAction(record, "A"));
			deletedRecord = mainPages.First(record => HasAction(record, "D"));
		}
		else if (mainPage is not null && !string.IsNullOrEmpty(mainPage.UserAction))
		{
			change = ActionToChange(mainPage.UserAction);
			currentRecord = change == ProcedureChange.Deleted ? null : mainPage;
			deletedRecord = change == ProcedureChange.Deleted ? mainPage : null;
		}
		else if (changedContinuation is not null && mainPage is not null)
		{
			// Only a continuation page changed, was added or was removed, and the procedure carries
			// on: it is Changed, and each page says what happened to it.
			change = ActionToChange(changedContinuation.UserAction!) == ProcedureChange.None
				? ProcedureChange.None
				: ProcedureChange.Changed;
			currentRecord = mainPage;
		}
		else if (changedContinuation is not null)
		{
			// Continuation pages with no main page this cycle and no procedure to join: the page's
			// own change.
			change = ActionToChange(changedContinuation.UserAction!);
			currentRecord = null;
			deletedRecord = change == ProcedureChange.Deleted ? changedContinuation : null;
		}
		else
		{
			change = ProcedureChange.None;
			currentRecord = mainPage ?? pages[0];
		}

		// With no current record, the procedure is a deleted main page or changed continuation pages.
		DtppRecord infoRecord = currentRecord ?? mainPage ?? changedContinuation!;

		IReadOnlyList<ProcedurePage> previousPages = previousRecords is not null && pages.Any(record => HasAction(record, "D"))
			? FindPreviousPages(previousRecords, pages, baseName, infoRecord.ProcUid)
			: [];

		string? previousPdfName = change is ProcedureChange.Deleted or ProcedureChange.ReAdded
			? previousPages.FirstOrDefault(page => !ProcedureNaming.IsContinuation(page.ChartName))?.PdfName
			: null;

		bool isRemovedFromAirportOnly = deletedRecord is not null
			&& string.Equals(deletedRecord.PdfName, DtppFiles.DeletedFromAirportPdfName, StringComparison.OrdinalIgnoreCase);

		return new Procedure
		{
			Name = baseName,
			ChartCode = group[0].ChartCode,
			ChartSeq = group[0].ChartSeq,
			Pages = [.. pages.Select(record => new ProcedurePage(record.ChartName, record.PdfName, record.UserAction))],
			Change = change,
			CurrentPdfName = currentRecord?.PdfName,
			PreviousPdfName = previousPdfName,
			PreviousPages = previousPages,
			IsRemovedFromAirportOnly = isRemovedFromAirportOnly,
			ProcUid = infoRecord.ProcUid,
			AmdtNum = Normalize(infoRecord.AmdtNum),
			AmdtDate = ParseAmdtDate(infoRecord.AmdtDate),
			ComputerCode = Normalize(infoRecord.Faanfd18),
			Civil = Normalize(infoRecord.Civil),
		};
	}

	/// <summary>
	/// Finds each of a procedure's pages in the previous cycle's metafile (the caller already scoped
	/// <paramref name="previousRecords"/> to the airport), by page number: under the procedure's
	/// base name, then a joined orphan page's own base name, then the same
	/// <see cref="DtppMetafileXmlDataModel.Record.ProcUid"/>. A deletion placeholder is never a match.
	/// </summary>
	/// <returns>At most one page per page number, in page order.</returns>
	private static List<ProcedurePage> FindPreviousPages(
		IReadOnlyList<DtppRecord> previousRecords, List<DtppRecord> pages, string baseName, int? procUid)
	{
		string[] names =
		[
			baseName,
			.. pages
				.Select(page => ProcedureNaming.BaseName(page.ChartName))
				.Where(name => !name.Equals(baseName, StringComparison.OrdinalIgnoreCase))
				.Distinct(StringComparer.OrdinalIgnoreCase),
		];

		List<ProcedurePage> found = [];

		foreach (int pageNumber in pages.Select(page => ProcedureNaming.PageNumber(page.ChartName)).Distinct())
		{
			List<DtppRecord> candidates = [.. previousRecords.Where(record =>
				ProcedureNaming.PageNumber(record.ChartName) == pageNumber
				&& !string.IsNullOrWhiteSpace(record.PdfName)
				&& !DtppFiles.IsPlaceholderPdf(record.PdfName))];

			DtppRecord? match = names
				.Select(name => candidates.FirstOrDefault(record =>
					ProcedureNaming.BaseName(record.ChartName).Equals(name, StringComparison.OrdinalIgnoreCase)))
				.FirstOrDefault(record => record is not null)
				?? candidates.FirstOrDefault(record => procUid is not null && record.ProcUid == procUid);

			if (match is not null)
			{
				found.Add(new ProcedurePage(match.ChartName, match.PdfName, match.UserAction));
			}
		}

		return found;
	}

	private static ProcedureAirspaceClass ComputeAirspaceClass(IEnumerable<ClsArspCsvDataModel.ClsArsp> rows)
	{
		bool hasB = false;
		bool hasC = false;
		bool hasD = false;
		bool hasE = false;

		foreach (ClsArspCsvDataModel.ClsArsp row in rows)
		{
			hasB |= IsYes(row.ClassBAirspace);
			hasC |= IsYes(row.ClassCAirspace);
			hasD |= IsYes(row.ClassDAirspace);
			hasE |= IsYes(row.ClassEAirspace);
		}

		if (hasB) return ProcedureAirspaceClass.B;
		if (hasC) return ProcedureAirspaceClass.C;
		if (hasD) return ProcedureAirspaceClass.D;
		if (hasE) return ProcedureAirspaceClass.E;
		return ProcedureAirspaceClass.None;
	}

	private static bool HasAction(DtppRecord record, string action) =>
		string.Equals(record.UserAction, action, StringComparison.OrdinalIgnoreCase);

	private static ProcedureChange ActionToChange(string action) => action.ToUpperInvariant() switch
	{
		"A" => ProcedureChange.New,
		"C" => ProcedureChange.Changed,
		"D" => ProcedureChange.Deleted,
		_ => ProcedureChange.None,
	};

	private static bool IsYes(string? value) => value is not null && value.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase);

	private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

	private static DateOnly? ParseAmdtDate(string? value) =>
		DateOnly.TryParseExact(value?.Trim(), "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date)
			? date
			: null;
}
