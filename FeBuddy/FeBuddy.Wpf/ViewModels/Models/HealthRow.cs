namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>One row in the shell's systems-health popover (Internet / AIRAC data / Updates).</summary>
/// <param name="Name">What the row reports on, e.g. <c>Internet</c>.</param>
/// <param name="Detail">Its state in a few words, e.g. <c>connected</c>.</param>
/// <param name="Kind">The state's colour.</param>
/// <param name="Note">A warning line under <paramref name="Detail"/>, drawn amber, e.g. <c>next: no d-TPP Metafile yet</c>; <see langword="null"/> for none.</param>
/// <param name="OnlyIssueSummary">
/// The popover's heading when this row is the only one needing attention, e.g. <c>Ready except next
/// d-TPP Metafile</c>, instead of <c>1 needs attention</c>; <see langword="null"/> to keep that.
/// </param>
public sealed record HealthRow(string Name, string Detail, StatusKind Kind, string? Note = null, string? OnlyIssueSummary = null);
