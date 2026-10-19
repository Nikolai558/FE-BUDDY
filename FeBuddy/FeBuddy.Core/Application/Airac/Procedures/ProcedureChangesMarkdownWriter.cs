using System.Globalization;
using System.Text;

using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Domain.Procedures;
using FeBuddy.Core.Domain.Procedures.Models;
using FeBuddy.Core.Infrastructure.Dtpp;
using FeBuddy.Core.Infrastructure.Dtpp.Models;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// Writes <c>Procedure_Changes.md</c>: a human-readable, facility-by-facility list of every
/// included procedure that changed this cycle, with links to the FAA's chart and compare PDFs -
/// one for each page, by what happened to that page.
/// </summary>
/// <remarks>
/// A chart shared by more than one included airport (chiefly a STAR) is listed under each of them,
/// so every airport's list is complete, with a child line naming the other included airports it
/// also serves - see <see cref="Identity"/>. Regional minimums pages can serve a hundred airports,
/// so that line names the first few and counts the rest. A chart included only because its route
/// has a fix in the user's facility says so on a child line, naming the first few fixes.
/// </remarks>
public static class ProcedureChangesMarkdownWriter
{
	private const string NoteLine =
		"Note: In some cases, the link will return a 404 Error. This is because the FAA does not have a comparative document. " +
		"This is common with Military facilities.";

	/// <summary>How many other airports an "Also serves" line names before it counts the rest.</summary>
	private const int AlsoServesNamed = 10;

	/// <summary>How many fixes an "In ZDC" line names before it counts the rest.</summary>
	private const int FixesNamed = 5;

	/// <summary>
	/// Writes <c>Procedure_Changes.md</c> for the included airports and procedures.
	/// </summary>
	/// <param name="airports">The included airports (post-selection), each with only its included procedures.</param>
	/// <param name="settings">The parsed settings; <see cref="ProcedureSettings.PrimaryFacility"/> is read.</param>
	/// <param name="dtpp">The selected cycle's parsed FAA d-TPP Metafile (its <c>Cycle</c> and effective date).</param>
	/// <param name="previousDtpp">
	/// The previous cycle's parsed FAA d-TPP Metafile, or <see langword="null"/> when it is not
	/// available - only its <c>Cycle</c> is read, and only when a procedure actually carries
	/// <see cref="Procedure.PreviousPages"/> (which cannot happen when this is <see langword="null"/>).
	/// </param>
	/// <returns>The path written.</returns>
	public static string Generate(
		IReadOnlyList<ProcedureAirport> airports,
		ProcedureSettings settings,
		DtppMetafileDataCollection dtpp,
		DtppMetafileDataCollection? previousDtpp)
	{
		ArgumentNullException.ThrowIfNull(airports);
		ArgumentNullException.ThrowIfNull(settings);
		ArgumentNullException.ThrowIfNull(dtpp);

		string cycle = dtpp.Cycle;
		string previousCycle = previousDtpp?.Cycle ?? string.Empty;

		IReadOnlyList<(string Facility, IReadOnlyList<ProcedureAirport> Airports)> facilitySections =
			ProcedureOrdering.GroupByFacility(airports, settings.PrimaryFacility);

		List<ProcedureAirport> orderedAirports = [.. facilitySections.SelectMany(section => section.Airports)];

		(Dictionary<string, List<RenderEntry>> entriesByAirport, int changeCount) = BuildEntries(orderedAirports);

		StringBuilder builder = new();

		builder.AppendLine(Title(cycle, dtpp.FromEffectiveUtc));
		builder.AppendLine();
		builder.AppendLine(NoteLine);

		if (changeCount == 0)
		{
			builder.AppendLine();
			builder.AppendLine("No procedure changes this AIRAC for the selected airports and procedures.");
		}
		else
		{
			foreach ((string facility, IReadOnlyList<ProcedureAirport> facilityAirports) in facilitySections)
			{
				builder.AppendLine();
				builder.AppendLine($"## {facility}");

				List<(ProcedureAirport Airport, List<RenderEntry> Entries)> changedAirports = [.. facilityAirports
					.Select(airport => (Airport: airport, Entries: entriesByAirport.TryGetValue(airport.AptIdent, out List<RenderEntry>? entries) ? entries : []))
					.Where(pair => pair.Entries.Count > 0)];

				if (changedAirports.Count == 0)
				{
					builder.AppendLine("- No procedure changes this AIRAC.");
					continue;
				}

				foreach ((ProcedureAirport airport, List<RenderEntry> entries) in changedAirports)
				{
					WriteAirport(builder, airport, entries, cycle, previousCycle);
				}
			}
		}

		string directory = AiracOutputPaths.PublicationDocsDirectory(settings.OutputDirectory);
		Directory.CreateDirectory(directory);

		string path = Path.Combine(directory, settings.FileNames.FileName(ProcedureOutputFiles.Changes));
		File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

		return path;
	}

