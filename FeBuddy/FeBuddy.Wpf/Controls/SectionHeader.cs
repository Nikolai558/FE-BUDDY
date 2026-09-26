using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// The small uppercase heading that titles a block ("OUTPUT MODE", "RECENT OUTPUT").
/// Lookless: its look lives in
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
	/// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
	public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
		nameof(Label), typeof(string), typeof(SectionHeader), new PropertyMetadata(string.Empty));

	/// <summary>Identifies the <see cref="Aside"/> dependency property.</summary>
	public static readonly DependencyProperty AsideProperty = DependencyProperty.Register(
		nameof(Aside), typeof(object), typeof(SectionHeader), new PropertyMetadata(null, OnAsideChanged));

	/// <summary>Identifies the <see cref="TextStyle"/> dependency property.</summary>
	public static readonly DependencyProperty TextStyleProperty = DependencyProperty.Register(
		nameof(TextStyle), typeof(Style), typeof(SectionHeader), new PropertyMetadata(null));

	/// <summary>The heading text, in normal case; it is shown upper-cased.</summary>
	public string Label
	{
		get => (string)GetValue(LabelProperty);
		set => SetValue(LabelProperty, value);
	}

	/// <summary>
	/// Optional element shown right-aligned next to the label (e.g. a link button). It is this
	/// header's logical child, so its bindings find names and data from where it is declared even
	/// before the template shows it - a header inside something hidden is never templated, and an
	/// <c>ElementName</c> binding that fails then never recovers.
	/// </summary>
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

	/// <inheritdoc />
	protected override IEnumerator LogicalChildren =>
		Aside is { } aside ? new[] { aside }.GetEnumerator() : base.LogicalChildren;

	private static void OnAsideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		SectionHeader header = (SectionHeader)d;
		header.RemoveLogicalChild(e.OldValue);
		header.AddLogicalChild(e.NewValue);
	}
}
