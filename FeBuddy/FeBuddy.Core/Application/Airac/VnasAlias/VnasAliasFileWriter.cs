using System.Diagnostics;
using System.Globalization;
using System.Text;

using FeBuddy.Core.Application.Airac.VnasAlias.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;

namespace FeBuddy.Core.Application.Airac.VnasAlias;

/// <summary>
/// Writes <c>Upload_to_vNAS\vNAS_Alias.txt</c>, the one alias file a facility uploads to vNAS: the
/// user's own custom alias files first, then every FE-Buddy alias file marked for vNAS.
/// </summary>
/// <remarks>
/// <para>
/// vNAS takes a single alias file, so FE-Buddy's alias files cannot be uploaded on their own. The
/// file is laid out as:
/// </para>
/// <code>
/// .FeUseOnly ...                     the first .FeUseOnly line any custom file has, kept first
/// (custom alias file 1)
/// (custom alias file 2, ...)
/// ; ===== FE-Buddy aliases (AIRAC 2610) start here ... =====   <see cref="FeBuddySectionMarker"/>
/// ; ----- Airways.txt -----
/// (Airways.txt)
/// ; ----- Telephony.txt -----
/// (Telephony.txt)
/// </code>
/// <para>
/// A custom file is copied as it is, with two exceptions. A <c>.FeUseOnly</c> line must be the
/// first line of an alias file, so only the first one found is kept, and it is moved to the top.
/// And a custom file that is itself an old <c>vNAS_Alias.txt</c> - a facility that keeps the file it
/// last uploaded as its custom file - is cut at <see cref="FeBuddySectionMarker"/>, so last cycle's
/// FE-Buddy aliases are not merged in a second time.
/// </para>
/// <para>
/// A command from a custom file that another merged file has too is reported, as CRC runs only one
/// of each. Commands FE-Buddy's own files share are left to <c>Duplicate_Alias_Commands.txt</c>,
/// which already lists them.
/// </para>
/// <para>
/// When there is nothing to merge, no file is written, and one an earlier run left in
/// <c>Upload_to_vNAS</c> is deleted, so last run's file cannot be uploaded by mistake.
/// </para>
/// </remarks>
public static class VnasAliasFileWriter
{
	/// <summary>How the line that starts FE-Buddy's section begins; a custom file is cut where it has one.</summary>
	public const string FeBuddySectionMarker = "; ===== FE-Buddy aliases";

	private const string LogSource = "VnasAlias";
	private const string FeUseOnlyCommand = ".FeUseOnly";
	private const int DuplicatesListed = 10;

	/// <summary>
	/// Writes <c>vNAS_Alias.txt</c> into <paramref name="outputDirectory"/>'s
	/// <c>Upload_to_vNAS</c> folder.
	/// </summary>
	/// <param name="customFiles">
	/// The user's custom alias files as they were read, in merge order. One that could not be read is
	/// left out with a warning.
	/// </param>
	/// <param name="feBuddyAliasFiles">The full paths of FE-Buddy's alias files marked for vNAS, in the order to add them.</param>
	/// <param name="cycleId">The AIRAC cycle the FE-Buddy files are for, e.g. <c>2610</c>.</param>
	/// <param name="outputDirectory">The folder the run writes into - the cycle folder.</param>
	/// <returns>What was written, and every message.</returns>
	/// <exception cref="IOException">Thrown when an FE-Buddy alias file cannot be read or the file cannot be written.</exception>
	public static VnasAliasResult Write(
		IReadOnlyList<AliasSourceLoad> customFiles,
		IReadOnlyList<string> feBuddyAliasFiles,
		string cycleId,
		string outputDirectory)
	{
		ArgumentNullException.ThrowIfNull(customFiles);
		ArgumentNullException.ThrowIfNull(feBuddyAliasFiles);
		ArgumentException.ThrowIfNullOrWhiteSpace(cycleId);
		ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

		Stopwatch stopwatch = Stopwatch.StartNew();
		List<ServiceMessage> messages = [];

		foreach (AliasSourceLoad failed in customFiles.Where(f => !f.Succeeded))
		{
			Add(messages, new ServiceMessage(LogLevel.Warning, LogSource,
				$"Left {failed.Source.DisplayName} out of {AiracOutputPaths.VnasAliasFileName}: {failed.Problem} " +
				"Uploading the file without it would remove its aliases from vNAS.")
			{ IsAdvisory = true });
		}

		// Each merged file's lines, as they go into the file, and the name reports use for it.
		List<(string Name, IReadOnlyList<string> Lines)> custom = [];
		string? feUseOnly = null;

		foreach (AliasSourceLoad file in customFiles.Where(f => f.Succeeded))
		{
			List<string> lines = [];

			foreach (string line in SplitLines(file.Text!))
			{
				if (line.TrimStart().StartsWith(FeBuddySectionMarker, StringComparison.OrdinalIgnoreCase))
				{
					Add(messages, new ServiceMessage(LogLevel.Info, LogSource,
						$"{file.Source.DisplayName} ends with FE-Buddy aliases from an earlier {AiracOutputPaths.VnasAliasFileName}; " +
						"only the lines above them were merged, so they are not added twice."));
					break;
				}

				if (IsCommand(line, FeUseOnlyCommand))
				{
					feUseOnly ??= line;
					continue;
				}

				lines.Add(line);
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
				$"{AiracOutputPaths.VnasAliasFileName} was not written: no custom alias file could be read, and no FE-Buddy alias file is marked for vNAS." +
				DeleteEarlierFile(outputDirectory))
			{ IsAdvisory = true });

			return Result(null, customFiles.Count, 0, 0, [], 0, 0, messages, stopwatch);
		}

