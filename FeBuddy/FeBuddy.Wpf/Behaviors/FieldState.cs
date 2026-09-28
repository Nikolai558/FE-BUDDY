using System.Windows;

namespace FeBuddy.Wpf.Behaviors;

/// <summary>
/// <c>bhv:FieldState.Error="{Binding FieldErrors[SwLat]}"</c> on an input - marks that box as
/// carrying a validation failure so the themed styles can highlight it and show the reason.
/// </summary>
/// <remarks>
/// The bound value is the message itself (or <see langword="null"/> when the field is fine), which
/// keeps the view declarative: one binding per box, no code-behind, and the message the style
/// shows cannot drift from the message the validator produced.
/// </remarks>
public static class FieldState
{
	/// <summary>The validation message for this input, or <see langword="null"/>.</summary>
	public static readonly DependencyProperty ErrorProperty =
		DependencyProperty.RegisterAttached(
			"Error", typeof(string), typeof(FieldState),
			new PropertyMetadata(null, OnErrorChanged));

	private static readonly DependencyPropertyKey HasErrorKey =
		DependencyProperty.RegisterAttachedReadOnly(
			"HasError", typeof(bool), typeof(FieldState),
			new PropertyMetadata(false));

	/// <summary>Whether <see cref="ErrorProperty"/> is set - the trigger the styles key off.</summary>
	public static readonly DependencyProperty HasErrorProperty = HasErrorKey.DependencyProperty;

	/// <summary>Sets the validation message for an input.</summary>
	/// <param name="element">The input.</param>
	/// <param name="value">The message, or <see langword="null"/>.</param>
	public static void SetError(DependencyObject element, string? value) => element.SetValue(ErrorProperty, value);

	/// <summary>Gets the validation message for an input.</summary>
	/// <param name="element">The input.</param>
	/// <returns>The message, or <see langword="null"/>.</returns>
	public static string? GetError(DependencyObject element) => (string?)element.GetValue(ErrorProperty);

	/// <summary>Whether the input currently has a validation message.</summary>
	/// <param name="element">The input.</param>
	/// <returns><see langword="true"/> when a message is set.</returns>
	public static bool GetHasError(DependencyObject element) => (bool)element.GetValue(HasErrorProperty);

	private static void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		d.SetValue(HasErrorKey, !string.IsNullOrWhiteSpace(e.NewValue as string));
}
