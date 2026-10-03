namespace FeBuddy.Core.Application.AliasGuide.Models;

/// <summary>
/// The alias command guide's content, which <c>AliasGuideHtmlWriter</c> and
/// <c>AliasGuideMarkdownWriter</c> each lay out in their own format.
/// </summary>
/// <remarks>
/// Every piece of text is inline text (see <c>GuideInline</c>): <c>`code`</c> and
/// <c>**bold**</c>, where a code span can hold command markup (see <c>CommandMarkup</c>), e.g.
/// <c>`.apt{a:DTW}`</c>.
/// </remarks>
/// <param name="Title">The guide's title.</param>
/// <param name="Lead">The line under the title.</param>
/// <param name="About">The opening section, about alias commands in general.</param>
/// <param name="ReadingNotes">
/// The notes every format adds to its own "How to read this guide" section, after explaining how
/// it shows the parts to type and the parts to replace.
/// </param>
/// <param name="Sections">The command sections, in order.</param>
internal sealed record AliasGuideDocument(
	string Title,
	string Lead,
	GuideSection About,
	IReadOnlyList<string> ReadingNotes,
	IReadOnlyList<GuideSection> Sections);

/// <summary>One section of the guide, with its own heading and link target.</summary>
/// <param name="Id">The section's anchor, e.g. <c>chart-recall</c>.</param>
/// <param name="Title">The section's heading.</param>
/// <param name="Intro">The line under the heading, or <see langword="null"/> for none.</param>
/// <param name="Blocks">The section's content, in order.</param>
internal sealed record GuideSection(string Id, string Title, string? Intro, IReadOnlyList<GuideBlock> Blocks);

/// <summary>One piece of a section's content.</summary>
internal abstract record GuideBlock;

/// <summary>A paragraph.</summary>
/// <param name="Text">The paragraph's inline text.</param>
internal sealed record GuideParagraph(string Text) : GuideBlock;

/// <summary>A heading inside a section.</summary>
/// <param name="Text">The heading's plain text.</param>
internal sealed record GuideHeading(string Text) : GuideBlock;

/// <summary>A bulleted list.</summary>
/// <param name="Items">Each item's inline text.</param>
internal sealed record GuideList(IReadOnlyList<string> Items) : GuideBlock;

/// <summary>A table of commands: syntax, description and examples, one row per command.</summary>
/// <param name="Commands">The rows.</param>
internal sealed record GuideCommandTable(IReadOnlyList<GuideCommand> Commands) : GuideBlock;

/// <summary>A plain table, such as the approach type codes.</summary>
/// <param name="Headers">The column headings.</param>
/// <param name="Rows">Each row's cells, as inline text, as many as there are headings.</param>
internal sealed record GuideTable(IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows) : GuideBlock;

/// <summary>One command: how it is written, what it does, and examples.</summary>
/// <param name="Syntax">The command's pattern, in command markup, e.g. <c>.apt[a:FAA or ICAO airport ID]</c>.</param>
/// <param name="Description">What the command does, as inline text.</param>
/// <param name="Notes">Short notes shown under the description, as inline text; often none.</param>
/// <param name="Examples">Real commands, in command markup, e.g. <c>.apt{a:DTW}</c>.</param>
internal sealed record GuideCommand(string Syntax, string Description, IReadOnlyList<string> Notes, IReadOnlyList<string> Examples);

/// <summary>What a part of a command stands for, which sets its colour in the web page.</summary>
internal enum CommandPartKind
{
	/// <summary>Text typed exactly as shown: <c>.apt</c>, the <c>c</c> of a chart recall.</summary>
	Typed,

	/// <summary>An airport's ID.</summary>
	Airport,

	/// <summary>Any other ID or name: a NAVAID, an airway, a procedure, an operator, a chart.</summary>
	Identifier,

	/// <summary>An instrument approach's type code, e.g. <c>I</c> for an ILS.</summary>
	ApproachType,

	/// <summary>An approach's variant letter (<c>Z</c>) or circling letter (<c>A</c>).</summary>
	Variant,

	/// <summary>A runway, e.g. <c>22L</c>.</summary>
	Runway,

	/// <summary>A chart's page number.</summary>
	Page,
}

/// <summary>One part of a command as the guide shows it.</summary>
/// <param name="Text">The part's text: what to type, a real value, or a description of what goes there.</param>
/// <param name="Kind">What the part stands for.</param>
/// <param name="IsPlaceholder"><see langword="true"/> when the controller replaces it with a real value.</param>
/// <param name="IsOptional"><see langword="true"/> when the part can be left out.</param>
internal sealed record CommandPart(string Text, CommandPartKind Kind, bool IsPlaceholder, bool IsOptional);

/// <summary>One run of inline text.</summary>
/// <param name="Text">The run's text, for plain and bold runs.</param>
/// <param name="Style">How the run is shown.</param>
/// <param name="Parts">The command parts, for a code run; empty otherwise.</param>
internal sealed record InlineRun(string Text, InlineStyle Style, IReadOnlyList<CommandPart> Parts);

/// <summary>How a run of inline text is shown.</summary>
internal enum InlineStyle
{
	/// <summary>Plain text.</summary>
	Plain,

	/// <summary>Bold text.</summary>
	Bold,

	/// <summary>A command or code, in the mono font.</summary>
	Code,
}
