using System.Globalization;
using System.Text;

using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Domain.Procedures.Models;
using FeBuddy.Core.Infrastructure.Dtpp;
using FeBuddy.Core.Infrastructure.Dtpp.Models;

namespace FeBuddy.Core.Application.Airac.Procedures;

/// <summary>
/// Writes <c>Procedure_Changes.md</c>: a human-readable, facility-by-facility list of every
/// included procedure that changed this cycle, with links to the FAA's chart and compare PDFs.
/// </summary>
/// <remarks>
/// A chart shared by more than one included airport (chiefly a STAR) is written once, under the
/// airport it belongs to - see <see cref="Identity"/> and <see cref="FindOwnerIndex"/> - with a
/// child line naming the other included airports it also serves.
/// </remarks>
public static class ProcedureChangesMarkdownWriter
{
	private const string NoteLine =
		"Note: In some cases, the link will return a 404 Error. This is because the FAA does not have a comparative document. " +
		"This is common with Military facilities.";

	/// <summary>
	/// Writes <c>Procedure_Changes.md</c> for the included airports and procedures.
	/// </summary>
	/// <param name="airports">The included airports (post-selection), each with only its included procedures.</param>
	/// <param name="settings">The parsed settings; <see cref="ProcedureSettings.PrimaryFacility"/> is read.</param>
	/// <param name="dtpp">The selected cycle's parsed FAA d-TPP Metafile (its <c>Cycle</c> and effective date).</param>
	/// <param name="previousDtpp">
	/// The previous cycle's parsed FAA d-TPP Metafile, or <see langword="null"/> when it is not
	/// available - only its <c>Cycle</c> is read, and only when a procedure actually carries a
	/// <see cref="Procedure.PreviousPdfName"/> (which cannot happen when this is <see langword="null"/>).
	/// </param>
	/// <returns>The path written, plus any messages collected.</returns>
	public static ProcedureChangesWriteResult Generate(
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

		string path = Path.Combine(directory, ProcedureOutputFiles.Changes);
		File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

		return new ProcedureChangesWriteResult(path, []);
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

	private static void WriteEntry(StringBuilder builder, RenderEntry entry, string cycle, string previousCycle, string indent)
	{
		Procedure procedure = entry.Procedure;
		string childIndent = indent + "  ";

		string line = procedure.Change switch
		{
			ProcedureChange.Changed => $"[{procedure.ReportedName}]({DtppFiles.CompareUrl(cycle, procedure.ReportedPdfName)})",
			ProcedureChange.New => $"[{procedure.ReportedName}]({DtppFiles.ChartUrl(cycle, procedure.ReportedPdfName)})",
			ProcedureChange.Deleted => procedure.PreviousPdfName is { Length: > 0 } previousPdf
				? $"[{procedure.ReportedName}]({DtppFiles.ChartUrl(previousCycle, previousPdf)})"
				: $"{procedure.ReportedName} (previous chart not available)",
			ProcedureChange.ReAdded => ReAddedLine(procedure, cycle, previousCycle),
			_ => procedure.ReportedName,
		};

		builder.AppendLine($"{indent}- {line}");

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

		if (entry.AlsoServes.Count > 0)
		{
			builder.AppendLine($"{childIndent}- Also serves: {string.Join(", ", entry.AlsoServes)}");
		}
	}

	private static string ReAddedLine(Procedure procedure, string cycle, string previousCycle)
	{
		string oldToNew = procedure.PreviousPdfName is { Length: > 0 } previousPdf
			? $"[Old]({DtppFiles.ChartUrl(previousCycle, previousPdf)}) -> "
			: string.Empty;

		string newLink = procedure.CurrentPdfName is { Length: > 0 } currentPdf
			? $"[New]({DtppFiles.ChartUrl(cycle, currentPdf)})"
			: "[New](chart not available)";

		return $"{procedure.Name} {oldToNew}{newLink}";
	}

	/// <summary>
	/// Walks every included airport's included, changed procedures in document order and applies
	/// the shared-chart de-duplication rule: a chart listed at more than one included airport keeps
	/// only its owner's (or, failing that, the first airport's) entry, with the others recorded as
	/// "Also serves".
	/// </summary>
	/// <returns>Every airport's surviving entries, keyed by <see cref="ProcedureAirport.AptIdent"/>, plus the total change count.</returns>
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

		Dictionary<string, List<int>> indicesByIdentity = new(StringComparer.OrdinalIgnoreCase);

		for (int i = 0; i < changed.Count; i++)
		{
			string identity = Identity(changed[i].Procedure);

			if (!indicesByIdentity.TryGetValue(identity, out List<int>? indices))
			{
				indices = [];
				indicesByIdentity[identity] = indices;
			}

			indices.Add(i);
		}

		HashSet<int> dropped = [];
		Dictionary<int, IReadOnlyList<string>> alsoServesByIndex = [];

		foreach (List<int> indices in indicesByIdentity.Values)
		{
			if (indices.Count <= 1)
			{
				continue;
			}

			int keeperIndex = FindOwnerIndex(indices, changed) ?? indices[0];

			alsoServesByIndex[keeperIndex] = [.. indices
				.Where(index => index != keeperIndex)
				.Select(index => changed[index].Airport.AptIdent)];

			foreach (int index in indices)
			{
				if (index != keeperIndex)
				{
					dropped.Add(index);
				}
			}
		}

		Dictionary<string, List<RenderEntry>> entriesByAirport = new(StringComparer.OrdinalIgnoreCase);

		for (int i = 0; i < changed.Count; i++)
		{
			if (dropped.Contains(i))
			{
				continue;
			}

			(ProcedureAirport airport, Procedure procedure) = changed[i];
			IReadOnlyList<string> alsoServes = alsoServesByIndex.TryGetValue(i, out IReadOnlyList<string>? also) ? also : [];

			if (!entriesByAirport.TryGetValue(airport.AptIdent, out List<RenderEntry>? entries))
			{
				entries = [];
				entriesByAirport[airport.AptIdent] = entries;
			}

			entries.Add(new RenderEntry(procedure, alsoServes));
		}

		return (entriesByAirport, changed.Count - dropped.Count);
	}

