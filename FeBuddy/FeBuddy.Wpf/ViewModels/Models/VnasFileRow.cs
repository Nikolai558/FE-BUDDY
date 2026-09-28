namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>One row on the Upload to vNAS card: a label, then a checkbox per file.</summary>
/// <param name="Label">The row's label, e.g. <c>High</c>, <c>J</c> or <c>Alias file</c>.</param>
/// <param name="Files">The files in the row, in display order.</param>
public sealed record VnasFileRow(string Label, IReadOnlyList<VnasFileToggle> Files);
