using FeBuddy.Wpf.Mvvm;

using FeBuddy.Core.Application.Launch;
using FeBuddy.Core.Application.Launch.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>One box on Settings' Developer card: an optional launch step a dev launch runs when ticked.</summary>
/// <param name="step">The step.</param>
/// <param name="isChecked">Whether it runs, as saved.</param>
/// <param name="changed">Called after the box is ticked or unticked, to save the card.</param>
public sealed class DevLaunchStepToggle(OptionalLaunchStep step, bool isChecked, Action changed) : ObservableObject
{
	private bool _isChecked = isChecked;

	/// <summary>The step.</summary>
	public OptionalLaunchStep Step { get; } = step;

	/// <summary>The box's label, e.g. <c>Download AIRAC data</c>.</summary>
	public string Label { get; } = OptionalLaunchSteps.Name(step);

	/// <summary>What the step does, and what leaving it out means.</summary>
	public string Description { get; } = OptionalLaunchSteps.Description(step);

	/// <summary>Whether a dev launch runs the step.</summary>
	public bool IsChecked
	{
		get => _isChecked;
		set
		{
			if (SetProperty(ref _isChecked, value))
			{
				changed();
			}
		}
	}
}