	/// <summary>
	/// A changed chart's identity: its reported PDF name (New/Changed), or the previous cycle's PDF
	/// name (Deleted/ReAdded); falling back to chart code + procedure UID + base name when that PDF
	/// name is not available.
	/// </summary>
	private static string Identity(Procedure procedure)
	{
		string? pdfName = procedure.Change is ProcedureChange.Deleted or ProcedureChange.ReAdded
			? procedure.PreviousPdfName
			: procedure.ReportedPdfName;

		return pdfName is { Length: > 0 }
			? pdfName
			: $"{procedure.ChartCode}|{procedure.ProcUid}|{procedure.Name}";
	}

	/// <summary>
	/// Finds the member whose airport owns the shared chart - its <see cref="ProcedureAirport.Alnum"/>,
	/// zero-padded to 5 digits, equals the identity PDF name's leading 5 digits.
	/// </summary>
	/// <returns>The owner's index into <paramref name="changed"/>, or <see langword="null"/> when no member owns it (or the identity is not PDF-shaped).</returns>
	private static int? FindOwnerIndex(List<int> indices, List<(ProcedureAirport Airport, Procedure Procedure)> changed)
	{
		string identity = Identity(changed[indices[0]].Procedure);

		if (identity.Length < 5)
		{
			return null;
		}

		string leading5 = identity[..5];

		if (!leading5.All(char.IsAsciiDigit))
		{
			return null;
		}

		foreach (int index in indices)
		{
			if (changed[index].Airport.Alnum.ToString("D5", CultureInfo.InvariantCulture) == leading5)
			{
				return index;
			}
		}

		return null;
	}
}
