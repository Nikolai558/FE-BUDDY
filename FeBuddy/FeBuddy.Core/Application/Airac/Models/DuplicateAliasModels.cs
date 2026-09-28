namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>An alias file a run wrote, to check for duplicate commands.</summary>
/// <param name="FileKey">
/// FE-Buddy's name for the file, e.g. <c>Airports.txt</c>: it says how to find each command's
/// ARTCC, whatever the file is called.
/// </param>
/// <param name="FilePath">The file's full path, under the name the user gave it if they renamed it.</param>
public sealed record AliasFileWritten(string FileKey, string FilePath);

/// <summary>One line of an alias file that shares its command with another line.</summary>
/// <param name="FileKey">FE-Buddy's name for the alias file the line is in, e.g. <c>Faa_Chart_Recall.txt</c>.</param>
/// <param name="FileName">The alias file's name as written, which is <paramref name="FileKey"/> unless the user renamed it.</param>
/// <param name="Text">The whole line, as written.</param>
/// <param name="ArtccId">
/// The ARTCC responsible for the airport the line belongs to, e.g. <c>ZLA</c>; <see langword="null"/>
/// when the line belongs to no airport FE-Buddy can tell (an airway or NAVAID command, or an
/// airport NASR does not list).
/// </param>
public sealed record DuplicateAliasLine(string FileKey, string FileName, string Text, string? ArtccId);

/// <summary>An alias command more than one line uses, with every one of those lines.</summary>
/// <param name="Command">The command as its first line spells it, e.g. <c>.edwAPDc</c>.</param>
/// <param name="Lines">Every line that uses it, file by file in the order the files were checked, each file's lines in order.</param>
public sealed record DuplicateAliasCommand(string Command, IReadOnlyList<DuplicateAliasLine> Lines);

/// <summary>The outcome of writing <c>Duplicate_Alias_Commands.txt</c>.</summary>
/// <param name="FilePath">The report's full path.</param>
/// <param name="Duplicates">Every command more than one line uses; empty when the run's alias files have none.</param>
public sealed record DuplicateAliasReportResult(string FilePath, IReadOnlyList<DuplicateAliasCommand> Duplicates)
{
	/// <summary>How many lines use a duplicated command.</summary>
	public int LineCount => Duplicates.Sum(duplicate => duplicate.Lines.Count);
}
