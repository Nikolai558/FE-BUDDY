using FeBuddy.Wpf.Mvvm;

using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>One line of a duplicated alias command in the review window: what to do with it.</summary>
public sealed class DuplicateAliasLineItem : ObservableObject
{
	private readonly Action _onChanged;
	private DuplicateAliasAction _action;
	private string _newCommand;

	/// <summary>Creates the line's row.</summary>
	/// <param name="command">The duplicated command.</param>
	/// <param name="line">The line.</param>
	/// <param name="action">What to do with it, to start.</param>
	/// <param name="newCommand">Its new command, to start, for a rename.</param>
	/// <param name="onChanged">Called when the choice changes, to check the command again.</param>
	public DuplicateAliasLineItem(string command, DuplicateAliasLine line, DuplicateAliasAction action, string? newCommand, Action onChanged)
	{
		ArgumentNullException.ThrowIfNull(command);
		ArgumentNullException.ThrowIfNull(line);
		ArgumentNullException.ThrowIfNull(onChanged);

		Command = command;
		Line = line;
		_action = action;
		_newCommand = newCommand ?? string.Empty;
		_onChanged = onChanged;
	}

	/// <summary>The duplicated command.</summary>
	public string Command { get; }

	/// <summary>The line.</summary>
	public DuplicateAliasLine Line { get; }

	/// <summary>The alias file the line is in, as it was written.</summary>
	public string FileName => Line.FileName;

	/// <summary>The line as written, without its leading spaces.</summary>
	public string Text => Line.Text.Trim();

	/// <summary>A radio group name of this row's own, so its three choices don't join another row's.</summary>
	public string GroupName { get; } = Guid.NewGuid().ToString("N");

	/// <summary>Keep it, leave it out, or rename it.</summary>
	public DuplicateAliasAction Action
	{
		get => _action;
		set
		{
			if (SetProperty(ref _action, value))
			{
				OnPropertyChanged(nameof(IsRename));
				_onChanged();
			}
		}
	}

	/// <summary>Whether the line is to be renamed, so its new command box is in use.</summary>
	public bool IsRename => Action == DuplicateAliasAction.Rename;

	/// <summary>The line's new command, for a rename.</summary>
	public string NewCommand
	{
		get => _newCommand;
		set
		{
			if (SetProperty(ref _newCommand, value ?? string.Empty))
			{
				_onChanged();
			}
		}
	}

	/// <summary>The row's choice, to make and save.</summary>
	/// <returns>The choice.</returns>
	public DuplicateAliasRule ToChoice() =>
		new(Line.FileKey, Command, Line.Occurrence, Action, IsRename ? NewCommand.Trim() : null);
}
