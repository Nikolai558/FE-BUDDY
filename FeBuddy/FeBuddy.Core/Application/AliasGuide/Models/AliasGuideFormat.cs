namespace FeBuddy.Core.Application.AliasGuide.Models;

/// <summary>The kind of file the alias command guide is written as.</summary>
public enum AliasGuideFormat
{
	/// <summary>A self-contained web page, styled in a dark theme, ready to post on a facility's website.</summary>
	Html,

	/// <summary>GitHub-flavoured Markdown, for a wiki, a README or another Markdown site.</summary>
	Markdown,
}
