using System.Diagnostics;
using System.Globalization;
using System.Text;

using FeBuddy.Core.Application.Airac.ConcatenateAliases.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.ConcatenateAliases;

/// <summary>
/// Writes <c>Aliases\Combined_Alias.txt</c>, the one alias file a facility uploads to vNAS: every
/// alias file the run wrote first, then the user's own custom alias files.
/// </summary>
/// <remarks>
/// <para>
/// vNAS takes a single alias file, so FE-Buddy's alias files cannot be uploaded on their own. CRC
/// reads the file top to bottom and the last copy of a command wins, so the user's own files go last:
/// a command of theirs replaces FE-Buddy's. The file is laid out as:
/// </para>
/// <code>
/// .FeUseOnly ...                     the first .FeUseOnly line any custom file has, kept first
/// ; ===== FE-Buddy aliases (AIRAC 2610) start here ... =====   <see cref="FeBuddySectionMarker"/>
/// ; ----- Airways.txt -----
/// (Airways.txt)
/// ; ----- Telephony.txt -----
/// (Telephony.txt)
/// ; ===== End of FE-Buddy aliases ... =====                    <see cref="FeBuddySectionEndMarker"/>
///
/// (custom alias file 1)
/// (custom alias file 2, ...)
/// </code>
/// <para>
/// A custom file is copied as it is, with two exceptions. A <c>.FeUseOnly</c> line must be the
/// first line of an alias file, so only the first one found is kept, and it is moved to the top.
/// And a custom file that is itself an old combined file - a facility that keeps the file it
/// last uploaded as its custom file - loses its FE-Buddy section, from <see cref="FeBuddySectionMarker"/>
/// to <see cref="FeBuddySectionEndMarker"/>, so last cycle's FE-Buddy aliases are not merged in a
/// second time. A file from before the end line existed (FE-Buddy's section was last) loses
/// everything from its start line down.
/// </para>
/// <para>
/// A command from a custom file that another merged file has too is reported, naming the files in
/// the order they are merged, so the last one named is the copy CRC uses. Commands FE-Buddy's own
/// files share are left to <c>Duplicate_Alias_Commands.txt</c>, which already lists them.
/// </para>
/// <para>
/// When there is nothing to merge, no file is written, and one an earlier run left in
/// <c>Aliases</c> is deleted, so last run's file cannot be uploaded by mistake.
/// </para>
/// <para>
/// The names are FE-Buddy's. A file the user renamed (the File Names tab) is written, and headed,
/// under its new name - <c>Combined_Alias.txt</c> itself included.
/// </para>
/// </remarks>
public static class CombinedAliasFileWriter
{
	/// <summary>How the line that starts FE-Buddy's section begins; a custom file loses its section from there.</summary>
	public const string FeBuddySectionMarker = "; ===== FE-Buddy aliases";

	/// <summary>How the line that ends FE-Buddy's section begins; a custom file's lines after it are kept.</summary>
	public const string FeBuddySectionEndMarker = "; ===== End of FE-Buddy aliases";

	private const string LogSource = "CombinedAliasFileWriter";
	private const string FeUseOnlyCommand = ".FeUseOnly";
	private const int DuplicatesListed = 10;

