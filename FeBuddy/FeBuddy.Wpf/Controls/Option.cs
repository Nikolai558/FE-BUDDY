using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// One option row: a control (usually a CheckBox) with its explanation indented underneath.
/// Lookless: the indent and the description's text style live in the Option style in
/// Theme/Controls.Inputs.xaml. Spacing between rows is layout, so it stays on the Option's Margin.
/// <code>
/// &lt;ctl:Option Description="Writes one alias command per airport."&gt;
///     &lt;CheckBox Content="Alias file" IsChecked="{Binding GenerateAliasFile}" /&gt;
/// &lt;/ctl:Option&gt;
/// </code>
/// </summary>
public sealed class Option : ContentControl
{
	/// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
	public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
		nameof(Description), typeof(string), typeof(Option), new PropertyMetadata(null));

	/// <summary>The explanation shown under the control. Leave unset for none.</summary>
	public string? Description
	{
		get => (string?)GetValue(DescriptionProperty);
		set => SetValue(DescriptionProperty, value);
	}
}
