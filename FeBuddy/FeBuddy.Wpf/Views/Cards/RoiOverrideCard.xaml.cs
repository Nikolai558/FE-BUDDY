using System.Windows;
using System.Windows.Controls;

using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.Wpf.Views.Cards;

/// <summary>The shared Region of Interest card. See RoiOverrideCard.xaml.</summary>
public partial class RoiOverrideCard : UserControl
{
	/// <summary>Identifies the <see cref="AdditionalContent"/> dependency property.</summary>
	public static readonly DependencyProperty AdditionalContentProperty = DependencyProperty.Register(
		nameof(AdditionalContent), typeof(object), typeof(RoiOverrideCard), new PropertyMetadata(null));

	/// <summary>Identifies the <see cref="Outputs"/> dependency property.</summary>
	public static readonly DependencyProperty OutputsProperty = DependencyProperty.Register(
		nameof(Outputs), typeof(SubServiceOutputKinds), typeof(RoiOverrideCard), new PropertyMetadata(SubServiceOutputKinds.None));

	/// <summary>Creates the card.</summary>
	public RoiOverrideCard() => InitializeComponent();

	/// <summary>
	/// The outputs the region narrows on this tab, for the card's tags (see <c>ctl:Card.Outputs</c>):
	/// the GeoJSON on most tabs, the alias file too on Departures and Arrivals.
	/// </summary>
	public SubServiceOutputKinds Outputs
	{
		get => (SubServiceOutputKinds)GetValue(OutputsProperty);
		set => SetValue(OutputsProperty, value);
	}

	/// <summary>Optional sub-service-specific controls shown under the corners.</summary>
	public object? AdditionalContent
	{
		get => GetValue(AdditionalContentProperty);
		set => SetValue(AdditionalContentProperty, value);
	}
}
