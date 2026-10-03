namespace FeBuddy.Core.Application.AliasGuide.Models;

/// <summary>What the alias command guide says about where and when it was written.</summary>
/// <param name="Version">The FE-Buddy version that wrote the guide, e.g. <c>3.0.0</c>, or <c>dev</c>: the web page's opening comment names it.</param>
/// <param name="GeneratedUtc">When the guide was written: its footer says the page was updated that day.</param>
public sealed record AliasGuideOptions(string Version, DateTime GeneratedUtc);
