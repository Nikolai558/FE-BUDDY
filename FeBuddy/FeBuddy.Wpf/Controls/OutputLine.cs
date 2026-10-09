using System.Windows;
using System.Windows.Controls;

using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// One output's line on a card whose settings treat the outputs differently: the output's tag,
/// then what the card does to it. The tag is struck through while the output is off (it follows the
/// page's <see cref="Card.OutputsOnProperty"/>). Its look is in Theme/Controls.Surfaces.xaml.
/// <code>
/// &lt;ctl:OutputLine Kind="Geojson" Text="Only airways that cross the region, clipped at its edge." /&gt;
/// &lt;ctl:OutputLine Kind="Alias" Text="{Binding AreaAliasNote}" /&gt;
/// </code>
/// </summary>
public sealed class OutputLine : Control
{
	/// <summary>Identifies the <see cref="Kind"/> dependency property.</summary>
	public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
		nameof(Kind), typeof(SubServiceOutputKinds), typeof(OutputLine),
		new PropertyMetadata(SubServiceOutputKinds.None, (d, _) => ((OutputLine)d).RefreshTag()));

	/// <summary>Identifies the <see cref="Text"/> dependency property.</summary>
	public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
		nameof(Text), typeof(string), typeof(OutputLine), new PropertyMetadata(null));

	private static readonly DependencyPropertyKey OutputKey = DependencyProperty.RegisterReadOnly(
		nameof(Output), typeof(OutputTag), typeof(OutputLine), new PropertyMetadata(null));

	/// <summary>Identifies the <see cref="Output"/> dependency property.</summary>
	public static readonly DependencyProperty OutputProperty = OutputKey.DependencyProperty;

	/// <summary>The output the line is about: one kind, e.g. <c>Geojson</c>.</summary>
	public SubServiceOutputKinds Kind
	{
		get => (SubServiceOutputKinds)GetValue(KindProperty);
		set => SetValue(KindProperty, value);
	}

	/// <summary>What the card does to that output.</summary>
	public string? Text
	{
		get => (string?)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	/// <summary>The line's tag, on or off as the page has the output.</summary>
	public OutputTag? Output => (OutputTag?)GetValue(OutputProperty);

	/// <summary>Rebuilds the tag after <see cref="Kind"/> or the page's outputs change.</summary>
	internal void RefreshTag() =>
		SetValue(OutputKey, Kind == SubServiceOutputKinds.None ? null : new OutputTag(Kind, Card.GetOutputsOn(this).HasFlag(Kind)));
}