	/// <summary>One procedure to render under one airport, with the other included airports it also serves (if any).</summary>
	private sealed record RenderEntry(Procedure Procedure, IReadOnlyList<string> AlsoServes);

	private static string Title(string cycle, DateTime? fromEffectiveUtc) =>
		fromEffectiveUtc is { } effective
			? $"# AIRAC {cycle} ({effective.ToString("ddMMMyyyy", CultureInfo.InvariantCulture).ToUpperInvariant()})"
			: $"# AIRAC {cycle}";

	private static void WriteAirport(StringBuilder builder, ProcedureAirport airport, List<RenderEntry> entries, string cycle, string previousCycle)
	{
		builder.AppendLine($"- {airport.AptIdent}");

		bool allChangedOrReAdded = entries.All(entry => entry.Procedure.Change is ProcedureChange.Changed or ProcedureChange.ReAdded);

		if (allChangedOrReAdded)
		{
			foreach (RenderEntry entry in entries)
			{
				WriteEntry(builder, entry, cycle, previousCycle, indent: "  ");
			}

			return;
		}

		foreach ((string heading, ProcedureChange[] changes) in GroupHeadings)
		{
			List<RenderEntry> group = [.. entries.Where(entry => changes.Contains(entry.Procedure.Change))];

			if (group.Count == 0)
			{
				continue;
			}

			builder.AppendLine($"  - {heading}");

			foreach (RenderEntry entry in group)
			{
				WriteEntry(builder, entry, cycle, previousCycle, indent: "    ");
			}
		}
	}

	/// <summary>The group bullets, in order; <see cref="ProcedureChange.ReAdded"/> folds into "Changed".</summary>
	private static readonly (string Heading, ProcedureChange[] Changes)[] GroupHeadings =
	[
		("Changed:", [ProcedureChange.Changed, ProcedureChange.ReAdded]),
		("Deleted:", [ProcedureChange.Deleted]),
		("New:", [ProcedureChange.New]),
	];

	/// <summary>
	/// Writes one procedure: its name linked by its first page, then a child line linking every
	/// other page, then any notes.
	/// </summary>
	private static void WriteEntry(StringBuilder builder, RenderEntry entry, string cycle, string previousCycle, string indent)
	{
		Procedure procedure = entry.Procedure;
		string childIndent = indent + "  ";

		// Page order, and one entry per page number: a re-added page is listed twice, deleted and added.
		List<IGrouping<int, ProcedurePage>> pages = [.. procedure.Pages
			.GroupBy(page => ProcedureNaming.PageNumber(page.ChartName))
			.OrderBy(page => page.Key)];

		IGrouping<int, ProcedurePage>? firstPage = pages.FirstOrDefault(page => page.Key == 1);
		string[] otherPages = [.. pages.Where(page => page.Key != 1).SelectMany(page => OtherPageLinks(procedure, page, cycle, previousCycle))];

		builder.AppendLine($"{indent}- {FirstLine(procedure, firstPage, cycle, previousCycle)}");

		if (otherPages.Length > 0)
		{
			// A continuation page whose procedure has no first page this cycle lists "Pages", not "More pages".
			builder.AppendLine($"{childIndent}- {(firstPage is null ? "Pages" : "More pages")}: {string.Join(", ", otherPages)}");
		}

		if (procedure.Change == ProcedureChange.ReAdded)
		{
			builder.AppendLine($"{childIndent}- Info: This procedure was initially listed by the FAA as \"Deleted\" but then " +
				"was added as a \"New\" procedure in the d-Tpp MetaFile. Check carefully");
		}
		else if (procedure.Change == ProcedureChange.Deleted && procedure.IsRemovedFromAirportOnly)
		{
			builder.AppendLine($"{childIndent}- Info: The FAA removed it from this airport's list; the procedure itself " +
				"may still be in use elsewhere.");
		}

		// Only there when nothing but its route brought it in, at an airport outside the facility (Features #367, #371).
		if (procedure.IncludedForFacilityFixes is { } facilityFixes)
		{
			builder.AppendLine($"{childIndent}- In {facilityFixes.Facility}: its route has a fix there ({FixList(facilityFixes.Fixes)})");
		}

		if (entry.AlsoServes.Count > 0)
		{
			builder.AppendLine($"{childIndent}- Also serves: {AlsoServes(entry.AlsoServes)}");
		}
	}

