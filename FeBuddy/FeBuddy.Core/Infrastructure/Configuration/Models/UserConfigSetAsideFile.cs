namespace FeBuddy.Core.Infrastructure.Configuration.Models;

/// <summary>One settings file a newer FE-Buddy saved, which this one set aside because it can't read its layout.</summary>
/// <param name="Profile">The profile it held, or <see langword="null"/> for <c>Shared.json</c>.</param>
/// <param name="Layout">The layout it was saved in.</param>
/// <param name="RestoredLayout">
/// The layout of the backup put back in its place, or <see langword="null"/> when there was none and it
/// starts on every default.
/// </param>
public sealed record UserConfigSetAsideFile(string? Profile, int Layout, int? RestoredLayout);
