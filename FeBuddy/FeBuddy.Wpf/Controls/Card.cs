using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

using FeBuddy.Wpf.Behaviors;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// The standard rounded panel every page is built from, with an optional title. Its whole look
/// (background, border, corner, padding, the space below it, the title) comes from the implicit
/// Card style in Theme/Controls.Surfaces.xaml; a card that must look different uses one of the
/// named variants there (Card.Warn, Card.Danger, Card.Accent, Card.Popup) rather than local overrides.
/// <code>
/// &lt;ctl:Card Header="Outputs"&gt;
///     ...controls...
/// &lt;/ctl:Card&gt;
/// </code>
/// A card outlines itself in red while it holds a problem (see <see cref="NeedsAttention"/>):
/// <code>
/// &lt;ctl:Card Header="Fix Uses" bhv:FieldState.Error="{Binding FieldErrors[FixUses]}"&gt;
/// </code>
/// On an AIRAC Service tab, a card says which outputs its settings affect (<see cref="Outputs"/>)
/// with a tag for each on its title row, and greys out with a note while all of them are off:
/// <code>
/// &lt;ctl:Card Header="Procedures" Outputs="Alias, Geojson"&gt;
/// </code>
/// Which outputs are on, and the sub-service's name, are set once for the whole page
/// (<see cref="OutputsOnProperty"/>, <see cref="OutputsOwnerProperty"/>) and inherited by every card
/// on it. Off an AIRAC Service tab no owner is set, so no tags show.
/// </summary>
public sealed class Card : ContentControl
{
	/// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
	public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
		nameof(Header), typeof(string), typeof(Card), new PropertyMetadata(null));

	private static readonly DependencyPropertyKey NeedsAttentionKey = DependencyProperty.RegisterReadOnly(
		nameof(NeedsAttention), typeof(bool), typeof(Card), new PropertyMetadata(false));

	/// <summary>Identifies the <see cref="NeedsAttention"/> dependency property.</summary>
	public static readonly DependencyProperty NeedsAttentionProperty = NeedsAttentionKey.DependencyProperty;

	/// <summary>Identifies the <see cref="Outputs"/> dependency property.</summary>
	public static readonly DependencyProperty OutputsProperty = DependencyProperty.Register(
		nameof(Outputs), typeof(SubServiceOutputKinds), typeof(Card),
		new PropertyMetadata(SubServiceOutputKinds.None, OnOutputStateChanged));

	/// <summary>
	/// Which of the sub-service's outputs are on, set once on the page and inherited by every card
	/// and <see cref="OutputLine"/> on it. Every output counts as on until it is set.
	/// </summary>
	public static readonly DependencyProperty OutputsOnProperty = DependencyProperty.RegisterAttached(
		"OutputsOn", typeof(SubServiceOutputKinds), typeof(Card),
		new FrameworkPropertyMetadata(OutputKinds.Every, FrameworkPropertyMetadataOptions.Inherits, OnOutputStateChanged));

	/// <summary>
	/// The sub-service the page is for (<c>Departures</c>), set once on the page and inherited.
	/// Output tags show only where it is set: on the AIRAC Service's tabs.
	/// </summary>
	public static readonly DependencyProperty OutputsOwnerProperty = DependencyProperty.RegisterAttached(
		"OutputsOwner", typeof(string), typeof(Card),
		new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnOutputStateChanged));

	private static readonly DependencyPropertyKey OutputTagsKey = DependencyProperty.RegisterReadOnly(
		nameof(OutputTags), typeof(IReadOnlyList<OutputTag>), typeof(Card), new PropertyMetadata(Array.Empty<OutputTag>()));

	/// <summary>Identifies the <see cref="OutputTags"/> dependency property.</summary>
	public static readonly DependencyProperty OutputTagsProperty = OutputTagsKey.DependencyProperty;

	private static readonly DependencyPropertyKey OutputsOffNoteKey = DependencyProperty.RegisterReadOnly(
		nameof(OutputsOffNote), typeof(string), typeof(Card), new PropertyMetadata(null));

	/// <summary>Identifies the <see cref="OutputsOffNote"/> dependency property.</summary>
	public static readonly DependencyProperty OutputsOffNoteProperty = OutputsOffNoteKey.DependencyProperty;

	private bool _checkQueued;

	static Card() =>
		EventManager.RegisterClassHandler(typeof(Card), FieldState.ErrorChangedEvent,
			new RoutedEventHandler((sender, _) => ((Card)sender).QueueCheck()));

	/// <summary>Creates the card.</summary>
	public Card() => Loaded += (_, _) => QueueCheck();

	/// <summary>
	/// The card's title, written in normal case ("Region of Interest"); SectionHeader sets the
	/// casing. Leave unset for a card with no title.
	/// </summary>
	public string? Header
	{
		get => (string?)GetValue(HeaderProperty);
		set => SetValue(HeaderProperty, value);
	}

	/// <summary>
	/// Whether the card holds a validation problem: the card itself or a box inside it carries a
	/// <see cref="FieldState"/> error. The style outlines such a card in red, so the area a tab's
	/// problem list talks about is easy to find.
	/// </summary>
	public bool NeedsAttention => (bool)GetValue(NeedsAttentionProperty);

	/// <summary>
	/// The outputs this card's settings affect, e.g. <c>Alias, Geojson</c>: a tag for each on the
	/// title row. <see cref="SubServiceOutputKinds.None"/> (the default) for a card that affects none.
	/// </summary>
	public SubServiceOutputKinds Outputs
	{
		get => (SubServiceOutputKinds)GetValue(OutputsProperty);
		set => SetValue(OutputsProperty, value);
	}

	/// <summary>The tags the title row shows: one per output in <see cref="Outputs"/>, struck through while off.</summary>
	public IReadOnlyList<OutputTag> OutputTags => (IReadOnlyList<OutputTag>)GetValue(OutputTagsProperty);

	/// <summary>
	/// What the card says while every output it affects is off (it is greyed out then), e.g.
	/// <c>GeoJSON is off for Departures. Turn it on in the General tab.</c>; otherwise <see langword="null"/>.
	/// </summary>
	public string? OutputsOffNote => (string?)GetValue(OutputsOffNoteProperty);

	/// <summary>Gets the outputs that are on, inherited from the page.</summary>
	/// <param name="element">Any element on the page.</param>
	/// <returns>The outputs that are on.</returns>
	public static SubServiceOutputKinds GetOutputsOn(DependencyObject element) =>
		(SubServiceOutputKinds)element.GetValue(OutputsOnProperty);

	/// <summary>Sets the outputs that are on, for every card on the page below <paramref name="element"/>.</summary>
	/// <param name="element">The page, or the element holding it.</param>
	/// <param name="value">The outputs that are on.</param>
	public static void SetOutputsOn(DependencyObject element, SubServiceOutputKinds value) =>
		element.SetValue(OutputsOnProperty, value);

	/// <summary>Gets the sub-service the page is for, inherited from the page.</summary>
	/// <param name="element">Any element on the page.</param>
	/// <returns>Its name, or <see langword="null"/> off the AIRAC Service's tabs.</returns>
	public static string? GetOutputsOwner(DependencyObject element) =>
		(string?)element.GetValue(OutputsOwnerProperty);

	/// <summary>Sets the sub-service the page is for, for every card on the page below <paramref name="element"/>.</summary>
	/// <param name="element">The page, or the element holding it.</param>
	/// <param name="value">The sub-service's name, e.g. <c>Departures</c>.</param>
	public static void SetOutputsOwner(DependencyObject element, string? value) =>
		element.SetValue(OutputsOwnerProperty, value);

	/// <inheritdoc />
	/// <remarks>A card whose outputs are all off is greyed out, whatever else enables it.</remarks>
	protected override bool IsEnabledCore => base.IsEnabledCore && OutputsOffNote is null;

	/// <summary>Brings a card's tags, note and greying, or an <see cref="OutputLine"/>'s tag, up to date.</summary>
	private static void OnOutputStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		switch (d)
		{
			case Card card:
				card.RefreshOutputs();
				break;

			case OutputLine line:
				line.RefreshTag();
				break;
		}
	}

	private void RefreshOutputs()
	{
		string? owner = GetOutputsOwner(this);
		SubServiceOutputKinds on = GetOutputsOn(this);

		IReadOnlyList<OutputTag> tags = owner is null ? [] : OutputTag.For(Outputs, on);

		SetValue(OutputTagsKey, tags);
		SetValue(OutputsOffNoteKey, owner is null ? null : OutputTag.OffNote(Outputs, on, owner));
		CoerceValue(IsEnabledProperty);
	}

	/// <summary>
	/// Re-checks the card once layout settles, so list rows created during layout are in place
	/// and a burst of field changes costs one check.
	/// </summary>
	private void QueueCheck()
	{
		if (_checkQueued)
		{
			return;
		}

		_checkQueued = true;
		Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
		{
			_checkQueued = false;
			SetValue(NeedsAttentionKey, HasErrorWithin(this));
		});
	}

	private static bool HasErrorWithin(DependencyObject element)
	{
		if (FieldState.GetHasError(element))
		{
			return true;
		}

		int count = VisualTreeHelper.GetChildrenCount(element);

		for (int i = 0; i < count; i++)
		{
			if (HasErrorWithin(VisualTreeHelper.GetChild(element, i)))
			{
				return true;
			}
		}

		return false;
	}
}
