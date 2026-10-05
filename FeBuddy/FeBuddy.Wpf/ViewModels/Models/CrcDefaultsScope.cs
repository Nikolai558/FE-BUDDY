namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// The answer to the CRC ERAM Defaults card's question: which of the GeoJSON files a tab writes get
/// CRC-ERAM defaults.
/// </summary>
/// <remarks>Saved by name, so renaming a value loses that choice from saved settings.</remarks>
public enum CrcDefaultsScope
{
	/// <summary>None of them.</summary>
	None = 0,

	/// <summary>Every GeoJSON file the tab writes.</summary>
	AllGeojsonFiles = 1,

	/// <summary>Only the ones the user ticks.</summary>
	SpecificFiles = 2,
}
