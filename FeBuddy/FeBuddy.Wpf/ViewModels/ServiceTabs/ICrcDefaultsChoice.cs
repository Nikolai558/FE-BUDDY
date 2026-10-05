using System.Collections.ObjectModel;

using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// The top of an AIRAC sub-service's CRC ERAM Defaults card (<c>Views/Cards/CrcFileChoice</c>):
/// which of the GeoJSON files the tab writes get CRC-ERAM defaults.
/// </summary>
public interface ICrcDefaultsChoice
{
	/// <summary>Which of the GeoJSON files get CRC-ERAM defaults.</summary>
	CrcDefaultsScope CrcDefaultsScope { get; set; }

	/// <summary>Whether <see cref="CrcDefaultsScope"/> is "specific files", so <see cref="CrcFileRows"/> shows.</summary>
	bool IsCrcDefaultsSpecific { get; }

	/// <summary>Every GeoJSON file the tab's settings write, one row per group, to tick for CRC-ERAM defaults.</summary>
	ObservableCollection<CrcFileRow> CrcFileRows { get; }
}
