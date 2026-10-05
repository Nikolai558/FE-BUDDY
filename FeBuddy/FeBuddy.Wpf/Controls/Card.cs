using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

using FeBuddy.Wpf.Behaviors;

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
