using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Views.Cards;

/// <summary>One output on a sub-service's Outputs card, shown as on or off. See OutputStatusRow.xaml.</summary>
public partial class OutputStatusRow : UserControl
{
	/// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
	public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
		nameof(Label), typeof(string), typeof(OutputStatusRow), new PropertyMetadata(null));

	/// <summary>Identifies the <see cref="IsOn"/> dependency property.</summary>
	public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(
		nameof(IsOn), typeof(bool), typeof(OutputStatusRow), new PropertyMetadata(false));

	/// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
	public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
		nameof(Description), typeof(string), typeof(OutputStatusRow), new PropertyMetadata(null));

	/// <summary>Creates the row.</summary>
	public OutputStatusRow() => InitializeComponent();

	/// <summary>The output's name, e.g. <c>Alias file</c>.</summary>
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
}
