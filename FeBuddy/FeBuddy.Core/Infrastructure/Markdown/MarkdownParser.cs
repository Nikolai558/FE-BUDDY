using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using FeBuddy.Core.Infrastructure.Markdown.Models;

namespace FeBuddy.Core.Infrastructure.Markdown;

/// <summary>
/// A small, forgiving Markdown parser for the text FE-Buddy shows from GitHub - release notes in
/// the update window. It covers what those bodies actually use, not all of CommonMark:
/// <list type="bullet">
/// <item>ATX headings, paragraphs (soft breaks joined, hard breaks kept), horizontal rules,
/// fenced code, block quotes, and bullet / numbered lists nested by indentation.</item>
/// <item>Inline <c>**bold**</c>, <c>*italic*</c>, <c>~~strike~~</c>, <c>`code`</c>,
/// <c>[links](url)</c>, <c>&lt;autolinks&gt;</c>, bare <c>https://</c> URLs, backslash escapes,
/// and (optionally) GitHub <c>#123</c> issue references.</item>
/// <item>HTML comments are dropped, as GitHub does. Images become a link to the image.</item>
/// </list>
/// Anything it does not recognise comes through as plain text, so the worst case is the raw
/// Markdown the window used to show.
/// </summary>
public static partial class MarkdownParser
{
	/// <summary>Parses <paramref name="markdown"/> into blocks.</summary>
	/// <param name="markdown">The Markdown text. <see langword="null"/> or blank yields no blocks.</param>
	/// <param name="issueUrlBase">
	/// When set, a <c>#123</c> reference becomes a link to this base plus the number
	/// (e.g. <c>https://github.com/owner/repo/issues/</c>). When <see langword="null"/>, it stays plain text.
	/// </param>
	/// <returns>The parsed blocks, in document order.</returns>
	public static IReadOnlyList<MarkdownBlock> Parse(string? markdown, string? issueUrlBase = null)
	{
		if (string.IsNullOrWhiteSpace(markdown))
		{
			return [];
		}

		string[] lines = markdown
			.Replace("\r\n", "\n", StringComparison.Ordinal)
			.Replace('\r', '\n')
			.Replace("\t", "    ", StringComparison.Ordinal)
			.Split('\n');

		return ParseBlocks(lines, issueUrlBase);
	}

	/// <summary>Parses inline Markdown (one paragraph's worth) into styled spans.</summary>
	/// <param name="text">The inline text.</param>
	/// <param name="issueUrlBase">See <see cref="Parse"/>.</param>
	/// <returns>The spans, with neighbours of the same style and link merged.</returns>
	public static IReadOnlyList<MarkdownSpan> ParseInlines(string text, string? issueUrlBase = null)
	{
		ArgumentNullException.ThrowIfNull(text);

		var spans = new List<MarkdownSpan>();
		ParseInline(text, MarkdownStyle.None, null, issueUrlBase, spans);
		return Merge(spans);
	}

	// ------------------------------------------------------------------ blocks

	private static List<MarkdownBlock> ParseBlocks(IReadOnlyList<string> lines, string? issueUrlBase)
	{
		var blocks = new List<MarkdownBlock>();
		var paragraph = new List<string>();

		void FlushParagraph()
		{
			if (paragraph.Count > 0)
			{
				blocks.Add(new MarkdownParagraph(ParseInlines(JoinParagraph(paragraph), issueUrlBase)));
				paragraph.Clear();
			}
		}

		int i = 0;
		while (i < lines.Count)
		{
			string line = lines[i];

			if (IsBlank(line))
			{
				FlushParagraph();
				i++;
				continue;
			}

			if (IsCommentStart(line))
			{
				FlushParagraph();
				i = SkipComment(lines, i);
				continue;
			}

			Match fence = FencePattern().Match(line);
			if (fence.Success)
			{
				FlushParagraph();
				blocks.Add(ParseFence(lines, ref i, fence.Groups["fence"].Value));
				continue;
			}

			Match heading = HeadingPattern().Match(line);
			if (heading.Success)
			{
				FlushParagraph();
				string text = ClosingHashesPattern().Replace(heading.Groups["text"].Value, string.Empty).Trim();
				blocks.Add(new MarkdownHeading(heading.Groups["hashes"].Length, ParseInlines(text, issueUrlBase)));
				i++;
				continue;
			}

			if (RulePattern().IsMatch(line))
			{
				FlushParagraph();
				blocks.Add(new MarkdownRule());
				i++;
				continue;
			}

			if (QuotePattern().IsMatch(line))
			{
				FlushParagraph();
				blocks.Add(ParseQuote(lines, ref i, issueUrlBase));
				continue;
			}

			if (ListItemPattern().IsMatch(line))
			{
				FlushParagraph();
				blocks.Add(ParseList(lines, ref i, issueUrlBase));
				continue;
			}

			paragraph.Add(line);
			i++;
		}

		FlushParagraph();
		return blocks;
	}

