using System.Net;
using System.Text;

using FeBuddy.Core.Application.AliasGuide.Models;
using FeBuddy.Core.Infrastructure.GitHub;

namespace FeBuddy.Core.Application.AliasGuide;

/// <summary>
/// Lays the alias command guide out as one self-contained web page: dark, light on CSS, no
/// scripts and nothing fetched from elsewhere, so a facility can drop it into its own website.
/// </summary>
/// <remarks>
/// <para>
/// Each command table has a Syntax, a Description and an Example column. A part the controller
/// replaces is a pill coloured by what it stands for (<c>&lt;span class="part k-airport"&gt;</c>),
/// with a dashed edge when it is optional; text typed as shown is plain.
/// </para>
/// <para>
/// Every colour, font and size is a variable at the top of the style sheet, and the comment at the
/// top of the page says so, so the page is easy to restyle by hand. On a narrow screen a command
/// table's rows stack and a long command wraps between its parts; any other table scrolls sideways.
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
		.examples code { display: block; margin-bottom: .35rem; }
		.note { display: inline-block; margin: .45rem .4rem 0 0; padding: .05rem .6rem; border: 1px solid var(--border);
		        border-radius: 999px; font-size: .8rem; color: var(--text-muted); }
		.table-wrap { overflow-x: auto; }
		.key { display: flex; flex-wrap: wrap; gap: .5rem 1.25rem; list-style: none; padding: 0; }
		.legend { list-style: none; padding: 0; }
		.legend li { margin: .4rem 0; }
		footer { color: var(--text-muted); font-size: .85rem; text-align: center; margin-top: 2rem; }

		/* Narrow screens: each command's syntax, description and examples stack, and a long
		   command wraps between its parts. */
		@media (max-width: 720px) {
		  section { padding: 1rem; }
		  code.cmd { white-space: normal; }
		  .commands thead { display: none; }
		  .commands, .commands tbody, .commands tr, .commands td { display: block; }
		  .commands td { border: 0; padding: .3rem 0; }
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

		html.AppendLine("<header>");
		html.AppendLine($"<h1>{Escape(guide.Title)}</h1>");
		html.AppendLine($"<p class=\"lead\">{Inline(guide.Lead)}</p>");
		html.AppendLine("</header>");

		html.AppendLine("<nav aria-label=\"Contents\">");
		html.AppendLine($"<a href=\"#{guide.About.Id}\">{Escape(guide.About.Title)}</a>");
		html.AppendLine($"<a href=\"#notation\">{Escape(AliasGuideContent.NotationTitle)}</a>");
		foreach (GuideSection section in guide.Sections)
		{
			html.AppendLine($"<a href=\"#{section.Id}\">{Escape(section.Title)}</a>");
		}

		html.AppendLine("</nav>");

		AppendSection(html, guide.About);
		AppendNotation(html, guide.ReadingNotes);

		foreach (GuideSection section in guide.Sections)
		{
			AppendSection(html, section);
		}

		html.AppendLine($"<footer>Made with <a href=\"{GitHubRepository.WebUrl}\">FE-Buddy</a> {Escape(credit)}.</footer>");
		html.AppendLine("</main>");
		html.AppendLine("</body>");
		html.AppendLine("</html>");

		return html.ToString();
	}

	private static void AppendSection(StringBuilder html, GuideSection section)
	{
		html.AppendLine($"<section id=\"{section.Id}\">");
		html.AppendLine($"<h2>{Escape(section.Title)}</h2>");

		if (section.Intro is { } intro)
		{
			html.AppendLine($"<p class=\"intro\">{Inline(intro)}</p>");
		}

		foreach (GuideBlock block in section.Blocks)
		{
			switch (block)
			{
				case GuideParagraph paragraph:
					html.AppendLine($"<p>{Inline(paragraph.Text)}</p>");
					break;

				case GuideHeading heading:
					html.AppendLine($"<h3>{Escape(heading.Text)}</h3>");
					break;

				case GuideList list:
					html.AppendLine("<ul>");
					foreach (string item in list.Items)
					{
						html.AppendLine($"<li>{Inline(item)}</li>");
					}

					html.AppendLine("</ul>");
					break;

				case GuideCommandTable commands:
					AppendCommands(html, commands);
					break;

				case GuideTable table:
					AppendTable(html, table);
					break;
			}
		}

		html.AppendLine("</section>");
	}

	/// <summary>The "How to read this guide" section: how the page shows each part, the colour key, then the shared notes.</summary>
	private static void AppendNotation(StringBuilder html, IReadOnlyList<string> notes)
	{
		html.AppendLine("<section id=\"notation\">");
		html.AppendLine($"<h2>{Escape(AliasGuideContent.NotationTitle)}</h2>");
		html.AppendLine("<ul class=\"legend\">");
		html.AppendLine($"<li><code class=\"cmd\">.apt</code> {Escape(AliasGuideContent.TypedLegend)}</li>");
		html.AppendLine($"<li><code class=\"cmd\"><span class=\"part k-ident\">Highlighted</span></code> {Escape(AliasGuideContent.PlaceholderLegend)}");
		html.AppendLine("<ul class=\"key\">");
		foreach (CommandPartKind kind in AliasGuideContent.KeyKinds)
		{
			html.AppendLine($"<li><code class=\"cmd\"><span class=\"part {KindClass(kind)}\">{Escape(AliasGuideContent.KindLabel(kind))}</span></code></li>");
		}

		html.AppendLine("</ul></li>");
		html.AppendLine($"<li><code class=\"cmd\"><span class=\"part k-page opt\">Dashed</span></code> {Escape(AliasGuideContent.OptionalLegend)}</li>");
		html.AppendLine("</ul>");

		foreach (string note in notes)
		{
			html.AppendLine($"<p>{Inline(note)}</p>");
		}

		html.AppendLine("</section>");
	}

	private static void AppendCommands(StringBuilder html, GuideCommandTable table)
	{
		html.AppendLine("<div class=\"table-wrap\">");
		html.AppendLine("<table class=\"commands\">");
		html.AppendLine("<thead><tr><th>Syntax</th><th>Description</th><th>Example</th></tr></thead>");
		html.AppendLine("<tbody>");

		foreach (GuideCommand command in table.Commands)
		{
			html.AppendLine("<tr>");
			html.AppendLine($"<td>{Command(command.Syntax)}</td>");

			StringBuilder description = new($"<p>{Inline(command.Description)}</p>");
			foreach (string note in command.Notes)
			{
				description.Append($"<span class=\"note\">{Inline(note)}</span>");
			}

			html.AppendLine($"<td>{description}</td>");
			html.AppendLine($"<td class=\"examples\">{string.Concat(command.Examples.Select(Command))}</td>");
			html.AppendLine("</tr>");
		}

		html.AppendLine("</tbody>");
		html.AppendLine("</table>");
		html.AppendLine("</div>");
	}

	private static void AppendTable(StringBuilder html, GuideTable table)
	{
		html.AppendLine("<div class=\"table-wrap\">");
		html.AppendLine("<table>");
		html.AppendLine($"<thead><tr>{string.Concat(table.Headers.Select(header => $"<th>{Escape(header)}</th>"))}</tr></thead>");
		html.AppendLine("<tbody>");

		foreach (IReadOnlyList<string> row in table.Rows)
		{
			html.AppendLine($"<tr>{string.Concat(row.Select(cell => $"<td>{Inline(cell)}</td>"))}</tr>");
		}

		html.AppendLine("</tbody>");
		html.AppendLine("</table>");
		html.AppendLine("</div>");
	}

	/// <summary>A command in a table cell: its parts, with no code background of its own.</summary>
	private static string Command(string markup) => $"<code class=\"cmd\">{Parts(CommandMarkup.Parse(markup))}</code>";

	/// <summary>
	/// Inline text: plain, <c>&lt;strong&gt;</c> and <c>&lt;code&gt;</c> runs. Code with a pill in it
	/// has no background of its own (the pills set it apart), so it never looks boxed twice.
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
				_ => Escape(run.Text),
			});
		}

		return html.ToString();
	}

	/// <summary>
	/// A command's parts: typed text as it is, and every other part as a pill in its kind's colour.
	/// A <c>&lt;wbr&gt;</c> between parts lets a long command wrap on a narrow screen, where it is
	/// allowed to (pills sit side by side with no space for a line to break at).
	/// </summary>
	private static string Parts(IReadOnlyList<CommandPart> parts)
	{
		StringBuilder html = new();

		foreach (CommandPart part in parts)
		{
			if (html.Length > 0)
			{
				html.Append("<wbr>");
			}

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
