using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Views.Cards;

/// <summary>The shared Outputs card. See OutputsCard.xaml.</summary>
public partial class OutputsCard : UserControl
{
	/// <summary>Identifies the <see cref="GeojsonDescription"/> dependency property.</summary>
	public static readonly DependencyProperty GeojsonDescriptionProperty = DependencyProperty.Register(
		nameof(GeojsonDescription), typeof(string), typeof(OutputsCard), new PropertyMetadata(null));

	/// <summary>Identifies the <see cref="GeojsonTemplate"/> dependency property.</summary>
	public static readonly DependencyProperty GeojsonTemplateProperty = DependencyProperty.Register(
		nameof(GeojsonTemplate), typeof(DataTemplate), typeof(OutputsCard), new PropertyMetadata(null));

	/// <summary>Identifies the <see cref="AliasFileName"/> dependency property.</summary>
	public static readonly DependencyProperty AliasFileNameProperty = DependencyProperty.Register(
		nameof(AliasFileName), typeof(string), typeof(OutputsCard), new PropertyMetadata(string.Empty));

	/// <summary>Identifies the <see cref="AliasDescription"/> dependency property.</summary>
	public static readonly DependencyProperty AliasDescriptionProperty = DependencyProperty.Register(
		nameof(AliasDescription), typeof(string), typeof(OutputsCard), new PropertyMetadata(null));

	/// <summary>Identifies the <see cref="AliasOptions"/> dependency property.</summary>
	public static readonly DependencyProperty AliasOptionsProperty = DependencyProperty.Register(
		nameof(AliasOptions), typeof(object), typeof(OutputsCard), new PropertyMetadata(null));

	/// <summary>Creates the card.</summary>
	public OutputsCard() => InitializeComponent();

	/// <summary>What the "GeoJSON files" checkbox writes. Unused when <see cref="GeojsonTemplate"/> is set.</summary>
	public string? GeojsonDescription
	{
		get => (string?)GetValue(GeojsonDescriptionProperty);
		set => SetValue(GeojsonDescriptionProperty, value);
	}

	/// <summary>
	/// Replaces the "GeoJSON files" checkbox for a tab whose GeoJSON choice is more than on / off.
	/// Its DataContext is the tab's view model.
	/// </summary>
	public DataTemplate? GeojsonTemplate
	{
		get => (DataTemplate?)GetValue(GeojsonTemplateProperty);
		set => SetValue(GeojsonTemplateProperty, value);
	}

	/// <summary>The alias file's name, shown as "Alias file (Airports.txt)".</summary>
	public string AliasFileName
	{
		get => (string)GetValue(AliasFileNameProperty);
		set => SetValue(AliasFileNameProperty, value);
	}

	/// <summary>What the alias file holds, shown under its checkbox.</summary>
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
