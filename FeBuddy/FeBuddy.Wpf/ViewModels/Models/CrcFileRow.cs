namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>One row on the CRC ERAM Defaults card's "Specific files" list: a label, then a checkbox per GeoJSON file.</summary>
/// <param name="Label">The row's label, e.g. <c>High</c> or <c>J</c>.</param>
/// <param name="Files">The files in the row, in display order.</param>
public sealed record CrcFileRow(string Label, IReadOnlyList<CrcFileToggle> Files);
