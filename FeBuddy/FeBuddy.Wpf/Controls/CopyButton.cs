using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// A tiny "copy to clipboard" icon button. Set <see cref="Value"/>; on click it
/// copies that text and flips its glyph to a check for ~1 s (the <see cref="Copied"/>
/// read-only property, used by the Copy.Button style's trigger).
/// </summary>
public sealed class CopyButton : Button
{
	/// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
	public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
		nameof(Value), typeof(string), typeof(CopyButton), new PropertyMetadata(string.Empty));

	private static readonly DependencyPropertyKey CopiedKey = DependencyProperty.RegisterReadOnly(
		nameof(Copied), typeof(bool), typeof(CopyButton), new PropertyMetadata(false));

	/// <summary>Identifies the read-only <see cref="Copied"/> dependency property.</summary>
	public static readonly DependencyProperty CopiedProperty = CopiedKey.DependencyProperty;

	/// <summary>Creates the button with its "Copy" tooltip.</summary>
	public CopyButton()
	{
		Click += OnClick;
		ToolTip = "Copy";
	}

	/// <summary>Text placed on the clipboard when clicked.</summary>
	public string? Value
	{
		get => (string?)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	/// <summary><see langword="true"/> for about a second after a successful copy.</summary>
	public bool Copied => (bool)GetValue(CopiedProperty);

	private void OnClick(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrEmpty(Value))
		{
			return;
		}

		try
		{
			Clipboard.SetText(Value);
		}
		catch
		{
			return; // clipboard can be locked by another process
		}

		SetValue(CopiedKey, true);
		var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.1) };
		timer.Tick += (_, _) =>
		{
			timer.Stop();
			SetValue(CopiedKey, false);
		};
		timer.Start();
	}
}
