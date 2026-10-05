using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One row on the File Conversions picker: a source, one of a source's files, or an output - a
/// radio button with a line under it.
/// </summary>
/// <param name="name">What the row is called, e.g. <c>FAA ERAM Adaptation Files</c>.</param>
/// <param name="description">The line under it.</param>
/// <remarks>
/// Each list keeps its own pick: ticking a row raises <see cref="Picked"/>, and the screen unticks
/// the rest of its list.
/// </remarks>
public sealed class ConversionChoice(string name, string description) : ObservableObject
{
	private bool _isSelected;

	/// <summary>Raised when the row is ticked.</summary>
	public event EventHandler? Picked;

	/// <summary>What the row is called.</summary>
	public string Name { get; } = name;

	/// <summary>The line under it.</summary>
	public string Description { get; } = description;

	/// <summary>For a source: the files to pick from, or empty when the source asks no more.</summary>
	public IReadOnlyList<ConversionChoice> Files { get; init; } = [];

	/// <summary>For a file, or a source with no <see cref="Files"/>: what it can be converted to.</summary>
	public IReadOnlyList<ConversionChoice> Outputs { get; init; } = [];

	/// <summary>For an output: the conversion page it opens.</summary>
	public ConversionTabViewModel? Conversion { get; init; }

	/// <summary>Whether the row is ticked. Bound two-way to its radio button.</summary>
	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (SetProperty(ref _isSelected, value) && value)
			{
				Picked?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}
