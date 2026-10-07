using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.Models;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The window the AIRAC run opens when it stops at duplicated alias commands (issue #318): for each
/// command, which line keeps it and which are left out or renamed. Finishing hands the choices back
/// to the run, which makes them and saves them; stopping (or closing the window) ends the run with no
/// alias file saved.
/// </summary>
/// <remarks>
/// A line starts with its saved choice, when there is one. Otherwise the first line of a command
/// with no line kept keeps it, and the rest are left out, so finishing straight away does what
/// CRC would have done anyway: run the first. Each command is checked as the choices change, by
/// the run's own rules (<see cref="DuplicateAliasChoices.Problem"/>) - a new command against every
/// command in the run and every other new command.
/// </remarks>
public sealed class DuplicateAliasReviewViewModel : ObservableObject
{
	private readonly IReadOnlySet<string> _taken;
	private bool _canFinish;

	/// <summary>Creates the window's model.</summary>
	/// <param name="review">What the run asks.</param>
	public DuplicateAliasReviewViewModel(DuplicateAliasReview review)
	{
		ArgumentNullException.ThrowIfNull(review);

		_taken = review.TakenCommands;
		Groups = [.. review.Duplicates.Select(duplicate => NewGroup(duplicate, review.SavedChoices))];
		FinishCommand = new RelayCommand(() => Close(Choices()), () => CanFinish);
		StopCommand = new RelayCommand(() => Close(null));

		Revalidate();
	}

	/// <summary>Raised when the window should close.</summary>
	public event EventHandler? CloseRequested;

	/// <summary>Each duplicated command.</summary>
	public IReadOnlyList<DuplicateAliasGroupItem> Groups { get; }

	/// <summary>The window's opening sentence.</summary>
	public string Intro => Groups.Count == 1
		? "One alias command is used by more than one line, so CRC can only run one of them. Keep one line, and leave out or rename the others."
		: $"{Groups.Count} alias commands are used by more than one line, so CRC can only run one line of each. For each command, keep one line, and leave out or rename the others.";

	/// <summary>Whether every command is settled, so the run can go on.</summary>
	public bool CanFinish
	{
		get => _canFinish;
		private set => SetProperty(ref _canFinish, value);
	}

	/// <summary>The choices, once finished; <see langword="null"/> when the user stopped the run.</summary>
	public IReadOnlyList<DuplicateAliasRule>? Result { get; private set; }

	/// <summary>Hands the choices to the run.</summary>
	public ICommand FinishCommand { get; }

	/// <summary>Stops the run.</summary>
	public ICommand StopCommand { get; }

	private DuplicateAliasGroupItem NewGroup(DuplicateAliasCommand duplicate, IReadOnlyList<DuplicateAliasRule> saved)
	{
		DuplicateAliasRule?[] savedChoices = [.. duplicate.Lines.Select(line => saved.FirstOrDefault(choice => choice.IsFor(duplicate.Command, line)))];
		bool kept = savedChoices.Any(choice => choice?.Action == DuplicateAliasAction.Keep);
		List<DuplicateAliasLineItem> lines = [];

		for (int i = 0; i < duplicate.Lines.Count; i++)
		{
			DuplicateAliasAction action = savedChoices[i]?.Action ?? (kept ? DuplicateAliasAction.Ignore : DuplicateAliasAction.Keep);
			kept |= action == DuplicateAliasAction.Keep;

			lines.Add(new DuplicateAliasLineItem(duplicate.Command, duplicate.Lines[i], action, savedChoices[i]?.NewCommand, Revalidate));
		}

		return new DuplicateAliasGroupItem(duplicate.Command, lines);
	}

	/// <summary>Checks every command again: a new command in one can clash with another's.</summary>
	private void Revalidate()
	{
		foreach (DuplicateAliasGroupItem group in Groups)
		{
			HashSet<string> givenOut = new(
				DuplicateAliasChoices.NewCommands(Groups.Where(other => !ReferenceEquals(other, group)).SelectMany(other => other.Lines).Select(line => line.ToChoice())),
				StringComparer.OrdinalIgnoreCase);

			group.Problem = DuplicateAliasChoices.Problem(
				new DuplicateAliasCommand(group.Command, [.. group.Lines.Select(line => line.Line)]),
				[.. group.Lines.Select(line => line.ToChoice())],
				_taken,
				givenOut);
		}

		CanFinish = Groups.All(group => !group.HasProblem);
		CommandManager.InvalidateRequerySuggested();
	}

	private DuplicateAliasRule[] Choices() => [.. Groups.SelectMany(group => group.Lines).Select(line => line.ToChoice())];

	private void Close(IReadOnlyList<DuplicateAliasRule>? result)
	{
		Result = result;
		CloseRequested?.Invoke(this, EventArgs.Empty);
	}
}