		StringBuilder builder = new();

		if (feUseOnly is not null)
		{
			builder.AppendLine(feUseOnly);
		}

		for (int i = 0; i < custom.Count; i++)
		{
			if (i > 0)
			{
				builder.AppendLine();
			}

			AppendLines(builder, custom[i].Lines);
		}

		if (feBuddy.Count > 0)
		{
			if (custom.Count > 0)
			{
				builder.AppendLine();
			}

			builder.AppendLine(CultureInfo.InvariantCulture,
				$"{FeBuddySectionMarker} (AIRAC {cycleId}) start here. FE-Buddy replaces everything below this line every cycle. =====");

			foreach ((string name, IReadOnlyList<string> lines) in feBuddy)
			{
				builder.AppendLine(CultureInfo.InvariantCulture, $"; ----- {name} -----");
				AppendLines(builder, lines);
			}
		}

		string path = AiracOutputPaths.VnasAliasFilePath(outputDirectory);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

		int duplicates = ReportDuplicates(custom, feBuddy, messages);

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
	/// Adds an advisory warning for every command a custom file has that another merged file has
	/// too, and returns how many there are. A command repeated inside one file is that file's
	/// business, and one only FE-Buddy's own files share is already in
	/// <c>Duplicate_Alias_Commands.txt</c>, so neither is reported here.
	/// </summary>
	private static int ReportDuplicates(
		IReadOnlyList<(string Name, IReadOnlyList<string> Lines)> custom,
		IReadOnlyList<(string Name, IReadOnlyList<string> Lines)> feBuddy,
		List<ServiceMessage> messages)
	{
		Dictionary<string, List<string>> filesByCommand = new(StringComparer.OrdinalIgnoreCase);
		HashSet<string> inCustomFile = new(StringComparer.OrdinalIgnoreCase);
		List<string> order = [];

		IEnumerable<(string Name, IReadOnlyList<string> Lines, bool IsCustom)> files =
			custom.Select(f => (f.Name, f.Lines, true)).Concat(feBuddy.Select(f => (f.Name, f.Lines, false)));

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

			Add(messages, new ServiceMessage(LogLevel.Warning, LogSource,
				$"{duplicates.Length:N0} alias command(s) from your custom alias files are also in another file merged into " +
				$"{AiracOutputPaths.VnasAliasFileName}, so CRC can only run one of each: {listed}" +
				(duplicates.Length > DuplicatesListed ? ", ..." : string.Empty) +
				". Remove the extra copies from your custom alias files, or untick the FE-Buddy file.")
			{ IsAdvisory = true });
		}

		return duplicates.Length;
	}

	/// <summary>
	/// Deletes the <c>vNAS_Alias.txt</c> an earlier run left, when this run writes none, so it cannot
	/// be uploaded by mistake.
	/// </summary>
	/// <returns>A sentence for the "not written" message saying what happened to it; empty when there was none.</returns>
	private static string DeleteEarlierFile(string outputDirectory)
	{
		string path = AiracOutputPaths.VnasAliasFilePath(outputDirectory);

		if (!File.Exists(path))
		{
			return string.Empty;
		}

		try
		{
			File.Delete(path);
			return " The one an earlier run wrote was deleted, so it cannot be uploaded by mistake.";
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return $" The one an earlier run wrote could not be deleted ({ex.Message}): do not upload it.";
		}
	}

	private static VnasAliasResult Result(
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
				$"Wrote {path}: {customCommands:N0} command(s) from {customFilesMerged} custom alias file(s), " +
				$"then {feBuddyCommands:N0} from {feBuddyFiles.Count} FE-Buddy alias file(s).");
		}

		stopwatch.Stop();

		return new VnasAliasResult
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
		text.TrimStart('﻿').ReplaceLineEndings("\n").Split('\n');

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