	private static MarkdownCodeBlock ParseFence(IReadOnlyList<string> lines, ref int i, string fence)
	{
		int indent = Indent(lines[i]);
		var code = new List<string>();
		i++;

		while (i < lines.Count && !lines[i].TrimStart().StartsWith(fence, StringComparison.Ordinal))
		{
			// Content lines lose up to the opening fence's indentation, as CommonMark does.
			string line = lines[i];
			code.Add(line[Math.Min(indent, Indent(line))..]);
			i++;
		}

		// Step over the closing fence (an unclosed fence runs to the end of the document).
		i++;
		return new MarkdownCodeBlock(string.Join('\n', code));
	}

	private static MarkdownQuote ParseQuote(IReadOnlyList<string> lines, ref int i, string? issueUrlBase)
	{
		var inner = new List<string>();

		while (i < lines.Count && !IsBlank(lines[i]))
		{
			Match quote = QuotePattern().Match(lines[i]);
			if (quote.Success)
			{
				inner.Add(quote.Groups["text"].Value);
			}
			else if (IsBlockStart(lines[i]))
			{
				break;
			}
			else
			{
				// Lazy continuation: an unmarked line carries on the quoted paragraph.
				inner.Add(lines[i]);
			}

			i++;
		}

		return new MarkdownQuote(ParseBlocks(inner, issueUrlBase));
	}

	private static MarkdownList ParseList(IReadOnlyList<string> lines, ref int i, string? issueUrlBase)
	{
		Match first = ListItemPattern().Match(lines[i]);
		bool ordered = first.Groups["number"].Success;
		int start = ordered ? int.Parse(first.Groups["number"].Value, CultureInfo.InvariantCulture) : 1;
		var items = new List<MarkdownListItem>();

		while (i < lines.Count)
		{
			Match item = ListItemPattern().Match(lines[i]);
			if (!item.Success || item.Groups["number"].Success != ordered)
			{
				break;
			}

			// The item's text starts at its content column; anything indented at least that far
			// belongs to the item (continuation text or a nested list).
			int markerIndent = item.Groups["indent"].Length;
			int gap = item.Groups["gap"].Length;
			int contentColumn = markerIndent + item.Groups["marker"].Length + (gap is 0 or > 4 ? 1 : gap);

			var itemLines = new List<string> { item.Groups["text"].Value };
			i++;

			while (i < lines.Count)
			{
				string line = lines[i];
				int indent = Indent(line);

				if (IsBlank(line))
				{
					// A blank line only stays inside the item if indented content follows it.
					int next = NextNonBlank(lines, i);
					if (next < lines.Count && Indent(lines[next]) >= contentColumn)
					{
						itemLines.Add(string.Empty);
						i++;
						continue;
					}

					break;
				}

				if (indent >= contentColumn)
				{
					itemLines.Add(line[contentColumn..]);
				}
				else if (indent > markerIndent && ListItemPattern().IsMatch(line))
				{
					// A nested marker that falls short of the content column still nests.
					itemLines.Add(line[indent..]);
				}
				else if (IsBlockStart(line))
				{
					break;
				}
				else
				{
					// Lazy continuation of the item's paragraph.
					itemLines.Add(line.TrimStart());
				}

				i++;
			}

			items.Add(new MarkdownListItem(ParseBlocks(itemLines, issueUrlBase)));

			// Blank lines between items keep the list going; anything else after them ends it.
			int following = NextNonBlank(lines, i);
			if (following < lines.Count && following != i && IsSibling(lines[following], ordered))
			{
				i = following;
			}
		}

		return new MarkdownList(ordered, start, items);
	}

