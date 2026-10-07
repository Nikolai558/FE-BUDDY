using System.Globalization;
using System.Text;

using FeBuddy.Core.Application.Airac.Airports;
using FeBuddy.Core.Application.Airac.Arrivals;
using FeBuddy.Core.Application.Airac.Departures;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Procedures;
using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// Finds the alias commands more than one line of a run's alias files uses, and lists them in
/// <c>Duplicate_Alias_Commands.txt</c> in the cycle folder, grouped by the ARTCC responsible for
/// each line's airport.
/// </summary>
/// <remarks>
/// <para>
/// CRC runs only one of the lines that share a command, so every duplicate is a command a
/// controller cannot rely on. Commands are compared ignoring case, the way CRC matches them:
/// <c>.EDWAPDC</c> and <c>.edwAPDc</c> are the same command. Every alias file the run wrote is
/// checked together, so a clash between two files - a departure and a STAR that the FAA published
/// under one name - is found as well as a clash within one.
/// </para>
/// <para>
/// The ARTCC comes from NASR (<c>APT_BASE.RESP_ARTCC_ID</c>) for the airport each command names:
/// the identifier after <c>.apt</c> in <c>Airports.txt</c>, and the lower-case airport at the start
/// of a <c>Departures.txt</c>, <c>Arrivals.txt</c> or <c>Faa_Chart_Recall.txt</c> command. A
/// <c>Telephony.txt</c> command is listed under <c>TELEPHONY</c>, and any other command that belongs
/// to no airport - an airway or a NAVAID - under <c>OTHER</c>. The report is written for every run
/// that writes an alias file, saying so when there is nothing to fix, so an older report is never
/// left behind to mislead.
/// </para>
/// <para>
/// A file is told apart by its key, FE-Buddy's name for it, so a file the user renamed (the File
/// Names tab) is read the same way; the report names each file as it was written.
/// </para>
/// </remarks>
public static class DuplicateAliasReport
{
	/// <summary>The group for <c>Telephony.txt</c> commands, which belong to an operator rather than an airport.</summary>
	internal const string TelephonyGroup = "TELEPHONY";

	/// <summary>The group for any other command that belongs to no airport FE-Buddy can tie to an ARTCC.</summary>
	internal const string OtherGroup = "OTHER";

	/// <summary>
	/// Finds the duplicates in the given alias files and writes the report.
	/// </summary>
	/// <param name="aliasFiles">Every alias file the run wrote, in the order to check (and list) them.</param>
	/// <param name="nasr">The cycle's parsed NASR data; only <c>Apt</c> is read, for each airport's ARTCC.</param>
	/// <param name="cycleId">The cycle the run was for, e.g. <c>2609</c>.</param>
	/// <param name="outputDirectory">The cycle folder the report goes in.</param>
	/// <param name="primaryFacility">The ARTCC listed first (the user's own facility), or <see langword="null"/> to list every ARTCC alphabetically.</param>
	/// <param name="generatedUtc">When the report was made, for its heading.</param>
	/// <param name="fileName">The report's name, when the user gave it one of their own (see <see cref="OutputFileNames"/>).</param>
	/// <returns>The report's path and the duplicates it lists.</returns>
	public static DuplicateAliasReportResult Write(
		IReadOnlyList<AliasFileWritten> aliasFiles,
		NasrCsvDataCollection nasr,
		string cycleId,
		string outputDirectory,
		string? primaryFacility,
		DateTime generatedUtc,
		string fileName = AiracOutputPaths.DuplicateAliasReportFileName)
	{
		ArgumentNullException.ThrowIfNull(aliasFiles);
		ArgumentNullException.ThrowIfNull(nasr);
		ArgumentException.ThrowIfNullOrWhiteSpace(cycleId);
		ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
		ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

		IReadOnlyList<DuplicateAliasCommand> duplicates = Find(aliasFiles, nasr);
		string text = Format(duplicates, aliasFiles, cycleId, primaryFacility, generatedUtc);

		Directory.CreateDirectory(outputDirectory);
		string path = Path.Combine(outputDirectory, fileName);
		File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

		return new DuplicateAliasReportResult(path, duplicates);
	}