	/// <summary>
	/// Writes <c>Combined_Alias.txt</c> into <paramref name="outputDirectory"/>'s <c>Aliases</c> folder.
	/// </summary>
	/// <param name="customFiles">
	/// The user's custom alias files as they were read, in merge order. One that could not be read is
	/// left out with a warning.
	/// </param>
	/// <param name="feBuddyAliasFiles">The full paths of FE-Buddy's alias files, in the order to add them.</param>
	/// <param name="cycleId">The AIRAC cycle the FE-Buddy files are for, e.g. <c>2610</c>.</param>
	/// <param name="outputDirectory">The folder the run writes into - the cycle folder.</param>
	/// <param name="fileName">The file's name, when the user gave it one of their own (see <see cref="OutputFileNames"/>).</param>
	/// <returns>What was written, and every message.</returns>
	/// <exception cref="IOException">Thrown when an FE-Buddy alias file cannot be read or the file cannot be written.</exception>
	public static CombinedAliasResult Write(
		IReadOnlyList<AliasSourceLoad> customFiles,
		IReadOnlyList<string> feBuddyAliasFiles,
		string cycleId,
		string outputDirectory,
		string fileName = AiracOutputPaths.CombinedAliasFileName)
	{
		ArgumentNullException.ThrowIfNull(customFiles);
		ArgumentNullException.ThrowIfNull(feBuddyAliasFiles);
		ArgumentException.ThrowIfNullOrWhiteSpace(cycleId);
		ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
		ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = [];

		foreach (AliasSourceLoad failed in customFiles.Where(f => !f.Succeeded))
		{
			Add(messages, new ServiceMessage(LogLevel.Warning, LogSource,
				$"Left {failed.Source.DisplayName} out of {fileName}: {failed.Problem} " +
				"Uploading the file without it would remove its aliases from vNAS.")
			{ IsAdvisory = true });
		}

		// Each merged file's lines, as they go into the file, and the name reports use for it.
		List<(string Name, IReadOnlyList<string> Lines)> custom = [];
		string? feUseOnly = null;

		foreach (AliasSourceLoad file in customFiles.Where(f => f.Succeeded))
		{
			List<string> lines = [];
			bool inFeBuddySection = false;
			bool hadFeBuddySection = false;

			foreach (string line in SplitLines(file.Text!))
			{
				string trimmed = line.TrimStart();

				if (trimmed.StartsWith(FeBuddySectionMarker, StringComparison.OrdinalIgnoreCase))
				{
					inFeBuddySection = true;
					hadFeBuddySection = true;
					continue;
				}

				if (trimmed.StartsWith(FeBuddySectionEndMarker, StringComparison.OrdinalIgnoreCase))
				{
					inFeBuddySection = false;
					hadFeBuddySection = true;
					continue;
				}

				if (inFeBuddySection)
				{
					continue;
				}

				if (IsCommand(line, FeUseOnlyCommand))
				{
					feUseOnly ??= line;
					continue;
				}

				lines.Add(line);
			}

			if (hadFeBuddySection)
			{
				Add(messages, new ServiceMessage(LogLevel.Info, LogSource,
					$"{file.Source.DisplayName} holds FE-Buddy aliases from an earlier {fileName}; " +
					"they were left out, so they are not added twice."));
			}

			custom.Add((file.Source.FileName, TrimBlankEnds(lines)));
		}

		List<(string Name, IReadOnlyList<string> Lines)> feBuddy =
		[
			.. feBuddyAliasFiles.Select(path => (Path.GetFileName(path), TrimBlankEnds(SplitLines(File.ReadAllText(path))))),
		];

		int customCommands = custom.Sum(f => f.Lines.Count(line => CommandOf(line) is not null));
		int feBuddyCommands = feBuddy.Sum(f => f.Lines.Count(line => CommandOf(line) is not null));

		if (custom.Count == 0 && feBuddy.Count == 0)
		{
			Add(messages, new ServiceMessage(LogLevel.Warning, LogSource,
				$"{fileName} was not written: no custom alias file could be read, and the run wrote no alias file." +
				DeleteEarlierFile(outputDirectory, fileName).Sentence)
			{ IsAdvisory = true });

			return Result(null, customFiles.Count, 0, 0, [], 0, 0, messages, stopwatch);
		}

		StringBuilder builder = new();

		if (feUseOnly is not null)
		{
			builder.AppendLine(feUseOnly);
		}

		if (feBuddy.Count > 0)
		{
			builder.AppendLine(CultureInfo.InvariantCulture,
				$"{FeBuddySectionMarker} (AIRAC {cycleId}) start here. FE-Buddy replaces everything down to the end line every cycle. =====");

			foreach ((string name, IReadOnlyList<string> lines) in feBuddy)
			{
				builder.AppendLine(CultureInfo.InvariantCulture, $"; ----- {name} -----");
				AppendLines(builder, lines);
			}

			builder.AppendLine(CultureInfo.InvariantCulture,
				$"{FeBuddySectionEndMarker}. Your own aliases go below this line: CRC uses the last copy of a command, so yours replace FE-Buddy's. =====");
		}

		// One blank line before each custom file, except one that starts the file. A file left
		// with nothing (an old combined file that was only FE-Buddy's section) adds nothing.
		bool anythingAbove = feBuddy.Count > 0;

		foreach (IReadOnlyList<string> lines in custom.Select(f => f.Lines).Where(lines => lines.Count > 0))
		{
			if (anythingAbove)
			{
				builder.AppendLine();
			}

			AppendLines(builder, lines);
			anythingAbove = true;
		}

		string path = AiracOutputPaths.CombinedAliasFilePath(outputDirectory, fileName);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

		int duplicates = ReportDuplicates(custom, feBuddy, fileName, messages);

		return Result(path, customFiles.Count, custom.Count, customCommands, [.. feBuddy.Select(f => f.Name)], feBuddyCommands, duplicates, messages, stopwatch);
	}

