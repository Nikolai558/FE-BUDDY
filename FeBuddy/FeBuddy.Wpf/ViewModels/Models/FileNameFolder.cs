namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>One folder on the File Names tab, and the files the run writes into it.</summary>
/// <param name="Folder">The folder, from the cycle folder down, e.g. <c>AIRAC_2610\Geojson</c>.</param>
/// <param name="Files">Its files, in the order the sub-services list them.</param>
public sealed record FileNameFolder(string Folder, IReadOnlyList<FileNameRow> Files);
