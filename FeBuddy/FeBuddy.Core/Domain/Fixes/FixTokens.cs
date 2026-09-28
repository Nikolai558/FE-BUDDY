using System.Text;

namespace FeBuddy.Core.Domain.Fixes;

/// <summary>
/// Turns an arbitrary NASR value into the token FE-Buddy uses in file names and as a CRC class
/// name.
/// </summary>
/// <remarks>
/// Shared by <see cref="FixUses"/> and <see cref="FixCharts"/>, whose tokens both feed file names
/// and CRC-ERAM class names (see <c>FixOutputFiles</c>).
/// </remarks>
public static class FixTokens
{
	/// <summary>
	/// Turns a value into its token: upper-cased, with every run of characters that are not ASCII
	/// letters or digits collapsed to a single <c>-</c>, and any leading or trailing <c>-</c>
	/// removed.
	/// </summary>
	/// <param name="value">The value to tokenize, e.g. <c>VFR TERMINAL AREA</c> or <c>a/b c</c>.</param>
	/// <returns>The token, e.g. <c>VFR-TERMINAL-AREA</c> or <c>A-B-C</c>.</returns>
	public static string Token(string value)
	{
		ArgumentNullException.ThrowIfNull(value);

		StringBuilder token = new();
		bool lastWasSeparator = true; // Suppresses a leading '-' from leading non-alphanumeric characters.

		foreach (char c in value.Trim())
		{
			if (char.IsAsciiLetterOrDigit(c))
			{
				token.Append(char.ToUpperInvariant(c));
				lastWasSeparator = false;
			}
			else if (!lastWasSeparator)
			{
				token.Append('-');
				lastWasSeparator = true;
			}
		}

		// A trailing separator (trailing non-alphanumeric characters) would otherwise leave a
		// dangling '-'.
		if (token.Length > 0 && token[^1] == '-')
		{
			token.Length--;
		}

		return token.ToString();
	}
}
