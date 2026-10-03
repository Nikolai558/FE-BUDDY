using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.Core.Application.AliasGuide;

/// <summary>
/// Reads the markup the guide's content writes a command in, so each part can be coloured (or, in
/// Markdown, bracketed) by what it stands for.
/// </summary>
/// <remarks>
/// <para>Outside brackets, text is typed exactly as shown. Inside:</para>
/// <list type="table">
///   <item><term><c>[a:airport ID]</c></term><description>a placeholder: the controller replaces it with a real value</description></item>
///   <item><term><c>[p?:page]</c></term><description>an optional placeholder</description></item>
///   <item><term><c>{a:DTW}</c></term><description>a real value, in an example</description></item>
/// </list>
/// <para>
/// The letter before the colon is the part's kind: <c>a</c> airport, <c>i</c> any other ID or
/// name, <c>t</c> approach type, <c>v</c> variant, <c>r</c> runway, <c>p</c> page. So
/// <c>.[a:airport ID][i:procedure]c</c> is a pattern and <c>.{a:dtw}{i:CLVIN}c</c> an example of it.
/// </para>
/// </remarks>
internal static class CommandMarkup
{
	/// <summary>Splits command markup into its parts.</summary>
	/// <param name="markup">The markup, e.g. <c>.apt[a:FAA or ICAO airport ID]</c>.</param>
	/// <returns>The parts, in order.</returns>
	/// <exception cref="FormatException">The markup has an unclosed bracket, an unknown kind or nothing inside a bracket.</exception>
	internal static IReadOnlyList<CommandPart> Parse(string markup)
	{
		ArgumentNullException.ThrowIfNull(markup);

		List<CommandPart> parts = [];
		int typedStart = 0;
		int i = 0;

		while (i < markup.Length)
		{
			char open = markup[i];

			if (open is not ('[' or '{'))
			{
				i++;
				continue;
			}

			char close = open == '[' ? ']' : '}';
			int end = markup.IndexOf(close, i + 1);

			if (end < 0)
			{
				throw new FormatException($"'{open}' at {i} in '{markup}' is never closed.");
			}

			if (i > typedStart)
			{
				parts.Add(new CommandPart(markup[typedStart..i], CommandPartKind.Typed, IsPlaceholder: false, IsOptional: false));
			}

			parts.Add(ParseBracket(markup, markup[(i + 1)..end], isPlaceholder: open == '['));
			i = end + 1;
			typedStart = i;
		}

		if (typedStart < markup.Length)
		{
			parts.Add(new CommandPart(markup[typedStart..], CommandPartKind.Typed, IsPlaceholder: false, IsOptional: false));
		}

		return parts;
	}

	/// <summary>The command as a controller types it: every part's text, joined.</summary>
	/// <param name="parts">The parts.</param>
	/// <returns>e.g. <c>.dtwI22Lc</c> for an example.</returns>
	internal static string Flatten(IEnumerable<CommandPart> parts) => string.Concat(parts.Select(part => part.Text));

	/// <summary>Reads one bracket's inside: <c>a:airport ID</c> or <c>p?:page</c>.</summary>
	private static CommandPart ParseBracket(string markup, string inside, bool isPlaceholder)
	{
		int colon = inside.IndexOf(':', StringComparison.Ordinal);
		string kindText = colon >= 0 ? inside[..colon] : string.Empty;
		bool optional = kindText.EndsWith('?');
		string text = colon >= 0 ? inside[(colon + 1)..] : string.Empty;

		if (text.Length == 0 || KindOf(optional ? kindText[..^1] : kindText) is not { } kind)
		{
			throw new FormatException($"'{inside}' in '{markup}' is not a kind letter, a colon and some text.");
		}

		return new CommandPart(text, kind, isPlaceholder, optional);
	}

	private static CommandPartKind? KindOf(string letter) => letter switch
	{
		"a" => CommandPartKind.Airport,
		"i" => CommandPartKind.Identifier,
		"t" => CommandPartKind.ApproachType,
		"v" => CommandPartKind.Variant,
		"r" => CommandPartKind.Runway,
		"p" => CommandPartKind.Page,
		_ => null,
	};
}
