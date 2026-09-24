using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Views.Cards;

/// <summary>The shared "What files do you want?" card. See GeojsonFilesCard.xaml.</summary>
public partial class GeojsonFilesCard : UserControl
{
	public static readonly DependencyProperty LinesLabelProperty = Register(nameof(LinesLabel), "Lines");
	public static readonly DependencyProperty LinesDescriptionProperty = Register(nameof(LinesDescription), null);
	public static readonly DependencyProperty SymbolsLabelProperty = Register(nameof(SymbolsLabel), "Symbols");
	public static readonly DependencyProperty SymbolsDescriptionProperty = Register(nameof(SymbolsDescription), null);
	public static readonly DependencyProperty TextLabelProperty = Register(nameof(TextLabel), "Text");
	public static readonly DependencyProperty TextDescriptionProperty = Register(nameof(TextDescription), null);
	public static readonly DependencyProperty FootnoteProperty = Register(nameof(Footnote), null);

	public GeojsonFilesCard() => InitializeComponent();

	/// <summary>The Lines option's label. Defaults to "Lines".</summary>
	public string? LinesLabel { get => (string?)GetValue(LinesLabelProperty); set => SetValue(LinesLabelProperty, value); }

	/// <summary>What the Lines file holds, shown under its option.</summary>
	public string? LinesDescription { get => (string?)GetValue(LinesDescriptionProperty); set => SetValue(LinesDescriptionProperty, value); }

	/// <summary>The Symbols option's label. Defaults to "Symbols".</summary>
	public string? SymbolsLabel { get => (string?)GetValue(SymbolsLabelProperty); set => SetValue(SymbolsLabelProperty, value); }

	/// <summary>What the Symbols file holds, shown under its option.</summary>
	public string? SymbolsDescription { get => (string?)GetValue(SymbolsDescriptionProperty); set => SetValue(SymbolsDescriptionProperty, value); }

	/// <summary>The Text option's label. Defaults to "Text".</summary>
	public string? TextLabel { get => (string?)GetValue(TextLabelProperty); set => SetValue(TextLabelProperty, value); }

	/// <summary>What the Text file holds, shown under its option.</summary>
	public string? TextDescription { get => (string?)GetValue(TextDescriptionProperty); set => SetValue(TextDescriptionProperty, value); }

	/// <summary>Optional small print under the options, e.g. where the files are written.</summary>
	public string? Footnote { get => (string?)GetValue(FootnoteProperty); set => SetValue(FootnoteProperty, value); }

	private static DependencyProperty Register(string name, string? defaultValue) =>
		DependencyProperty.Register(name, typeof(string), typeof(GeojsonFilesCard), new PropertyMetadata(defaultValue));
}
