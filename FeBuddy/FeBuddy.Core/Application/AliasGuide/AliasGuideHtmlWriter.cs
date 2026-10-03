using System.Net;
using System.Text;

using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.Core.Application.AliasGuide;

/// <summary>
/// Lays the alias command guide out as one self-contained web page: dark, light on CSS, no
/// scripts and nothing fetched from elsewhere, so a facility can drop it into its own website.
/// </summary>
/// <remarks>
/// <para>
/// Each command table has a Syntax, a Description and an Example column. A command never wraps:
/// the Syntax and Example columns are as wide as their longest line, a syntax breaks only where the
/// content does (<c>&lt;br&gt;</c>), and the description takes the rest of the width and wraps. A
/// part the controller replaces is a pill coloured by what it stands for
/// (<c>&lt;span class="part k-airport"&gt;</c>), with a dashed edge when it is optional; text typed
/// as shown is plain.
/// </para>
/// <para>
/// Every colour, font and size is a variable at the top of the style sheet, and the comment at the
/// top of the page says so, so the page is easy to restyle by hand. The page's source is indented
/// like an outline, a nested list included, so it is just as easy to edit. On a narrow screen a
/// command table's rows stack; a table still too wide for the screen scrolls sideways.
/// </para>
/// </remarks>
internal static class AliasGuideHtmlWriter
{
	private const string Styles = """
		/* =====================================================================
		   Make it yours: the colours, fonts and sizes are these variables.
		   ===================================================================== */
		:root {
		  --bg: #0b0f14;              /* page */
		  --surface: #0e1621;         /* section cards */
		  --surface-raised: #132030;  /* code and pills */
		  --border: #1e2c3b;
		  --text: #e6edf3;
		  --text-muted: #8b9dad;
		  --accent: #f4b740;          /* links */
		  --font: "Segoe UI", system-ui, -apple-system, Roboto, Helvetica, Arial, sans-serif;
		  --mono: "Cascadia Mono", Consolas, Menlo, monospace;
		  --radius: 12px;
		  --width: 1100px;

		  /* The parts of a command a controller replaces (see "How to read this guide"). */
		  --part-airport: #5ec8f2;
		  --part-ident: #f4b740;
		  --part-type: #4ade80;
		  --part-variant: #c084fc;
		  --part-runway: #fb7185;
		  --part-page: #94a3b8;
		}

		* { box-sizing: border-box; }
		body { margin: 0; background: var(--bg); color: var(--text); font: 16px/1.6 var(--font); }
		main { max-width: var(--width); margin: 0 auto; padding: 40px 20px 64px; }
		h1 { font-size: 2rem; line-height: 1.2; margin: 0; }
		h2 { font-size: 1.3rem; margin: 0; }
		h3 { font-size: 1.05rem; margin: 1.75rem 0 .5rem; }
		a { color: var(--accent); }
		.lead, .intro { color: var(--text-muted); margin: .35rem 0 0; }
		nav { display: flex; flex-wrap: wrap; gap: .5rem; margin: 1.5rem 0; }
		nav a { padding: .25rem .8rem; border: 1px solid var(--border); border-radius: 999px; font-size: .9rem; text-decoration: none; }
		section { background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius); padding: 1.25rem 1.5rem; margin: 1.25rem 0; }
		code { font-family: var(--mono); font-size: .92em; background: var(--surface-raised); border-radius: 5px; padding: .1em .35em; }
		code.cmd { background: none; padding: 0; white-space: nowrap; }

		/* A part to replace: a pill in its kind's colour. Dashed when optional. */
		.part { --c: var(--text); color: var(--c); border: 1px solid color-mix(in srgb, var(--c) 50%, transparent);
		        background: color-mix(in srgb, var(--c) 12%, transparent); border-radius: 6px; padding: 0 .3em; margin: 0 1px; white-space: nowrap; }
		.part.opt { border-style: dashed; }
		.k-airport { --c: var(--part-airport); }
		.k-ident { --c: var(--part-ident); }
		.k-type { --c: var(--part-type); }
		.k-variant { --c: var(--part-variant); }
		.k-runway { --c: var(--part-runway); }
		.k-page { --c: var(--part-page); }

		table { width: 100%; border-collapse: collapse; margin: .75rem 0 0; }
		th { text-align: left; font-size: .75rem; letter-spacing: .08em; text-transform: uppercase; color: var(--text-muted); }
		th, td { padding: .7rem .75rem; border-bottom: 1px solid var(--border); vertical-align: top; }
		tr:last-child td { border-bottom: 0; }
		td p { margin: 0; }
		td ul { margin: .35rem 0 0; padding-left: 1.25rem; }
		/* A command never wraps: Syntax and Example are as wide as their longest line, and the description has the rest. */
		.commands .syntax, .commands .examples { width: 1%; white-space: nowrap; }
		.examples code { display: block; margin-bottom: .35rem; }
		.note { display: inline-block; margin: .45rem .4rem 0 0; padding: .05rem .6rem; border: 1px solid var(--border);
		        border-radius: 999px; font-size: .8rem; color: var(--text-muted); }
		.table-wrap { overflow-x: auto; }
		.key { display: flex; flex-wrap: wrap; gap: .5rem 1.25rem; list-style: none; padding: 0; }
		.legend { list-style: none; padding: 0; }
		.legend li { margin: .4rem 0; }
		footer { color: var(--text-muted); font-size: .85rem; text-align: center; margin-top: 2rem; }

		/* Narrow screens: each command's syntax, description and examples stack. A command still
		   never wraps; one too wide for the screen scrolls sideways with its table. */
		@media (max-width: 720px) {
		  section { padding: 1rem; }
		  .commands thead { display: none; }
		  .commands, .commands tbody, .commands tr, .commands td { display: block; }
		  .commands td { border: 0; padding: .3rem 0; }
		  .commands .syntax, .commands .examples { width: auto; }
		  .commands tr { border-bottom: 1px solid var(--border); padding: .6rem 0; }
		}
		""";

