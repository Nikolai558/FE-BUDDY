using System.Globalization;
using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;

using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>One saved choice for a duplicated alias command, as the Preview Settings tab lists it.</summary>
public sealed class SavedDuplicateChoiceRow
{
	/// <summary>Creates the row.</summary>
	/// <param name="choice">The saved choice.</param>
	/// <param name="remove">Removes it from the saved choices.</param>
	public SavedDuplicateChoiceRow(DuplicateAliasRule choice, Action<DuplicateAliasRule> remove)
	{
		ArgumentNullException.ThrowIfNull(choice);
		ArgumentNullException.ThrowIfNull(remove);

		Choice = choice;
		RemoveCommand = new RelayCommand(() => remove(choice));
	}

	/// <summary>The saved choice.</summary>
	public DuplicateAliasRule Choice { get; }

	/// <summary>The command and the line, e.g. <c>.orfNUTIYf in Arrivals.txt</c>, or <c>... (line 2)</c> for a later line of the same file.</summary>
	public string Line => Choice.Occurrence == 1
		? $"{Choice.Command} in {Choice.FileKey}"
		: string.Create(CultureInfo.InvariantCulture, $"{Choice.Command} in {Choice.FileKey} (line {Choice.Occurrence} with it)");

	/// <summary>What happens to it, e.g. <c>Rename to .orfNUTIYa</c>.</summary>
	public string Action => Choice.Action switch
	{
		DuplicateAliasAction.Keep => "Keep",
		DuplicateAliasAction.Ignore => "Leave out",
		_ => $"Rename to {Choice.NewCommand}",
	};

	/// <summary>Removes the choice, so the next run asks again (or lists the duplicate).</summary>
	public ICommand RemoveCommand { get; }
}
