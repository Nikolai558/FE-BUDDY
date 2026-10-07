using System.Globalization;
using System.Text;

using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// The user's choices for duplicated alias commands (issue #318): which of a command's lines keeps
/// it, which are left out, and which get a command of their own - whether a set of choices settles
/// a command, making them in the run's alias files, and saving them for later runs.
/// </summary>
/// <remarks>
/// <para>
/// A command is settled when every line that uses it has a choice, at most one line keeps it, and
/// each new command is free: no line in the run uses it - in FE-Buddy's alias files or the user's
/// custom ones - and no other choice gives it out. A command a saved choice no longer settles (a new
/// line joined it, or its new command is now taken) goes back to the user, with the old choices
/// filled in.
/// </para>
/// <para>
/// Choices are saved one per line of text, as <c>FileKey|Command|Occurrence|Action|NewCommand</c>
/// (see <see cref="ToConfig"/>).
/// </para>
/// </remarks>
public static class DuplicateAliasChoices
{
	private const char Separator = '|';

	/// <summary>Why a new command can't be used whatever else is in the run, or <see langword="null"/> when its shape is fine.</summary>
	/// <param name="newCommand">The command typed, e.g. <c>.orfNUTIYa</c>.</param>
	/// <returns>A sentence saying what is wrong, or <see langword="null"/>.</returns>
	public static string? NewCommandProblem(string? newCommand)
	{
		string command = newCommand?.Trim() ?? string.Empty;

		if (command.Length == 0)
		{
			return "Type the new command.";
		}

		if (command[0] != '.' || command.Length < 2)
		{
			return "A command starts with a dot and has at least one more character, such as .orfNUTIYa.";
		}

		if (command.Any(character => char.IsWhiteSpace(character) || char.IsControl(character)))
		{
			return "A command can't have a space in it.";
		}

		return command.Contains(Separator, StringComparison.Ordinal) ? "A command can't have a | in it." : null;
	}

	/// <summary>Why <paramref name="choices"/> don't settle <paramref name="duplicate"/>, or <see langword="null"/> when they do.</summary>
	/// <param name="duplicate">The duplicated command and its lines.</param>
	/// <param name="choices">The choice for each of its lines, in line order; <see langword="null"/> for a line with none.</param>
	/// <param name="taken">Every command the run already uses, which a new command must not be.</param>
	/// <param name="givenOut">The new commands other choices already give out.</param>
	/// <returns>A sentence saying what to fix, or <see langword="null"/>.</returns>
	public static string? Problem(
		DuplicateAliasCommand duplicate,
		IReadOnlyList<DuplicateAliasRule?> choices,
		IReadOnlySet<string> taken,
		IReadOnlySet<string> givenOut)
	{
		ArgumentNullException.ThrowIfNull(duplicate);
		ArgumentNullException.ThrowIfNull(choices);
		ArgumentNullException.ThrowIfNull(taken);
		ArgumentNullException.ThrowIfNull(givenOut);

		if (choices.Count != duplicate.Lines.Count || choices.Any(choice => choice is null))
		{
			return $"Choose what happens to each line of {duplicate.Command}.";
		}

		if (choices.Count(choice => choice!.Action == DuplicateAliasAction.Keep) > 1)
		{
			return $"Only one line can keep {duplicate.Command}. Leave out or rename the others.";
		}

		HashSet<string> ownNewCommands = new(StringComparer.OrdinalIgnoreCase);

		foreach (DuplicateAliasRule rename in choices.OfType<DuplicateAliasRule>().Where(choice => choice.Action == DuplicateAliasAction.Rename))
		{
			if (NewCommandProblem(rename.NewCommand) is { } shape)
			{
				return shape;
			}

			string newCommand = rename.NewCommand!.Trim();

			if (taken.Contains(newCommand))
			{
				return $"{newCommand} is already used in this run. Choose another command.";
			}

			if (givenOut.Contains(newCommand) || !ownNewCommands.Add(newCommand))
			{
				return $"{newCommand} is already given to another line. Choose another command.";
			}
		}

		return null;
	}

