namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>One GeoJSON file an AIRAC Service run wrote, found by <see cref="AiracOutputCatalog"/>.</summary>
/// <param name="FullPath">The file's full path.</param>
/// <param name="RelativePath">
/// The path inside the cycle folder, e.g. <c>Geojson\Airways_High_Lines.geojson</c>. The same
/// file from another cycle's run has the same relative path.
/// </param>
/// <param name="SubFolder">
/// The folder below <c>Geojson</c> it sits in, e.g. <c>ZOB\CLE</c> for a departure, or empty
/// for a file at the top.
/// </param>
/// <param name="UploadToVnas">Whether it is in <c>Upload_to_vNAS</c>.</param>
/// <param name="SizeBytes">The file's size.</param>
/// <param name="LastWriteUtc">When it was last written.</param>
public sealed record AiracOutputGeojsonFile(
	string FullPath,
	string RelativePath,
	string SubFolder,
	bool UploadToVnas,
	long SizeBytes,
	DateTime LastWriteUtc)
{
	/// <summary>The file name without its extension, e.g. <c>Airways_High_Lines</c>.</summary>
	public string Name => Path.GetFileNameWithoutExtension(FullPath);
}
