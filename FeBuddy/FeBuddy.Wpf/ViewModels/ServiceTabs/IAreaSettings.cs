using System.Windows.Input;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// The Area card (<c>Views/Cards/AreaCard</c>): which one of the tab's area filters the run uses -
/// its ARTCCs, an ROI, everything, or (Procedures) none - and, for the ROI, which one.
/// </summary>
/// <remarks>
/// Each choice is a yes/no the card's radio buttons bind two-way: setting one to
/// <see langword="true"/> picks it, and setting it to <see langword="false"/> (the radio button that
/// was picked letting go) does nothing.
/// </remarks>
public interface IAreaSettings
{
	/// <summary>The sub-service's name, as in "An ROI specific to Airports".</summary>
	string Title { get; }

	/// <summary>Whether the tab offers its ARTCCs (Procedures: its facilities) as an area.</summary>
	bool OffersArtccs { get; }

	/// <summary>Whether the tab offers no area at all, only what is listed (Procedures).</summary>
	bool OffersNone { get; }

	/// <summary>Whether the run uses the tab's ARTCCs.</summary>
	bool AreaArtccs { get; set; }

	/// <summary>Whether the run uses an ROI.</summary>
	bool AreaRoi { get; set; }

	/// <summary>Whether the run takes everything.</summary>
	bool AreaEverything { get; set; }

	/// <summary>Whether the run uses only what is listed (Procedures).</summary>
	bool AreaNone { get; set; }

	/// <summary>Whether the ROI is the default one, from Settings or the Map page.</summary>
	bool UseDefaultRoi { get; set; }

	/// <summary>Whether the ROI is this tab's own.</summary>
	bool OverrideRoi { get; set; }

	/// <summary>Southwest corner latitude of the tab's own ROI.</summary>
	string SwLat { get; set; }

	/// <summary>Southwest corner longitude of the tab's own ROI.</summary>
	string SwLon { get; set; }

	/// <summary>Northeast corner latitude of the tab's own ROI.</summary>
	string NeLat { get; set; }

	/// <summary>Northeast corner longitude of the tab's own ROI.</summary>
	string NeLon { get; set; }

	/// <summary>The default ROI's corners and where to change it, or that none is set and what to do.</summary>
	string DefaultRoiSummary { get; }

	/// <summary>Whether a default ROI is set.</summary>
	bool HasDefaultRoi { get; }

	/// <summary>Per-field validation messages, keyed by property name (<c>SwLat</c> ...) or card (<c>Area</c>).</summary>
	ServiceFieldErrors FieldErrors { get; }

	/// <summary>Opens the shared ROI picker and copies what the user confirms into the four boxes.</summary>
	ICommand PickRoiOnMapCommand { get; }
}
