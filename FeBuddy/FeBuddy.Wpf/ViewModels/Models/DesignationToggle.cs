using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One airway designation with an include/exclude toggle. The designation is derived from
/// <c>AWY_ID</c> (leading letters); toggles default on, and the <b>deselected</b> set is what gets
/// persisted to <c>ExcludedDesignations</c>.
/// </summary>
/// <param name="designation">The designation, e.g. <c>J</c>.</param>
/// <param name="included">Whether it starts included.</param>
/// <param name="onChanged">Called when the user ticks or unticks it.</param>
public sealed class DesignationToggle(string designation, bool included, Action onChanged) : ObservableObject
{
	private bool _included = included;

	/// <summary>The designation, e.g. <c>J</c>, <c>V</c>, <c>AT</c>.</summary>
	public string Designation { get; } = designation;

	/// <summary><see langword="true"/> to include this designation in the output.</summary>
	public bool Included
	{
		get => _included;
		set
		{
			if (SetProperty(ref _included, value))
			{
				onChanged();
			}
		}
	}
}