	/// <summary>How many alias commands <paramref name="text"/> holds: lines that start with a dot.</summary>
	/// <param name="text">An alias file's text.</param>
	/// <returns>The number of command lines.</returns>
	public static int CountCommands(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		return SplitLines(text).Count(line => CommandOf(line) is not null);
	}

	/// <summary>
	/// Adds one advisory listing every command a custom file has that another merged file has too,
	/// and returns how many there are. Each is listed with its files in the order they are merged,
	/// so the last one named is the copy CRC uses - a custom file's, over FE-Buddy's. It is a notice,
	/// not a warning: replacing one of FE-Buddy's commands is what the custom files are last for. A
	/// command repeated inside one file is that file's business, and one only FE-Buddy's own files
	/// share is already in <c>Duplicate_Alias_Commands.txt</c>, so neither is reported here.
	/// </summary>
	private static int ReportDuplicates(
		IReadOnlyList<(string Name, IReadOnlyList<string> Lines)> custom,
		IReadOnlyList<(string Name, IReadOnlyList<string> Lines)> feBuddy,
		string fileName,
		List<ServiceMessage> messages)
	{
		Dictionary<string, List<string>> filesByCommand = new(StringComparer.OrdinalIgnoreCase);
		HashSet<string> inCustomFile = new(StringComparer.OrdinalIgnoreCase);
		List<string> order = [];

		IEnumerable<(string Name, IReadOnlyList<string> Lines, bool IsCustom)> files =
			feBuddy.Select(f => (f.Name, f.Lines, false)).Concat(custom.Select(f => (f.Name, f.Lines, true)));

		foreach ((string name, IReadOnlyList<string> lines, bool isCustom) in files)
		{
			foreach (string command in lines.Select(CommandOf).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!filesByCommand.TryGetValue(command, out List<string>? names))
				{
					names = [];
					filesByCommand[command] = names;
					order.Add(command);
				}

				names.Add(name);

				if (isCustom)
				{
					inCustomFile.Add(command);
				}
			}
		}

		string[] duplicates = [.. order.Where(command => filesByCommand[command].Count > 1 && inCustomFile.Contains(command))];

		if (duplicates.Length > 0)
		{
			string listed = string.Join(", ", duplicates
				.Take(DuplicatesListed)
				.Select(command => $"{command} ({string.Join(", ", filesByCommand[command])})"));

			Add(messages, new ServiceMessage(LogLevel.Info, LogSource,
				$"{duplicates.Length:N0} alias command(s) from your custom alias files are also in another file merged into " +
				$"{fileName}: {listed}" +
				(duplicates.Length > DuplicatesListed ? ", ..." : string.Empty) +
				". CRC uses the last copy of a command - the one from the last file named - and your custom alias files " +
				"come after FE-Buddy's, so a command of yours replaces FE-Buddy's. To use FE-Buddy's instead, remove yours.")
			{ IsAdvisory = true });
		}

