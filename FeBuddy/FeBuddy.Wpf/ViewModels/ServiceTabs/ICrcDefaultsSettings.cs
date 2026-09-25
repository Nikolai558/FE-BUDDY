using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// The CRC ERAM Defaults card (<c>Views/Cards/CrcDefaultsCard</c>): the values the isDefaults
/// Features hold. Only the rows the vNAS files chosen for CRC-ERAM defaults need are shown (see
/// <see cref="IVnasUploadSettings"/>); the rest keep their values but stay hidden.
/// </summary>
public interface ICrcDefaultsSettings
{
	/// <summary>Whether any file gets CRC-ERAM defaults, so the card shows.</summary>
	bool HasCrcDefaultsInUse { get; }

	/// <summary>The Lines defaults in use: one row per class (one column in the panel each).</summary>
	IReadOnlyList<EramClassDefault> LineDefaultsInUse { get; }

	/// <summary>The Symbols defaults in use: one row per class.</summary>
	IReadOnlyList<EramClassDefault> SymbolDefaultsInUse { get; }

	/// <summary>The Text defaults in use: one row per class.</summary>
	IReadOnlyList<EramClassDefault> TextDefaultsInUse { get; }
}
