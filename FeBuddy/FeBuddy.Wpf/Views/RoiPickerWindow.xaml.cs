using System.Windows;
using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.Map.Models;

using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// Modal host for the shared <see cref="Controls.RoiEditor"/>, used by Settings ▸ Default ROI
/// and each sub-service's ROI override.
/// </summary>
public partial class RoiPickerWindow : ChromeWindow
{
	private RegionOfInterest? _result;

	private RoiPickerWindow(RegionOfInterest? initial, MapLayer? baseLayer)
	{
		InitializeComponent();
		Editor.BaseLayer = baseLayer;
		Editor.InitialRoi = initial;
		Editor.RoiSet += (_, roi) => { _result = roi; Close(); };
		Editor.Cancelled += (_, _) => Close();
	}

	/// <summary>
	/// Shows the picker modally. Returns the confirmed region, or <see langword="null"/> when
	/// the user cancelled.
	/// </summary>
	/// <param name="owner">The window to centre on.</param>
	/// <param name="initial">The ROI to seed the editor with, if any.</param>
	/// <param name="baseLayer">The reference base outline (US states).</param>
	/// <returns>The confirmed <see cref="RegionOfInterest"/>, or <see langword="null"/>.</returns>
	public static RegionOfInterest? Pick(Window? owner, RegionOfInterest? initial, MapLayer? baseLayer)
	{
		RoiPickerWindow window = new(initial, baseLayer) { Owner = owner };
		window.ShowDialog();
		return window._result;
	}
}
