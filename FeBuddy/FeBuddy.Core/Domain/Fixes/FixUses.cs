namespace FeBuddy.Core.Domain.Fixes;

/// <summary>
/// The NASR <c>FIX_USE_CODE</c> vocabulary (<c>FIX_BASE.FIX_USE_CODE</c>) and the rules FE-Buddy
/// applies to it: the mapped display name and the file-naming/CRC-class token.
/// </summary>
/// <remarks>
/// A fix use code FE-Buddy does not recognize (NASR occasionally adds one) is not rejected: its
/// raw code becomes its <see cref="Name"/>, sanitized into a usable name - see
/// <see cref="IsKnown"/>.
/// </remarks>
public static class FixUses
{
	private static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["CN"] = "COMPUTER-NAV",
		["MR"] = "MIL-RPRTNG-PNT",
		["MW"] = "MIL-WYPNT",
		["NRS"] = "NRS-WYPNT",
		["RADAR"] = "RADAR",
		["RP"] = "RPRTNG-PNT",
		["VFR"] = "VFR-WYPNT",
		["WP"] = "WYPNT",
	};

	/// <summary>
	/// Every fix use name FE-Buddy recognizes, in the display order the GUI offers them (e.g. for
	/// <c>ExcludedFixUses</c>).
	/// </summary>
	public static IReadOnlyList<string> All { get; } =
	[
		"COMPUTER-NAV", "MIL-RPRTNG-PNT", "MIL-WYPNT", "NRS-WYPNT", "RADAR", "RPRTNG-PNT", "VFR-WYPNT", "WYPNT",
	];

	private static readonly IReadOnlySet<string> KnownNames = new HashSet<string>(All, StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Maps a NASR <c>FIX_USE_CODE</c> value to its display name, e.g. <c>WP</c> to <c>WYPNT</c>.
	/// </summary>
	/// <param name="code">The NASR <c>FIX_USE_CODE</c> value.</param>
	/// <returns>
	/// The mapped name for a known code. For any other code, the code itself with every character
	/// in <see cref="Path.GetInvalidFileNameChars"/> replaced by a space, runs of whitespace
	/// collapsed to one space, and the result trimmed; when that leaves nothing (a blank code),
	/// <c>UNKNOWN</c>.
	/// </returns>
	public static string Name(string? code)
	{
		string trimmed = code?.Trim() ?? string.Empty;

		if (Names.TryGetValue(trimmed, out string? name))
		{
			return name;
		}

		char[] invalidChars = Path.GetInvalidFileNameChars();
		char[] chars = trimmed.ToCharArray();

		for (int i = 0; i < chars.Length; i++)
		{
			if (Array.IndexOf(invalidChars, chars[i]) >= 0)
			{
				chars[i] = ' ';
			}
		}

		string[] words = new string(chars).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
		return words.Length == 0 ? "UNKNOWN" : string.Join(' ', words);
	}

	/// <summary>Whether <paramref name="name"/> is one of the names in <see cref="All"/>, ignoring case.</summary>
	/// <param name="name">The fix use name.</param>
	/// <returns><see langword="true"/> when FE-Buddy recognizes the name.</returns>
	public static bool IsKnown(string name) => KnownNames.Contains(name);

	/// <summary>
	/// The file-name/CRC-class token for a fix use name. Known names (see <see cref="All"/>) are
	/// already tokens.
	/// </summary>
	/// <param name="name">The fix use name.</param>
	/// <returns>The token.</returns>
	public static string Token(string name) => FixTokens.Token(name);
}
