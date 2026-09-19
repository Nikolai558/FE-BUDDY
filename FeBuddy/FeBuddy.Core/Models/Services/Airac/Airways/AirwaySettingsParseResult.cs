using FeBuddy.Core.Models.Services.General;
using FeBuddy.Core.Services.General;

namespace FeBuddy.Core.Models.Services.Airac.Airways;

/// <summary>
/// The result of parsing a raw Airways settings dictionary: the typed settings object plus
/// any levelled messages noticed along the way (currently, unrecognized dictionary keys).
/// </summary>
/// <param name="Settings">The fully-parsed, typed settings.</param>
/// <param name="Messages">Levelled messages noticed while parsing, e.g. an unrecognized key at <see cref="LogLevel.Warning"/> (remediation plan 3.8).</param>
public sealed record AirwaySettingsParseResult(
	AirwaySettings Settings,
	IReadOnlyList<ServiceMessage> Messages)
{
	/// <summary>Backwards-compatible text-only view of the Warning/Error entries in <see cref="Messages"/>.</summary>
	public IReadOnlyList<string> Warnings =>
		Messages.Where(m => m.Level is LogLevel.Warning or LogLevel.Error).Select(m => m.Text).ToArray();
}