	/// <summary>
	/// Finds every command more than one line of the given alias files uses, ignoring case.
	/// </summary>
	/// <param name="aliasFiles">The alias files, in the order to check them.</param>
	/// <param name="nasr">The cycle's parsed NASR data, for each airport's ARTCC.</param>
	/// <returns>The duplicated commands, in the order their first line was met.</returns>
	internal static IReadOnlyList<DuplicateAliasCommand> Find(IReadOnlyList<AliasFileWritten> aliasFiles, NasrCsvDataCollection nasr)
	{
		// Two passes rather than holding every line: an Airports.txt line is a few hundred
		// characters and there are tens of thousands of them, but duplicates are rare.
		Dictionary<string, int> counts = new(StringComparer.OrdinalIgnoreCase);

		foreach (AliasFileWritten file in aliasFiles)
		{
			foreach (string line in File.ReadLines(file.FilePath))
			{
				if (CommandOf(line) is { } command)
				{
					counts[command] = counts.GetValueOrDefault(command) + 1;
				}
			}
		}

		HashSet<string> duplicated = new(counts.Where(pair => pair.Value > 1).Select(pair => pair.Key), StringComparer.OrdinalIgnoreCase);

		if (duplicated.Count == 0)
		{
			return [];
		}

		ArtccLookup artccs = new(nasr);
		Dictionary<string, (string Spelling, List<DuplicateAliasLine> Lines)> linesByCommand = new(StringComparer.OrdinalIgnoreCase);
		List<string> order = [];

		foreach (AliasFileWritten file in aliasFiles)
		{
			string fileName = Path.GetFileName(file.FilePath);

			// Which of this file's lines with each command a line is, for saved choices.
			Dictionary<string, int> occurrences = new(StringComparer.OrdinalIgnoreCase);

			foreach (string line in File.ReadLines(file.FilePath))
			{
				if (CommandOf(line) is not { } command || !duplicated.Contains(command))
				{
					continue;
				}

				if (!linesByCommand.TryGetValue(command, out (string Spelling, List<DuplicateAliasLine> Lines) entry))
				{
					entry = (command, []);
					linesByCommand[command] = entry;
					order.Add(command);
				}

				int occurrence = occurrences.GetValueOrDefault(command) + 1;
				occurrences[command] = occurrence;

				entry.Lines.Add(new DuplicateAliasLine(file.FileKey, fileName, line, artccs.For(file.FileKey, command), occurrence));
			}
		}

		return [.. order.Select(command => new DuplicateAliasCommand(linesByCommand[command].Spelling, linesByCommand[command].Lines))];
	}

	/// <summary>A line's command - the text up to its first space - or <see langword="null"/> for a line that holds none.</summary>
	internal static string? CommandOf(string line)
	{
		string trimmed = line.TrimStart();

		if (trimmed.Length < 2 || trimmed[0] != '.')
		{
			return null;
		}

		int end = trimmed.IndexOfAny([' ', '\t']);
		return end < 0 ? trimmed : trimmed[..end];
	}

