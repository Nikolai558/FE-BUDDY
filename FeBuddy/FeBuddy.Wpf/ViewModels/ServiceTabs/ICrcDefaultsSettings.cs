using System.Collections.ObjectModel;

using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

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