	/// <summary>Writes the guide as a web page.</summary>
	/// <param name="guide">The guide's content.</param>
	/// <param name="options">The version and date the page names.</param>
	/// <returns>The page's HTML.</returns>
	internal static string Write(AliasGuideDocument guide, AliasGuideOptions options)
	{
		string credit = AliasGuideWriter.Credit(options);
		StringBuilder html = new();

		html.AppendLine("<!DOCTYPE html>");
		html.AppendLine("<html lang=\"en\">");
		html.AppendLine("<head>");
		html.AppendLine("<meta charset=\"utf-8\">");
		html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
		html.AppendLine($"<title>{Escape(guide.Title)}</title>");
		html.AppendLine("<!--");
		html.AppendLine($"  {Escape(guide.Title)}, made by FE-Buddy {Escape(credit)}.");
		html.AppendLine();
		html.AppendLine("  Make it yours:");
		html.AppendLine("  - Colours, fonts and sizes are the variables at the top of the <style> block.");
		html.AppendLine("  - Each part of a command to replace is a <span class=\"part k-...\"> pill, coloured by the");
		html.AppendLine("    --part-... variable for its kind.");
		html.AppendLine("  - Each section is a <section> with an id. Delete one your facility does not use, and its");
		html.AppendLine("    link in the <nav>.");
		html.AppendLine("  - To put the guide in a page of your own site, copy the <style> block and everything inside <main>.");
		html.AppendLine("-->");
		html.AppendLine("<style>");
		html.AppendLine(Styles);
		html.AppendLine("</style>");
		html.AppendLine("</head>");
		html.AppendLine("<body>");
		html.AppendLine("<main>");

		Line(html, 1, "<header>");
		Line(html, 2, $"<h1>{Escape(guide.Title)}</h1>");
		Line(html, 2, $"<p class=\"lead\">{Inline(guide.Lead)}</p>");
		Line(html, 1, "</header>");

		html.AppendLine();
		Line(html, 1, "<nav aria-label=\"Contents\">");
		Line(html, 2, $"<a href=\"#notation\">{Escape(AliasGuideContent.NotationTitle)}</a>");
		foreach (GuideSection section in guide.Sections)
		{
			Line(html, 2, $"<a href=\"#{section.Id}\">{Escape(section.Title)}</a>");
		}

		Line(html, 1, "</nav>");

		AppendNotation(html, guide.ReadingNotes);

		foreach (GuideSection section in guide.Sections)
		{
			AppendSection(html, section);
		}

		html.AppendLine();
		Line(html, 1, $"<footer>{Escape(AliasGuideWriter.Updated(options))}</footer>");
		html.AppendLine("</main>");
		html.AppendLine("</body>");
		html.AppendLine("</html>");

		return html.ToString();
	}

