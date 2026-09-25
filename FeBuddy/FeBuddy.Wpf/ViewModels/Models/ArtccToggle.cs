
using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One ARTCC in the Departures tab's ARTCC filter. The <b>selected</b> set is what gets
/// persisted to <c>ArtccFilter</c>; none selected means every ARTCC.
/// </summary>
/// <param name="artcc">The ARTCC identifier.</param>
/// <param name="isSelected">Whether it starts selected.</param>
/// <param name="onChanged">Called when the user ticks or unticks it.</param>
public sealed class ArtccToggle(string artcc, bool isSelected, Action onChanged) : ObservableObject
{
	private bool _isSelected = isSelected;

	/// <summary>The ARTCC identifier, e.g. <c>ZSE</c>.</summary>
	public string Artcc { get; } = artcc;

	/// <summary><see langword="true"/> to include this ARTCC's departures.</summary>
	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (SetProperty(ref _isSelected, value))
			{
				onChanged();
			}
		}
	}
}