	/// <summary>The fixes, e.g. <c>DUCKY, OTTTO</c>, or the first five and <c>and 3 more</c>.</summary>
	private static string FixList(IReadOnlyList<string> fixes) =>
		fixes.Count <= FixesNamed
			? string.Join(", ", fixes)
			: $"{string.Join(", ", fixes.Take(FixesNamed))} and {fixes.Count - FixesNamed} more";

	/// <summary>The other airports, e.g. <c>LGB, SLI, TOA</c>, or the first ten and <c>and 31 more</c>.</summary>
	private static string AlsoServes(IReadOnlyList<string> airports) =>
		airports.Count <= AlsoServesNamed
			? string.Join(", ", airports)
			: $"{string.Join(", ", airports.Take(AlsoServesNamed))} and {airports.Count - AlsoServesNamed} more";

	/// <summary>
	/// The procedure's name, linked by its first page: e.g. <c>[CARTR TWO (RNAV)](…_cmp.pdf)</c>,
	/// <c>[THRNE FOUR (RNAV)](…) (unchanged)</c>, or a re-added procedure's <c>NAME [Old](…) -> [New](…)</c>.
	/// A procedure with no first page this cycle is just its name.
	/// </summary>
	private static string FirstLine(Procedure procedure, IGrouping<int, ProcedurePage>? firstPage, string cycle, string previousCycle)
	{
		if (firstPage is null)
		{
			return procedure.Name;
		}

		if (procedure.Change == ProcedureChange.ReAdded)
		{
			return $"{procedure.Name} {OldToNew(procedure, firstPage, cycle, previousCycle)}";
		}

		(string? url, string? note) = PageTarget(procedure, firstPage.First(), cycle, previousCycle);
		string name = url is null ? procedure.Name : $"[{procedure.Name}]({url})";

		return note is null ? name : $"{name} ({note})";
	}

	/// <summary>
	/// One continuation page's links, e.g. <c>[Page 2](…_cmp.pdf)</c>, <c>[Page 3 (unchanged)](…)</c>,
	/// or a re-added procedure's <c>Page 2 [Old](…) -> [New](…)</c>. Page 2 is <c>CONT.1</c>.
	/// </summary>
	private static IEnumerable<string> OtherPageLinks(Procedure procedure, IGrouping<int, ProcedurePage> page, string cycle, string previousCycle)
	{
		string label = $"Page {page.Key}";

		if (procedure.Change == ProcedureChange.ReAdded)
		{
			return [$"{label} {OldToNew(procedure, page, cycle, previousCycle)}"];
		}

		// Normally one record; two when a joined orphan page shares its number with the procedure's own page.
		return page.Select(record =>
		{
			(string? url, string? note) = PageTarget(procedure, record, cycle, previousCycle);
			string text = note is null ? label : $"{label} ({note})";

			return url is null ? text : $"[{text}]({url})";
		});
	}

	/// <summary>
	/// Where one page links, by its own <c>useraction</c>, and what to note about it: a changed page
	/// links to its compare PDF; an added one to its chart (a "new page" unless the whole procedure
	/// is new); a deleted one to its chart in the previous cycle ("removed" unless the whole
	/// procedure was deleted); and an unchanged one to its chart ("unchanged").
	/// </summary>
	/// <returns>The URL, or <see langword="null"/> when the page has none; and the note, if any.</returns>
	private static (string? Url, string? Note) PageTarget(Procedure procedure, ProcedurePage page, string cycle, string previousCycle) =>
		page.UserAction?.Trim().ToUpperInvariant() switch
		{
			"C" => (DtppFiles.CompareUrl(cycle, page.PdfName), null),
			"A" => (DtppFiles.ChartUrl(cycle, page.PdfName), procedure.Change == ProcedureChange.New ? null : "new page"),
			"D" => RemovedPageTarget(procedure, page, previousCycle),
			_ => (DtppFiles.ChartUrl(cycle, page.PdfName), "unchanged"),
		};