	/// <summary>One line of the page, indented two spaces a level so its source reads like an outline.</summary>
	private static void Line(StringBuilder html, int depth, string text) => html.Append(' ', depth * 2).AppendLine(text);

	private static void AppendSection(StringBuilder html, GuideSection section)
	{
		html.AppendLine();
		Line(html, 1, $"<section id=\"{section.Id}\">");
		Line(html, 2, $"<h2>{Escape(section.Title)}</h2>");

		if (section.Intro is { } intro)
		{
			Line(html, 2, $"<p class=\"intro\">{Inline(intro)}</p>");
		}

		foreach (GuideBlock block in section.Blocks)
		{
			switch (block)
			{
				case GuideParagraph paragraph:
					Line(html, 2, $"<p>{Inline(paragraph.Text)}</p>");
					break;

				case GuideHeading heading:
					Line(html, 2, $"<h3>{Escape(heading.Text)}</h3>");
					break;

				case GuideList list:
					AppendList(html, 2, list.Items);
					break;

				case GuideCommandTable commands:
					AppendCommands(html, commands);
					break;

				case GuideTable table:
					AppendTable(html, table);
					break;
			}
		}

		Line(html, 1, "</section>");
	}

	/// <summary>
	/// A bulleted list, each bullet's own bullets nested in it. A bullet with none is one line; one
	/// with some has its text on a line of its own, then its list.
	/// </summary>
	private static void AppendList(StringBuilder html, int depth, IReadOnlyList<GuideListItem> items)
	{
		Line(html, depth, "<ul>");

		foreach (GuideListItem item in items)
		{
			if (item.Items.Count == 0)
			{
				Line(html, depth + 1, $"<li>{Inline(item.Text)}</li>");
				continue;
			}

			Line(html, depth + 1, "<li>");
			Line(html, depth + 2, Inline(item.Text));
			AppendList(html, depth + 2, item.Items);
			Line(html, depth + 1, "</li>");
		}

		Line(html, depth, "</ul>");
	}

	/// <summary>The "How to read this guide" section: how the page shows each part, the colour key, then the shared notes.</summary>
	private static void AppendNotation(StringBuilder html, IReadOnlyList<GuideListItem> notes)
	{
		html.AppendLine();
		Line(html, 1, "<section id=\"notation\">");
		Line(html, 2, $"<h2>{Escape(AliasGuideContent.NotationTitle)}</h2>");
		Line(html, 2, "<ul class=\"legend\">");
		Line(html, 3, $"<li>{Escape(AliasGuideContent.TypedLegend)} \"<code class=\"cmd\">.apt</code>\"</li>");
		Line(html, 3, "<li>");
		Line(html, 4, $"<code class=\"cmd\"><span class=\"part k-ident\">Highlighted</span></code> {Escape(AliasGuideContent.PlaceholderLegend)}");
		Line(html, 4, "<ul class=\"key\">");
		foreach (CommandPartKind kind in AliasGuideContent.KeyKinds)
		{
			Line(html, 5, $"<li><code class=\"cmd\"><span class=\"part {KindClass(kind)}\">{Escape(AliasGuideContent.KindLabel(kind))}</span></code></li>");
		}

		Line(html, 4, "</ul>");
		Line(html, 3, "</li>");
		Line(html, 3, $"<li><code class=\"cmd\"><span class=\"part k-page opt\">Dashed</span></code> {Escape(AliasGuideContent.OptionalLegend)}</li>");
		Line(html, 2, "</ul>");
		AppendList(html, 2, notes);
		Line(html, 1, "</section>");
	}

