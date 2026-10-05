using NetTopologySuite.Features;

namespace FeBuddy.Core.Infrastructure.Geojson;

/// <summary>
/// The GeoJSON files one run writes: each one goes through <see cref="GeojsonFileWriter"/>, and
/// the set remembers which landed on disk and how many rendered Features each holds.
/// </summary>
/// <remarks>
/// Every file's symbols that carry the same properties are grouped into MultiPoint Features first
/// (<see cref="SymbolFeatureMerger"/>), unless the caller asks for each symbol on its own, as the
/// ERAM conversion's Raw layout does.
/// </remarks>
/// <param name="coordinatePrecision">Decimal places kept for every coordinate written (0 keeps them all).</param>
public sealed class GeojsonFileSet(int coordinatePrecision)
{
	private readonly List<string> _filesWritten = [];
	private readonly Dictionary<string, int> _renderedFeatureCounts = [];
	private readonly int _coordinatePrecision = coordinatePrecision;

	/// <summary>Every file written, in the order written.</summary>
	public IReadOnlyList<string> FilesWritten => _filesWritten;

	/// <summary>How many rendered Features each written file holds, after its symbols are grouped, keyed by its path.</summary>
	public IReadOnlyDictionary<string, int> RenderedFeatureCountsByFile => _renderedFeatureCounts;

	/// <summary>
	/// Writes one file - or nothing, when it would hold no rendered Features.
	/// </summary>
	/// <param name="collection">The Features, including any isDefaults Feature.</param>
	/// <param name="renderedFeatureCount">How many of them draw something (everything but an isDefaults Feature).</param>
	/// <param name="directory">The folder to write into; created if missing.</param>
	/// <param name="fileName">The file name, including <c>.geojson</c>.</param>
	/// <param name="groupSymbols">
	/// Whether symbols that carry the same properties become one MultiPoint Feature. Leave it on;
	/// <see langword="false"/> keeps one Feature per symbol.
	/// </param>
	public void Write(FeatureCollection collection, int renderedFeatureCount, string directory, string fileName, bool groupSymbols = true)
	{
		ArgumentNullException.ThrowIfNull(collection);

		if (groupSymbols && renderedFeatureCount > 0)
		{
			FeatureCollection grouped = SymbolFeatureMerger.Merge(collection);

			// Only symbols are grouped, and each of them is rendered, so the Features gone are rendered ones.
			renderedFeatureCount -= collection.Count - grouped.Count;
			collection = grouped;
		}

		string? path = GeojsonFileWriter.Write(collection, renderedFeatureCount, directory, fileName, _coordinatePrecision);

		if (path is not null)
		{
			_filesWritten.Add(path);
			_renderedFeatureCounts[path] = renderedFeatureCount;
		}
	}
}
