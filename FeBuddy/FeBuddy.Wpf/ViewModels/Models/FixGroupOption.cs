namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One choice in the Fixes tab's Combinations editor: a chart or a fix use, offered by its
/// chart or fix use drop-down.
/// </summary>
/// <param name="Token">The file-name/CRC-class token, e.g. <c>WYPNT</c> or <c>NO-CHART</c>.</param>
/// <param name="Label">The display label, e.g. <c>WYPNT</c> or <c>No chart</c>.</param>
public sealed record FixGroupOption(string Token, string Label)
{
	/// <summary>Its <see cref="Label"/>, so a ComboBox with no explicit display path still shows it.</summary>
	public override string ToString() => Label;
}
