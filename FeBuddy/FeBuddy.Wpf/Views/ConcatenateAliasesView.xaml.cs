using System.Windows;
using System.Windows.Controls;

using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.Views;

/// <summary>The Concatenate Aliases sub-service tab. See ConcatenateAliasesView.xaml.</summary>
public partial class ConcatenateAliasesView : UserControl
{
	/// <summary>Creates the view.</summary>
	public ConcatenateAliasesView() => InitializeComponent();

	/// <summary>
	/// Once the user leaves a row's address box, a GitHub address is shown as the file's Raw link.
	/// Not while typing: rewriting the text then would move the cursor out from under them.
	/// </summary>
	private void OnLocationLostFocus(object sender, RoutedEventArgs e)
	{
		if (sender is FrameworkElement { DataContext: AliasSourceRow row })
		{
			row.TidyLocation();
		}
	}
}