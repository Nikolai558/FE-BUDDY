using System.Windows;

using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// The modal question Export FE-Buddy Alias Command Guide asks first: a web page, Markdown, or
/// both. Shows an <see cref="AliasGuideFormatViewModel"/> and closes itself when it asks.
/// </summary>
public partial class AliasGuideFormatWindow : ChromeWindow
{
	/// <summary>Creates the window for <paramref name="viewModel"/>.</summary>
	/// <param name="viewModel">The choice to show.</param>
	public AliasGuideFormatWindow(AliasGuideFormatViewModel viewModel)
	{
		ArgumentNullException.ThrowIfNull(viewModel);

		InitializeComponent();
		DataContext = viewModel;
		viewModel.CloseRequested += (_, _) => Close();
	}

	/// <summary>Asks which format to export the guide in.</summary>
	/// <param name="owner">The window to centre on.</param>
	/// <returns>The formats to write, or <see langword="null"/> when the user cancelled.</returns>
	public static IReadOnlyList<AliasGuideFormat>? Ask(Window? owner)
	{
		AliasGuideFormatViewModel viewModel = new();
		new AliasGuideFormatWindow(viewModel) { Owner = owner }.ShowDialog();

		return viewModel.Confirmed ? viewModel.Formats : null;
	}
}
