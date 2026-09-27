namespace FeBuddy.Core.Domain.Telephony;

/// <summary>
/// The Telephony alias file's command names: a lower-case <c>.id</c>, then an identifier or
/// telephony with only its letters and digits, upper case - <c>.idAVA</c>, <c>.idRYANAIR</c>.
/// </summary>
/// <remarks>
/// Letters and digits are all a CRC alias command name may hold (<c>\.\w+</c>), so spaces, hyphens
/// and apostrophes are dropped: <c>RYAN AIR</c> and <c>RYANAIR</c> both become <c>.idRYANAIR</c> -
/// which is why one command can end up showing more than one operator.
/// </remarks>
public static class TelephonyNaming
{
	/// <summary>What every Telephony command starts with.</summary>
	public const string CommandPrefix = ".id";

	/// <summary>
	/// The command for an identifier or telephony, or <see langword="null"/> when it has no letter or
	/// digit to make one from.
	/// </summary>
	/// <param name="text">The identifier or telephony, e.g. <c>AVA</c> or <c>RYAN AIR</c>.</param>
	/// <returns>e.g. <c>.idAVA</c>, <c>.idRYANAIR</c>.</returns>
	public static string? CommandName(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		string lettersAndDigits = string.Concat(text.ToUpperInvariant().Where(char.IsAsciiLetterOrDigit));
		return lettersAndDigits.Length > 0 ? CommandPrefix + lettersAndDigits : null;
	}

	/// <summary>
	/// Whether <paramref name="code"/> is an ICAO three-letter designator: exactly three letters,
	/// ignoring case and surrounding spaces. The register's placeholders for "none" (<c>...</c>,
	/// <c>--</c>) are not.
	/// </summary>
	/// <param name="code">The register's <c>3-Ltr</c> value.</param>
	/// <returns><see langword="true"/> for a designator.</returns>
	public static bool IsThreeLetterDesignator(string code)
	{
		ArgumentNullException.ThrowIfNull(code);

		string trimmed = code.Trim();
		return trimmed.Length == 3 && trimmed.All(char.IsAsciiLetter);
	}
}
