using System.Net;
using System.Text.RegularExpressions;

using FeBuddy.Core.Infrastructure.Telephony.Models;

namespace FeBuddy.Core.Infrastructure.Telephony.Parsers;

/// <summary>
/// Reads the two FAA telephony pages (<see cref="TelephonyFiles"/>) into
/// <see cref="TelephonyHtmlDataModel"/> rows.
/// </summary>
/// <remarks>
/// <para>
/// The pages are generated from the FAA's publishing system and are plain and regular: each table
/// has a <c>thead</c> of headings and a <c>tbody</c> of rows, one <c>td</c> per column, the text
/// wrapped in a <c>p</c>. No merged cells, no nested tables. A general HTML parser is not needed:
/// tables, rows and cells are found with a few patterns, tags are stripped, and entities decoded.
/// </para>
/// <para>
/// Columns are found by their heading, not their position, so the order the FAA prints them in
/// does not matter. Heading text is compared after decoding, removing soft hyphens, turning the
/// Unicode hyphens the FAA uses (<c>3&#x2010;Ltr</c>) into <c>-</c> and ignoring case. A table
/// without any expected heading (page furniture) is skipped; a table with some but not all, a row
/// whose cell count differs from its table's headings, or a page with no matching table or no rows
/// at all throws <see cref="InvalidDataException"/> - the FAA has changed the layout, and a
/// silently half-read register would be worse than a clear failure.
/// </para>
/// </remarks>
public static partial class TelephonyHtmlParser
{
	/// <summary>The register's headings, in the order <see cref="TelephonyHtmlDataModel.Assignment"/> takes them.</summary>
	private static readonly string[] RegisterHeadings = ["COMPANY", "COUNTRY", "TELEPHONY", "3-LTR"];

	/// <summary>The U.S. special call signs' headings, in the order <see cref="TelephonyHtmlDataModel.SpecialCallSign"/> takes them.</summary>
	private static readonly string[] SpecialCallSignHeadings = ["TELEPHONY/CALL SIGN", "IDENTIFIER", "COMPANY OR OPERATING AGENCY", "EXPIRATION DATE"];

	private const string RegisterPageName = "The FAA telephony register (JO 7340.2, Chapter 3, Section 1)";
	private const string SpecialCallSignsPageName = "The FAA U.S. special call signs (JO 7340.2, Chapter 3, Section 4)";

	/// <summary>
	/// Parses both kept pages into one collection.
	/// </summary>
	/// <param name="registerPath">The Section 1 page (the register).</param>
	/// <param name="specialCallSignsPath">The Section 4 page, or <see langword="null"/> to leave the special call signs out.</param>
	/// <returns>The parsed rows.</returns>
	/// <exception cref="InvalidDataException">Thrown when a page's layout is not the one described above.</exception>
	public static TelephonyDataCollection Parse(string registerPath, string? specialCallSignsPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(registerPath);

		return new TelephonyDataCollection
		{
			Assignments = ParseRegister(registerPath),
			SpecialCallSigns = specialCallSignsPath is null ? [] : ParseSpecialCallSigns(specialCallSignsPath),
		};
	}

	/// <summary>Parses the Section 1 page (the register) from a file.</summary>
	/// <param name="path">The page's path.</param>
	/// <returns>Every row, in page order.</returns>
	/// <exception cref="InvalidDataException">Thrown when the page's layout is not the one described in the class remarks.</exception>
	public static List<TelephonyHtmlDataModel.Assignment> ParseRegister(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		return ParseRegisterHtml(File.ReadAllText(path));
	}

