namespace FeBuddy.Installer.UI.ViewModels;

/// <summary>One row of the step rail.</summary>
/// <param name="page">The page the step stands for.</param>
/// <param name="number">Its number, from 1.</param>
/// <param name="title">Its label.</param>
public sealed class StepItem(SetupPage page, int number, string title) : ObservableObject
{
	private string _state = "Upcoming";

	/// <summary>The page the step stands for.</summary>
	public SetupPage Page { get; } = page;

	/// <summary>Its number, from 1.</summary>
	public int Number { get; } = number;

	/// <summary>Its label.</summary>
	public string Title { get; } = title;

	/// <summary><c>Done</c>, <c>Current</c> or <c>Upcoming</c> - the Step style's triggers read it.</summary>
	public string State
	{
		get => _state;
		set => Set(ref _state, value);
	}
}
