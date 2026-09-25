using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Views.Cards;

/// <summary>The shared Run card at the foot of a tab that starts a run. See RunCard.xaml.</summary>
public partial class RunCard : UserControl
{
	/// <summary>Identifies the <see cref="Note"/> dependency property.</summary>
	public static readonly DependencyProperty NoteProperty = Register(nameof(Note));

	/// <summary>Identifies the <see cref="Blocker"/> dependency property.</summary>
	public static readonly DependencyProperty BlockerProperty = Register(nameof(Blocker));

	/// <summary>Creates the card.</summary>
	public RunCard() => InitializeComponent();

	/// <summary>What the user should know before running, e.g. that unsaved settings are saved first.</summary>
	public string? Note { get => (string?)GetValue(NoteProperty); set => SetValue(NoteProperty, value); }

	/// <summary>Why the run cannot start right now, or <see langword="null"/> when it can.</summary>
	public string? Blocker { get => (string?)GetValue(BlockerProperty); set => SetValue(BlockerProperty, value); }

	private static DependencyProperty Register(string name) =>
		DependencyProperty.Register(name, typeof(string), typeof(RunCard), new PropertyMetadata(null));
}
