using System.Windows;
using System.Windows.Data;

using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// The modal Reset FE-Buddy window. Shows a <see cref="ResetViewModel"/>'s choices and closes
/// itself when the view-model asks; the view-model's <see cref="ResetViewModel.Confirmed"/> says
/// which button closed it.
/// </summary>
/// <remarks>
/// It opens at the height its tallest choice needs - Delete my settings, which says what goes - as
/// far as the screen allows, and keeps that height (issue #354). Sized to its content instead, it
/// grew downwards when that choice was picked, from where it had opened, and took its buttons off
/// the bottom of the screen. At a fixed height its body fills the rest and scrolls when it must.
/// </remarks>
public partial class ResetWindow : ChromeWindow
{
	/// <summary>The space kept above and below the window when it would otherwise fill the screen's height.</summary>
	private const double ScreenMargin = 24;

	/// <summary>Creates the window for <paramref name="viewModel"/>.</summary>
	/// <param name="viewModel">The choices to show.</param>
	public ResetWindow(ResetViewModel viewModel)
	{
		ArgumentNullException.ThrowIfNull(viewModel);

		InitializeComponent();
		DataContext = viewModel;
		viewModel.CloseRequested += (_, _) => Close();

		// No taller than the screen it opens on, from the start: once the owner is set, before the
		// window is sized to its content.
		SourceInitialized += (_, _) => MaxHeight = OwnerWorkArea().Height;

		// Before the first render, so the window is never seen at another size.
		Loaded += (_, _) => KeepTallestHeight();
	}

	/// <summary>
	/// Measures the window with what deleting the settings shows, fixes its height there (no taller
	/// than the work area of the screen its owner is on, less a margin), and centers it again on its
	/// owner, inside that work area.
	/// </summary>
	private void KeepTallestHeight()
	{
		// Shown for the measurement only: SetCurrentValue leaves the binding in place, and
		// UpdateTarget hands the choice back to it.
		SettingsLoss.SetCurrentValue(VisibilityProperty, Visibility.Visible);
		UpdateLayout();
		double tallest = ActualHeight;
		BindingOperations.GetBindingExpression(SettingsLoss, VisibilityProperty)?.UpdateTarget();

		Rect area = OwnerWorkArea();

		SizeToContent = SizeToContent.Manual;
		Height = Math.Min(tallest, Math.Max(area.Height - (2 * ScreenMargin), MinHeight));

		double top = Owner is { } owner
			? owner.Top + ((owner.ActualHeight - Height) / 2)
			: area.Top + ((area.Height - Height) / 2);

		Top = Math.Clamp(top, area.Top, Math.Max(area.Top, area.Bottom - Height));
	}
}
