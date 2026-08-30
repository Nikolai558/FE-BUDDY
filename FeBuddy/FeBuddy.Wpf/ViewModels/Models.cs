namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>A generated output file, shown in "recent output" lists. Display-only.</summary>
public sealed record OutputFile(string Name, string Size, string When);

/// <summary>A single line in a status checklist (e.g. generator readiness).</summary>
public sealed record StatusRow(string Label, string Detail, StatusKind Kind);

/// <summary>A resource card on the Info screen.</summary>
public sealed record InfoLink(string Glyph, string Title, string Blurb, string Action);
