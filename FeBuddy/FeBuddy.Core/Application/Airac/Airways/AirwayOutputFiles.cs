using System.Text.RegularExpressions;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.Core.Application.Airac.Airways;

/// <summary>
/// The files the Airways sub-service writes, by file key - the name the <c>UploadToVnas</c> and
/// <c>CrcDefaultsFor</c> settings use (see <see cref="VnasFileChoices"/>).
/// </summary>
/// <remarks>
/// A GeoJSON file's key is its name without <c>.geojson</c>: <c>Airways_&lt;group&gt;_&lt;kind&gt;</c>,
/// where the group is an altitude class (<c>High</c>, <c>Low</c>, <c>Other</c>) or a designation
/// (<c>J</c>, <c>V</c>, ...) depending on <c>OutputBy</c>, and the kind is <c>Lines</c>,
/// <c>Symbols</c> or <c>Text</c>. The alias file's key is its name.
/// </remarks>
public static partial class AirwayOutputFiles
{
	/// <summary>The alias file.</summary>
	public const string Alias = "Airways.txt";

	/// <summary>A GeoJSON file's key, e.g. <c>Airways_High_Lines</c>.</summary>
	/// <param name="group">The altitude class or designation the file holds.</param>
	/// <param name="kind">The kind of feature the file holds.</param>
	/// <returns>The file key.</returns>
	public static string GeojsonKey(string group, CrcFeatureKind kind) =>
		$"Airways_{group}_{AiracOutputPaths.FileKindSuffix(kind)}";

	/// <summary>Whether a key has the shape of an Airways GeoJSON file key, ignoring case.</summary>
	/// <param name="key">The file key.</param>
	/// <returns><see langword="true"/> for e.g. <c>Airways_High_Lines</c> or <c>Airways_J_Text</c>.</returns>
	public static bool IsGeojsonKey(string key) => GeojsonKeyPattern().IsMatch(key);

	/// <summary>Whether a GeoJSON file key is for a file of the given kind, e.g. <c>Airways_J_Lines</c> for Lines.</summary>
	/// <param name="key">The file key.</param>
	/// <param name="kind">The kind.</param>
	/// <returns><see langword="true"/> when the key is a GeoJSON key of that kind.</returns>
	public static bool IsKind(string key, CrcFeatureKind kind) =>
		IsGeojsonKey(key) && key.EndsWith($"_{AiracOutputPaths.FileKindSuffix(kind)}", StringComparison.OrdinalIgnoreCase);

	// Designations are the ID's leading letters (AirwayClassifier.DeriveDesignation), so a group
	// is always letters only.
	[GeneratedRegex("^Airways_[A-Za-z]+_(Lines|Symbols|Text)$", RegexOptions.IgnoreCase)]
	private static partial Regex GeojsonKeyPattern();
}
