using FeBuddy.Core.Domain.Navaids;

namespace FeBuddy.Core.Application.Airac.Navaids.Models;

/// <summary>
/// How a merged <see cref="NavaidOutputBy.All"/> Symbols file assigns each NAVAID's CRC symbol
/// style. Meaningless (and not read) when <see cref="NavaidSettings.OutputBy"/> is
/// <see cref="NavaidOutputBy.Type"/>: each type's own file already draws every NAVAID in it the
/// same way, from that file's own CRC defaults.
/// </summary>
public enum NavaidSymbolStyleBy
{
	/// <summary>
	/// Every Symbol Feature carries its own <c>style</c>, from <see cref="NavaidTypes.SymbolStyleFor"/>
	/// (or <see cref="NavaidSettings.FanMarkerStyle"/> for a fan marker). The file's CRC defaults
	/// carry no <c>style</c> of their own.
	/// </summary>
	Type = 0,

	/// <summary>Every NAVAID in the file is drawn with the one style set in the file's CRC defaults.</summary>
	File = 1,
}
