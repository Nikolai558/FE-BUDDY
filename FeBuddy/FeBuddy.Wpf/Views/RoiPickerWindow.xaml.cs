using System.Windows;

using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// A map popup for picking an ROI, used by Settings ▸ Default ROI and each sub-service's ROI
/// override. It is the Map page's own <see cref="MapWorkspace"/> - the same layers, tools and
/// ROI card - opened in editing mode; the ROI card's save button returns the box and closes it.
/// </summary>
public partial class RoiPickerWindow : ChromeWindow
{
	private readonly PickTarget _target;

	private RoiPickerWindow(PickTarget target)
	{
		InitializeComponent();
		_target = target;
		Title = target.Title;
		target.Done = Close;
		Workspace.DataContext = new MapViewModel(target);

		// Big enough to work in, but never bigger than the screen it opens on.
		Rect area = SystemParameters.WorkArea;
		Width = Math.Min(Width, area.Width * 0.94);
		Height = Math.Min(Height, area.Height * 0.94);
	}

	/// <summary>
	/// Shows the picker modally. Returns the confirmed region, or <see langword="null"/> when
	/// the user cancelled.
	/// </summary>
	/// <param name="owner">The window to centre on.</param>
	/// <param name="initial">The ROI to start from, if any.</param>
	/// <param name="title">What is being picked, e.g. <c>Airways ROI override</c>; the window and ROI card title.</param>
	/// <param name="reference">A second ROI drawn dashed for comparison (the default ROI, for an override), if any.</param>
	/// <returns>The confirmed <see cref="RegionOfInterest"/>, or <see langword="null"/>.</returns>
	public static RegionOfInterest? Pick(Window? owner, RegionOfInterest? initial, string title, RegionOfInterest? reference = null)
	{
		RoiPickerWindow window = new(new PickTarget(title, initial, reference)) { Owner = owner };
		window.ShowDialog();
		return window._target.Result;
	}

	/// <summary>The popup's ROI: returns the saved box to the caller and closes the popup.</summary>
	private sealed class PickTarget(string title, RegionOfInterest? initial, RegionOfInterest? reference) : IRoiTarget
	{
		public event EventHandler? CurrentChanged
		{
			add { }
			remove { }
		}

		public Action? Done { get; set; }

		public RegionOfInterest? Result { get; private set; }

		public string Title => title;

		public string Description => reference is null
			? "Draw the box on the map or type its corners, then use it. Nothing changes until you do."
			: "The dashed box is the default ROI. Draw this one's box, then use it. Nothing changes until you do.";

		public string SaveLabel => "Use this ROI";

		public bool CanClear => false;

		public bool StartsEditing => true;

		public RegionOfInterest? Current => initial;

		public RegionOfInterest? Reference => reference;

		public void Commit(RegionOfInterest? roi)
		{
			Result = roi;
			Done?.Invoke();
		}

		public void Cancel() => Done?.Invoke();
	}
}
