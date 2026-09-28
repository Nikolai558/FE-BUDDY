using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// One number in a <see cref="FilterPicker"/>'s popup.
/// </summary>
/// <param name="number">The value this option stands for.</param>
/// <param name="onChanged">Called when the user ticks or unticks it.</param>
public sealed class FilterOption(int number, Action onChanged) : ObservableObject
{
	private readonly Action _onChanged = onChanged;
	private bool _isSelected;

	/// <summary>The number this option stands for.</summary>
	public int Number { get; } = number;

	/// <summary>Whether it is part of the selection.</summary>
	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (SetProperty(ref _isSelected, value))
			{
				_onChanged();
			}
		}
	}

	/// <summary>Sets the state without reporting the change, used while syncing from the value.</summary>
	/// <param name="selected">The new state.</param>
	internal void SetSilently(bool selected) => SetProperty(ref _isSelected, selected, nameof(IsSelected));
}