	/// <summary>Builds the report's text.</summary>
	internal static string Format(
		IReadOnlyList<DuplicateAliasCommand> duplicates,
		IReadOnlyList<AliasFileWritten> aliasFiles,
		string cycleId,
		string? primaryFacility,
		DateTime generatedUtc)
	{
		StringBuilder report = new();

		report.AppendLine(CultureInfo.InvariantCulture, $"FE-Buddy duplicate alias commands - AIRAC {cycleId}");
		report.AppendLine(CultureInfo.InvariantCulture, $"Generated {generatedUtc:yyyy-MM-dd HH:mm}Z");
		report.AppendLine();
		report.AppendLine("Each command below is used by more than one line of the alias files this run wrote, so CRC can only");
		report.AppendLine("run one of them. Commands are compared ignoring case, the way CRC matches them.");
		report.AppendLine("Solutions are required at ARTCC level. Consult the FE-Buddy developers if unable to resolve at a local level.");
		report.AppendLine();
		report.AppendLine("Files checked: " + string.Join(", ", aliasFiles.Select(file => Path.GetFileName(file.FilePath))));
		report.AppendLine();

		if (duplicates.Count == 0)
		{
			report.AppendLine("Summary: no duplicate alias commands.");
			return report.ToString();
		}

		IReadOnlyList<(string Group, IReadOnlyList<DuplicateAliasCommand> Commands)> groups = GroupByArtcc(duplicates, primaryFacility);
		int lineCount = duplicates.Sum(duplicate => duplicate.Lines.Count);

		string groupCounts = string.Join(", ", groups.Select(group => string.Create(CultureInfo.InvariantCulture, $"{group.Group} {group.Commands.Count:N0}")));
		report.AppendLine(CultureInfo.InvariantCulture, $"Summary: {duplicates.Count:N0} duplicate command(s) on {lineCount:N0} line(s) - {groupCounts}");

		if (groups.Any(group => group.Group == TelephonyGroup))
		{
			string telephony = duplicates.SelectMany(duplicate => duplicate.Lines).First(IsTelephony).FileName;
			report.AppendLine($"{TelephonyGroup} holds the commands from {telephony}, which belong to an operator rather than an airport.");
		}

		if (groups.Any(group => group.Group == OtherGroup))
		{
			report.AppendLine($"{OtherGroup} holds the commands FE-Buddy can't tie to an airport's ARTCC, such as airways and NAVAIDs.");
		}

		foreach ((string group, IReadOnlyList<DuplicateAliasCommand> commands) in groups)
		{
			report.AppendLine();
			report.AppendLine(group);

			foreach (DuplicateAliasCommand duplicate in commands)
			{
				report.AppendLine(CultureInfo.InvariantCulture, $"\t{duplicate.Command}  ({duplicate.Lines.Count} lines)");

				foreach (DuplicateAliasLine line in duplicate.Lines)
				{
					report.AppendLine(CultureInfo.InvariantCulture, $"\t\t{line.FileName}  {line.Text}");
				}
			}
		}

		return report.ToString();
	}

	/// <summary>
	/// Groups the duplicates by ARTCC: <paramref name="primaryFacility"/> first, the rest
	/// alphabetically, then <see cref="TelephonyGroup"/>, then <see cref="OtherGroup"/>. A command whose
	/// lines fall in two groups (two ARTCCs' airports, or an airport and a telephony) is listed under
	/// both, so each sees it.
	/// </summary>
	private static IReadOnlyList<(string Group, IReadOnlyList<DuplicateAliasCommand> Commands)> GroupByArtcc(
		IReadOnlyList<DuplicateAliasCommand> duplicates,
		string? primaryFacility)
	{
		Dictionary<string, List<DuplicateAliasCommand>> byGroup = new(StringComparer.OrdinalIgnoreCase);

		foreach (DuplicateAliasCommand duplicate in duplicates)
		{
			foreach (string group in duplicate.Lines.Select(GroupOf).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!byGroup.TryGetValue(group, out List<DuplicateAliasCommand>? commands))
				{
					commands = [];
					byGroup[group] = commands;
				}

				commands.Add(duplicate);
			}
		}

		string primary = primaryFacility?.Trim() ?? string.Empty;

