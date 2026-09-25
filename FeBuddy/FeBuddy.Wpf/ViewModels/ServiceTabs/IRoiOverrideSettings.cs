using System.Windows.Input;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>The Region of Interest card (<c>Views/Cards/RoiOverrideCard</c>).</summary>
public interface IRoiOverrideSettings
{
	/// <summary>The sub-service's name, used in "Override the default ROI for ...".</summary>
	string Title { get; }

	/// <summary>Whether this sub-service uses its own ROI instead of the shared default one.</summary>
	bool OverrideRoi { get; set; }

	/// <summary>Southwest corner latitude of the override ROI.</summary>
	string SwLat { get; set; }

	/// <summary>Southwest corner longitude of the override ROI.</summary>
	string SwLon { get; set; }

	/// <summary>Northeast corner latitude of the override ROI.</summary>
	string NeLat { get; set; }

	/// <summary>Northeast corner longitude of the override ROI.</summary>
	string NeLon { get; set; }

	/// <summary>What the run uses when the override is off, shown under the checkbox.</summary>
	string RoiFallbackHint { get; }

	/// <summary>Per-field validation messages, keyed by property name (<c>SwLat</c> ...).</summary>
	ServiceFieldErrors FieldErrors { get; }

	/// <summary>Opens the shared ROI picker and copies what the user confirms into the four boxes.</summary>
	ICommand PickRoiOnMapCommand { get; }
}
