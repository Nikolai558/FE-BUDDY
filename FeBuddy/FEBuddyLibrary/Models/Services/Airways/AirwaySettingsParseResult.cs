namespace FEBuddyLibrary.Models.Services.Airways;

/// <summary>
/// The result of parsing a raw Airways settings dictionary: the typed settings object plus
/// any non-fatal warnings noticed along the way (currently, unrecognized dictionary keys).
/// </summary>
/// <param name="Settings">The fully-parsed, typed settings.</param>
/// <param name="Warnings">Non-fatal problems noticed while parsing (e.g. an unrecognized key).</param>
public sealed record AirwaySettingsParseResult(
	AirwaySettings Settings,
	IReadOnlyList<string> Warnings);
