using System.Collections.ObjectModel;
using System.Windows.Input;

using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.Infrastructure;

// The contracts behind the shared sub-service cards in Views/Cards. Each card binds to its tab's
// view model by these property names, so a sub-service tab gets a card by implementing the
// matching interface here and dropping the card into its view - no per-tab card markup.

/// <summary>
/// What a sub-service produces, for the Outputs card (<c>Views/Cards/OutputsCard</c>). Its
/// default GeoJSON row also binds <c>GenerateGeojson</c>; a tab whose GeoJSON choice is more
/// than on / off (Airways) gives the card its own GeoJSON row instead.
/// </summary>
public interface IOutputSettings
{
	/// <summary>The sub-service's name, used in the "at least one output" reminder.</summary>
	string Title { get; }

	/// <summary>Write the sub-service's alias file.</summary>
	bool GenerateAliasFile { get; set; }
}

/// <summary>
/// The three GeoJSON files a sub-service can write, for the What Files Do You Want? card
/// (<c>Views/Cards/GeojsonFilesCard</c>).
/// </summary>
public interface IGeojsonFileChoices
{
	/// <summary>Write the <c>_Lines</c> file.</summary>
	bool EmitLines { get; set; }

	/// <summary>Write the <c>_Symbols</c> file.</summary>
	bool EmitSymbols { get; set; }

	/// <summary>Write the <c>_Text</c> file.</summary>
	bool EmitText { get; set; }
}

/// <summary>The FE-Buddy Properties card (<c>Views/Cards/FebPropertiesCard</c>).</summary>
public interface IFebPropertySettings
{
	/// <summary>Whether Features carry the selected <c>feb.*</c> properties.</summary>
	bool IncludeFebCustomProperties { get; set; }

	/// <summary>One toggle per <c>feb.*</c> property this sub-service can write.</summary>
	ObservableCollection<FebPropertyToggle> FebProperties { get; }
}

/// <summary>
/// The CRC ERAM Defaults card (<c>Views/Cards/CrcDefaultsCard</c>): per file, whether its
/// isDefaults Feature is written and the values it holds. A file's panel shows only while
/// that file is being written, hence the <see cref="IGeojsonFileChoices"/> base.
/// </summary>
public interface ICrcDefaultsSettings : IGeojsonFileChoices
{
	/// <summary>Write the CRC ERAM defaults into the <c>_Lines</c> file.</summary>
	bool IncludeCrcLineDefaults { get; set; }

	/// <summary>Write the CRC ERAM defaults into the <c>_Symbols</c> file.</summary>
	bool IncludeCrcSymbolDefaults { get; set; }

	/// <summary>Write the CRC ERAM defaults into the <c>_Text</c> file.</summary>
	bool IncludeCrcTextDefaults { get; set; }

	/// <summary>The Lines defaults: one row per class (one column in the panel each).</summary>
	ObservableCollection<EramClassDefault> LineDefaults { get; }

	/// <summary>The Symbols defaults: one row per class.</summary>
	ObservableCollection<EramClassDefault> SymbolDefaults { get; }

	/// <summary>The Text defaults: one row per class.</summary>
	ObservableCollection<EramClassDefault> TextDefaults { get; }
}

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
