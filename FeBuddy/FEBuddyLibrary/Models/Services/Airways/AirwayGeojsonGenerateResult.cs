namespace FEBuddyLibrary.Models.Services.Airways;

/// <summary>
/// The result of generating Airways GeoJSON files: which files were written, how many
/// rendered Features each contains, and any non-fatal warnings.
/// </summary>
/// <param name="FilesWritten">The full paths of every file actually written (empty files are skipped).</param>
/// <param name="RenderedFeatureCountsByFile">Rendered Feature count for each entry in <paramref name="FilesWritten"/>, keyed by path.</param>
/// <param name="Warnings">Non-fatal problems noticed while generating GeoJSON.</param>
public sealed record AirwayGeojsonGenerateResult(
	IReadOnlyList<string> FilesWritten,
	IReadOnlyDictionary<string, int> RenderedFeatureCountsByFile,
	IReadOnlyList<string> Warnings);
