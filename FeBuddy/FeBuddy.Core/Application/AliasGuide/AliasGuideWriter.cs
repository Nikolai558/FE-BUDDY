using System.Globalization;
using System.Text;

using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.Core.Application.AliasGuide;

/// <summary>
/// Writes the FE-Buddy Alias Command Guide: a controller's explanation of every alias command
/// FE-Buddy makes, as a web page or as Markdown, for a facility to post on its own website
/// (Export FE-Buddy Alias Command Guide, on Info ▸ Alias Command Guide).
/// </summary>
/// <remarks>
/// The content is <see cref="AliasGuideContent"/>; <see cref="AliasGuideHtmlWriter"/> and
/// <see cref="AliasGuideMarkdownWriter"/> lay it out. Neither depends on a cycle's data, so the
/// guide can be written at any time, with or without a run.
/// </remarks>
public static class AliasGuideWriter
{
	/// <summary>Writes the guide.</summary>
	/// <param name="format">Web page or Markdown.</param>
	/// <param name="options">The version and date the guide names.</param>
	/// <returns>The whole file's text.</returns>
	public static string Write(AliasGuideFormat format, AliasGuideOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		AliasGuideDocument guide = AliasGuideContent.Build();

		return format == AliasGuideFormat.Markdown
			? AliasGuideMarkdownWriter.Write(guide, options)
			: AliasGuideHtmlWriter.Write(guide, options);
	}

	/// <summary>
	/// The file the guide is saved as in a format: <c>FE-Buddy Alias Command Guide.html</c> or
	/// <c>FE-Buddy Alias Command Guide.md</c>. The user picks only the folder, never the name.
	/// </summary>
	/// <param name="format">Web page or Markdown.</param>
	/// <returns>The file's name, with its extension.</returns>
	public static string FileName(AliasGuideFormat format) =>
		AliasGuideContent.Title + (format == AliasGuideFormat.Markdown ? ".md" : ".html");

	/// <summary>
	/// Writes the guide into a folder once for each format, under <see cref="FileName"/>, UTF-8
	/// without a byte order mark, replacing a guide already there.
	/// </summary>
	/// <param name="folder">The folder to write into.</param>
	/// <param name="formats">The formats to write; a format listed twice is written once.</param>
	/// <param name="options">The version and date the guide names.</param>
	/// <returns>The files written, in the order of <paramref name="formats"/>.</returns>
	/// <exception cref="IOException">A file could not be written.</exception>
	/// <exception cref="UnauthorizedAccessException">A file or the folder is not writable.</exception>
	public static IReadOnlyList<string> Export(string folder, IEnumerable<AliasGuideFormat> formats, AliasGuideOptions options)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(folder);
		ArgumentNullException.ThrowIfNull(formats);
		ArgumentNullException.ThrowIfNull(options);

		List<string> written = [];

		foreach (AliasGuideFormat format in formats.Distinct())
		{
			string path = Path.Combine(folder, FileName(format));
			File.WriteAllText(path, Write(format, options), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			written.Add(path);
		}

		return written;
	}

	/// <summary>The version and date the web page's opening comment credits, e.g. <c>v3.0.0 on 1 October 2026</c>.</summary>
	/// <param name="options">The version and date.</param>
	/// <returns>The version, with a <c>v</c> when it is a number, and the date.</returns>
	internal static string Credit(AliasGuideOptions options)
	{
		string version = options.Version is { Length: > 0 } text && char.IsAsciiDigit(text[0]) ? "v" + text : options.Version;

		return $"{version} on {Date(options)}";
	}

	/// <summary>The line every format ends with: <c>Page updated on 1 October 2026.</c></summary>
	/// <param name="options">The date.</param>
	/// <returns>The line, the date in English with no leading zero.</returns>
	internal static string Updated(AliasGuideOptions options) => $"Page updated on {Date(options)}.";

	private static string Date(AliasGuideOptions options) => options.GeneratedUtc.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
}