	private static bool IsSibling(string line, bool ordered)
	{
		Match item = ListItemPattern().Match(line);
		return item.Success && item.Groups["number"].Success == ordered;
	}

	private static int SkipComment(IReadOnlyList<string> lines, int i)
	{
		int from = lines[i].IndexOf("<!--", StringComparison.Ordinal) + 4;
		while (i < lines.Count)
		{
			if (lines[i].IndexOf("-->", from, StringComparison.Ordinal) >= 0)
			{
				return i + 1;
			}

			from = 0;
			i++;
		}

		return i;
	}

	private static string JoinParagraph(List<string> lines)
	{
		var text = new StringBuilder();
		for (int n = 0; n < lines.Count; n++)
		{
			string line = lines[n];
			string trimmed = line.Trim();

			if (n == lines.Count - 1)
			{
				text.Append(trimmed);
			}
			else if (line.EndsWith("  ", StringComparison.Ordinal))
			{
				text.Append(trimmed).Append('\n');
			}
			else if (trimmed.EndsWith('\\'))
			{
				text.Append(trimmed[..^1]).Append('\n');
			}
			else
			{
				text.Append(trimmed).Append(' ');
			}
		}

		return text.ToString();
	}

	private static bool IsBlockStart(string line)
		=> IsCommentStart(line)
			|| FencePattern().IsMatch(line)
			|| HeadingPattern().IsMatch(line)
			|| RulePattern().IsMatch(line)
			|| QuotePattern().IsMatch(line)
			|| ListItemPattern().IsMatch(line);

	private static bool IsCommentStart(string line) => line.TrimStart().StartsWith("<!--", StringComparison.Ordinal);

	private static bool IsBlank(string line) => string.IsNullOrWhiteSpace(line);

	private static int Indent(string line)
	{
		int n = 0;
		while (n < line.Length && line[n] == ' ')
		{
			n++;
		}

		return n;
	}

	private static int NextNonBlank(IReadOnlyList<string> lines, int i)
	{
		while (i < lines.Count && IsBlank(lines[i]))
		{
			i++;
		}

		return i;
	}

	// ----------------------------------------------------------------- inlines

