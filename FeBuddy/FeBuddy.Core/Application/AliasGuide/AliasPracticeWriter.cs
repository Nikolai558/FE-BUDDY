using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;

using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.Core.Application.AliasGuide;

/// <summary>
/// Writes the FE-Buddy Alias Command Practice: one self-contained web page that quizzes a
/// controller on FE-Buddy's alias commands (Export Alias Command Practice, on Info ▸ Alias Command
/// Guide).
/// </summary>
/// <remarks>
/// <para>
/// Each question (<see cref="AliasPracticeContent"/>) names a chart, a procedure, an airway or an
/// ISR card and an action. The controller types the command; the page trims the spaces at either
/// end, ignores case as CRC does, and marks it right in green or wrong in red. A wrong command is
/// lined up with the closest right one, part by part, to say what looks missing or out of place -
/// "Looks like you forgot to include the full runway number 16." Either way it then shows the
/// right command, how each part of it is built, and what is worth knowing about it.
/// </para>
/// <para>
/// It looks like the guide (<see cref="AliasGuideHtmlWriter.Styles"/>, the same colour
/// variables and pills) and, like it, fetches nothing. Unlike the guide it needs a script: the
/// questions are JSON in a <c>&lt;script type="application/json"&gt;</c> block, and the script
/// that runs the page is <c>AliasPractice.js</c>, built into FE-Buddy.
/// </para>
/// </remarks>
public static class AliasPracticeWriter
{
	/// <summary>The file the practice is saved as. The user picks only the folder.</summary>
	public const string FileName = AliasPracticeContent.Title + ".html";

	/// <summary>The script's name inside FE-Buddy (see FeBuddy.Core.csproj).</summary>
	internal const string ScriptResource = "FeBuddy.Core.AliasPractice.js";

	private const string Styles = """

		/* =====================================================================
		   The practice page: the guide's look, plus the quiz.
		   ===================================================================== */
		:root {
		  --good: #4ade80;            /* a right answer */
		  --bad: #fb7185;             /* a wrong one */
		}

		.toolbar { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: .75rem; margin: 1.5rem 0 0; }
		.filters { display: flex; flex-wrap: wrap; gap: .5rem; }
		.chip { font: inherit; font-size: .9rem; color: var(--accent); background: none; border: 1px solid var(--border); border-radius: 999px; padding: .25rem .8rem; cursor: pointer; }
		.chip[aria-pressed="true"] { background: color-mix(in srgb, var(--accent) 15%, transparent); border-color: color-mix(in srgb, var(--accent) 60%, transparent); }
		.score { color: var(--text-muted); font-size: .9rem; margin: 0; }
		.progress { display: flex; justify-content: space-between; gap: 1rem; color: var(--text-muted); font-size: .85rem; margin: 0; }
		.action { margin: 1rem 0 0; font-size: .75rem; font-weight: 600; letter-spacing: .08em; text-transform: uppercase; color: var(--accent); }
		.name { font-size: 1.5rem; margin: .2rem 0 0; }
		.detail { color: var(--text-muted); margin: .2rem 0 0; }
		form { display: flex; gap: .5rem; margin: 1.25rem 0 0; }
		.answer { flex: 1; min-width: 0; font: 1.1rem var(--mono); color: var(--text); background: var(--bg); border: 1px solid var(--border); border-radius: 8px; padding: .55rem .75rem; }
		.answer:focus { outline: 2px solid color-mix(in srgb, var(--accent) 60%, transparent); outline-offset: 1px; }
		.button { font: inherit; font-weight: 600; color: var(--bg); background: var(--accent); border: 0; border-radius: 8px; padding: .55rem 1.1rem; cursor: pointer; }
		.button.secondary { color: var(--accent); background: none; border: 1px solid var(--border); }
		.result { margin: 1.25rem 0 0; }
		.verdict { margin: 0; padding: .6rem .9rem; border: 1px solid var(--border); border-radius: 8px; font-weight: 600; }
		.verdict.good { color: var(--good); border-color: color-mix(in srgb, var(--good) 50%, transparent); background: color-mix(in srgb, var(--good) 12%, transparent); }
		.verdict.bad { color: var(--bad); border-color: color-mix(in srgb, var(--bad) 50%, transparent); background: color-mix(in srgb, var(--bad) 12%, transparent); }
		.hints { margin: .75rem 0 0; padding-left: 1.25rem; }
		.hints li { margin: .25rem 0; }
		.correct { margin: .75rem 0 0; }
		.breakdown td:first-child { width: 1%; white-space: nowrap; }
		.next { margin-top: 1.25rem; }
		.actions { display: flex; flex-wrap: wrap; gap: .5rem; }
		""";

