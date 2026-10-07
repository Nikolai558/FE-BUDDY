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
/// <param name="Occurrence">
/// Which of its file's lines with this command it is, from 1: two lines of one file can share a
/// command, and a saved choice (<see cref="DuplicateAliasRule"/>) has to tell them apart.
/// </param>
public sealed record DuplicateAliasLine(string FileKey, string FileName, string Text, string? ArtccId, int Occurrence = 1);

/// <summary>What to do with one line of a duplicated command.</summary>
public enum DuplicateAliasAction
{
	/// <summary>Keep the line as it is. Only one line of a command can.</summary>
	Keep = 0,

	/// <summary>Leave the line out of its alias file.</summary>
	Ignore = 1,

	/// <summary>Keep the line under a command of the user's own (<see cref="DuplicateAliasRule.NewCommand"/>).</summary>
	Rename = 2,
}

/// <summary>
/// The user's choice for one line of a duplicated command, saved so later runs make it again
/// without asking. The line is named by its file, its command and which of that file's lines with
/// the command it is, never by its text: a chart's address changes every cycle.
/// </summary>
/// <param name="FileKey">FE-Buddy's name for the alias file, e.g. <c>Departures.txt</c>.</param>
/// <param name="Command">The duplicated command, e.g. <c>.orfNUTIYf</c>; matched ignoring case, as CRC does.</param>
/// <param name="Occurrence">Which of the file's lines with <paramref name="Command"/> it is, from 1.</param>
/// <param name="Action">Keep it, leave it out, or rename it.</param>
/// <param name="NewCommand">The line's new command, for <see cref="DuplicateAliasAction.Rename"/>; otherwise <see langword="null"/>.</param>
public sealed record DuplicateAliasRule(string FileKey, string Command, int Occurrence, DuplicateAliasAction Action, string? NewCommand = null)
{
	/// <summary>Whether this choice is for <paramref name="line"/> of <paramref name="command"/>.</summary>
	/// <param name="command">The duplicated command.</param>
	/// <param name="line">One of its lines.</param>
	/// <returns><see langword="true"/> when the file, the command and the occurrence all match.</returns>
	public bool IsFor(string command, DuplicateAliasLine line)
	{
		ArgumentNullException.ThrowIfNull(command);
		ArgumentNullException.ThrowIfNull(line);

		return Occurrence == line.Occurrence
			&& FileKey.Equals(line.FileKey, StringComparison.OrdinalIgnoreCase)
			&& Command.Equals(command, StringComparison.OrdinalIgnoreCase);
	}
}

/// <summary>A duplicated command the choices settle, with the choice for each of its lines, in line order.</summary>
/// <param name="Duplicate">The command and its lines.</param>
/// <param name="Choices">A choice per line.</param>
public sealed record SettledDuplicate(DuplicateAliasCommand Duplicate, IReadOnlyList<DuplicateAliasRule> Choices);

/// <summary>What a set of choices does with a run's duplicated commands (see <c>DuplicateAliasChoices.Plan</c>).</summary>
/// <param name="Settled">The commands the choices settle.</param>
/// <param name="Unsettled">The commands they don't: a line has no choice, or the choices break a rule.</param>
/// <param name="Unused">The choices for no line of any duplicated command: the duplicate has gone.</param>
public sealed record DuplicateAliasPlan(
	IReadOnlyList<SettledDuplicate> Settled,
	IReadOnlyList<DuplicateAliasCommand> Unsettled,
	IReadOnlyList<DuplicateAliasRule> Unused);

/// <summary>
/// What the run asks the user about, when it stops for them to choose (see
/// <see cref="AiracServiceSettings.ReviewDuplicateAliases"/>): the duplicated commands no saved choice
/// settles, and what a new command must not be.
/// </summary>
/// <param name="Duplicates">The duplicated commands to choose for, each with every line that uses it.</param>
/// <param name="SavedChoices">Earlier choices for some of their lines, to start from (a choice that no longer settles its command, say).</param>
/// <param name="TakenCommands">
/// Every command the run already uses - in its alias files, the user's custom alias files, and the
/// new commands of the choices already made - which a new command must not be.
/// </param>
public sealed record DuplicateAliasReview(
	IReadOnlyList<DuplicateAliasCommand> Duplicates,
	IReadOnlyList<DuplicateAliasRule> SavedChoices,
	IReadOnlySet<string> TakenCommands);

/// <summary>
/// The user's answer to a <see cref="DuplicateAliasReview"/>: a choice for every line of every
/// command, or <see langword="null"/> to stop the run.
/// </summary>
/// <param name="review">What to choose for.</param>
/// <param name="cancellationToken">Cancels the run.</param>
/// <returns>The choices, or <see langword="null"/> to stop the run with no alias file saved.</returns>
public delegate Task<IReadOnlyList<DuplicateAliasRule>?> DuplicateAliasReviewer(DuplicateAliasReview review, CancellationToken cancellationToken);

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