	private static (string? Url, string? Note) RemovedPageTarget(Procedure procedure, ProcedurePage page, string previousCycle)
	{
		bool isProcedureDeleted = procedure.Change == ProcedureChange.Deleted;

		return PreviousPdf(procedure, ProcedureNaming.PageNumber(page.ChartName)) is { } previousPdf
			? (DtppFiles.ChartUrl(previousCycle, previousPdf), isProcedureDeleted ? null : "removed")
			: (null, isProcedureDeleted ? "previous chart not available" : "removed, previous chart not available");
	}

	/// <summary>
	/// A re-added procedure's page: <c>[Old](…) -> [New](…)</c>, the old chart being the previous
	/// cycle's (left out when it cannot be found).
	/// </summary>
	private static string OldToNew(Procedure procedure, IGrouping<int, ProcedurePage> page, string cycle, string previousCycle)
	{
		string oldLink = PreviousPdf(procedure, page.Key) is { } previousPdf
			? $"[Old]({DtppFiles.ChartUrl(previousCycle, previousPdf)}) -> "
			: string.Empty;

		ProcedurePage? current = page.FirstOrDefault(record =>
			!string.Equals(record.UserAction?.Trim(), "D", StringComparison.OrdinalIgnoreCase));

		string newLink = current is { PdfName.Length: > 0 }
			? $"[New]({DtppFiles.ChartUrl(cycle, current.PdfName)})"
			: "[New](chart not available)";

		return oldLink + newLink;
	}

	/// <summary>The page's PDF name in the previous cycle, or <see langword="null"/> when it was not found.</summary>
	private static string? PreviousPdf(Procedure procedure, int pageNumber) =>
		procedure.PreviousPages.FirstOrDefault(page => ProcedureNaming.PageNumber(page.ChartName) == pageNumber)?.PdfName;

	/// <summary>
	/// Walks every included airport's included, changed procedures in document order. Each is listed
	/// under its own airport; a chart more than one included airport lists names the others as
	/// "Also serves", in document order.
	/// </summary>
	/// <returns>Every airport's entries, keyed by <see cref="ProcedureAirport.AptIdent"/>, plus the total change count.</returns>
	private static (Dictionary<string, List<RenderEntry>> EntriesByAirport, int ChangeCount) BuildEntries(
		IReadOnlyList<ProcedureAirport> orderedAirports)
	{
		List<(ProcedureAirport Airport, Procedure Procedure)> changed = [];

		foreach (ProcedureAirport airport in orderedAirports)
		{
			foreach (Procedure procedure in airport.Procedures)
			{
				if (procedure.Change != ProcedureChange.None)
				{
					changed.Add((airport, procedure));
				}
			}
		}

		// Every included airport that lists each chart, once each, in document order.
		Dictionary<string, List<string>> airportsByIdentity = new(StringComparer.OrdinalIgnoreCase);

		foreach ((ProcedureAirport airport, Procedure procedure) in changed)
		{
			string identity = Identity(procedure);

			if (!airportsByIdentity.TryGetValue(identity, out List<string>? airports))
			{
				airports = [];
				airportsByIdentity[identity] = airports;
			}

			if (!airports.Contains(airport.AptIdent, StringComparer.OrdinalIgnoreCase))
			{
				airports.Add(airport.AptIdent);
			}
		}

		Dictionary<string, List<RenderEntry>> entriesByAirport = new(StringComparer.OrdinalIgnoreCase);

		foreach ((ProcedureAirport airport, Procedure procedure) in changed)
		{
			IReadOnlyList<string> alsoServes = [.. airportsByIdentity[Identity(procedure)]
				.Where(other => !other.Equals(airport.AptIdent, StringComparison.OrdinalIgnoreCase))];

			if (!entriesByAirport.TryGetValue(airport.AptIdent, out List<RenderEntry>? entries))
			{
				entries = [];
				entriesByAirport[airport.AptIdent] = entries;
			}

			entries.Add(new RenderEntry(procedure, alsoServes));
		}

		return (entriesByAirport, changed.Count);
	}

	/// <summary>
	/// A changed chart's identity: its main page's PDF name (New/Changed), or the previous cycle's
	/// (Deleted/ReAdded); falling back to chart code + procedure UID + base name when that PDF name
	/// is not available.
	/// </summary>
	private static string Identity(Procedure procedure)
	{
		string? pdfName = procedure.Change is ProcedureChange.Deleted or ProcedureChange.ReAdded
			? procedure.PreviousPdfName
			: procedure.CurrentPdfName;

		return pdfName is { Length: > 0 }
			? pdfName
			: $"{procedure.ChartCode}|{procedure.ProcUid}|{procedure.Name}";
	}
}
