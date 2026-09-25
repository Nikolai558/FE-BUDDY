namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One file (or, for Departures, Arrivals and NAVAIDs, one kind of file) a sub-service tab's
/// current settings will write - a choice on the Upload to vNAS card.
/// </summary>
/// <param name="Key">The file key Core names it by, e.g. <c>Airways_High_Lines</c> or <c>Airways.txt</c>.</param>
/// <param name="Group">The card row it sits in, e.g. <c>High</c>, <c>J</c> or <c>Alias file</c>.</param>
/// <param name="Label">Its checkbox label within that row, e.g. <c>Lines</c>.</param>
/// <param name="DisplayName">How the Preview Settings tab names it, e.g. <c>Airways_High_Lines</c>.</param>
/// <param name="IsGeojson">Whether it is GeoJSON, so can carry CRC-ERAM defaults.</param>
/// <param name="CrcRows">
/// The CRC defaults rows (class and kind) the file needs when it gets CRC-ERAM defaults; empty
/// for the alias file.
/// </param>
public sealed record OutputFileOption(
	string Key,
	string Group,
	string Label,
	string DisplayName,
	bool IsGeojson,
	IReadOnlyList<(string ClassName, EramFieldKind Kind)> CrcRows)
{
	/// <summary>The row the alias file sits in.</summary>
	public const string AliasGroup = "Alias file";

	/// <summary>A sub-service's alias file, e.g. <c>Airways.txt</c>.</summary>
	/// <param name="fileName">The alias file's name, which is also its key.</param>
	/// <returns>The option.</returns>
	public static OutputFileOption AliasFile(string fileName) => new(fileName, AliasGroup, fileName, fileName, IsGeojson: false, []);
}
