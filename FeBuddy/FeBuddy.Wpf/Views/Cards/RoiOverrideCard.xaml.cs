using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Views.Cards;

/// <summary>The shared Region of Interest card. See RoiOverrideCard.xaml.</summary>
public partial class RoiOverrideCard : UserControl
{
	/// <summary>Identifies the <see cref="AdditionalContent"/> dependency property.</summary>
	public static readonly DependencyProperty AdditionalContentProperty = DependencyProperty.Register(
		nameof(AdditionalContent), typeof(object), typeof(RoiOverrideCard), new PropertyMetadata(null));

	/// <summary>Creates the card.</summary>
	public RoiOverrideCard() => InitializeComponent();

	/// <summary>Optional sub-service-specific controls shown under the corners.</summary>
	public object? AdditionalContent
	{
		get => GetValue(AdditionalContentProperty);
		set => SetValue(AdditionalContentProperty, value);
	}
}
