namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// The answer to the Upload to vNAS card's follow-up question: which of the GeoJSON files going
/// to vNAS get CRC-ERAM defaults.
/// </summary>
/// <remarks>Saved by name, so never rename a value.</remarks>
public enum CrcDefaultsScope
{
	/// <summary>None of them.</summary>
	None = 0,

	/// <summary>Every GeoJSON file marked for vNAS.</summary>
	AllVnasFiles = 1,

	/// <summary>Only the ones the user ticks.</summary>
	SpecificFiles = 2,
}
