using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Views.Cards;

/// <summary>The shared Outputs card. See OutputsCard.xaml.</summary>
public partial class OutputsCard : UserControl
{
	/// <summary>Identifies the <see cref="GeojsonDescription"/> dependency property.</summary>
	public static readonly DependencyProperty GeojsonDescriptionProperty = DependencyProperty.Register(
		nameof(GeojsonDescription), typeof(string), typeof(OutputsCard), new PropertyMetadata(null));

	/// <summary>Identifies the <see cref="GeojsonOptions"/> dependency property.</summary>
	public static readonly DependencyProperty GeojsonOptionsProperty = DependencyProperty.Register(
		nameof(GeojsonOptions), typeof(object), typeof(OutputsCard), new PropertyMetadata(null));

	/// <summary>Identifies the <see cref="AliasDescription"/> dependency property.</summary>
	public static readonly DependencyProperty AliasDescriptionProperty = DependencyProperty.Register(
		nameof(AliasDescription), typeof(string), typeof(OutputsCard), new PropertyMetadata(null));

	/// <summary>Identifies the <see cref="AliasOptions"/> dependency property.</summary>
	public static readonly DependencyProperty AliasOptionsProperty = DependencyProperty.Register(
		nameof(AliasOptions), typeof(object), typeof(OutputsCard), new PropertyMetadata(null));

	/// <summary>Creates the card.</summary>
	public OutputsCard() => InitializeComponent();

	/// <summary>What the GeoJSON files are, shown under their row.</summary>
	public string? GeojsonDescription
	{
		get => (string?)GetValue(GeojsonDescriptionProperty);
		set => SetValue(GeojsonDescriptionProperty, value);
	}

	/// <summary>Optional controls under the GeoJSON files, greyed out while they are off.</summary>
	public object? GeojsonOptions
	{
		get => GetValue(GeojsonOptionsProperty);
		set => SetValue(GeojsonOptionsProperty, value);
	}

	/// <summary>What the alias file holds, shown under its row.</summary>
	public string? AliasDescription
	{
		get => (string?)GetValue(AliasDescriptionProperty);
		set => SetValue(AliasDescriptionProperty, value);
	}

	/// <summary>Optional controls under the alias file, greyed out while it is off.</summary>
	public object? AliasOptions
	{
		get => GetValue(AliasOptionsProperty);
		set => SetValue(AliasOptionsProperty, value);
	}
}