	/// <summary>Parses the Section 4 page (the U.S. special call signs) from a file.</summary>
	/// <param name="path">The page's path.</param>
	/// <returns>Every row, in page order.</returns>
	/// <exception cref="InvalidDataException">Thrown when the page's layout is not the one described in the class remarks.</exception>
	public static List<TelephonyHtmlDataModel.SpecialCallSign> ParseSpecialCallSigns(string path)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
		return ParseSpecialCallSignsHtml(File.ReadAllText(path));
	}

	/// <summary>Parses the Section 1 page's HTML.</summary>
	internal static List<TelephonyHtmlDataModel.Assignment> ParseRegisterHtml(string html) =>
		[.. ReadTables(html, RegisterHeadings, RegisterPageName).Select(cells => new TelephonyHtmlDataModel.Assignment
		{
			Company = cells[0],
			Country = cells[1],
			Telephony = cells[2],
			ThreeLetterDesignator = cells[3],
		})];

	/// <summary>Parses the Section 4 page's HTML.</summary>
	internal static List<TelephonyHtmlDataModel.SpecialCallSign> ParseSpecialCallSignsHtml(string html) =>
		[.. ReadTables(html, SpecialCallSignHeadings, SpecialCallSignsPageName).Select(cells => new TelephonyHtmlDataModel.SpecialCallSign
		{
			Telephony = cells[0],
			Identifier = cells[1],
			Agency = cells[2],
			ExpirationDate = cells[3],
		})];

	/// <summary>
	/// Reads every row of every table that has <paramref name="expectedHeadings"/>, each row's cells
	/// in the order of <paramref name="expectedHeadings"/>.
	/// </summary>
	private static List<string[]> ReadTables(string html, string[] expectedHeadings, string pageName)
	{
		ArgumentNullException.ThrowIfNull(html);

		List<string[]> rows = [];
		int matchingTables = 0;

		foreach (Match table in TableElement().Matches(html))
		{
			Match head = TableHead().Match(table.Value);

			if (!head.Success)
			{
				continue;
			}

			List<string> headings = [.. CellTexts(head.Value).Select(heading => heading.ToUpperInvariant())];
			int[] columns = [.. expectedHeadings.Select(heading => headings.IndexOf(heading))];

			// Page furniture, not one of the data tables.
			if (columns.All(column => column < 0))
			{
				continue;
			}

			if (columns.Any(column => column < 0))
			{
				throw new InvalidDataException(
					$"{pageName} has a table headed '{string.Join(" | ", headings)}' instead of '{string.Join(" | ", expectedHeadings)}'. " +
					"The FAA may have changed the page's layout.");
			}

			matchingTables++;

			foreach (Match row in TableRow().Matches(TableBody().Match(table.Value).Value))
			{
				List<string> cells = CellTexts(row.Value);

				if (cells.Count != headings.Count)
				{
					throw new InvalidDataException(
						$"{pageName} has a row of {cells.Count} cell(s) in a table of {headings.Count} column(s): '{string.Join(" | ", cells)}'. " +
						"The FAA may have changed the page's layout.");
				}

				rows.Add([.. columns.Select(column => cells[column])]);
			}
		}

		if (matchingTables == 0)
		{
			throw new InvalidDataException(
				$"{pageName} has no table headed '{string.Join(" | ", expectedHeadings)}'. The FAA may have changed the page's layout, " +
				"or the download is not the page.");
		}

		if (rows.Count == 0)
		{
			throw new InvalidDataException($"{pageName} has its tables but no rows in them.");
		}

		return rows;
	}

	/// <summary>The text of every <c>th</c> or <c>td</c> cell in <paramref name="html"/>, in order.</summary>
	private static List<string> CellTexts(string html) =>
		[.. Cell().Matches(html).Select(cell => CleanText(cell.Groups["content"].Value))];

	/// <summary>
	/// A cell's text: tags stripped, entities decoded, soft hyphens removed, the Unicode hyphens
	/// turned into <c>-</c>, non-breaking spaces into spaces, and whitespace collapsed and trimmed.
	/// </summary>
	/// <param name="html">The cell's inner HTML.</param>
	/// <returns>The plain text.</returns>
	internal static string CleanText(string html)
	{
		string text = WebUtility.HtmlDecode(Tag().Replace(html, " "));
		text = text.Replace("­", string.Empty, StringComparison.Ordinal).Replace(' ', ' ');
		text = UnicodeHyphen().Replace(text, "-");
		return Whitespace().Replace(text, " ").Trim();
	}

	[GeneratedRegex(@"<table\b[^>]*>.*?</table>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
	private static partial Regex TableElement();

	[GeneratedRegex(@"<thead\b[^>]*>.*?</thead>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
	private static partial Regex TableHead();

	[GeneratedRegex(@"<tbody\b[^>]*>.*?</tbody>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
	private static partial Regex TableBody();

	[GeneratedRegex(@"<tr\b[^>]*>.*?</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
	private static partial Regex TableRow();

	[GeneratedRegex(@"<t(?<kind>[hd])\b[^>]*>(?<content>.*?)</t\k<kind>>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
	private static partial Regex Cell();

	[GeneratedRegex(@"<[^>]*>")]
	private static partial Regex Tag();

	[GeneratedRegex(@"[‐-―−]")]
	private static partial Regex UnicodeHyphen();

	[GeneratedRegex(@"\s+")]
	private static partial Regex Whitespace();
}
