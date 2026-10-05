using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.Core.Application.Airac.Telephony.Models;

/// <summary>
/// Fully-parsed, typed settings for the Telephony sub-service. Built once by
/// <c>TelephonySettingsParser.Parse</c> from the raw <c>Dictionary&lt;string, string&gt;</c> the GUI
/// (or <c>FeBuddy.Harness</c>) supplies.
/// </summary>
/// <remarks>
/// Telephony has one output - its alias file, <c>Telephony.txt</c> - and every operator in the FAA
/// pages gets its commands, with the user's own virtual airlines added after them. No GeoJSON, no
/// region of interest, no <c>feb.*</c> properties.
/// </remarks>
public sealed record TelephonySettings
{
	/// <summary>The folder the run writes into - the <c>AIRAC_&lt;cycle&gt;</c> folder when run by the AIRAC Service.</summary>
	public required string OutputDirectory { get; init; }

	/// <summary>The user's virtual airlines, in list order, each written after the FAA's operators. Default: none.</summary>
	public IReadOnlyList<VirtualAirline> VirtualAirlines { get; init; } = [];

	/// <summary>
	/// Whether the VATSIM-Radar Virtual Airline List's virtual airlines are written too, after the
	/// user's own (see <see cref="VatsimRadarVirtualAirlines"/>). Default: no.
	/// </summary>
	public bool IncludeVatsimRadarVirtualAirlines { get; init; }

	/// <summary>
	/// The names the user gave files in place of FE-Buddy's, by file key. Not part of the settings
	/// block: <see cref="TelephonyService"/> takes it from the AIRAC Service. Default: none renamed.
	/// </summary>
	public OutputFileNames FileNames { get; init; } = OutputFileNames.None;
}
