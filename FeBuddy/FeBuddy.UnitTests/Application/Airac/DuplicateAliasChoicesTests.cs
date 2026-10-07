using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.UnitTests.Application.Airac;

/// <summary>
/// Covers <see cref="DuplicateAliasChoices"/> (issue #318): what a new command can be, when choices
/// settle a duplicated command, sorting a run's duplicates by the saved choices, making the choices
/// in the alias files, and saving them.
/// </summary>
public sealed class DuplicateAliasChoicesTests : IDisposable
{
	private const DuplicateAliasAction Keep = DuplicateAliasAction.Keep;
	private const DuplicateAliasAction Ignore = DuplicateAliasAction.Ignore;
	private const DuplicateAliasAction Rename = DuplicateAliasAction.Rename;

	private static readonly HashSet<string> Nothing = new(StringComparer.OrdinalIgnoreCase);

	private readonly string _directory =
		Path.Combine(Path.GetTempPath(), "FeBuddyTests_DuplicateAliasChoices_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_directory))
		{
			Directory.Delete(_directory, recursive: true);
		}
	}

	/// <summary>.orfNUTIYf in Departures.txt and Arrivals.txt.</summary>
	private static DuplicateAliasCommand Nutiy() => new(".orfNUTIYf",
	[
		new DuplicateAliasLine("Departures.txt", "Departures.txt", ".orfNUTIYf .FF A B", "ZDC"),
		new DuplicateAliasLine("Arrivals.txt", "Arrivals.txt", ".ORFNUTIYF .FF C D", "ZDC"),
	]);

	private static DuplicateAliasRule Choice(string fileKey, DuplicateAliasAction action, string? newCommand = null, string command = ".orfNUTIYf", int occurrence = 1) =>
		new(fileKey, command, occurrence, action, newCommand);

	private static HashSet<string> Taken(params string[] commands) => new(commands, StringComparer.OrdinalIgnoreCase);

	private AliasFileWritten AliasFile(string fileName, params string[] lines)
	{
		Directory.CreateDirectory(_directory);
		string path = Path.Combine(_directory, fileName);
		File.WriteAllLines(path, lines);
		return new AliasFileWritten(fileName, path);
	}

	// ---- one line, one command ----

	[Theory]
	[InlineData(".orfNUTIYa", null)]
	[InlineData("  .x  ", null)]
	[InlineData("", "Type the new command.")]
	[InlineData(null, "Type the new command.")]
	[InlineData("orfNUTIYa", "A command starts with a dot and has at least one more character, such as .orfNUTIYa.")]
	[InlineData(".", "A command starts with a dot and has at least one more character, such as .orfNUTIYa.")]
	[InlineData(".orf NUTIY", "A command can't have a space in it.")]
	[InlineData(".orf|NUTIY", "A command can't have a | in it.")]
	public void a_new_command_starts_with_a_dot_and_has_no_space(string? newCommand, string? problem) =>
		Assert.Equal(problem, DuplicateAliasChoices.NewCommandProblem(newCommand));

	[Fact]
	public void a_choice_is_for_its_file_command_and_occurrence_ignoring_case()
	{
		DuplicateAliasLine line = Nutiy().Lines[1];

		Assert.True(Choice("ARRIVALS.TXT", Keep, command: ".orfnutiyf").IsFor(".ORFNUTIYF", line));
		Assert.False(Choice("Departures.txt", Keep).IsFor(".orfNUTIYf", line));
		Assert.False(Choice("Arrivals.txt", Keep, occurrence: 2).IsFor(".orfNUTIYf", line));
		Assert.False(Choice("Arrivals.txt", Keep, command: ".orfOTHERf").IsFor(".orfNUTIYf", line));
	}

	// ---- whether choices settle a command ----

	[Fact]
	public void keeping_one_line_and_leaving_out_or_renaming_the_rest_settles_a_command()
	{
		Assert.Null(DuplicateAliasChoices.Problem(Nutiy(), [Choice("Departures.txt", Keep), Choice("Arrivals.txt", Ignore)], Nothing, Nothing));
		Assert.Null(DuplicateAliasChoices.Problem(Nutiy(), [Choice("Departures.txt", Ignore), Choice("Arrivals.txt", Rename, " .orfNUTIYa ")], Nothing, Nothing));
	}

	[Fact]
	public void every_line_needs_a_choice_and_only_one_can_keep_the_command()
	{
		Assert.Equal("Choose what happens to each line of .orfNUTIYf.",
			DuplicateAliasChoices.Problem(Nutiy(), [Choice("Departures.txt", Keep), null], Nothing, Nothing));
		Assert.Equal("Choose what happens to each line of .orfNUTIYf.",
			DuplicateAliasChoices.Problem(Nutiy(), [Choice("Departures.txt", Keep)], Nothing, Nothing));
		Assert.Equal("Only one line can keep .orfNUTIYf. Leave out or rename the others.",
			DuplicateAliasChoices.Problem(Nutiy(), [Choice("Departures.txt", Keep), Choice("Arrivals.txt", Keep)], Nothing, Nothing));
	}

