namespace FeBuddy.Core.Infrastructure.Configuration.Models;

/// <summary>A folder setting in an import, and what the import does with it.</summary>
/// <param name="Key">The setting's dotted path.</param>
/// <param name="Label">The setting's name for people, e.g. <c>Default output directory</c>.</param>
/// <param name="Path">
/// The file's folder made this user's (tokens expanded, another user's profile swapped for this
/// one), or empty when the file does not set it.
/// </param>
/// <param name="Note">
/// For a skipped folder, why it cannot be used and what happens instead, e.g. <c>is not found on
/// this PC, so this PC's folder is kept</c>. For a taken one, anything worth knowing, e.g. that it is
/// created later; otherwise <see langword="null"/>.
/// </param>
public sealed record ImportedFolder(string Key, string Label, string Path, string? Note = null);
