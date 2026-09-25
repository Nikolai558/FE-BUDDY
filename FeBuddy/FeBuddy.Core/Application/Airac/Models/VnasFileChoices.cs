namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>
/// Which of a sub-service's output files the user will upload to vNAS, and which of those carry
/// CRC-ERAM defaults.
/// </summary>
/// <remarks>
/// <para>
/// Files are named by their file key: a GeoJSON file's name without <c>.geojson</c> (e.g.
/// <c>Airways_High_Lines</c>), or the alias file's name (e.g. <c>Airways.txt</c>). Departures,
/// which writes thousands of GeoJSON files, is chosen per kind instead
/// (<c>Departures_Lines</c> covers every procedure's Lines file). Each sub-service lists its
/// keys in its <c>*OutputFiles</c> class. Keys match ignoring case.
/// </para>
/// <para>
/// A file marked for vNAS is written under <c>Upload_to_vNAS</c> (see
/// <see cref="AiracOutputPaths"/>). CRC-ERAM defaults are only ever written to files marked for
/// vNAS: CRC reads its maps from vNAS, so defaults anywhere else would never be used.
/// </para>
/// </remarks>
public sealed class VnasFileChoices
{
	/// <summary>Creates the choices.</summary>
	/// <param name="uploadFiles">The files marked for vNAS.</param>
	/// <param name="crcDefaultsFiles">The files that get CRC-ERAM defaults; every one must be in <paramref name="uploadFiles"/>.</param>
	/// <exception cref="ArgumentException">Thrown when a CRC-defaults file is not marked for vNAS.</exception>
	public VnasFileChoices(IEnumerable<string> uploadFiles, IEnumerable<string> crcDefaultsFiles)
	{
		ArgumentNullException.ThrowIfNull(uploadFiles);
		ArgumentNullException.ThrowIfNull(crcDefaultsFiles);

		UploadFiles = new HashSet<string>(uploadFiles, StringComparer.OrdinalIgnoreCase);
		CrcDefaultsFiles = new HashSet<string>(crcDefaultsFiles, StringComparer.OrdinalIgnoreCase);

		if (!CrcDefaultsFiles.IsSubsetOf(UploadFiles))
		{
			throw new ArgumentException(
				"CRC-ERAM defaults are only written to files uploaded to vNAS, so every CRC-defaults file must also be marked for vNAS.",
				nameof(crcDefaultsFiles));
		}
	}

	/// <summary>No file goes to vNAS, and none gets CRC-ERAM defaults.</summary>
	public static VnasFileChoices None { get; } = new([], []);

	/// <summary>The keys of the files marked for vNAS.</summary>
	public IReadOnlySet<string> UploadFiles { get; }

	/// <summary>The keys of the files that get CRC-ERAM defaults; always a subset of <see cref="UploadFiles"/>.</summary>
	public IReadOnlySet<string> CrcDefaultsFiles { get; }

	/// <summary>Whether a file is marked for vNAS.</summary>
	/// <param name="fileKey">The file's key.</param>
	/// <returns><see langword="true"/> when it goes under <c>Upload_to_vNAS</c>.</returns>
	public bool IsUploaded(string fileKey) => UploadFiles.Contains(fileKey);

	/// <summary>Whether a file gets CRC-ERAM defaults.</summary>
	/// <param name="fileKey">The file's key.</param>
	/// <returns><see langword="true"/> when its isDefaults Feature is written.</returns>
	public bool HasCrcDefaults(string fileKey) => CrcDefaultsFiles.Contains(fileKey);
}
