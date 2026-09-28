using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>One file an AIRAC Service run will write, as the File Names tab lists it.</summary>
/// <param name="Key">
/// The file key Core names it by, e.g. <c>Airways_High_Lines</c> or <c>Airways.txt</c> (see
/// <see cref="OutputFileNames"/>); for files that can't be renamed, the key of their kind, e.g.
/// <c>Departures_Lines</c>.
/// </param>
/// <param name="Folder">
/// Its folder inside the cycle folder, e.g. <c>Geojson</c> or <c>Upload_to_vNAS\Geojson</c>; empty
/// for the cycle folder itself.
/// </param>
/// <param name="FileName">
/// FE-Buddy's name for it, e.g. <c>Airways_High_Lines.geojson</c>, or the pattern the names follow,
/// e.g. <c>&lt;airport&gt;_&lt;procedure&gt;_Lines.geojson</c>.
/// </param>
/// <param name="SubService">What writes it, e.g. <c>Airways</c>.</param>
/// <param name="CanRename">
/// Whether the user can give it a name of their own: <see langword="false"/> only for the
/// Departures and Arrivals files a run writes one per procedure, named from the FAA's data.
/// </param>
public sealed record OutputFileEntry(string Key, string Folder, string FileName, string SubService, bool CanRename)
{
	/// <summary>A file the user can rename, listed under FE-Buddy's name for it.</summary>
	/// <param name="key">The file key.</param>
	/// <param name="folder">Its folder inside the cycle folder.</param>
	/// <param name="subService">What writes it.</param>
	/// <returns>The entry.</returns>
	public static OutputFileEntry Renamable(string key, string folder, string subService) =>
		new(key, folder, OutputFileNames.DefaultFileName(key), subService, CanRename: true);
}
