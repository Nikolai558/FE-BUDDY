namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>
/// Which of a sub-service's GeoJSON files carry CRC-ERAM defaults: an isDefaults Feature at the top
/// of the file that CRC applies to every feature in it.
/// </summary>
/// <remarks>
/// Files are named by their file key: a GeoJSON file's name without <c>.geojson</c> (e.g.
/// <c>Airways_High_Lines</c>). Departures and Arrivals, which write thousands of GeoJSON files, are
/// chosen per kind instead (<c>Departures_Lines</c> covers every procedure's Lines file). Each
/// sub-service lists its keys in its <c>*OutputFiles</c> class. Keys match ignoring case.
/// </remarks>
public sealed class CrcDefaultsFiles
{
	/// <summary>Creates the choice.</summary>
	/// <param name="files">The keys of the files that get CRC-ERAM defaults.</param>
	public CrcDefaultsFiles(IEnumerable<string> files)
	{
		ArgumentNullException.ThrowIfNull(files);

		Files = new HashSet<string>(files, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>No file gets CRC-ERAM defaults.</summary>
	public static CrcDefaultsFiles None { get; } = new([]);

	/// <summary>The keys of the files that get CRC-ERAM defaults.</summary>
	public IReadOnlySet<string> Files { get; }

	/// <summary>Whether a file gets CRC-ERAM defaults.</summary>
	/// <param name="fileKey">The file's key.</param>
	/// <returns><see langword="true"/> when its isDefaults Feature is written.</returns>
	public bool HasCrcDefaults(string fileKey) => Files.Contains(fileKey);
}
