namespace FeBuddy.Core.Application.AliasGuide.Models;

/// <summary>What the alias command guide says about where it came from.</summary>
/// <param name="Facility">
/// The user's facility (Settings ▸ Facility Profile), e.g. <c>ZOB</c>: the guide's opening line
/// says the commands are merged into that facility's alias file. Only its letters and digits are
/// used; <see langword="null"/>, or nothing left, says "your facility's alias file" instead.
/// </param>
/// <param name="Version">The FE-Buddy version that wrote the guide, e.g. <c>3.0.0</c>, or <c>dev</c>.</param>
/// <param name="GeneratedUtc">When the guide was written, shown as a date in its footer.</param>
public sealed record AliasGuideOptions(string? Facility, string Version, DateTime GeneratedUtc);
