using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Views.Cards;

/// <summary>The shared "What files do you want?" card. See GeojsonFilesCard.xaml.</summary>
public partial class GeojsonFilesCard : UserControl
{
	/// <summary>Identifies the <see cref="LinesLabel"/> dependency property.</summary>
	public static readonly DependencyProperty LinesLabelProperty = Register(nameof(LinesLabel), "Lines");

	/// <summary>Identifies the <see cref="LinesFile"/> dependency property.</summary>
	public static readonly DependencyProperty LinesFileProperty = Register(nameof(LinesFile), null);

	/// <summary>Identifies the <see cref="LinesDescription"/> dependency property.</summary>
	public static readonly DependencyProperty LinesDescriptionProperty = Register(nameof(LinesDescription), null);

	/// <summary>Identifies the <see cref="SymbolsLabel"/> dependency property.</summary>
	public static readonly DependencyProperty SymbolsLabelProperty = Register(nameof(SymbolsLabel), "Symbols");

	/// <summary>Identifies the <see cref="SymbolsFile"/> dependency property.</summary>
	public static readonly DependencyProperty SymbolsFileProperty = Register(nameof(SymbolsFile), null);

	/// <summary>Identifies the <see cref="SymbolsDescription"/> dependency property.</summary>
	public static readonly DependencyProperty SymbolsDescriptionProperty = Register(nameof(SymbolsDescription), null);

	/// <summary>Identifies the <see cref="TextLabel"/> dependency property.</summary>
	public static readonly DependencyProperty TextLabelProperty = Register(nameof(TextLabel), "Text");

	/// <summary>Identifies the <see cref="TextFile"/> dependency property.</summary>
	public static readonly DependencyProperty TextFileProperty = Register(nameof(TextFile), null);

	/// <summary>Identifies the <see cref="TextDescription"/> dependency property.</summary>
	public static readonly DependencyProperty TextDescriptionProperty = Register(nameof(TextDescription), null);

	/// <summary>Identifies the <see cref="Footnote"/> dependency property.</summary>
	public static readonly DependencyProperty FootnoteProperty = Register(nameof(Footnote), null);

	/// <summary>Identifies the <see cref="Footer"/> dependency property.</summary>
	public static readonly DependencyProperty FooterProperty =
		DependencyProperty.Register(nameof(Footer), typeof(object), typeof(GeojsonFilesCard), new PropertyMetadata(null));

	/// <summary>Identifies the <see cref="ShowLines"/> dependency property.</summary>
	public static readonly DependencyProperty ShowLinesProperty =
		DependencyProperty.Register(nameof(ShowLines), typeof(bool), typeof(GeojsonFilesCard), new PropertyMetadata(true));

	/// <summary>Creates the card.</summary>
	public GeojsonFilesCard() => InitializeComponent();

	/// <summary>The Lines option's label. Defaults to "Lines".</summary>
	public string? LinesLabel { get => (string?)GetValue(LinesLabelProperty); set => SetValue(LinesLabelProperty, value); }

	/// <summary>The Lines file's name, or its pattern (<c>*_Lines.geojson</c>), shown as code under its option.</summary>
	public string? LinesFile { get => (string?)GetValue(LinesFileProperty); set => SetValue(LinesFileProperty, value); }

	/// <summary>What the Lines file holds, shown under its name.</summary>
	public string? LinesDescription { get => (string?)GetValue(LinesDescriptionProperty); set => SetValue(LinesDescriptionProperty, value); }

	/// <summary>The Symbols option's label. Defaults to "Symbols".</summary>
	public string? SymbolsLabel { get => (string?)GetValue(SymbolsLabelProperty); set => SetValue(SymbolsLabelProperty, value); }

	/// <summary>The Symbols file's name, or its pattern (<c>*_Symbols.geojson</c>), shown as code under its option.</summary>
	public string? SymbolsFile { get => (string?)GetValue(SymbolsFileProperty); set => SetValue(SymbolsFileProperty, value); }

	/// <summary>What the Symbols file holds, shown under its name.</summary>
	public string? SymbolsDescription { get => (string?)GetValue(SymbolsDescriptionProperty); set => SetValue(SymbolsDescriptionProperty, value); }

	/// <summary>The Text option's label. Defaults to "Text".</summary>
	public string? TextLabel { get => (string?)GetValue(TextLabelProperty); set => SetValue(TextLabelProperty, value); }

	/// <summary>The Text file's name, or its pattern (<c>*_Text.geojson</c>), shown as code under its option.</summary>
	public string? TextFile { get => (string?)GetValue(TextFileProperty); set => SetValue(TextFileProperty, value); }

	/// <summary>What the Text file holds, shown under its name.</summary>
	public string? TextDescription { get => (string?)GetValue(TextDescriptionProperty); set => SetValue(TextDescriptionProperty, value); }

	/// <summary>Optional small print under the options.</summary>
	public string? Footnote { get => (string?)GetValue(FootnoteProperty); set => SetValue(FootnoteProperty, value); }

	/// <summary>
	/// Optional content under the options, for more than a line of small print: Departures and
	/// Arrivals say where their files are saved and how they're named.
	/// </summary>
	public object? Footer { get => GetValue(FooterProperty); set => SetValue(FooterProperty, value); }

	/// <summary>Whether the Lines option is offered. Default <see langword="true"/>; off for a sub-service with no Lines file (Fixes, NAVAIDs, Wx Stations).</summary>
	public bool ShowLines { get => (bool)GetValue(ShowLinesProperty); set => SetValue(ShowLinesProperty, value); }

	private static DependencyProperty Register(string name, string? defaultValue) =>
		DependencyProperty.Register(name, typeof(string), typeof(GeojsonFilesCard), new PropertyMetadata(defaultValue));
}
