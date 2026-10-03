using System.Text;

using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.Core.Application.AliasGuide;

/// <summary>
/// Lays the alias command guide out as GitHub-flavoured Markdown, for a wiki, a README or any
/// other site that takes Markdown.
/// </summary>
/// <remarks>
/// Markdown has no colour, so a part the controller replaces is written in the usual command-line
/// way instead: <c>&lt;airport ID&gt;</c>, or <c>[page]</c> when it is optional. Command tables are
/// Markdown tables, with a syntax's lines and a cell's notes and examples on lines of their own
/// (<c>&lt;br&gt;</c>). How wide a column is, and whether it wraps, is up to whatever shows the file.
/// </remarks>
internal static class AliasGuideMarkdownWriter
{
	/// <summary>Writes the guide as Markdown.</summary>
	/// <param name="guide">The guide's content.</param>
	/// <param name="options">The date the guide names.</param>
	/// <returns>The Markdown text.</returns>
	internal static string Write(AliasGuideDocument guide, AliasGuideOptions options)
	{
		StringBuilder markdown = new();

		markdown.AppendLine($"# {guide.Title}");
		markdown.AppendLine();
		markdown.AppendLine(Inline(guide.Lead));
		markdown.AppendLine();

		IEnumerable<string> titles = [AliasGuideContent.NotationTitle, .. guide.Sections.Select(section => section.Title)];
		markdown.AppendLine("**Contents:** " + string.Join(" · ", titles.Select(title => $"[{title}](#{Slug(title)})")));

		AppendNotation(markdown, guide.ReadingNotes);

		foreach (GuideSection section in guide.Sections)
		{
			AppendSection(markdown, section);
		}

		markdown.AppendLine();
		markdown.AppendLine("---");
		markdown.AppendLine();
		markdown.AppendLine($"*{AliasGuideWriter.Updated(options)}*");

		return markdown.ToString();
	}

	/// <summary>
	/// The anchor GitHub gives a heading: lower case, spaces as hyphens, and anything but letters,
	/// digits, hyphens and underscores left out. <c>In-Scope Reference (ISR)</c> is <c>in-scope-reference-isr</c>.
	/// </summary>
	/// <param name="heading">The heading's text.</param>
	/// <returns>The anchor, without its <c>#</c>.</returns>
	internal static string Slug(string heading) =>
		string.Concat(heading.ToLowerInvariant()
			.Where(c => char.IsAsciiLetterOrDigit(c) || c is ' ' or '-' or '_')
			.Select(c => c == ' ' ? '-' : c));

	private static void AppendSection(StringBuilder markdown, GuideSection section)
	{
		markdown.AppendLine();
		markdown.AppendLine($"## {section.Title}");

		if (section.Intro is { } intro)
		{
			markdown.AppendLine();
			markdown.AppendLine(Inline(intro));
		}

		foreach (GuideBlock block in section.Blocks)
		{
			markdown.AppendLine();

			switch (block)
			{
				case GuideParagraph paragraph:
					markdown.AppendLine(Inline(paragraph.Text));
					break;

				case GuideHeading heading:
					markdown.AppendLine($"### {heading.Text}");
					break;

				case GuideList list:
					foreach (string item in list.Items)
					{
						markdown.AppendLine($"- {Inline(item)}");
					}

					break;

				case GuideCommandTable commands:
					AppendCommands(markdown, commands);
					break;

				case GuideTable table:
					AppendRow(markdown, table.Headers);
					AppendRow(markdown, [.. table.Headers.Select(_ => "---")]);
					foreach (IReadOnlyList<string> row in table.Rows)
					{
						AppendRow(markdown, [.. row.Select(Cell)]);
					}

					break;
			}
		}
	}

	/// <summary>The "How to read this guide" section: how Markdown shows each part, then the shared notes.</summary>
	private static void AppendNotation(StringBuilder markdown, IReadOnlyList<string> notes)
	{
		markdown.AppendLine();
		markdown.AppendLine($"## {AliasGuideContent.NotationTitle}");
		markdown.AppendLine();
		markdown.AppendLine($"- {AliasGuideContent.TypedLegend} \"`.apt`\"");
		markdown.AppendLine("- `<airport ID>` Angle brackets: replace them, and what is inside them, with the real value.");
		markdown.AppendLine("- `[page]` Square brackets: optional. Replace them in the same way, or leave them out.");

		foreach (string note in notes)
		{
			markdown.AppendLine($"- {Inline(note)}");
		}
	}

	private static void AppendCommands(StringBuilder markdown, GuideCommandTable table)
	{
		AppendRow(markdown, ["Syntax", "Description", "Example"]);
		AppendRow(markdown, ["---", "---", "---"]);

		foreach (GuideCommand command in table.Commands)
		{
			IEnumerable<string> description = [Cell(command.Description), .. command.Notes.Select(note => $"*{Cell(note)}*")];

			AppendRow(markdown,
			[
				string.Join("<br>", command.Syntax.Select(line => Code(CommandMarkup.Parse(line)))),
				string.Join("<br>", description),
				string.Join("<br>", command.Examples.Select(example => Code(CommandMarkup.Parse(example)))),
			]);
		}
	}

	private static void AppendRow(StringBuilder markdown, IReadOnlyList<string> cells) =>
		markdown.AppendLine($"| {string.Join(" | ", cells)} |");

	/// <summary>Inline text for a table cell, where a <c>|</c> would end the cell.</summary>
	private static string Cell(string text) => Inline(text).Replace("|", "\\|", StringComparison.Ordinal);

	/// <summary>Inline text as Markdown; a line break is a <c>&lt;br&gt;</c>, which a table cell can hold too.</summary>
	private static string Inline(string text) => string.Concat(GuideInline.Parse(text).Select(run => run.Style switch
	{
		InlineStyle.Bold => $"**{run.Text}**",
		InlineStyle.Code => Code(run.Parts),
		InlineStyle.Link => $"[{run.Text}]({run.Url})",
		InlineStyle.LineBreak => "<br>",
		_ => run.Text,
	}));

	/// <summary>A command as a code span: <c>&lt;placeholder&gt;</c>, <c>[optional]</c>, and real values as they are typed.</summary>
	private static string Code(IReadOnlyList<CommandPart> parts) =>
		"`" + string.Concat(parts.Select(part => part switch
		{
			{ IsPlaceholder: true, IsOptional: true } => $"[{part.Text}]",
			{ IsPlaceholder: true } => $"<{part.Text}>",
			_ => part.Text,
		})) + "`";
}
