using System.Windows;
using System.Windows.Controls;

using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.Wpf.Views.Cards;

/// <summary>The shared Area card. See AreaCard.xaml.</summary>
public partial class AreaCard : UserControl
{
	/// <summary>Identifies the <see cref="Outputs"/> dependency property.</summary>
	public static readonly DependencyProperty OutputsProperty = DependencyProperty.Register(
		nameof(Outputs), typeof(SubServiceOutputKinds), typeof(AreaCard), new PropertyMetadata(SubServiceOutputKinds.None));

	/// <summary>Identifies the <see cref="Intro"/> dependency property.</summary>
	public static readonly DependencyProperty IntroProperty = DependencyProperty.Register(
		nameof(Intro), typeof(string), typeof(AreaCard), new PropertyMetadata(string.Empty));

	/// <summary>Identifies the <see cref="ArtccsLabel"/> dependency property.</summary>
	public static readonly DependencyProperty ArtccsLabelProperty = DependencyProperty.Register(
		nameof(ArtccsLabel), typeof(string), typeof(AreaCard), new PropertyMetadata("ARTCCs"));

	/// <summary>Identifies the <see cref="ArtccsDescription"/> dependency property.</summary>
	public static readonly DependencyProperty ArtccsDescriptionProperty = DependencyProperty.Register(
		nameof(ArtccsDescription), typeof(string), typeof(AreaCard), new PropertyMetadata(string.Empty));

	/// <summary>Identifies the <see cref="RoiDescription"/> dependency property.</summary>
	public static readonly DependencyProperty RoiDescriptionProperty = DependencyProperty.Register(
		nameof(RoiDescription), typeof(string), typeof(AreaCard), new PropertyMetadata(string.Empty));

	/// <summary>Identifies the <see cref="EverythingDescription"/> dependency property.</summary>
	public static readonly DependencyProperty EverythingDescriptionProperty = DependencyProperty.Register(
		nameof(EverythingDescription), typeof(string), typeof(AreaCard), new PropertyMetadata(string.Empty));

	/// <summary>Identifies the <see cref="NoneDescription"/> dependency property.</summary>
	public static readonly DependencyProperty NoneDescriptionProperty = DependencyProperty.Register(
		nameof(NoneDescription), typeof(string), typeof(AreaCard), new PropertyMetadata(string.Empty));

	/// <summary>Identifies the <see cref="ArtccsContent"/> dependency property.</summary>
	public static readonly DependencyProperty ArtccsContentProperty = DependencyProperty.Register(
		nameof(ArtccsContent), typeof(object), typeof(AreaCard), new PropertyMetadata(null));

	/// <summary>Identifies the <see cref="RoiContent"/> dependency property.</summary>
	public static readonly DependencyProperty RoiContentProperty = DependencyProperty.Register(
		nameof(RoiContent), typeof(object), typeof(AreaCard), new PropertyMetadata(null));

	/// <summary>Identifies the <see cref="Footer"/> dependency property.</summary>
	public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(
		nameof(Footer), typeof(object), typeof(AreaCard), new PropertyMetadata(null));

	/// <summary>Creates the card.</summary>
	public AreaCard() => InitializeComponent();

	/// <summary>
	/// The outputs the area narrows on this tab, for the card's tags (see <c>ctl:Card.Outputs</c>):
	/// the GeoJSON on most tabs, the alias file too on Departures, Arrivals and Airways.
	/// </summary>
	public SubServiceOutputKinds Outputs
	{
		get => (SubServiceOutputKinds)GetValue(OutputsProperty);
		set => SetValue(OutputsProperty, value);
	}

	/// <summary>The line above the choices, e.g. <c>Choose which arrivals to include.</c></summary>
	public string Intro
	{
		get => (string)GetValue(IntroProperty);
		set => SetValue(IntroProperty, value);
	}

	/// <summary>The ARTCCs choice's name: <c>ARTCCs</c>, or <c>Facilities</c> on Procedures.</summary>
	public string ArtccsLabel
	{
		get => (string)GetValue(ArtccsLabelProperty);
		set => SetValue(ArtccsLabelProperty, value);
	}

	/// <summary>What the ARTCCs choice gets, after its name, e.g. <c>only arrivals for airports in the ARTCCs you tick</c>.</summary>
	public string ArtccsDescription
	{
		get => (string)GetValue(ArtccsDescriptionProperty);
		set => SetValue(ArtccsDescriptionProperty, value);
	}

	/// <summary>What the ROI choice gets, after its name, e.g. <c>only arrivals inside an ROI</c>.</summary>
	public string RoiDescription
	{
		get => (string)GetValue(RoiDescriptionProperty);
		set => SetValue(RoiDescriptionProperty, value);
	}

	/// <summary>What the Everything choice gets, after its name, e.g. <c>every arrival in the cycle</c>.</summary>
	public string EverythingDescription
	{
		get => (string)GetValue(EverythingDescriptionProperty);
		set => SetValue(EverythingDescriptionProperty, value);
	}

	/// <summary>What the None choice gets, after its name (Procedures).</summary>
	public string NoneDescription
	{
		get => (string)GetValue(NoneDescriptionProperty);
		set => SetValue(NoneDescriptionProperty, value);
	}

	/// <summary>The ARTCC (or facility) checkboxes, shown under the ARTCCs choice while it's picked.</summary>
	public object? ArtccsContent
	{
		get => GetValue(ArtccsContentProperty);
		set => SetValue(ArtccsContentProperty, value);
	}

	/// <summary>Anything more the ROI needs, under its corners while it's picked (Departures and Arrivals: which procedures it keeps).</summary>
	public object? RoiContent
	{
		get => GetValue(RoiContentProperty);
		set => SetValue(RoiContentProperty, value);
	}

	/// <summary>Notes under the choices, e.g. that the alias file isn't narrowed.</summary>
	public object? Footer
	{
		get => GetValue(FooterProperty);
		set => SetValue(FooterProperty, value);
	}
}