	private static void ParseInline(string text, MarkdownStyle style, string? url, string? issueUrlBase, List<MarkdownSpan> spans)
	{
		var literal = new StringBuilder();

		void Flush()
		{
			if (literal.Length > 0)
			{
				spans.Add(new MarkdownSpan(literal.ToString(), style, url));
				literal.Clear();
			}
		}

		int i = 0;
		while (i < text.Length)
		{
			char c = text[i];

			// \* and friends: the next punctuation character is literal.
			if (c == '\\' && i + 1 < text.Length && char.IsAscii(text[i + 1]) && (char.IsPunctuation(text[i + 1]) || char.IsSymbol(text[i + 1])))
			{
				literal.Append(text[i + 1]);
				i += 2;
				continue;
			}

			if (c == '`')
			{
				int run = RunLength(text, i, '`');
				int close = FindCodeClose(text, i + run, run);
				if (close < 0)
				{
					literal.Append('`', run);
					i += run;
					continue;
				}

				string code = text[(i + run)..close];
				if (code.Length > 2 && code[0] == ' ' && code[^1] == ' ')
				{
					code = code[1..^1];
				}

				Flush();
				spans.Add(new MarkdownSpan(code, style | MarkdownStyle.Code, url));
				i = close + run;
				continue;
			}

			// [label](url) and ![alt](url). An image becomes a link to the image.
			bool image = c == '!' && i + 1 < text.Length && text[i + 1] == '[';
			if ((c == '[' || image) && TryParseLink(text, image ? i + 1 : i, out string label, out string href, out int end))
			{
				Flush();
				string? target = url ?? SafeUrl(href);
				if (label.Length == 0)
				{
					label = href;
				}

				if (image)
				{
					spans.Add(new MarkdownSpan(label, style, target));
				}
				else
				{
					ParseInline(label, style, target, issueUrlBase, spans);
				}

				i = end;
				continue;
			}

			if (url is null && c == '<')
			{
				Match auto = AutolinkPattern().Match(text, i);
				if (auto.Success && auto.Index == i)
				{
					Flush();
					string target = auto.Groups["url"].Value;
					spans.Add(new MarkdownSpan(target, style, target));
					i += auto.Length;
					continue;
				}
			}

			if (url is null && c == 'h' && IsWordStart(text, i))
			{
				Match bare = BareUrlPattern().Match(text, i);
				if (bare.Success && bare.Index == i)
				{
					string target = TrimUrl(bare.Value);
					Flush();
					spans.Add(new MarkdownSpan(target, style, target));
					i += target.Length;
					continue;
				}
			}

			if (url is null && issueUrlBase is not null && c == '#' && IsWordStart(text, i))
			{
				Match issue = IssuePattern().Match(text, i);
				if (issue.Success && issue.Index == i)
				{
					Flush();
					spans.Add(new MarkdownSpan(issue.Value, style, issueUrlBase + issue.Groups["number"].Value));
					i += issue.Length;
					continue;
				}
			}

			if (c is '*' or '_' or '~')
			{
				int run = RunLength(text, i, c);
				int width = c == '~' ? 2 : Math.Min(run, 3);
				bool opens = (c != '~' || run == 2)
					&& i + run < text.Length
					&& !char.IsWhiteSpace(text[i + run])
					&& (c != '_' || IsWordStart(text, i));

				int close = opens ? FindEmphasisClose(text, i + run, c, width) : -1;
				if (close > i + run)
				{
					MarkdownStyle added = c == '~'
						? MarkdownStyle.Strikethrough
						: width switch
						{
							1 => MarkdownStyle.Italic,
							2 => MarkdownStyle.Bold,
							_ => MarkdownStyle.Bold | MarkdownStyle.Italic,
						};

					Flush();
					ParseInline(text[(i + run)..close], style | added, url, issueUrlBase, spans);
					i = close + width;
					continue;
				}

				literal.Append(c, run);
				i += run;
				continue;
			}

			literal.Append(c);
			i++;
		}

		Flush();
	}

	private static bool TryParseLink(string text, int open, out string label, out string href, out int end)
	{
		label = href = string.Empty;
		end = -1;

		int closeLabel = FindMatching(text, open, '[', ']');
		if (closeLabel < 0 || closeLabel + 1 >= text.Length || text[closeLabel + 1] != '(')
		{
			return false;
		}

		int closeHref = FindMatching(text, closeLabel + 1, '(', ')');
		if (closeHref < 0)
		{
			return false;
		}

		// Drop an optional title: [x](url "title").
		string destination = text[(closeLabel + 2)..closeHref].Trim();
		int space = destination.IndexOf(' ', StringComparison.Ordinal);
		if (space >= 0)
		{
			destination = destination[..space];
		}

		label = text[(open + 1)..closeLabel];
		href = destination.TrimStart('<').TrimEnd('>');
		end = closeHref + 1;
		return true;
	}

	private static int FindMatching(string text, int open, char opener, char closer)
	{
		int depth = 0;
		for (int i = open; i < text.Length; i++)
		{
			char c = text[i];
			if (c == '\\')
			{
				i++;
			}
			else if (c == opener)
			{
				depth++;
			}
			else if (c == closer && --depth == 0)
			{
				return i;
			}
		}

		return -1;
	}

	private static int FindCodeClose(string text, int from, int run)
	{
		int i = from;
		while (i < text.Length)
		{
			if (text[i] == '`')
			{
				int length = RunLength(text, i, '`');
				if (length == run)
				{
					return i;
				}

				i += length;
			}
			else
			{
				i++;
			}
		}

		return -1;
	}