	[Fact]
	public void a_new_command_must_be_well_formed_and_free()
	{
		DuplicateAliasRule keep = Choice("Departures.txt", Keep);

		Assert.Equal("Type the new command.",
			DuplicateAliasChoices.Problem(Nutiy(), [keep, Choice("Arrivals.txt", Rename)], Nothing, Nothing));
		Assert.Equal(".ZOBATIS is already used in this run. Choose another command.",
			DuplicateAliasChoices.Problem(Nutiy(), [keep, Choice("Arrivals.txt", Rename, ".ZOBATIS")], Taken(".zobATIS"), Nothing));
		Assert.Equal(".orfNUTIYa is already given to another line. Choose another command.",
			DuplicateAliasChoices.Problem(Nutiy(), [keep, Choice("Arrivals.txt", Rename, ".orfNUTIYa")], Nothing, Taken(".ORFNUTIYA")));
		Assert.Equal(".X is already given to another line. Choose another command.",
			DuplicateAliasChoices.Problem(Nutiy(), [Choice("Departures.txt", Rename, ".x"), Choice("Arrivals.txt", Rename, ".X")], Nothing, Nothing));
	}

	// ---- sorting a run's duplicates ----

	[Fact]
	public void the_plan_settles_what_the_choices_cover_and_finds_those_for_no_duplicate()
	{
		DuplicateAliasCommand other = new(".dtwI22Lc",
		[
			new DuplicateAliasLine("Faa_Chart_Recall.txt", "Faa_Chart_Recall.txt", ".dtwI22Lc .OPENURL a", "ZOB", 1),
			new DuplicateAliasLine("Faa_Chart_Recall.txt", "Faa_Chart_Recall.txt", ".dtwI22Lc .OPENURL b", "ZOB", 2),
		]);
		DuplicateAliasRule keep = Choice("Departures.txt", Keep);
		DuplicateAliasRule ignore = Choice("Arrivals.txt", Ignore);
		DuplicateAliasRule half = Choice("Faa_Chart_Recall.txt", Keep, command: ".dtwI22Lc");
		DuplicateAliasRule gone = Choice("Airports.txt", Ignore, command: ".aptGONE");

		DuplicateAliasPlan plan = DuplicateAliasChoices.Plan([Nutiy(), other], [gone, ignore, keep, half], Nothing);

		SettledDuplicate settled = Assert.Single(plan.Settled);
		Assert.Equal(".orfNUTIYf", settled.Duplicate.Command);
		Assert.Equal([keep, ignore], settled.Choices);
		Assert.Equal(".dtwI22Lc", Assert.Single(plan.Unsettled).Command);
		Assert.Equal([gone], plan.Unused);
	}

	/// <summary>Two commands can't both be renamed to one new command: the second stays unsettled.</summary>
	[Fact]
	public void a_new_command_given_out_by_one_duplicate_unsettles_another()
	{
		DuplicateAliasCommand second = new(".orfOTHERf",
		[
			new DuplicateAliasLine("Departures.txt", "Departures.txt", ".orfOTHERf .FF A", "ZDC"),
			new DuplicateAliasLine("Arrivals.txt", "Arrivals.txt", ".orfOTHERf .FF B", "ZDC"),
		]);

		DuplicateAliasPlan plan = DuplicateAliasChoices.Plan(
			[Nutiy(), second],
			[
				Choice("Departures.txt", Keep), Choice("Arrivals.txt", Rename, ".orfX"),
				Choice("Departures.txt", Keep, command: ".orfOTHERf"), Choice("Arrivals.txt", Rename, ".ORFX", command: ".orfOTHERf"),
			],
			Nothing);

		Assert.Equal(".orfNUTIYf", Assert.Single(plan.Settled).Duplicate.Command);
		Assert.Equal(".orfOTHERf", Assert.Single(plan.Unsettled).Command);
		Assert.Equal([".orfX"], DuplicateAliasChoices.NewCommands(plan.Settled[0].Choices));
	}

	// ---- making the choices ----