	/// <summary>Writes the practice page.</summary>
	/// <param name="options">The version and date the page names.</param>
	/// <returns>The page's HTML.</returns>
	public static string Write(AliasGuideOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		StringBuilder html = new();

		html.AppendLine("<!DOCTYPE html>");
		html.AppendLine("<html lang=\"en\">");
		html.AppendLine("<head>");
		html.AppendLine("<meta charset=\"utf-8\">");
		html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
		html.AppendLine($"<title>{Escape(AliasPracticeContent.Title)}</title>");
		html.AppendLine("<!--");
		html.AppendLine($"  {Escape(AliasPracticeContent.Title)}, made by FE-Buddy {Escape(AliasGuideWriter.Credit(options))}.");
		html.AppendLine();
		html.AppendLine("  A quiz on FE-Buddy's alias commands: each question names a chart, a procedure, an airway or an");
		html.AppendLine("  ISR card and what to do with it; type the command and press Enter.");
		html.AppendLine();
		html.AppendLine("  Make it yours: colours, fonts and sizes are the variables at the top of the <style> block.");
		html.AppendLine("  The questions are FE-Buddy's own, in the <script id=\"practice-data\"> block.");
		html.AppendLine("-->");
		html.AppendLine("<style>");
		html.AppendLine(AliasGuideHtmlWriter.Styles);
		html.AppendLine(Styles);
		html.AppendLine("</style>");
		html.AppendLine("</head>");
		html.AppendLine("<body>");
		html.AppendLine("<main>");

		Line(html, 1, "<header>");
		Line(html, 2, $"<h1>{Escape(AliasPracticeContent.Title)}</h1>");
		Line(html, 2, "<p class=\"lead\">Each question names a chart, a procedure, an airway or an ISR card, and what to do with it. "
			+ "Type the alias command and press Enter. Commands are not case-sensitive.</p>");
		Line(html, 1, "</header>");

		html.AppendLine();
		Line(html, 1, "<div class=\"toolbar\">");
		Line(html, 2, "<div class=\"filters\" role=\"group\" aria-label=\"Which questions\">");
		Line(html, 3, "<button type=\"button\" class=\"chip\" data-section=\"\" aria-pressed=\"true\">All</button>");
		foreach (PracticeAction action in Enum.GetValues<PracticeAction>())
		{
			string section = Escape(AliasPracticeContent.SectionTitle(action));
			Line(html, 3, $"<button type=\"button\" class=\"chip\" data-section=\"{section}\" aria-pressed=\"false\">{section}</button>");
		}

		Line(html, 2, "</div>");
		Line(html, 2, "<p class=\"score\" id=\"score\" aria-live=\"polite\"></p>");
		Line(html, 1, "</div>");

		html.AppendLine();
		Line(html, 1, "<section id=\"quiz\">");
		Line(html, 2, "<p class=\"progress\"><span id=\"progress\"></span><span id=\"section\"></span></p>");
		Line(html, 2, "<p class=\"action\" id=\"action\"></p>");
		Line(html, 2, "<h2 class=\"name\" id=\"name\"></h2>");
		Line(html, 2, "<p class=\"detail\" id=\"detail\"></p>");
		Line(html, 2, "<form id=\"form\" autocomplete=\"off\">");
		Line(html, 3, "<input id=\"answer\" class=\"answer\" type=\"text\" spellcheck=\"false\" autocapitalize=\"off\" autocorrect=\"off\" "
			+ "aria-label=\"Your command\" placeholder=\"Type the command, then press Enter\">");
		Line(html, 3, "<button type=\"submit\" class=\"button\">Check</button>");
		Line(html, 2, "</form>");
		Line(html, 2, "<div class=\"result\" id=\"result\" hidden>");
		Line(html, 3, "<p class=\"verdict\" id=\"verdict\" role=\"status\"></p>");
		Line(html, 3, "<div id=\"details\">");
		Line(html, 4, "<ul class=\"hints\" id=\"hints\"></ul>");
		Line(html, 4, "<p class=\"correct\" id=\"correct\"></p>");
		Line(html, 4, "<h3>How it&#39;s built</h3>");
		Line(html, 4, "<table class=\"breakdown\"><tbody id=\"breakdown\"></tbody></table>");
		Line(html, 4, "<div id=\"notes-box\">");
		Line(html, 5, "<h3>Good to know</h3>");
		Line(html, 5, "<ul id=\"notes\"></ul>");
		Line(html, 4, "</div>");
		Line(html, 4, "<button type=\"button\" class=\"button next\" id=\"next\">Next question</button>");
		Line(html, 3, "</div>");
		Line(html, 2, "</div>");
		Line(html, 1, "</section>");

		html.AppendLine();
		Line(html, 1, "<section id=\"done\" hidden>");
		Line(html, 2, "<h2 class=\"name\">All done</h2>");
		Line(html, 2, "<p id=\"final\"></p>");
		Line(html, 2, "<p class=\"actions\">");
		Line(html, 3, "<button type=\"button\" class=\"button\" id=\"missed\">Practice the ones I missed</button>");
		Line(html, 3, "<button type=\"button\" class=\"button secondary\" id=\"again\">Start over</button>");
		Line(html, 2, "</p>");
		Line(html, 1, "</section>");

		html.AppendLine();
		Line(html, 1, "<noscript><section><p>This practice page needs JavaScript turned on.</p></section></noscript>");
		Line(html, 1, $"<footer>{Escape(AliasGuideWriter.Updated(options))}</footer>");
		html.AppendLine("</main>");
		html.AppendLine("<script type=\"application/json\" id=\"practice-data\">");
		html.AppendLine(Data());
		html.AppendLine("</script>");
		html.AppendLine("<script>");
		html.Append(Script());
		html.AppendLine("</script>");
		html.AppendLine("</body>");
		html.AppendLine("</html>");

		return html.ToString();
	}

