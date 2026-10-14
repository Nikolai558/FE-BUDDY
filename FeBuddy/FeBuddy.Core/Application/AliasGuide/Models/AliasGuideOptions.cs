namespace FeBuddy.Core.Application.AliasGuide.Models;

/// <summary>What the alias command guide says about where and when it was written, and the user's own help command.</summary>
/// <param name="Version">The FE-Buddy version that wrote the guide, e.g. <c>3.0.0</c>, or <c>dev</c>: the web page's opening comment names it.</param>
/// <param name="GeneratedUtc">When the guide was written: its footer says the page was updated that day.</param>
/// <param name="HelpCommand">
/// The chart recall help command's name, without its dot, as the user's alias file writes it; or
/// <see langword="null"/> when it is off or not known, and the guide shows its default name and says a
/// facility may have renamed it or left it out.
/// </param>
public sealed record AliasGuideOptions(string Version, DateTime GeneratedUtc, string? HelpCommand = null);
