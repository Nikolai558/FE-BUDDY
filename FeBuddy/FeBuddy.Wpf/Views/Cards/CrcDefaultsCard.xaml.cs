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

	/// <summary>Identifies the <see cref="Note"/> dependency property.</summary>
	public static readonly DependencyProperty NoteProperty = Register(
		nameof(Note),
		"The values written as the isDefaults feature at the top of each vNAS file chosen for CRC-ERAM defaults. Every box shown must be filled in.");

	/// <summary>Identifies the <see cref="ShowInclude"/> dependency property.</summary>
	public static readonly DependencyProperty ShowIncludeProperty = DependencyProperty.Register(
		nameof(ShowInclude), typeof(bool), typeof(CrcDefaultsCard), new PropertyMetadata(false));

	/// <summary>Identifies the <see cref="IncludeLines"/> dependency property.</summary>
	public static readonly DependencyProperty IncludeLinesProperty = RegisterInclude(nameof(IncludeLines));

	/// <summary>Identifies the <see cref="IncludeSymbols"/> dependency property.</summary>
	public static readonly DependencyProperty IncludeSymbolsProperty = RegisterInclude(nameof(IncludeSymbols));

	/// <summary>Identifies the <see cref="IncludeText"/> dependency property.</summary>
	public static readonly DependencyProperty IncludeTextProperty = RegisterInclude(nameof(IncludeText));

	/// <summary>Creates the card.</summary>
	public CrcDefaultsCard() => InitializeComponent();

	/// <summary>The line under the header saying what the values are for.</summary>
	public string Note { get => (string)GetValue(NoteProperty); set => SetValue(NoteProperty, value); }

	/// <summary>Whether each panel has an Include box (the File Conversions). Off by default.</summary>
	public bool ShowInclude { get => (bool)GetValue(ShowIncludeProperty); set => SetValue(ShowIncludeProperty, value); }

	/// <summary>The Lines panel's Include box. Two-way; on by default.</summary>
	public bool IncludeLines { get => (bool)GetValue(IncludeLinesProperty); set => SetValue(IncludeLinesProperty, value); }

	/// <summary>The Symbols panel's Include box. Two-way; on by default.</summary>
	public bool IncludeSymbols { get => (bool)GetValue(IncludeSymbolsProperty); set => SetValue(IncludeSymbolsProperty, value); }

	/// <summary>The Text panel's Include box. Two-way; on by default.</summary>
	public bool IncludeText { get => (bool)GetValue(IncludeTextProperty); set => SetValue(IncludeTextProperty, value); }

	/// <summary>The Lines panel's heading. Defaults to "Lines".</summary>
	public string LinesTitle { get => (string)GetValue(LinesTitleProperty); set => SetValue(LinesTitleProperty, value); }

	/// <summary>The Symbols panel's heading. Defaults to "Symbols".</summary>
	public string SymbolsTitle { get => (string)GetValue(SymbolsTitleProperty); set => SetValue(SymbolsTitleProperty, value); }

	/// <summary>The Text panel's heading. Defaults to "Text".</summary>
	public string TextTitle { get => (string)GetValue(TextTitleProperty); set => SetValue(TextTitleProperty, value); }

	private static DependencyProperty Register(string name, string defaultValue) =>
		DependencyProperty.Register(name, typeof(string), typeof(CrcDefaultsCard), new PropertyMetadata(defaultValue));

	private static DependencyProperty RegisterInclude(string name) =>
		DependencyProperty.Register(name, typeof(bool), typeof(CrcDefaultsCard),
			new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
}