	private static int FindEmphasisClose(string text, int from, char marker, int width)
	{
		int i = from;
		while (i < text.Length)
		{
			char c = text[i];

			if (c == '\\')
			{
				i += 2;
				continue;
			}

			if (c == '`')
			{
				// Emphasis never closes inside a code span.
				int run = RunLength(text, i, '`');
				int close = FindCodeClose(text, i + run, run);
				i = close < 0 ? i + run : close + run;
				continue;
			}

			if (c == marker)
			{
				int run = RunLength(text, i, marker);
				bool closes = run == width
					&& !char.IsWhiteSpace(text[i - 1])
					&& (marker != '_' || i + run >= text.Length || !char.IsLetterOrDigit(text[i + run]));

				if (closes)
				{
					return i;
				}

				i += run;
				continue;
			}

			i++;
		}

		return -1;
	}

	private static int RunLength(string text, int i, char c)
	{
		int n = 0;
		while (i + n < text.Length && text[i + n] == c)
		{
			n++;
		}

		return n;
	}

	private static bool IsWordStart(string text, int i) => i == 0 || !char.IsLetterOrDigit(text[i - 1]);

	/// <summary>Strips trailing punctuation a sentence put after a bare URL (and an unmatched closing paren).</summary>
	private static string TrimUrl(string url)
	{
		while (url.Length > 0)
		{
			char last = url[^1];
			if (".,;:!?'\"*_~".Contains(last, StringComparison.Ordinal)
				|| (last == ')' && url.Count(ch => ch == ')') > url.Count(ch => ch == '(')))
			{
				url = url[..^1];
				continue;
			}

			break;
		}

		return url;
	}

	/// <summary>Only absolute web and mail links are clickable; anything else renders as plain text.</summary>
	private static string? SafeUrl(string href)
		=> Uri.TryCreate(href, UriKind.Absolute, out Uri? uri)
			&& (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeMailto)
				? href
				: null;

	private static List<MarkdownSpan> Merge(List<MarkdownSpan> spans)
	{
		var merged = new List<MarkdownSpan>(spans.Count);
		foreach (MarkdownSpan span in spans)
		{
			if (merged.Count > 0 && merged[^1].Style == span.Style && merged[^1].Url == span.Url)
			{
				merged[^1] = merged[^1] with { Text = merged[^1].Text + span.Text };
			}
			else
			{
				merged.Add(span);
			}
		}

		return merged;
	}

	// ---------------------------------------------------------------- patterns

	[GeneratedRegex(@"^ {0,3}(?<hashes>#{1,6})(?:[ ]+(?<text>.*))?$")]
	private static partial Regex HeadingPattern();

	[GeneratedRegex(@"(?:^|[ ]+)#+[ ]*$")]
	private static partial Regex ClosingHashesPattern();

	[GeneratedRegex(@"^ {0,3}(?:(?:-[ ]*){3,}|(?:\*[ ]*){3,}|(?:_[ ]*){3,})$")]
	private static partial Regex RulePattern();

	[GeneratedRegex(@"^ {0,3}(?<fence>`{3,}|~{3,})")]
	private static partial Regex FencePattern();

	[GeneratedRegex(@"^ {0,3}> ?(?<text>.*)$")]
	private static partial Regex QuotePattern();

	[GeneratedRegex(@"^(?<indent> *)(?<marker>[-*+]|(?<number>\d{1,9})[.)])(?:(?<gap>[ ]+)(?<text>.*))?$")]
	private static partial Regex ListItemPattern();

	[GeneratedRegex(@"<(?<url>(?:https?|mailto):[^<>\s]+)>")]
	private static partial Regex AutolinkPattern();

	[GeneratedRegex(@"https?://[^\s<>]+")]
	private static partial Regex BareUrlPattern();

	[GeneratedRegex(@"#(?<number>\d+)(?![\w])")]
	private static partial Regex IssuePattern();
}
