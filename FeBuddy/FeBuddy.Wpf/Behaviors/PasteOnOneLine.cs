using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Behaviors;

/// <summary>
/// <c>bhv:PasteOnOneLine.IsEnabled="True"</c> on a single-line TextBox - pastes a list copied one
/// item per line (or per cell) as one line, the items separated by <c>", "</c>.
/// </summary>
/// <remarks>
/// A TextBox that doesn't accept returns keeps only the first line of what is pasted, so a column
/// of airport IDs copied from a spreadsheet would arrive as its first ID alone.
/// </remarks>
public static class PasteOnOneLine
{
	private static readonly char[] LineBreaks = ['\r', '\n', '\t'];

	/// <summary>Whether pasted lines are joined.</summary>
	public static readonly DependencyProperty IsEnabledProperty =
		DependencyProperty.RegisterAttached(
			"IsEnabled", typeof(bool), typeof(PasteOnOneLine),
			new PropertyMetadata(false, OnIsEnabledChanged));

	/// <summary>Sets whether pasted lines are joined.</summary>
	/// <param name="element">The TextBox.</param>
	/// <param name="value">The value.</param>
	public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

	/// <summary>Gets whether pasted lines are joined.</summary>
	/// <param name="element">The TextBox.</param>
	/// <returns>The value.</returns>
	public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

	private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not TextBox box)
		{
			return;
		}

		DataObject.RemovePastingHandler(box, OnPasting);

		if (e.NewValue is true)
		{
			DataObject.AddPastingHandler(box, OnPasting);
		}
	}

	private static void OnPasting(object sender, DataObjectPastingEventArgs e)
	{
		if (sender is not TextBox box
			|| e.DataObject.GetData(DataFormats.UnicodeText) is not string text
			|| text.IndexOfAny(LineBreaks) < 0)
		{
			return;
		}

		e.CancelCommand();

		string oneLine = string.Join(", ", text.Split(LineBreaks, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
		box.SelectedText = oneLine;
		box.CaretIndex = box.SelectionStart + box.SelectionLength;
	}
}
