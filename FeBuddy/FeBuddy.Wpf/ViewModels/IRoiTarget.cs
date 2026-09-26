using FeBuddy.Core.Domain.Geo.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Where the ROI a map edits ends up. The Map page edits the saved default ROI
/// (<see cref="DefaultRoiTarget"/>); a map popup edits whatever opened it (Settings' pending
/// default ROI, or a sub-service's override) and closes on save or cancel.
/// </summary>
public interface IRoiTarget
{
	/// <summary>The ROI card's title, e.g. <c>Default Region of Interest</c>.</summary>
	string Title { get; }

	/// <summary>One line under the title saying what saving does.</summary>
	string Description { get; }

	/// <summary>The save button's text, e.g. <c>Save</c> or <c>Use this ROI</c>.</summary>
	string SaveLabel { get; }

	/// <summary>Whether saving with no box clears the ROI (the Map page) rather than being refused.</summary>
	bool CanClear { get; }

	/// <summary>Whether the map opens with ROI editing already on (a popup opened to pick one).</summary>
	bool StartsEditing { get; }

	/// <summary>The ROI as it stands, or <see langword="null"/> for none.</summary>
	RegionOfInterest? Current { get; }

	/// <summary>A second ROI drawn dashed for comparison, or <see langword="null"/>.</summary>
	RegionOfInterest? Reference { get; }

	/// <summary>Raised when <see cref="Current"/> changes from outside the map (e.g. Settings saved one).</summary>
	event EventHandler? CurrentChanged;

	/// <summary>Takes the ROI the user saved.</summary>
	/// <param name="roi">The ROI, or <see langword="null"/> to clear it (only when <see cref="CanClear"/>).</param>
	void Commit(RegionOfInterest? roi);

	/// <summary>The user cancelled an edit. A popup closes; the Map page just stops editing.</summary>
	void Cancel();
}