	/// <summary>
	/// Writes the practice page into a folder as <see cref="FileName"/>, UTF-8 without a byte order
	/// mark, replacing one already there.
	/// </summary>
	/// <param name="folder">The folder to write into.</param>
	/// <param name="options">The version and date the page names.</param>
	/// <returns>The file written.</returns>
	/// <exception cref="IOException">The file could not be written.</exception>
	/// <exception cref="UnauthorizedAccessException">The file or the folder is not writable.</exception>
	public static string Export(string folder, AliasGuideOptions options)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(folder);
		ArgumentNullException.ThrowIfNull(options);

		string path = Path.Combine(folder, FileName);
		File.WriteAllText(path, Write(options), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		return path;
	}

	/// <summary>
	/// The questions as the page's script reads them: each with its section, action, name and
	/// detail; every accepted command with its parts, its HTML and how each part is built; its
	/// notes; and what to say when its ID or name part is wrong. HTML-sensitive characters are
	/// escaped, so nothing in it can end the script block.
	/// </summary>
	/// <returns>The JSON.</returns>
	internal static string Data() => JsonSerializer.Serialize(new
	{
		typeNames = AliasPracticeContent.ApproachTypeNames,
		questions = AliasPracticeContent.Questions.Select(question => new
		{
			section = AliasPracticeContent.SectionTitle(question.Action),
			action = AliasPracticeContent.ActionLabel(question.Action),
			subject = JsonNamingPolicy.CamelCase.ConvertName(question.Subject.ToString()),
			name = question.Name,
			detail = question.Detail,
			answers = question.Answers.Select(answer => Answer(question, answer)),
			notes = question.Notes.Select(AliasGuideHtmlWriter.Inline),
			identifierHint = AliasPracticeContent.IdentifierHint(question) is { } hint ? AliasGuideHtmlWriter.Inline(hint) : null,
		}),
	});

	/// <summary>The script that runs the page, built into FE-Buddy from <c>AliasPractice.js</c>.</summary>
	/// <returns>The script's text.</returns>
	/// <exception cref="InvalidOperationException">The script is missing from FE-Buddy, which a build always includes.</exception>
	internal static string Script()
	{
		using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ScriptResource)
			?? throw new InvalidOperationException($"The practice page's script, {ScriptResource}, is missing from FE-Buddy.");
		using StreamReader reader = new(stream);
		return reader.ReadToEnd();
	}

	/// <summary>One accepted command: its parts, as the script lines them up; its HTML; and what each part is.</summary>
	private static object Answer(PracticeQuestion question, string markup)
	{
		IReadOnlyList<CommandPart> parts = CommandMarkup.Parse(markup);
		bool hasRunway = parts.Any(part => part.Kind == CommandPartKind.Runway);

		return new
		{
			parts = parts.Select(part => new { t = part.Text, k = JsonNamingPolicy.CamelCase.ConvertName(part.Kind.ToString()) }),
			html = AliasGuideHtmlWriter.Command([markup]),
			breakdown = parts.Select(part => new
			{
				html = part.Kind == CommandPartKind.Typed
					? $"<code>{Escape(part.Text)}</code>"
					: $"<code class=\"cmd\">{AliasGuideHtmlWriter.Parts([part])}</code>",
				text = AliasPracticeContent.PartMeaning(question, part, hasRunway),
			}),
		};
	}

	/// <summary>One line of the page, indented two spaces a level, as the guide's is.</summary>
	private static void Line(StringBuilder html, int depth, string text) => html.Append(' ', depth * 2).AppendLine(text);

	private static string Escape(string text) => WebUtility.HtmlEncode(text);
}
