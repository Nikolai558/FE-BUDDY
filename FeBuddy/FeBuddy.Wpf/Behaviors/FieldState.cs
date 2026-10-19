using System.Windows;

namespace FeBuddy.Wpf.Behaviors;

/// <summary>
/// <c>bhv:FieldState.Error="{Binding FieldErrors[SwLat]}"</c> on an input - marks that box as
/// carrying a validation failure so the themed styles can highlight it and show the reason. On a
/// <c>ctl:Card</c> it marks the card as a whole; either way the card outlines itself in red (see
/// <c>Card.NeedsAttention</c>).
/// </summary>
/// <remarks>
/// <para>
/// The bound value is the message itself (or <see langword="null"/> when the field is fine), which
/// keeps the view declarative: one binding per box, no code-behind, and the message the style
/// shows cannot drift from the message the validator produced.
/// </para>
/// <para>
/// A box that also has a hint sets <c>bhv:FieldState.Hint</c>, not <c>ToolTip</c>: the themed TextBox and
/// ComboBox styles show the hint as the tool-tip and swap in the error while there is one. A local
/// <c>ToolTip</c> would always win over the style, so the error would never show.
/// </para>
/// </remarks>
public static class FieldState
{
	/// <summary>The validation message for this input, or <see langword="null"/>.</summary>
	public static readonly DependencyProperty ErrorProperty =
		DependencyProperty.RegisterAttached(
			"Error", typeof(string), typeof(FieldState),
			new PropertyMetadata(null, OnErrorChanged));

	/// <summary>The tool-tip a box shows while it has no error, e.g. <c>BCG group: an integer 1–40.</c></summary>
	public static readonly DependencyProperty HintProperty =
		DependencyProperty.RegisterAttached(
			"Hint", typeof(string), typeof(FieldState),
			new PropertyMetadata(null));

	/// <summary>
	/// A warning for this input, or <see langword="null"/>: a value that isn't wrong as such, but that
	/// saving will ask about. The themed TextBox shows it like an error, in the warning colour; it never
	/// blocks a save, and an error, while there is one, shows instead.
	/// </summary>
	public static readonly DependencyProperty WarningProperty =
		DependencyProperty.RegisterAttached(
			"Warning", typeof(string), typeof(FieldState),
			new PropertyMetadata(null, OnWarningChanged));

	private static readonly DependencyPropertyKey HasErrorKey =
		DependencyProperty.RegisterAttachedReadOnly(
			"HasError", typeof(bool), typeof(FieldState),
			new PropertyMetadata(false));

	/// <summary>Whether <see cref="ErrorProperty"/> is set - the trigger the styles key off.</summary>
	public static readonly DependencyProperty HasErrorProperty = HasErrorKey.DependencyProperty;

	private static readonly DependencyPropertyKey HasWarningKey =
		DependencyProperty.RegisterAttachedReadOnly(
			"HasWarning", typeof(bool), typeof(FieldState),
			new PropertyMetadata(false));

	/// <summary>Whether <see cref="WarningProperty"/> is set - the trigger the TextBox style keys off.</summary>
	public static readonly DependencyProperty HasWarningProperty = HasWarningKey.DependencyProperty;

	/// <summary>
	/// Bubbles up from an element whose <see cref="ErrorProperty"/> changed, so the card around it
	/// can outline itself (see <c>Card.NeedsAttention</c>).
	/// </summary>
	public static readonly RoutedEvent ErrorChangedEvent = EventManager.RegisterRoutedEvent(
		"ErrorChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(FieldState));

	/// <summary>Sets the validation message for an input.</summary>
	/// <param name="element">The input.</param>
	/// <param name="value">The message, or <see langword="null"/>.</param>
	public static void SetError(DependencyObject element, string? value) => element.SetValue(ErrorProperty, value);

	/// <summary>Gets the validation message for an input.</summary>
	/// <param name="element">The input.</param>
	/// <returns>The message, or <see langword="null"/>.</returns>
	public static string? GetError(DependencyObject element) => (string?)element.GetValue(ErrorProperty);

	/// <summary>Sets the tool-tip an input shows while it has no error.</summary>
	/// <param name="element">The input.</param>
	/// <param name="value">The hint, or <see langword="null"/>.</param>
	public static void SetHint(DependencyObject element, string? value) => element.SetValue(HintProperty, value);

	/// <summary>Gets the tool-tip an input shows while it has no error.</summary>
	/// <param name="element">The input.</param>
	/// <returns>The hint, or <see langword="null"/>.</returns>
	public static string? GetHint(DependencyObject element) => (string?)element.GetValue(HintProperty);

	/// <summary>Whether the input currently has a validation message.</summary>
	/// <param name="element">The input.</param>
	/// <returns><see langword="true"/> when a message is set.</returns>
	public static bool GetHasError(DependencyObject element) => (bool)element.GetValue(HasErrorProperty);

	/// <summary>Sets the warning for an input.</summary>
	/// <param name="element">The input.</param>
	/// <param name="value">The warning, or <see langword="null"/>.</param>
	public static void SetWarning(DependencyObject element, string? value) => element.SetValue(WarningProperty, value);

	/// <summary>Gets the warning for an input.</summary>
	/// <param name="element">The input.</param>
	/// <returns>The warning, or <see langword="null"/>.</returns>
	public static string? GetWarning(DependencyObject element) => (string?)element.GetValue(WarningProperty);

	/// <summary>Whether the input currently has a warning.</summary>
	/// <param name="element">The input.</param>
	/// <returns><see langword="true"/> when a warning is set.</returns>
	public static bool GetHasWarning(DependencyObject element) => (bool)element.GetValue(HasWarningProperty);

	private static void OnWarningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		d.SetValue(HasWarningKey, !string.IsNullOrWhiteSpace(e.NewValue as string));

	private static void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		d.SetValue(HasErrorKey, !string.IsNullOrWhiteSpace(e.NewValue as string));

		if (d is UIElement element)
		{
			element.RaiseEvent(new RoutedEventArgs(ErrorChangedEvent, element));
		}
	}
}
