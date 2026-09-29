using System.Windows;

using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// The modal Reset FE-Buddy window. Shows a <see cref="ResetViewModel"/>'s choices and closes
/// itself when the view-model asks; the view-model's <see cref="ResetViewModel.Confirmed"/> says
/// which button closed it.
/// </summary>
public partial class ResetWindow : ChromeWindow
{
	/// <summary>The height the rest of the window takes - title bar, padding, heading and buttons - with room to spare.</summary>
	private const double ReservedHeight = 200;

	/// <summary>The least the body shrinks to, even on a very short screen.</summary>
	private const double MinimumBodyHeight = 160;

	/// <summary>Creates the window for <paramref name="viewModel"/>.</summary>
	/// <param name="viewModel">The choices to show.</param>
	public ResetWindow(ResetViewModel viewModel)
	{
		ArgumentNullException.ThrowIfNull(viewModel);

		InitializeComponent();
		DataContext = viewModel;
		viewModel.CloseRequested += (_, _) => Close();

		// A long body scrolls rather than pushing the buttons off the bottom of the screen.
		Body.MaxHeight = Math.Max(MinimumBodyHeight, SystemParameters.WorkArea.Height - ReservedHeight);
	}
}
