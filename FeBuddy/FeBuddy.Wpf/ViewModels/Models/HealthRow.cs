namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>One row in the shell's systems-health popover (Internet / AIRAC data / Updates).</summary>
/// <param name="Name">What the row reports on, e.g. <c>Internet</c>.</param>
/// <param name="Detail">Its state in a few words, e.g. <c>connected</c>.</param>
/// <param name="Kind">The state's colour.</param>
public sealed record HealthRow(string Name, string Detail, StatusKind Kind);
