namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One entry in a Yes / No drop-down: the stored <paramref name="Value"/> (<c>Y</c> / <c>N</c>)
/// and the <paramref name="Label"/> the user sees.
/// </summary>
/// <param name="Value">The stored value, <c>Y</c> or <c>N</c>.</param>
/// <param name="Label">The text shown in the drop-down, <c>Yes</c> or <c>No</c>.</param>
public sealed record YesNoOption(string Value, string Label)
{
	/// <summary>Returns <see cref="Label"/>, which is what the ComboBox displays.</summary>
	/// <returns>The label.</returns>
	public override string ToString() => Label;
}