	/// <summary>
	/// Sorts a run's duplicated commands into those <paramref name="choices"/> settle and those they
	/// don't, and finds the choices for no duplicate at all.
	/// </summary>
	/// <param name="duplicates">The run's duplicated commands (<see cref="DuplicateAliasReport.Find"/>).</param>
	/// <param name="choices">The choices to use.</param>
	/// <param name="taken">Every command the run already uses (<see cref="CommandsIn"/>).</param>
	/// <returns>The plan.</returns>
	public static DuplicateAliasPlan Plan(
		IReadOnlyList<DuplicateAliasCommand> duplicates,
		IReadOnlyList<DuplicateAliasRule> choices,
		IReadOnlySet<string> taken)
	{
		ArgumentNullException.ThrowIfNull(duplicates);
		ArgumentNullException.ThrowIfNull(choices);
		ArgumentNullException.ThrowIfNull(taken);

		List<SettledDuplicate> settled = [];
		List<DuplicateAliasCommand> unsettled = [];
		HashSet<string> givenOut = new(StringComparer.OrdinalIgnoreCase);
		HashSet<DuplicateAliasRule> used = [];

		foreach (DuplicateAliasCommand duplicate in duplicates)
		{
			DuplicateAliasRule?[] mine = [.. duplicate.Lines.Select(line => choices.FirstOrDefault(choice => choice.IsFor(duplicate.Command, line)))];
			used.UnionWith(mine.OfType<DuplicateAliasRule>());

			if (Problem(duplicate, mine, taken, givenOut) is null)
			{
				settled.Add(new SettledDuplicate(duplicate, [.. mine.OfType<DuplicateAliasRule>()]));
				givenOut.UnionWith(NewCommands(mine.OfType<DuplicateAliasRule>()));
			}
			else
			{
				unsettled.Add(duplicate);
			}
		}

		return new DuplicateAliasPlan(settled, unsettled, [.. choices.Where(choice => !used.Contains(choice))]);
	}

	/// <summary>The new commands some choices give out, trimmed.</summary>
	/// <param name="choices">The choices.</param>
	/// <returns>Each rename's new command.</returns>
	public static IEnumerable<string> NewCommands(IEnumerable<DuplicateAliasRule> choices)
	{
		ArgumentNullException.ThrowIfNull(choices);

		return choices
			.Where(choice => choice.Action == DuplicateAliasAction.Rename && choice.NewCommand is not null)
			.Select(choice => choice.NewCommand!.Trim());
	}

	/// <summary>Every command in the run's alias files and the user's custom alias files, ignoring case.</summary>
	/// <param name="aliasFiles">The alias files the run wrote.</param>
	/// <param name="customTexts">The text of each custom alias file that could be read.</param>
	/// <returns>The commands.</returns>
	public static HashSet<string> CommandsIn(IReadOnlyList<AliasFileWritten> aliasFiles, IEnumerable<string> customTexts)
	{
		ArgumentNullException.ThrowIfNull(aliasFiles);
		ArgumentNullException.ThrowIfNull(customTexts);

		HashSet<string> commands = new(StringComparer.OrdinalIgnoreCase);

		IEnumerable<string> lines = aliasFiles
			.SelectMany(file => File.ReadLines(file.FilePath))
			.Concat(customTexts.SelectMany(text => text.ReplaceLineEndings("\n").Split('\n')));

		foreach (string line in lines)
		{
			if (DuplicateAliasReport.CommandOf(line) is { } command)
			{
				commands.Add(command);
			}
		}

		return commands;
	}

