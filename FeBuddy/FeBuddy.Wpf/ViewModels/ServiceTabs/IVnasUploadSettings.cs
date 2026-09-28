using System.Collections.ObjectModel;

using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// The Upload to vNAS card (<c>Views/Cards/VnasUploadCard</c>): which of the files the tab will
/// write go to vNAS, and which of those get CRC-ERAM defaults.
/// </summary>
public interface IVnasUploadSettings
{
	/// <summary>Every file the tab's settings will write, one row per group, to tick for vNAS.</summary>
	ObservableCollection<VnasFileRow> VnasFileRows { get; }

	/// <summary>Whether the tab writes any file at all (otherwise the card says so).</summary>
	bool HasOutputFiles { get; }

	/// <summary>Whether a GeoJSON file is marked for vNAS, so the CRC-ERAM question applies.</summary>
	bool HasVnasGeojsonFiles { get; }

	/// <summary>Which of the vNAS GeoJSON files get CRC-ERAM defaults.</summary>
	CrcDefaultsScope CrcDefaultsScope { get; set; }

	/// <summary>Whether <see cref="CrcDefaultsScope"/> is "specific files", so <see cref="CrcFileRows"/> shows.</summary>
	bool IsCrcDefaultsSpecific { get; }

	/// <summary>The GeoJSON files marked for vNAS, in the same rows, to tick for CRC-ERAM defaults.</summary>
	ObservableCollection<VnasFileRow> CrcFileRows { get; }
}
