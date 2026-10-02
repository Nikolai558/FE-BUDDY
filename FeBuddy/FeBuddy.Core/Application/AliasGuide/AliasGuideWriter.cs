using System.Globalization;
using System.Text;

using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.Core.Application.AliasGuide;

/// <summary>
/// Writes the FE-Buddy Alias Command Guide: a controller's explanation of every alias command
/// FE-Buddy makes, as a web page or as Markdown, for a facility to post on its own website
/// (Info ▸ What's New in v3.0? ▸ Export FE-Buddy Alias Command Guide).
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
	/// <param name="options">The facility, version and date the guide names.</param>
	/// <returns>The whole file's text.</returns>
	public static string Write(AliasGuideFormat format, AliasGuideOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		AliasGuideDocument guide = AliasGuideContent.Build(options.Facility);

		return format == AliasGuideFormat.Markdown
			? AliasGuideMarkdownWriter.Write(guide, options)
			: AliasGuideHtmlWriter.Write(guide, options);
	}

	/// <summary>Writes the guide to a file, UTF-8 without a byte order mark, replacing any file already there.</summary>
	/// <param name="path">The file to write.</param>
	/// <param name="format">Web page or Markdown.</param>
	/// <param name="options">The facility, version and date the guide names.</param>
	/// <exception cref="IOException">The file could not be written.</exception>
	/// <exception cref="UnauthorizedAccessException">The file or its folder is not writable.</exception>
	public static void Export(string path, AliasGuideFormat format, AliasGuideOptions options)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		File.WriteAllText(path, Write(format, options), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
	}

	/// <summary>The format a file name asks for: Markdown for <c>.md</c> or <c>.markdown</c>, a web page for anything else.</summary>
	/// <param name="path">The file's name or path.</param>
	/// <returns>The format to write it in.</returns>
	public static AliasGuideFormat FormatFor(string path)
	{
		string extension = Path.GetExtension(path ?? string.Empty);

		return extension.Equals(".md", StringComparison.OrdinalIgnoreCase) || extension.Equals(".markdown", StringComparison.OrdinalIgnoreCase)
			? AliasGuideFormat.Markdown
			: AliasGuideFormat.Html;
	}

	/// <summary>The version and date the footer credits, e.g. <c>v3.0.0 on 1 October 2026</c>.</summary>
	/// <param name="options">The version and date.</param>
	/// <returns>The version, with a <c>v</c> when it is a number, and the date.</returns>
	internal static string Credit(AliasGuideOptions options)
	{
		string version = options.Version is { Length: > 0 } text && char.IsAsciiDigit(text[0]) ? "v" + text : options.Version;
		string date = options.GeneratedUtc.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

		return $"{version} on {date}";
	}
}
