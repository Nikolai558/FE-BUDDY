using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Views.Cards;

/// <summary>The shared CRC ERAM Defaults card. See CrcDefaultsCard.xaml.</summary>
public partial class CrcDefaultsCard : UserControl
{
	/// <summary>Identifies the <see cref="LinesTitle"/> dependency property.</summary>
	public static readonly DependencyProperty LinesTitleProperty = Register(nameof(LinesTitle), "Lines");

	/// <summary>Identifies the <see cref="SymbolsTitle"/> dependency property.</summary>
	public static readonly DependencyProperty SymbolsTitleProperty = Register(nameof(SymbolsTitle), "Symbols");

	/// <summary>Identifies the <see cref="TextTitle"/> dependency property.</summary>
	public static readonly DependencyProperty TextTitleProperty = Register(nameof(TextTitle), "Text");

	/// <summary>Creates the card.</summary>
	public CrcDefaultsCard() => InitializeComponent();

	/// <summary>The Lines panel's heading. Defaults to "Lines".</summary>
	public string LinesTitle { get => (string)GetValue(LinesTitleProperty); set => SetValue(LinesTitleProperty, value); }

	/// <summary>The Symbols panel's heading. Defaults to "Symbols".</summary>
	public string SymbolsTitle { get => (string)GetValue(SymbolsTitleProperty); set => SetValue(SymbolsTitleProperty, value); }

	/// <summary>The Text panel's heading. Defaults to "Text".</summary>
	public string TextTitle { get => (string)GetValue(TextTitleProperty); set => SetValue(TextTitleProperty, value); }

	private static DependencyProperty Register(string name, string defaultValue) =>
		DependencyProperty.Register(name, typeof(string), typeof(CrcDefaultsCard), new PropertyMetadata(defaultValue));
}
