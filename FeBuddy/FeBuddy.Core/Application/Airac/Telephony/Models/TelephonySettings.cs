using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.Core.Application.Airac.Telephony.Models;

/// <summary>
/// Fully-parsed, typed settings for the Telephony sub-service. Built once by
/// <c>TelephonySettingsParser.Parse</c> from the raw <c>Dictionary&lt;string, string&gt;</c> the GUI
/// (or <c>FeBuddy.Harness</c>) supplies.
/// </summary>
/// <remarks>
/// Telephony has one output - its alias file, <c>Telephony.txt</c> - and nothing to choose about
/// what it covers: every operator in the FAA pages gets its commands. No GeoJSON, no region of
/// interest, no <c>feb.*</c> properties.
/// </remarks>
public sealed record TelephonySettings
{
	/// <summary>The folder the run writes into - the <c>AIRAC_&lt;cycle&gt;</c> folder when run by the AIRAC Service.</summary>
	public required string OutputDirectory { get; init; }

	/// <summary>
	/// Whether the alias file goes to vNAS: copied into <c>Upload_to_vNAS\vNAS_Alias.txt</c> as well as
	/// written to the <c>Aliases</c> folder. Its only possible file key is <c>Telephony.txt</c>.
	/// </summary>
	public VnasFileChoices Vnas { get; init; } = VnasFileChoices.None;

	/// <summary>
	/// The names the user gave files in place of FE-Buddy's, by file key. Not part of the settings
	/// block: <see cref="TelephonyService"/> takes it from the AIRAC Service. Default: none renamed.
	/// </summary>
	public OutputFileNames FileNames { get; init; } = OutputFileNames.None;
}