	/// <summary>A command table: per command a row of its syntax, its description (its details and notes under it) and its examples.</summary>
	private static void AppendCommands(StringBuilder html, GuideCommandTable table)
	{
		Line(html, 2, "<div class=\"table-wrap\">");
		Line(html, 3, "<table class=\"commands\">");
		Line(html, 4, "<thead><tr><th>Syntax</th><th>Description</th><th>Example</th></tr></thead>");
		Line(html, 4, "<tbody>");

		foreach (GuideCommand command in table.Commands)
		{
			Line(html, 5, "<tr>");
			Line(html, 6, $"<td class=\"syntax\">{Command(command.Syntax)}</td>");
			Line(html, 6, "<td>");
			Line(html, 7, $"<p>{Inline(command.Description)}</p>");

			if (command.Details.Count > 0)
			{
				AppendList(html, 7, [.. command.Details.Select(detail => new GuideListItem(detail, []))]);
			}

			foreach (string note in command.Notes)
			{
				Line(html, 7, $"<span class=\"note\">{Inline(note)}</span>");
			}

			Line(html, 6, "</td>");
			Line(html, 6, "<td class=\"examples\">");
			foreach (string example in command.Examples)
			{
				Line(html, 7, Command([example]));
			}

			Line(html, 6, "</td>");
			Line(html, 5, "</tr>");
		}

		Line(html, 4, "</tbody>");
		Line(html, 3, "</table>");
		Line(html, 2, "</div>");
	}

	/// <summary>A plain table, a row to a line.</summary>
	private static void AppendTable(StringBuilder html, GuideTable table)
	{
		Line(html, 2, "<div class=\"table-wrap\">");
		Line(html, 3, "<table>");
		Line(html, 4, $"<thead><tr>{string.Concat(table.Headers.Select(header => $"<th>{Escape(header)}</th>"))}</tr></thead>");
		Line(html, 4, "<tbody>");

		foreach (IReadOnlyList<string> row in table.Rows)
		{
			Line(html, 5, $"<tr>{string.Concat(row.Select(cell => $"<td>{Inline(cell)}</td>"))}</tr>");
		}

		Line(html, 4, "</tbody>");
		Line(html, 3, "</table>");
		Line(html, 2, "</div>");
	}

	/// <summary>
	/// A command in a table cell, its lines (each in command markup) one under another, with no code
	/// background of its own.
	/// </summary>
	private static string Command(IReadOnlyList<string> lines) =>
		$"<code class=\"cmd\">{string.Join("<br>", lines.Select(line => Parts(CommandMarkup.Parse(line))))}</code>";

	/// <summary>
	/// Inline text: plain, <c>&lt;strong&gt;</c>, <c>&lt;code&gt;</c>, <c>&lt;a&gt;</c> and
	/// <c>&lt;br&gt;</c> runs. Code with a pill in it has no background of its own (the pills set it
	/// apart), so it never looks boxed twice.
	/// </summary>
	private static string Inline(string text)
	{
		StringBuilder html = new();

		foreach (InlineRun run in GuideInline.Parse(text))
		{
			html.Append(run.Style switch
			{
				InlineStyle.Bold => $"<strong>{Escape(run.Text)}</strong>",
				InlineStyle.Code when run.Parts.Any(part => part.Kind != CommandPartKind.Typed) => $"<code class=\"cmd\">{Parts(run.Parts)}</code>",
				InlineStyle.Code => $"<code>{Parts(run.Parts)}</code>",
				InlineStyle.Link => $"<a href=\"{Escape(run.Url!)}\">{Escape(run.Text)}</a>",
				InlineStyle.LineBreak => "<br>",
				_ => Escape(run.Text),
			});
		}

		return html.ToString();
	}

	/// <summary>
	/// A command's parts, side by side: typed text as it is, and every other part as a pill in its
	/// kind's colour. Nothing between them lets a line break there.
	/// </summary>
	private static string Parts(IReadOnlyList<CommandPart> parts)
	{
		StringBuilder html = new();

		foreach (CommandPart part in parts)
		{
			if (part.Kind == CommandPartKind.Typed)
			{
				html.Append(Escape(part.Text));
				continue;
			}

			string label = AliasGuideContent.KindLabel(part.Kind) + (part.IsOptional ? " (optional)" : string.Empty);
			string optional = part.IsOptional ? " opt" : string.Empty;

			html.Append($"<span class=\"part {KindClass(part.Kind)}{optional}\" title=\"{Escape(label)}\">{Escape(part.Text)}</span>");
		}

		return html.ToString();
	}

	private static string KindClass(CommandPartKind kind) => kind switch
	{
		CommandPartKind.Airport => "k-airport",
		CommandPartKind.ApproachType => "k-type",
		CommandPartKind.Variant => "k-variant",
		CommandPartKind.Runway => "k-runway",
		CommandPartKind.Page => "k-page",
		_ => "k-ident",
	};

	private static string Escape(string text) => WebUtility.HtmlEncode(text);
}
