using FEBuddyLibrary.Models.Services.General;
using FEBuddyLibrary.Services.General;

namespace FEBuddyLibrary.Models.Services.Airac.Airways;

/// <summary>
/// The result of generating Airways GeoJSON files: which files were written, how many
/// rendered Features each contains, and any levelled messages.
/// </summary>
/// <param name="FilesWritten">The full paths of every file actually written (empty files are skipped).</param>
/// <param name="RenderedFeatureCountsByFile">Rendered Feature count for each entry in <paramref name="FilesWritten"/>, keyed by path.</param>
/// <param name="Messages">Levelled messages raised while generating GeoJSON (remediation plan 3.8).</param>
public sealed record AirwayGeojsonGenerateResult(
	IReadOnlyList<string> FilesWritten,
	IReadOnlyDictionary<string, int> RenderedFeatureCountsByFile,
	IReadOnlyList<ServiceMessage> Messages)
{
	/// <summary>Backwards-compatible text-only view of the Warning/Error entries in <see cref="Messages"/>.</summary>
	public IReadOnlyList<string> Warnings =>
		Messages.Where(m => m.Level is LogLevel.Warning or LogLevel.Error).Select(m => m.Text).ToArray();
}
