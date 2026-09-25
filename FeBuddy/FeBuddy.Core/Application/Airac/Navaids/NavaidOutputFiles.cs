using System.Text.RegularExpressions;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Navaids;

namespace FeBuddy.Core.Application.Airac.Navaids;

/// <summary>
/// The files the NAVAIDs sub-service writes, by file key - the name the <c>UploadToVnas</c> and
/// <c>CrcDefaultsFor</c> settings use (see <see cref="VnasFileChoices"/>).
/// </summary>
/// <remarks>
/// A GeoJSON file's key is its name without <c>.geojson</c>; the alias file's is its name.
/// <para>
/// <see cref="NavaidOutputBy.All"/> writes at most <see cref="Symbols"/> and <see cref="Text"/>.
/// <see cref="NavaidOutputBy.Type"/> instead writes a pair per NAVAID type present, keyed by
/// <see cref="TypeKey"/>: <c>NAVAIDs_&lt;Token&gt;s_Symbols</c> /
/// <c>NAVAIDs_&lt;Token&gt;s_Text</c>, e.g. <c>NAVAIDs_VORTACs_Symbols.geojson</c> for VORTAC or
/// <c>NAVAIDs_VOR-DMEs_Text.geojson</c> for VOR/DME (see <see cref="NavaidTypes.Token"/>).
/// </para>
/// </remarks>
public static partial class NavaidOutputFiles
{
	/// <summary>The CRC-defaults class name for <see cref="NavaidOutputBy.All"/>'s merged Symbols and Text files.</summary>
	public const string AllClass = "NAVAIDs";

	/// <summary><c>NAVAIDs_Symbols.geojson</c>: a symbol per included NAVAID, written in <see cref="NavaidOutputBy.All"/> mode.</summary>
	public const string Symbols = "NAVAIDs_Symbols";

	/// <summary><c>NAVAIDs_Text.geojson</c>: a label per included NAVAID, written in <see cref="NavaidOutputBy.All"/> mode.</summary>
	public const string Text = "NAVAIDs_Text";

	/// <summary>The alias file.</summary>
	public const string Alias = "NAVAIDs.txt";

	/// <summary>
	/// The file key for one NAVAID type's Symbols or Text file, written in
	/// <see cref="NavaidOutputBy.Type"/> mode - e.g. <c>NAVAIDs_VORTACs_Symbols</c> for VORTAC
	/// Symbols, or <c>NAVAIDs_VOR-DMEs_Text</c> for VOR/DME Text.
	/// </summary>
	/// <param name="navType">The NASR <c>NAV_TYPE</c> value.</param>
	/// <param name="kind">The kind of feature the file holds (<see cref="CrcFeatureKind.Symbol"/> or <see cref="CrcFeatureKind.Text"/>).</param>
	/// <returns>The file key.</returns>
	public static string TypeKey(string navType, CrcFeatureKind kind) =>
		$"NAVAIDs_{TypeGroup(navType)}_{AiracOutputPaths.FileKindSuffix(kind)}";

	/// <summary>A NAVAID type's group name in its file keys and CRC class name, e.g. <c>VORTACs</c> or <c>VOR-DMEs</c>.</summary>
	/// <param name="navType">The NASR <c>NAV_TYPE</c> value.</param>
	/// <returns>The group name: <see cref="NavaidTypes.Token"/> plus a trailing <c>s</c>.</returns>
	public static string TypeGroup(string navType) => $"{NavaidTypes.Token(navType)}s";

	/// <summary>Whether a key names one of the NAVAIDs GeoJSON files, ignoring case.</summary>
	/// <param name="key">The file key.</param>
	/// <returns>
	/// <see langword="true"/> for <see cref="Symbols"/>, <see cref="Text"/>, or a per-type key
	/// shaped like <c>NAVAIDs_&lt;Token&gt;s_Symbols</c> / <c>NAVAIDs_&lt;Token&gt;s_Text</c>.
	/// </returns>
	public static bool IsGeojsonKey(string key) =>
		key.Equals(Symbols, StringComparison.OrdinalIgnoreCase)
		|| key.Equals(Text, StringComparison.OrdinalIgnoreCase)
		|| TypeKeyPattern().IsMatch(key);

	/// <summary>
	/// Reads a per-type key back into its type's token and kind of file, e.g.
	/// <c>NAVAIDs_VOR-DMEs_Text</c> is <c>VOR-DME</c>, Text.
	/// </summary>
	/// <param name="key">The file key.</param>
	/// <param name="token">The type's token (<see cref="NavaidTypes.Token"/>), upper-cased; empty when the key is not a per-type key.</param>
	/// <param name="kind">The kind of file: <see cref="CrcFeatureKind.Symbol"/> or <see cref="CrcFeatureKind.Text"/>.</param>
	/// <returns><see langword="true"/> for a per-type key.</returns>
	public static bool TryParseTypeKey(string key, out string token, out CrcFeatureKind kind)
	{
		Match match = TypeKeyPattern().Match(key);

		if (!match.Success)
		{
			token = string.Empty;
			kind = default;
			return false;
		}

		token = match.Groups["token"].Value.ToUpperInvariant();
		kind = match.Groups["kind"].Value.Equals("Symbols", StringComparison.OrdinalIgnoreCase)
			? CrcFeatureKind.Symbol
			: CrcFeatureKind.Text;
		return true;
	}

	[GeneratedRegex(@"^NAVAIDs_(?<token>[A-Za-z0-9-]+)s_(?<kind>Symbols|Text)$", RegexOptions.IgnoreCase)]
	private static partial Regex TypeKeyPattern();
}
