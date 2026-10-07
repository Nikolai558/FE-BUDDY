using System.Windows;

using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// Asks the user what to do with each duplicated alias command, when the AIRAC run stops for it
/// (<see cref="DuplicateAliasReviewViewModel"/>). Closing the window stops the run, like
/// <b>Stop the run</b>.
/// </summary>
public partial class DuplicateAliasReviewWindow : ChromeWindow
{
	/// <summary>Creates the window for <paramref name="viewModel"/>.</summary>
	/// <param name="viewModel">What to ask.</param>
	public DuplicateAliasReviewWindow(DuplicateAliasReviewViewModel viewModel)
	{
		ArgumentNullException.ThrowIfNull(viewModel);

		InitializeComponent();
		DataContext = viewModel;
		viewModel.CloseRequested += (_, _) => Close();
	}

	/// <summary>Shows the window modally.</summary>
	/// <param name="owner">The window to centre on.</param>
	/// <param name="review">What the run asks.</param>
	/// <returns>The user's choices, or <see langword="null"/> when they stopped the run.</returns>
	public static IReadOnlyList<DuplicateAliasRule>? Review(Window? owner, DuplicateAliasReview review)
	{
		DuplicateAliasReviewViewModel viewModel = new(review);
		new DuplicateAliasReviewWindow(viewModel) { Owner = owner }.ShowDialog();

		return viewModel.Result;
	}
}
