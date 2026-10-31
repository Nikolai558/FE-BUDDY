using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Views.Cards;

/// <summary>The What You'll Get with the Current Settings card at the top of every AIRAC sub-service tab. See SummaryCard.xaml.</summary>
public partial class SummaryCard : UserControl
{
	/// <summary>Identifies the <see cref="ShowsAliasFileNote"/> dependency property.</summary>
	public static readonly DependencyProperty ShowsAliasFileNoteProperty = DependencyProperty.Register(
		nameof(ShowsAliasFileNote), typeof(bool), typeof(SummaryCard), new PropertyMetadata(true));

	/// <summary>Creates the card.</summary>
	public SummaryCard() => InitializeComponent();

	/// <summary>
	/// Whether the card ends with the tab's note that its alias file isn't exported, while there is
	/// one. <see langword="false"/> on a tab with an Outputs card, which has the note instead.
	/// </summary>
	public bool ShowsAliasFileNote
	{
		get => (bool)GetValue(ShowsAliasFileNoteProperty);
		set => SetValue(ShowsAliasFileNoteProperty, value);
	}
}
