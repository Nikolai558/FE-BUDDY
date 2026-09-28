using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One fix use or chart in the Fixes tab's Fix Uses or Charts picker. The <b>selected</b> set is
/// what gets persisted as included; an unselected entry is left out of the matching file layout.
/// </summary>
/// <param name="token">The file-name/CRC-class token, e.g. <c>WYPNT</c> or <c>ENROUTE-LOW</c>.</param>
/// <param name="label">The label shown on the checkbox, e.g. <c>WYPNT</c> or <c>ENROUTE LOW</c>.</param>
/// <param name="isSelected">Whether it starts selected (included).</param>
/// <param name="onChanged">Called when the user ticks or unticks it.</param>
public sealed class FixGroupToggle(string token, string label, bool isSelected, Action onChanged) : ObservableObject
{
	private bool _isSelected = isSelected;

	/// <summary>The file-name/CRC-class token, e.g. <c>WYPNT</c> or <c>ENROUTE-LOW</c>.</summary>
	public string Token { get; } = token;

	/// <summary>The label shown on the checkbox, e.g. <c>WYPNT</c> or <c>ENROUTE LOW</c>.</summary>
	public string Label { get; } = label;

	/// <summary><see langword="true"/> to include this fix use or chart in the output.</summary>
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
