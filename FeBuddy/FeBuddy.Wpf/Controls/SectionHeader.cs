using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// The small uppercase heading that titles a block, matching the reference site's
/// "ONLINE ATC" / "TOP 3 CONTROLLERS" pattern. Lookless: its look lives in
/// Theme/Controls.Surfaces.xaml, in two levels -
/// <list type="bullet">
/// <item>the implicit style: a card or section title. Every ctl:Card draws its Header with one;
/// use it directly only for a title that is not on a card (a sidebar, a nav group).</item>
/// <item>SectionHeader.Group: a heading for a group of controls inside a card.</item>
/// </list>
/// Write <see cref="Label"/> in normal case; the style upper-cases it.
/// <code>
/// &lt;ctl:SectionHeader Label="Output Mode" /&gt;
///
/// &lt;ctl:SectionHeader Label="Recent Output"&gt;
///     &lt;ctl:SectionHeader.Aside&gt;
///         &lt;Button Style="{StaticResource Button.Subtle}" Content="Open folder" /&gt;
///     &lt;/ctl:SectionHeader.Aside&gt;
/// &lt;/ctl:SectionHeader&gt;
/// </code>
/// </summary>
public sealed class SectionHeader : Control
{
	public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
		nameof(Label), typeof(string), typeof(SectionHeader), new PropertyMetadata(string.Empty));

	public static readonly DependencyProperty AsideProperty = DependencyProperty.Register(
		nameof(Aside), typeof(object), typeof(SectionHeader), new PropertyMetadata(null));

	public static readonly DependencyProperty TextStyleProperty = DependencyProperty.Register(
		nameof(TextStyle), typeof(Style), typeof(SectionHeader), new PropertyMetadata(null));

	/// <summary>The heading text, in normal case; it is shown upper-cased.</summary>
	public string Label
	{
		get => (string)GetValue(LabelProperty);
		set => SetValue(LabelProperty, value);
	}

	/// <summary>Optional element shown right-aligned next to the label (e.g. a link button).</summary>
	public object? Aside
	{
		get => GetValue(AsideProperty);
		set => SetValue(AsideProperty, value);
	}

	/// <summary>The label's TextBlock style. Set by the theme's SectionHeader styles, not at call sites.</summary>
	public Style? TextStyle
	{
		get => (Style?)GetValue(TextStyleProperty);
		set => SetValue(TextStyleProperty, value);
	}
}
