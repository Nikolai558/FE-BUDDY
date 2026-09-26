using System.Text.RegularExpressions;

using FeBuddy.Core.Application.Airac.Fixes.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.Core.Application.Airac.Fixes;

/// <summary>
/// The files the Fixes sub-service writes, by file key - the name the <c>UploadToVnas</c> and
/// <c>CrcDefaultsFor</c> settings use (see <see cref="VnasFileChoices"/>).
/// </summary>
/// <remarks>
/// A file's key is its name without <c>.geojson</c>. There is no alias file.
/// <para>
/// <see cref="FixOutputBy.All"/> writes at most <see cref="Symbols"/> and <see cref="Text"/>.
/// Every other mode instead writes a pair per group present, keyed by <see cref="GroupKey"/>:
/// <c>Fix_&lt;group&gt;_Symbols</c> / <c>Fix_&lt;group&gt;_Text</c> - the group being a fix use
/// token (<see cref="FixOutputBy.FixUse"/>), a chart token (<see cref="FixOutputBy.Chart"/>), or a
/// combination group (<see cref="FixOutputBy.ChartAndFixUse"/>, see <see cref="CombinationGroup"/>).
/// </para>
/// </remarks>
public static partial class FixOutputFiles
{
	/// <summary>The CRC-defaults class name for <see cref="FixOutputBy.All"/>'s merged Symbols and Text files.</summary>
	public const string AllClass = "Fix";

	/// <summary><c>Fix_Symbols.geojson</c>: a symbol per included fix, written in <see cref="FixOutputBy.All"/> mode.</summary>
	public const string Symbols = "Fix_Symbols";

	/// <summary><c>Fix_Text.geojson</c>: a label per included fix, written in <see cref="FixOutputBy.All"/> mode.</summary>
	public const string Text = "Fix_Text";

	/// <summary>
	/// A group's Symbols or Text file key, written in every mode but <see cref="FixOutputBy.All"/>
	/// - e.g. <c>Fix_WYPNT_Symbols</c> for fix use WYPNT, or <c>Fix_ENROUTE-LOW_Text</c> for chart
	/// ENROUTE LOW.
	/// </summary>
	/// <param name="group">The group's token - a fix use token, a chart token, or a combination group (see <see cref="CombinationGroup"/>).</param>
	/// <param name="kind">The kind of feature the file holds (<see cref="CrcFeatureKind.Symbol"/> or <see cref="CrcFeatureKind.Text"/>).</param>
	/// <returns>The file key.</returns>
	public static string GroupKey(string group, CrcFeatureKind kind) =>
		$"Fix_{group}_{AiracOutputPaths.FileKindSuffix(kind)}";

	/// <summary>
	/// A chart + fix use combination's group name in its file keys and CRC class name, e.g.
	/// <c>ENROUTE-LOW-WYPNT</c>.
	/// </summary>
	/// <param name="chartToken">The chart's token.</param>
	/// <param name="fixUseToken">The fix use's token.</param>
	/// <returns>The group name.</returns>
	public static string CombinationGroup(string chartToken, string fixUseToken) => $"{chartToken}-{fixUseToken}";

	/// <summary>Whether a key names one of the Fixes GeoJSON files, ignoring case.</summary>
	/// <param name="key">The file key.</param>
	/// <returns>
	/// <see langword="true"/> for <see cref="Symbols"/>, <see cref="Text"/>, or a per-group key
	/// shaped like <c>Fix_&lt;group&gt;_Symbols</c> / <c>Fix_&lt;group&gt;_Text</c>.
	/// </returns>
	public static bool IsGeojsonKey(string key) =>
		key.Equals(Symbols, StringComparison.OrdinalIgnoreCase)
		|| key.Equals(Text, StringComparison.OrdinalIgnoreCase)
		|| GroupKeyPattern().IsMatch(key);

	/// <summary>
	/// Reads a per-group key back into its group and kind of file, e.g.
	/// <c>Fix_ENROUTE-LOW-WYPNT_Text</c> is <c>ENROUTE-LOW-WYPNT</c>, Text.
	/// </summary>
	/// <param name="key">The file key.</param>
	/// <param name="group">The group, upper-cased; empty when the key is not a per-group key.</param>
	/// <param name="kind">The kind of file: <see cref="CrcFeatureKind.Symbol"/> or <see cref="CrcFeatureKind.Text"/>.</param>
	/// <returns><see langword="true"/> for a per-group key.</returns>
	public static bool TryParseGroupKey(string key, out string group, out CrcFeatureKind kind)
	{
		Match match = GroupKeyPattern().Match(key);

		if (!match.Success)
		{
			group = string.Empty;
			kind = default;
			return false;
		}

		group = match.Groups["group"].Value.ToUpperInvariant();
		kind = match.Groups["kind"].Value.Equals("Symbols", StringComparison.OrdinalIgnoreCase)
			? CrcFeatureKind.Symbol
			: CrcFeatureKind.Text;
		return true;
	}

	[GeneratedRegex(@"^Fix_(?<group>[A-Za-z0-9-]+)_(?<kind>Symbols|Text)$", RegexOptions.IgnoreCase)]
	private static partial Regex GroupKeyPattern();
}
