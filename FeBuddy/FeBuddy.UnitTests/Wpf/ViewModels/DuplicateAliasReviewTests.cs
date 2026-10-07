using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.Models;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers choosing for duplicated alias commands in the app (issue #318): the window the run opens
/// (<see cref="DuplicateAliasReviewViewModel"/>) - where each line starts, checking each command as
/// the choices change, finishing and stopping - and the Preview Settings card
/// (<see cref="DuplicateAliasesCardViewModel"/>) that sets whether the run stops and lists the saved
/// choices, against a throwaway config.
/// </summary>
[Collection("AppLog")]
public sealed class DuplicateAliasReviewTests : IDisposable
{
	private const DuplicateAliasAction Keep = DuplicateAliasAction.Keep;
	private const DuplicateAliasAction Ignore = DuplicateAliasAction.Ignore;
	private const DuplicateAliasAction Rename = DuplicateAliasAction.Rename;

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_DuplicateAliasReview_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public DuplicateAliasReviewTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(Path.Combine(_root, "config"));
	}

	/// <summary>Restores the real config and log, and deletes the folder.</summary>
	public void Dispose()
	{
		UserConfigFile.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch (IOException)
		{
			// Best-effort cleanup.
		}
	}

	/// <summary>A command used by one line of each of the files named.</summary>
	private static DuplicateAliasCommand Duplicate(string command, params string[] fileKeys) =>
		new(command, [.. fileKeys.Select(file => new DuplicateAliasLine(file, file, $"  {command} .FF A B", "ZDC"))]);

	private static DuplicateAliasReviewViewModel Window(IReadOnlyList<DuplicateAliasRule>? saved = null, params DuplicateAliasCommand[] duplicates) =>
		new(new DuplicateAliasReview(duplicates, saved ?? [], new HashSet<string>([".zobATIS", .. duplicates.Select(d => d.Command)], StringComparer.OrdinalIgnoreCase)));

	// ---- the window ----

	/// <summary>With nothing saved, the first line keeps the command and the rest are left out, so finishing straight away is allowed.</summary>
	[Fact]
	public void each_command_starts_with_its_first_line_kept_and_the_rest_left_out()
	{
		DuplicateAliasReviewViewModel window = Window(null, Duplicate(".orfNUTIYf", "Departures.txt", "Arrivals.txt", "Faa_Chart_Recall.txt"));

		DuplicateAliasGroupItem group = Assert.Single(window.Groups);
		Assert.Equal([Keep, Ignore, Ignore], group.Lines.Select(line => line.Action));
		Assert.Equal("3 lines", group.LineCount);
		Assert.Equal(".orfNUTIYf .FF A B", group.Lines[0].Text);
		Assert.Equal("Departures.txt", group.Lines[0].FileName);
		Assert.NotEqual(group.Lines[0].GroupName, group.Lines[1].GroupName);
		Assert.True(window.CanFinish);
		Assert.StartsWith("One alias command is used by more than one line", window.Intro, StringComparison.Ordinal);
	}

	[Fact]
	public void a_saved_choice_fills_its_line_and_a_saved_keep_leaves_the_rest_left_out()
	{
		DuplicateAliasRule savedKeep = new("Arrivals.txt", ".orfNUTIYf", 1, Keep);
		DuplicateAliasRule savedRename = new("Departures.txt", ".orfOTHERf", 1, Rename, ".orfOTHERa");

		DuplicateAliasReviewViewModel window = Window(
			[savedKeep, savedRename],
			Duplicate(".orfNUTIYf", "Departures.txt", "Arrivals.txt"),
			Duplicate(".orfOTHERf", "Departures.txt", "Arrivals.txt"));

		Assert.Equal([Ignore, Keep], window.Groups[0].Lines.Select(line => line.Action));
		Assert.Equal([Rename, Keep], window.Groups[1].Lines.Select(line => line.Action));
		Assert.Equal(".orfOTHERa", window.Groups[1].Lines[0].NewCommand);
		Assert.True(window.Groups[1].Lines[0].IsRename);
		Assert.StartsWith("2 alias commands are used by more than one line", window.Intro, StringComparison.Ordinal);
	}

	[Fact]
	public void each_command_is_checked_as_its_choices_change()
	{
		DuplicateAliasReviewViewModel window = Window(null, Duplicate(".orfNUTIYf", "Departures.txt", "Arrivals.txt"));
		DuplicateAliasGroupItem group = window.Groups[0];
		List<string?> changed = [];
		group.Lines[1].PropertyChanged += (_, e) => changed.Add(e.PropertyName);

		group.Lines[1].Action = Keep;
		Assert.Equal("Only one line can keep .orfNUTIYf. Leave out or rename the others.", group.Problem);
		Assert.True(group.HasProblem);
		Assert.False(window.CanFinish);

		group.Lines[1].Action = Rename;
		Assert.Equal("Type the new command.", group.Problem);
		Assert.Contains(nameof(DuplicateAliasLineItem.IsRename), changed);

		group.Lines[1].NewCommand = ".ZOBATIS";
		Assert.Equal(".ZOBATIS is already used in this run. Choose another command.", group.Problem);

		group.Lines[1].NewCommand = " .orfNUTIYa ";
		Assert.Null(group.Problem);
		Assert.True(window.CanFinish);
	}

	/// <summary>Two commands given one new command: both say so, so either can be changed.</summary>
	[Fact]
	public void a_new_command_given_twice_is_flagged_on_both_commands()
	{
		DuplicateAliasReviewViewModel window = Window(
			null,
			Duplicate(".orfNUTIYf", "Departures.txt", "Arrivals.txt"),
			Duplicate(".orfOTHERf", "Departures.txt", "Arrivals.txt"));

		window.Groups[0].Lines[1].Action = Rename;
		window.Groups[0].Lines[1].NewCommand = ".orfX";
		window.Groups[1].Lines[1].Action = Rename;
		window.Groups[1].Lines[1].NewCommand = ".ORFX";

		Assert.Equal(".orfX is already given to another line. Choose another command.", window.Groups[0].Problem);
		Assert.Equal(".ORFX is already given to another line. Choose another command.", window.Groups[1].Problem);
		Assert.False(window.FinishCommand.CanExecute(null));
	}

	[Fact]
	public void finishing_hands_back_a_choice_for_every_line()
	{
		DuplicateAliasReviewViewModel window = Window(null, Duplicate(".orfNUTIYf", "Departures.txt", "Arrivals.txt"));
		window.Groups[0].Lines[1].Action = Rename;
		window.Groups[0].Lines[1].NewCommand = ".orfNUTIYa ";
		bool closed = false;
		window.CloseRequested += (_, _) => closed = true;

		window.FinishCommand.Execute(null);

		Assert.True(closed);
		Assert.Equal(
			[new DuplicateAliasRule("Departures.txt", ".orfNUTIYf", 1, Keep), new DuplicateAliasRule("Arrivals.txt", ".orfNUTIYf", 1, Rename, ".orfNUTIYa")],
			window.Result);
	}

	[Fact]
	public void stopping_hands_back_nothing()
	{
		DuplicateAliasReviewViewModel window = Window(null, Duplicate(".orfNUTIYf", "Departures.txt", "Arrivals.txt"));
		bool closed = false;
		window.CloseRequested += (_, _) => closed = true;

		window.StopCommand.Execute(null);

		Assert.True(closed);
		Assert.Null(window.Result);
	}

	/// <summary>A row for a line left out or kept saves no new command, even one typed before.</summary>
	[Fact]
	public void a_line_not_renamed_gives_no_new_command()
	{
		DuplicateAliasReviewViewModel window = Window(null, Duplicate(".orfNUTIYf", "Departures.txt", "Arrivals.txt"));
		DuplicateAliasLineItem line = window.Groups[0].Lines[1];

		line.NewCommand = ".typed";

		Assert.Null(line.ToChoice().NewCommand);
		Assert.Equal(Ignore, line.ToChoice().Action);
	}

	// ---- the Preview Settings card ----

	[Fact]
	public void the_card_saves_whether_the_run_stops_at_once()
	{
		DuplicateAliasesCardViewModel card = new(() => true);
		Assert.False(card.ReviewDuplicates);
		Assert.True(card.AppliesToRun);

		card.ReviewDuplicates = true;

		UserConfigFile.ReadAll();
		Assert.Equal("Y", UserConfigFile.GetValue(UserConfigKeys.DuplicateAliasesReview));
		Assert.True(DuplicateAliasesCardViewModel.LoadReview());
		Assert.False(new DuplicateAliasesCardViewModel(() => false).AppliesToRun);
	}

	[Fact]
	public void a_runs_choices_are_saved_and_listed_and_each_can_be_removed()
	{
		DuplicateAliasesCardViewModel card = new(() => true);
		DuplicateAliasRule keep = new("Departures.txt", ".orfNUTIYf", 1, Keep);
		DuplicateAliasRule rename = new("Arrivals.txt", ".orfNUTIYf", 1, Rename, ".orfNUTIYa");
		DuplicateAliasRule ignoreSecond = new("Faa_Chart_Recall.txt", ".dtwI22Lc", 2, Ignore);

		card.SaveChoices([]);
		Assert.False(card.HasSavedChoices);
		Assert.False(card.RemoveAllCommand.CanExecute(null));

		card.SaveChoices([keep, rename, ignoreSecond]);
		card.SaveChoices([keep with { Action = Ignore }]);

		UserConfigFile.ReadAll();
		Assert.Equal([rename, ignoreSecond, keep with { Action = Ignore }], DuplicateAliasesCardViewModel.LoadChoices());
		Assert.Equal(
			[
				(".orfNUTIYf in Arrivals.txt", "Rename to .orfNUTIYa"),
				(".dtwI22Lc in Faa_Chart_Recall.txt (line 2 with it)", "Leave out"),
				(".orfNUTIYf in Departures.txt", "Leave out"),
			],
			card.SavedChoices.Select(row => (row.Line, row.Action)));

		card.SavedChoices[0].RemoveCommand.Execute(null);
		Assert.Equal([ignoreSecond, keep with { Action = Ignore }], DuplicateAliasesCardViewModel.LoadChoices());

		card.RemoveAllCommand.Execute(null);
		Assert.Empty(DuplicateAliasesCardViewModel.LoadChoices());
		Assert.False(card.HasSavedChoices);
	}

	[Fact]
	public void a_saved_keep_is_listed_as_keep()
	{
		Assert.Equal("Keep", new SavedDuplicateChoiceRow(new DuplicateAliasRule("Departures.txt", ".x", 1, Keep), _ => { }).Action);
	}
}