		return duplicates.Length;
	}

	/// <summary>
	/// Deletes the <c>Combined_Alias.txt</c> an earlier run left, when this run writes none, so it cannot
	/// be uploaded by mistake. <see cref="AiracService"/> uses it too, for a run that rewrites alias
	/// files without writing a new one.
	/// </summary>
	/// <param name="outputDirectory">The run's cycle folder.</param>
	/// <param name="fileName">The file's name - the one this run would have written it under.</param>
	/// <returns>
	/// A sentence for a "not written" message saying what happened to it (empty when there was none),
	/// and whether it could not be deleted.
	/// </returns>
	internal static (string Sentence, bool Failed) DeleteEarlierFile(string outputDirectory, string fileName)
	{
		string path = AiracOutputPaths.CombinedAliasFilePath(outputDirectory, fileName);

		if (!File.Exists(path))
		{
			return (string.Empty, false);
		}

		try
		{
			File.Delete(path);
			return (" The one an earlier run wrote was deleted, so it cannot be uploaded by mistake.", false);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return ($" The one an earlier run wrote could not be deleted ({ex.Message}): do not upload it.", true);
		}
	}

	private static CombinedAliasResult Result(
		string? path,
		int customFileCount,
		int customFilesMerged,
		int customCommands,
		IReadOnlyList<string> feBuddyFiles,
		int feBuddyCommands,
		int duplicates,
		List<ServiceMessage> messages,
		Stopwatch stopwatch)
	{
		if (path is not null)
		{
			AppLog.Success(LogSource,
				$"Wrote {path}: {feBuddyCommands:N0} command(s) from {feBuddyFiles.Count} FE-Buddy alias file(s), " +
				$"then {customCommands:N0} from {customFilesMerged} custom alias file(s).");
		}

		stopwatch.Stop();

		return new CombinedAliasResult
		{
			FilePath = path,
			CustomFileCount = customFileCount,
			CustomFilesMerged = customFilesMerged,
			CustomCommandCount = customCommands,
			FeBuddyFiles = feBuddyFiles,
			FeBuddyCommandCount = feBuddyCommands,
			DuplicateCommandCount = duplicates,
			Messages = messages,
			Elapsed = stopwatch.Elapsed,
		};
	}

	private static void Add(List<ServiceMessage> messages, ServiceMessage message)
	{
		messages.Add(message);
		AppLog.Write(message.Level, message.Source, message.Text);
	}

	private static void AppendLines(StringBuilder builder, IReadOnlyList<string> lines)
	{
		foreach (string line in lines)
		{
			builder.AppendLine(line);
		}
	}

	/// <summary>A line's command - the text up to its first space - or <see langword="null"/> for a comment or blank line.</summary>
	private static string? CommandOf(string line) => DuplicateAliasReport.CommandOf(line);

	private static bool IsCommand(string line, string command) =>
		string.Equals(CommandOf(line), command, StringComparison.OrdinalIgnoreCase);

	private static IReadOnlyList<string> SplitLines(string text) =>
		text.TrimStart('\uFEFF').ReplaceLineEndings("\n").Split('\n');

	/// <summary>The lines without the blank lines at either end, so files join with exactly one blank line.</summary>
	private static IReadOnlyList<string> TrimBlankEnds(IReadOnlyList<string> lines)
	{
		int start = 0;
		int end = lines.Count;

		while (start < end && string.IsNullOrWhiteSpace(lines[start]))
		{
			start++;
		}

		while (end > start && string.IsNullOrWhiteSpace(lines[end - 1]))
		{
			end--;
		}

		return [.. lines.Skip(start).Take(end - start)];
	}
}
