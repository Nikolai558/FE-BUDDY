using System.Collections.ObjectModel;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Infrastructure.Configuration;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The Duplicate Alias Commands card on the AIRAC Service's Preview Settings tab (issue #318): what
/// the run does with a command more than one alias line uses - list it in
/// <c>Duplicate_Alias_Commands.txt</c>, or stop for the user to choose - and the choices saved from
/// earlier runs, each of which can be removed.
/// </summary>
/// <remarks>
/// Both are run-wide, not any one tab's, so they save as soon as they change rather than with a
/// tab's Save. The saved choices are made in every run, whichever way the card is set.
/// </remarks>
public sealed class DuplicateAliasesCardViewModel : ObservableObject, IPreviewOptions
{
	/// <summary>The settings node both values are saved under.</summary>
	public const string Node = "Services.AiracService.DuplicateAliases";

	private readonly Func<bool> _runWritesAliasFile;
	private bool _reviewDuplicates;
	private bool _appliesToRun;

	/// <summary>Creates the card and reads what is saved.</summary>
	/// <param name="runWritesAliasFile">Whether the run, as the tabs stand, writes any alias file; the card shows only then.</param>
	public DuplicateAliasesCardViewModel(Func<bool> runWritesAliasFile)
	{
		ArgumentNullException.ThrowIfNull(runWritesAliasFile);

		_runWritesAliasFile = runWritesAliasFile;
		RemoveAllCommand = new RelayCommand(() => Write([]), () => SavedChoices.Count > 0);
		Refresh();
	}

	/// <summary>
	/// <see langword="true"/> to stop the run for the user to choose; <see langword="false"/> (the
	/// default) to list the duplicates in <c>Duplicate_Alias_Commands.txt</c>. Saved at once.
	/// </summary>
	public bool ReviewDuplicates
	{
		get => _reviewDuplicates;
		set
		{
			if (SetProperty(ref _reviewDuplicates, value))
			{
				UserConfigFile.TrySetValue(UserConfigKeys.DuplicateAliasesReview, value ? "Y" : "N");
				UserConfigFile.Save(Node);
			}
		}
	}

	/// <summary>Whether the run writes an alias file, so there can be duplicates to handle.</summary>
	public bool AppliesToRun
	{
		get => _appliesToRun;
		private set => SetProperty(ref _appliesToRun, value);
	}

	/// <summary>The choices saved from earlier runs, in the order they were made.</summary>
	public ObservableCollection<SavedDuplicateChoiceRow> SavedChoices { get; } = [];

	/// <summary>Whether any choice is saved.</summary>
	public bool HasSavedChoices => SavedChoices.Count > 0;

	/// <summary>Removes every saved choice.</summary>
	public ICommand RemoveAllCommand { get; }

	/// <summary>The saved choices, as the run takes them.</summary>
	/// <returns>The choices; none when nothing is saved.</returns>
	public static IReadOnlyList<DuplicateAliasRule> LoadChoices() =>
		DuplicateAliasChoices.FromConfig(UserConfigFile.GetValue(UserConfigKeys.DuplicateAliasesChoices));

	/// <summary>Whether the run is to stop for the user's choices, as saved.</summary>
	/// <returns><see langword="true"/> when it is.</returns>
	public static bool LoadReview() =>
		string.Equals(UserConfigFile.GetValue(UserConfigKeys.DuplicateAliasesReview)?.Trim(), "Y", StringComparison.OrdinalIgnoreCase);

	/// <inheritdoc />
	public void Refresh()
	{
		_reviewDuplicates = LoadReview();
		OnPropertyChanged(nameof(ReviewDuplicates));
		AppliesToRun = _runWritesAliasFile();
		ShowChoices(LoadChoices());
	}

	/// <summary>Saves the choices a run's review made, replacing any saved choice for the same line.</summary>
	/// <param name="made">The choices.</param>
	public void SaveChoices(IReadOnlyList<DuplicateAliasRule> made)
	{
		ArgumentNullException.ThrowIfNull(made);

		if (made.Count > 0)
		{
			Write(DuplicateAliasChoices.Merge(LoadChoices(), made));
		}
	}

	private void Remove(DuplicateAliasRule choice) =>
		Write([.. LoadChoices().Where(saved => !DuplicateAliasChoices.SameLine(saved, choice))]);

	private void Write(IReadOnlyList<DuplicateAliasRule> choices)
	{
		UserConfigFile.TrySetValue(UserConfigKeys.DuplicateAliasesChoices, DuplicateAliasChoices.ToConfig(choices));
		UserConfigFile.Save(Node);
		ShowChoices(choices);
	}

	private void ShowChoices(IReadOnlyList<DuplicateAliasRule> choices)
	{
		SavedChoices.Clear();

		foreach (DuplicateAliasRule choice in choices)
		{
			SavedChoices.Add(new SavedDuplicateChoiceRow(choice, Remove));
		}

		OnPropertyChanged(nameof(HasSavedChoices));
		CommandManager.InvalidateRequerySuggested();
	}
}
