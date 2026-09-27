using System.Text.RegularExpressions;

namespace FeBuddy.Core.Domain.Procedures.ChartRecall;

/// <summary>
/// Text helpers the FAA Chart Recall codes share: reducing a name to the letters and digits a CRC
/// alias command may hold, and splitting a name into words.
/// </summary>
internal static partial class ChartRecallText
{
	/// <summary>
	/// Upper-cases <paramref name="text"/> and keeps only its ASCII letters and digits - all a CRC
	/// alias command name may hold (<c>\.\w+</c>) - e.g. <c>LA RIVER</c> becomes <c>LARIVER</c>.
	/// </summary>
	/// <param name="text">The text.</param>
	/// <returns>The letters and digits, upper case.</returns>
	internal static string LettersAndDigits(string text) => string.Concat(text.ToUpperInvariant().Where(char.IsAsciiLetterOrDigit));

	/// <summary>
	/// Splits <paramref name="text"/> into upper-case words of letters and digits. An apostrophe
	/// joins rather than splits (<c>O'HARE</c> is one word, <c>OHARE</c>); every other character
	/// that is not a letter or digit splits (<c>TRI-CITIES</c> is <c>TRI</c>, <c>CITIES</c>).
	/// </summary>
	/// <param name="text">The text.</param>
	/// <returns>The words, in order.</returns>
	internal static IReadOnlyList<string> Words(string text) =>
		[.. NonWordCharacters().Split(text.ToUpperInvariant().Replace("'", string.Empty, StringComparison.Ordinal))
			.Where(word => word.Length > 0)];

	/// <summary>Removes every bracketed part of <paramref name="text"/>, brackets included, e.g. <c>JALEX THREE (RNAV)</c> becomes <c>JALEX THREE </c>.</summary>
	/// <param name="text">The text.</param>
	/// <returns>The text without its bracketed parts.</returns>
	internal static string WithoutBrackets(string text) => BracketedPart().Replace(text, " ");

	[GeneratedRegex(@"[^A-Za-z0-9]+")]
	private static partial Regex NonWordCharacters();

	[GeneratedRegex(@"\([^)]*\)")]
	private static partial Regex BracketedPart();
}