	/// <summary>
	/// Makes the choices in the alias files: a line left out is dropped, and a renamed line has its
	/// command replaced. Every other line is written back as it was.
	/// </summary>
	/// <param name="aliasFiles">The alias files the run wrote.</param>
	/// <param name="choices">The choices to make, for settled commands only.</param>
	/// <returns>How many lines were left out and how many renamed.</returns>
	public static (int LeftOut, int Renamed) Apply(IReadOnlyList<AliasFileWritten> aliasFiles, IReadOnlyCollection<DuplicateAliasRule> choices)
	{
		ArgumentNullException.ThrowIfNull(aliasFiles);
		ArgumentNullException.ThrowIfNull(choices);

		int leftOut = 0;
		int renamed = 0;

		foreach (AliasFileWritten file in aliasFiles)
		{
			DuplicateAliasRule[] fileChoices = [.. choices.Where(choice =>
				choice.Action != DuplicateAliasAction.Keep && choice.FileKey.Equals(file.FileKey, StringComparison.OrdinalIgnoreCase))];

			if (fileChoices.Length == 0)
			{
				continue;
			}

			List<string> lines = [];
			Dictionary<string, int> occurrences = new(StringComparer.OrdinalIgnoreCase);

			foreach (string line in File.ReadLines(file.FilePath))
			{
				if (DuplicateAliasReport.CommandOf(line) is not { } command)
				{
					lines.Add(line);
					continue;
				}

				int occurrence = occurrences.GetValueOrDefault(command) + 1;
				occurrences[command] = occurrence;

				DuplicateAliasRule? choice = fileChoices.FirstOrDefault(c =>
					c.Occurrence == occurrence && c.Command.Equals(command, StringComparison.OrdinalIgnoreCase));

				switch (choice?.Action)
				{
					case DuplicateAliasAction.Ignore:
						leftOut++;
						break;

					case DuplicateAliasAction.Rename:
						int at = line.Length - line.TrimStart().Length;
						lines.Add(line[..at] + choice.NewCommand!.Trim() + line[(at + command.Length)..]);
						renamed++;
						break;

					default:
						lines.Add(line);
						break;
				}
			}

			// UTF-8 without a BOM, like every alias writer.
			File.WriteAllLines(file.FilePath, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}

		return (leftOut, renamed);
	}

	/// <summary>
	/// The choices in <paramref name="saved"/> with each of <paramref name="chosen"/> added, replacing
	/// any saved choice for the same line.
	/// </summary>
	/// <param name="saved">The choices saved so far.</param>
	/// <param name="chosen">The choices just made.</param>
	/// <returns>Every choice to save.</returns>
	public static IReadOnlyList<DuplicateAliasRule> Merge(IReadOnlyList<DuplicateAliasRule> saved, IReadOnlyList<DuplicateAliasRule> chosen)
	{
		ArgumentNullException.ThrowIfNull(saved);
		ArgumentNullException.ThrowIfNull(chosen);

		return [.. saved.Where(old => !chosen.Any(choice => SameLine(old, choice))), .. chosen];
	}

	/// <summary>Whether two choices are for the same line.</summary>
	/// <param name="a">One choice.</param>
	/// <param name="b">The other.</param>
	/// <returns><see langword="true"/> when the file, the command and the occurrence match.</returns>
	public static bool SameLine(DuplicateAliasRule a, DuplicateAliasRule b)
	{
		ArgumentNullException.ThrowIfNull(a);
		ArgumentNullException.ThrowIfNull(b);

		return a.Occurrence == b.Occurrence
			&& a.FileKey.Equals(b.FileKey, StringComparison.OrdinalIgnoreCase)
			&& a.Command.Equals(b.Command, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>The choices as saved: one per line, <c>FileKey|Command|Occurrence|Action|NewCommand</c>.</summary>
	/// <param name="choices">The choices.</param>
	/// <returns>The saved text; empty for none.</returns>
	public static string ToConfig(IEnumerable<DuplicateAliasRule> choices)
	{
		ArgumentNullException.ThrowIfNull(choices);

		return string.Join('\n', choices.Select(choice => string.Join(Separator,
			choice.FileKey,
			choice.Command,
			choice.Occurrence.ToString(CultureInfo.InvariantCulture),
			choice.Action.ToString(),
			choice.Action == DuplicateAliasAction.Rename ? choice.NewCommand?.Trim() : string.Empty)));
	}

	/// <summary>Reads saved choices. A line that isn't a whole, sensible choice is skipped.</summary>
	/// <param name="saved">The saved text (<see cref="ToConfig"/>), or <see langword="null"/>.</param>
	/// <returns>The choices.</returns>
	public static IReadOnlyList<DuplicateAliasRule> FromConfig(string? saved)
	{
		List<DuplicateAliasRule> choices = [];

		foreach (string line in (saved ?? string.Empty).ReplaceLineEndings("\n").Split('\n'))
		{
			string[] parts = line.Split(Separator);

			if (parts.Length != 5
				|| parts[0].Trim().Length == 0
				|| NewCommandProblem(parts[1]) is not null
				|| !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out int occurrence)
				|| occurrence < 1
				|| !Enum.TryParse(parts[3], ignoreCase: true, out DuplicateAliasAction action)
				|| !Enum.IsDefined(action)
				|| (action == DuplicateAliasAction.Rename && NewCommandProblem(parts[4]) is not null))
			{
				continue;
			}

			choices.Add(new DuplicateAliasRule(
				parts[0].Trim(), parts[1].Trim(), occurrence, action,
				action == DuplicateAliasAction.Rename ? parts[4].Trim() : null));
		}

		return choices;
	}
}