	[Fact]
	public void the_choices_drop_and_rename_their_lines_and_leave_every_other_line_alone()
	{
		AliasFileWritten recall = AliasFile("Faa_Chart_Recall.txt",
			"; a comment",
			".dtwI22Lc .OPENURL first",
			"",
			"  .DTWI22LC .OPENURL second",
			".dtwI22Lc .OPENURL third",
			".dtwI21Rc .OPENURL other");
		AliasFileWritten untouched = AliasFile("Airways.txt", ".J60F .FF A B");
		string before = File.ReadAllText(untouched.FilePath);

		(int leftOut, int renamed) = DuplicateAliasChoices.Apply(
			[recall, untouched],
			[
				Choice("Faa_Chart_Recall.txt", Keep, command: ".dtwI22Lc", occurrence: 1),
				Choice("faa_chart_recall.txt", Rename, " .dtwI22Lc2 ", command: ".dtwi22lc", occurrence: 2),
				Choice("Faa_Chart_Recall.txt", Ignore, command: ".dtwI22Lc", occurrence: 3),
			]);

		Assert.Equal((1, 1), (leftOut, renamed));
		Assert.Equal(
			["; a comment", ".dtwI22Lc .OPENURL first", "", "  .dtwI22Lc2 .OPENURL second", ".dtwI21Rc .OPENURL other"],
			File.ReadAllLines(recall.FilePath));
		Assert.Equal(before, File.ReadAllText(untouched.FilePath));
	}

	[Fact]
	public void the_run_uses_every_command_in_its_alias_files_and_the_custom_ones()
	{
		AliasFileWritten airways = AliasFile("Airways.txt", ".J60F .FF A B", "; .notACommand", "");

		HashSet<string> commands = DuplicateAliasChoices.CommandsIn([airways], [".zobATIS .MSG X\r\n\r\n.zobWX .MSG Y"]);

		Assert.Equal([".J60F", ".zobATIS", ".zobWX"], commands.Order(StringComparer.Ordinal));
		Assert.Contains(".j60f", commands);
	}

	// ---- saving them ----

	[Fact]
	public void a_new_choice_replaces_the_saved_one_for_its_line()
	{
		DuplicateAliasRule savedKeep = Choice("Departures.txt", Keep);
		DuplicateAliasRule savedOther = Choice("Airports.txt", Ignore, command: ".aptX");
		DuplicateAliasRule newIgnore = Choice("DEPARTURES.TXT", Ignore, command: ".ORFNUTIYF");

		Assert.Equal([savedOther, newIgnore], DuplicateAliasChoices.Merge([savedKeep, savedOther], [newIgnore]));
		Assert.True(DuplicateAliasChoices.SameLine(savedKeep, newIgnore));
		Assert.False(DuplicateAliasChoices.SameLine(savedKeep, savedKeep with { Occurrence = 2 }));
	}

	[Fact]
	public void choices_are_saved_one_per_line_and_read_back()
	{
		DuplicateAliasRule[] choices =
		[
			Choice("Departures.txt", Keep),
			Choice("Arrivals.txt", Rename, ".orfNUTIYa"),
			Choice("Faa_Chart_Recall.txt", Ignore, command: ".dtwI22Lc", occurrence: 2),
		];

		string saved = DuplicateAliasChoices.ToConfig(choices);

		Assert.Equal(
			"Departures.txt|.orfNUTIYf|1|Keep|\nArrivals.txt|.orfNUTIYf|1|Rename|.orfNUTIYa\nFaa_Chart_Recall.txt|.dtwI22Lc|2|Ignore|",
			saved);
		Assert.Equal(choices, DuplicateAliasChoices.FromConfig(saved));
		Assert.Empty(DuplicateAliasChoices.FromConfig(null));
		Assert.Empty(DuplicateAliasChoices.ToConfig([]));
	}

	/// <summary>A line edited by hand into something that isn't a whole choice is skipped; the rest still count.</summary>
	[Theory]
	[InlineData("Departures.txt|.orfNUTIYf|1|Keep")]
	[InlineData(" |.orfNUTIYf|1|Keep|")]
	[InlineData("Departures.txt|orfNUTIYf|1|Keep|")]
	[InlineData("Departures.txt|.orfNUTIYf|0|Keep|")]
	[InlineData("Departures.txt|.orfNUTIYf|one|Keep|")]
	[InlineData("Departures.txt|.orfNUTIYf|1|Hide|")]
	[InlineData("Departures.txt|.orfNUTIYf|1|7|")]
	[InlineData("Departures.txt|.orfNUTIYf|1|Rename|")]
	[InlineData("")]
	public void a_line_that_is_not_a_whole_choice_is_skipped(string line)
	{
		DuplicateAliasRule good = Choice("Arrivals.txt", Ignore);

		Assert.Equal([good], DuplicateAliasChoices.FromConfig(line + "\r\n" + DuplicateAliasChoices.ToConfig([good])));
	}

	[Fact]
	public void a_keep_or_ignore_choice_saves_no_new_command()
	{
		Assert.Equal("Departures.txt|.orfNUTIYf|1|Keep|", DuplicateAliasChoices.ToConfig([Choice("Departures.txt", Keep, ".stale")]));
		Assert.Null(Assert.Single(DuplicateAliasChoices.FromConfig("Departures.txt|.orfNUTIYf|1|ignore|.stale")).NewCommand);
	}
}