		return [.. byGroup
			.OrderBy(pair => GroupRank(pair.Key, primary))
			.ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
			.Select(pair => (pair.Key, (IReadOnlyList<DuplicateAliasCommand>)[.. pair.Value.OrderBy(command => command.Command, StringComparer.OrdinalIgnoreCase)]))];
	}

	/// <summary>The group a line is listed under: <see cref="TelephonyGroup"/> for a <c>Telephony.txt</c> line, else its ARTCC, else <see cref="OtherGroup"/>.</summary>
	private static string GroupOf(DuplicateAliasLine line) =>
		IsTelephony(line) ? TelephonyGroup : line.ArtccId ?? OtherGroup;

	/// <summary>Whether a line is from <c>Telephony.txt</c>, whatever the user named it.</summary>
	private static bool IsTelephony(DuplicateAliasLine line) =>
		line.FileKey.Equals(TelephonyOutputFiles.Alias, StringComparison.OrdinalIgnoreCase);

	/// <summary>Where a group sorts: the primary facility, then every other ARTCC, then TELEPHONY, then OTHER.</summary>
	private static int GroupRank(string group, string primaryFacility) => group switch
	{
		_ when group.Equals(primaryFacility, StringComparison.OrdinalIgnoreCase) => 0,
		TelephonyGroup => 2,
		OtherGroup => 3,
		_ => 1,
	};

	/// <summary>Finds the ARTCC responsible for the airport an alias command names, from NASR.</summary>
	private sealed class ArtccLookup
	{
		private readonly Dictionary<string, string> _byFaaId = new(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, string> _byIcaoId = new(StringComparer.OrdinalIgnoreCase);

		public ArtccLookup(NasrCsvDataCollection nasr)
		{
			foreach (AptCsvDataModel.AptBase airport in nasr.Apt?.AptBase ?? [])
			{
				string artcc = (airport.RespArtccId ?? string.Empty).Trim().ToUpperInvariant();

				if (artcc.Length == 0)
				{
					continue;
				}

				if (!string.IsNullOrWhiteSpace(airport.ArptId))
				{
					_byFaaId.TryAdd(airport.ArptId.Trim(), artcc);
				}

				if (!string.IsNullOrWhiteSpace(airport.IcaoId))
				{
					_byIcaoId.TryAdd(airport.IcaoId.Trim(), artcc);
				}
			}
		}

		/// <summary>The ARTCC for a command in an alias file, or <see langword="null"/> when it names no airport NASR lists.</summary>
		/// <param name="fileKey">FE-Buddy's name for the alias file, e.g. <c>Airports.txt</c>.</param>
		/// <param name="command">The command.</param>
		public string? For(string fileKey, string command)
		{
			if (fileKey.Equals(AirportOutputFiles.Alias, StringComparison.OrdinalIgnoreCase))
			{
				// .aptDTW or .aptKDTW: the airport's FAA or ICAO identifier follows .apt.
				string id = command.StartsWith(".apt", StringComparison.OrdinalIgnoreCase) ? command[4..] : string.Empty;
				return _byFaaId.GetValueOrDefault(id) ?? _byIcaoId.GetValueOrDefault(id);
			}

			if (fileKey.Equals(DepartureOutputFiles.Alias, StringComparison.OrdinalIgnoreCase)
				|| fileKey.Equals(ArrivalOutputFiles.Alias, StringComparison.OrdinalIgnoreCase)
				|| fileKey.Equals(ProcedureOutputFiles.Alias, StringComparison.OrdinalIgnoreCase))
			{
				return ForLowerCaseAirport(command);
			}

			// Airways.txt and Navaids.txt commands name no airport.
			return null;
		}

		/// <summary>
		/// The ARTCC for a command that starts with its airport in lower case, e.g. <c>.laxDOTSSf</c>.
		/// The lower-case run can be longer than the airport - a charted visual's <c>v</c>
		/// (<c>.ancvHIGHWAY25Rc</c>), or a code that starts with a digit (<c>.1u71U7c</c>) - so the
		/// longest start of it that is an airport wins. A command spelled all in capitals (a hand
		/// edit, say) has no lower-case run, so its whole run of letters and digits is tried instead.
		/// </summary>
		private string? ForLowerCaseAirport(string command)
		{
			string run = LeadingRun(command, character => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character));

			if (run.Length == 0 || run.All(char.IsAsciiDigit))
			{
				run = LeadingRun(command, char.IsAsciiLetterOrDigit);
			}

			for (int take = run.Length; take >= 2; take--)
			{
				if (_byFaaId.TryGetValue(run[..take], out string? artcc))
				{
					return artcc;
				}
			}

			return null;
		}

		/// <summary>The characters after the command's period for as long as <paramref name="belongs"/> holds.</summary>
		private static string LeadingRun(string command, Func<char, bool> belongs)
		{
			int length = 1;

			while (length < command.Length && belongs(command[length]))
			{
				length++;
			}

			return command[1..length];
		}
	}
}
