using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.Core.Application.AliasGuide;

/// <summary>
/// Reads the guide's inline text: <c>`code`</c> (which can hold command markup, see
/// <see cref="CommandMarkup"/>) and <c>**bold**</c>. Nothing else is special.
/// </summary>
internal static class GuideInline
{
	/// <summary>Splits inline text into its runs.</summary>
	/// <param name="text">The text, e.g. <c>Type **either** `.apt{a:DTW}` or `.apt{a:KDTW}`.</c></param>
	/// <returns>The runs, in order, none of them empty.</returns>
	/// <exception cref="FormatException">A backtick or <c>**</c> is never closed, or a code span's command markup is malformed.</exception>
	internal static IReadOnlyList<InlineRun> Parse(string text)
	{
		ArgumentNullException.ThrowIfNull(text);

		List<InlineRun> runs = [];
		int plainStart = 0;
		int i = 0;

		void FlushPlain(int end)
		{
			if (end > plainStart)
			{
				runs.Add(new InlineRun(text[plainStart..end], InlineStyle.Plain, []));
			}
		}

		while (i < text.Length)
		{
			if (text[i] == '`')
			{
				int close = Close(text, i, "`");
				FlushPlain(i);
				string code = text[(i + 1)..close];
				runs.Add(new InlineRun(code, InlineStyle.Code, CommandMarkup.Parse(code)));
				i = plainStart = close + 1;
			}
			else if (string.CompareOrdinal(text, i, "**", 0, 2) == 0)
			{
				int close = Close(text, i, "**");
				FlushPlain(i);
				runs.Add(new InlineRun(text[(i + 2)..close], InlineStyle.Bold, []));
				i = plainStart = close + 2;
			}
			else
			{
				i++;
			}
		}

		FlushPlain(text.Length);
		return runs;
	}

	/// <summary>Where the marker opened at <paramref name="open"/> closes.</summary>
	private static int Close(string text, int open, string marker)
	{
		int close = text.IndexOf(marker, open + marker.Length, StringComparison.Ordinal);

		return close > open + marker.Length
			? close
			: throw new FormatException($"'{marker}' at {open} in '{text}' is never closed, or closes on nothing.");
	}
}
