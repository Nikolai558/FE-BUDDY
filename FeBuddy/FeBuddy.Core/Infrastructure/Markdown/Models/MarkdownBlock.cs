namespace FeBuddy.Core.Infrastructure.Markdown.Models;

/// <summary>
/// One block of a parsed Markdown document (see <see cref="MarkdownParser"/>).
/// The UI walks these to build its own visuals, so nothing here knows about WPF.
/// </summary>
public abstract record MarkdownBlock;

/// <summary>An ATX heading (<c># </c> through <c>###### </c>).</summary>
/// <param name="Level">1 through 6.</param>
/// <param name="Spans">The heading text.</param>
public sealed record MarkdownHeading(int Level, IReadOnlyList<MarkdownSpan> Spans) : MarkdownBlock;

/// <summary>A paragraph. Soft line breaks are joined with a space; hard breaks are kept as <c>\n</c>.</summary>
/// <param name="Spans">The paragraph text.</param>
public sealed record MarkdownParagraph(IReadOnlyList<MarkdownSpan> Spans) : MarkdownBlock;

/// <summary>A bullet or numbered list.</summary>
/// <param name="Ordered"><see langword="true"/> for a numbered list.</param>
/// <param name="Start">The first item's number (numbered lists only; 1 for bullets).</param>
/// <param name="Items">The list items, in order.</param>
public sealed record MarkdownList(bool Ordered, int Start, IReadOnlyList<MarkdownListItem> Items) : MarkdownBlock;

/// <summary>One list item. Its blocks are usually a paragraph, optionally followed by a nested list.</summary>
/// <param name="Blocks">The item's content.</param>
public sealed record MarkdownListItem(IReadOnlyList<MarkdownBlock> Blocks);

/// <summary>A fenced code block, verbatim.</summary>
/// <param name="Text">The code, without the fences.</param>
public sealed record MarkdownCodeBlock(string Text) : MarkdownBlock;

/// <summary>A block quote (<c>&gt; </c> lines).</summary>
/// <param name="Blocks">The quoted content.</param>
public sealed record MarkdownQuote(IReadOnlyList<MarkdownBlock> Blocks) : MarkdownBlock;

/// <summary>A horizontal rule (<c>---</c>, <c>***</c> or <c>___</c>).</summary>
public sealed record MarkdownRule : MarkdownBlock;

/// <summary>Inline emphasis applied to a <see cref="MarkdownSpan"/>.</summary>
[Flags]
public enum MarkdownStyle
{
	/// <summary>Plain text.</summary>
	None = 0,

	/// <summary><c>**bold**</c> or <c>__bold__</c>.</summary>
	Bold = 1,

	/// <summary><c>*italic*</c> or <c>_italic_</c>.</summary>
	Italic = 2,

	/// <summary><c>`code`</c>.</summary>
	Code = 4,

	/// <summary><c>~~struck~~</c>.</summary>
	Strikethrough = 8,
}

/// <summary>
/// A run of inline text with one style. A span with a <see cref="Url"/> is a link; the parser
/// only ever sets absolute <c>http</c>, <c>https</c> or <c>mailto</c> URLs.
/// </summary>
/// <param name="Text">The text. May contain <c>\n</c> for a hard line break.</param>
/// <param name="Style">The emphasis flags.</param>
/// <param name="Url">The link target, or <see langword="null"/> for plain text.</param>
public readonly record struct MarkdownSpan(string Text, MarkdownStyle Style = MarkdownStyle.None, string? Url = null);
