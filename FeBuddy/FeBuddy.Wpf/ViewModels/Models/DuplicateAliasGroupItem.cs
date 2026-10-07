using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>One duplicated alias command in the review window: its lines, and what to fix before the choices settle it.</summary>
/// <param name="command">The command.</param>
/// <param name="lines">A row per line that uses it.</param>
public sealed class DuplicateAliasGroupItem(string command, IReadOnlyList<DuplicateAliasLineItem> lines) : ObservableObject
{
	private string? _problem;

	/// <summary>The command, e.g. <c>.orfNUTIYf</c>.</summary>
	public string Command { get; } = command;

	/// <summary>A row per line that uses it.</summary>
	public IReadOnlyList<DuplicateAliasLineItem> Lines { get; } = lines;

	/// <summary>How many lines use it, e.g. <c>2 lines</c>.</summary>
	public string LineCount => $"{Lines.Count} lines";

	/// <summary>What to fix before the choices settle the command, or <see langword="null"/>.</summary>
	public string? Problem
	{
		get => _problem;
		set
		{
			if (SetProperty(ref _problem, value))
			{
				OnPropertyChanged(nameof(HasProblem));
			}
		}
	}

	/// <summary>Whether there is a <see cref="Problem"/>.</summary>
	public bool HasProblem => Problem is not null;
}
