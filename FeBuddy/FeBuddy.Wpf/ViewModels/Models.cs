namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>One row in the shell's systems-health popover (Internet / AIRAC data / Updates).</summary>
public sealed record HealthRow(string Name, string Detail, StatusKind Kind);



