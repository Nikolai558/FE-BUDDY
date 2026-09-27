using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One tick on the Procedures tab's Chart Types or Procedures.json Fields card: a choice known
/// ahead of the cycle (unlike <see cref="ArtccToggle"/> or <see cref="FixGroupToggle"/>, which are
/// built from the cycle's data), so both cards build their toggles once, in the constructor.
/// </summary>
/// <param name="token">The settings token, e.g. <c>IAP</c> or <c>icaoId</c>.</param>
/// <param name="label">The label shown on the checkbox.</param>
/// <param name="isSelected">Whether it starts selected.</param>
/// <param name="onChanged">Called when the user ticks or unticks it.</param>
public sealed class ProcedureOptionToggle(string token, string label, bool isSelected, Action onChanged) : ObservableObject
{
	private bool _isSelected = isSelected;

	/// <summary>The settings token, e.g. <c>IAP</c> or <c>icaoId</c>.</summary>
	public string Token { get; } = token;

	/// <summary>The label shown on the checkbox, e.g. <c>Instrument approaches (IAP)</c>.</summary>
	public string Label { get; } = label;

	/// <summary><see langword="true"/> to include this choice.</summary>
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
