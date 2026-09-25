using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One NAVAID type (NASR <c>NAV_TYPE</c>) in the NAVAIDs tab's type filter. The <b>selected</b>
/// set is what gets persisted as included; an unselected type is left out of both the GeoJSON and
/// the alias file.
/// </summary>
/// <param name="type">The NAVAID type, e.g. <c>VORTAC</c>.</param>
/// <param name="isSelected">Whether it starts selected (included).</param>
/// <param name="onChanged">Called when the user ticks or unticks it.</param>
public sealed class NavaidTypeToggle(string type, bool isSelected, Action onChanged) : ObservableObject
{
	private bool _isSelected = isSelected;

	/// <summary>The NAVAID type, e.g. <c>VORTAC</c> or <c>FAN MARKER</c>.</summary>
	public string Type { get; } = type;

	/// <summary><see langword="true"/> to include this type's NAVAIDs in the output.</summary>
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
