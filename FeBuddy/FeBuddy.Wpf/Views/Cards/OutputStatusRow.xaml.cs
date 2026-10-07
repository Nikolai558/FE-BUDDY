using System.Windows;
using System.Windows.Controls;

using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.Wpf.Views.Cards;

/// <summary>One output on a sub-service's Outputs card, shown as on or off. See OutputStatusRow.xaml.</summary>
public partial class OutputStatusRow : UserControl
{
	/// <summary>Identifies the <see cref="Kind"/> dependency property.</summary>
	public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
		nameof(Kind), typeof(SubServiceOutputKinds), typeof(OutputStatusRow),
		new PropertyMetadata(SubServiceOutputKinds.None, (d, _) => ((OutputStatusRow)d).RefreshTag()));

	/// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
	public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
		nameof(Label), typeof(string), typeof(OutputStatusRow), new PropertyMetadata(null));

	/// <summary>Identifies the <see cref="IsOn"/> dependency property.</summary>
	public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(
		nameof(IsOn), typeof(bool), typeof(OutputStatusRow),
		new PropertyMetadata(false, (d, _) => ((OutputStatusRow)d).RefreshTag()));

	/// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
	public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
		nameof(Description), typeof(string), typeof(OutputStatusRow), new PropertyMetadata(null));

	private static readonly DependencyPropertyKey OutputTagKey = DependencyProperty.RegisterReadOnly(
		nameof(OutputTag), typeof(OutputTag), typeof(OutputStatusRow), new PropertyMetadata(null));

	/// <summary>Identifies the <see cref="OutputTag"/> dependency property.</summary>
	public static readonly DependencyProperty OutputTagProperty = OutputTagKey.DependencyProperty;

	/// <summary>Creates the row.</summary>
	public OutputStatusRow() => InitializeComponent();

	/// <summary>Which output the row is: its tag names it, in the same colour as on the cards below.</summary>
	public SubServiceOutputKinds Kind
	{
		get => (SubServiceOutputKinds)GetValue(KindProperty);
		set => SetValue(KindProperty, value);
	}

	/// <summary>The output's file, when the tag alone doesn't name it, e.g. <c>Procedure_Changes.md</c>.</summary>
	public string? Label
	{
		get => (string?)GetValue(LabelProperty);
		set => SetValue(LabelProperty, value);
	}

	/// <summary>Whether the output is written.</summary>
	public bool IsOn
	{
		get => (bool)GetValue(IsOnProperty);
		set => SetValue(IsOnProperty, value);
	}

	/// <summary>What the output writes, shown underneath; hidden when empty.</summary>
	public string? Description
	{
		get => (string?)GetValue(DescriptionProperty);
		set => SetValue(DescriptionProperty, value);
	}

	/// <summary>The row's output tag.</summary>
	public OutputTag? OutputTag => (OutputTag?)GetValue(OutputTagProperty);

	private void RefreshTag() =>
		SetValue(OutputTagKey, Kind == SubServiceOutputKinds.None ? null : new OutputTag(Kind, IsOn));
}
