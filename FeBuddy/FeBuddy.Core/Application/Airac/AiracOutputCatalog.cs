using System.Globalization;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Infrastructure.FileSystem;

namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// Finds what earlier AIRAC Service runs left on disk: the <c>AIRAC_&lt;cycle&gt;</c> folders in
/// the output directory, and the GeoJSON files inside one of them (both the ordinary
/// <c>Geojson</c> folder and <c>Upload_to_vNAS\Geojson</c>, see <see cref="AiracOutputPaths"/>).
/// The Map screen lists these so the user can put a run's output on the map.
/// </summary>
public static class AiracOutputCatalog
{
	private const string CyclePrefix = "AIRAC_";

	/// <summary>
	/// The cycles that have an output folder, newest first, e.g. <c>["2611", "2610"]</c>. Only
	/// folders named <c>AIRAC_</c> plus four digits count.
	/// </summary>
	/// <param name="outputDirectory">The folder the user pointed output at.</param>
	/// <param name="addFeBuddyOutputFolder">Whether runs write inside a <c>FE-Buddy_Output</c> folder.</param>
	/// <returns>The cycle IDs; empty when the folder does not exist or cannot be read.</returns>
	public static IReadOnlyList<string> FindCycleIds(string outputDirectory, bool addFeBuddyOutputFolder)
	{
		string root = ServiceOutputPaths.Resolve(outputDirectory, addFeBuddyOutputFolder);

		try
		{
			if (!Directory.Exists(root))
			{
				return [];
			}

			return [.. Directory.EnumerateDirectories(root, CyclePrefix + "*")
				.Select(Path.GetFileName)
				.Select(name => name![CyclePrefix.Length..])
				.Where(IsCycleId)
				.OrderByDescending(id => id, StringComparer.Ordinal)];
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return [];
		}
	}

	/// <summary>
	/// Every <c>.geojson</c> file a run of one cycle wrote, ordinary folder first. Within each,
	/// the files at the top come first, then each sub-folder's (Departures and Arrivals write
	/// one per procedure under <c>&lt;ARTCC&gt;\&lt;airport&gt;</c>), all in name order.
	/// </summary>
	/// <param name="cycleDirectory">The cycle's folder, from <see cref="AiracOutputPaths.CycleDirectory"/>.</param>
	/// <returns>The files; empty when neither folder exists or they cannot be read.</returns>
	public static IReadOnlyList<AiracOutputGeojsonFile> FindGeojsonFiles(string cycleDirectory)
	{
		List<AiracOutputGeojsonFile> files = [];
		AddFolder(cycleDirectory, uploadToVnas: false, files);
		AddFolder(cycleDirectory, uploadToVnas: true, files);
		return files;
	}

	private static void AddFolder(string cycleDirectory, bool uploadToVnas, List<AiracOutputGeojsonFile> into)
	{
		string folder = AiracOutputPaths.FileDirectory(cycleDirectory, isGeojson: true, uploadToVnas);

		try
		{
			if (!Directory.Exists(folder))
			{
				return;
			}

			string root = Path.GetFullPath(folder);
			into.AddRange(new DirectoryInfo(root).EnumerateFiles("*.geojson", SearchOption.AllDirectories)
				.Select(file => (File: file, SubFolder: Path.GetRelativePath(root, file.DirectoryName!)))
				.Select(f => new AiracOutputGeojsonFile(
					f.File.FullName,
					Path.GetRelativePath(cycleDirectory, f.File.FullName),
					f.SubFolder == "." ? string.Empty : f.SubFolder,
					uploadToVnas,
					f.File.Length,
					f.File.LastWriteTimeUtc))
				.OrderBy(f => f.SubFolder.Length == 0 ? 0 : 1)
				.ThenBy(f => f.SubFolder, StringComparer.OrdinalIgnoreCase)
				.ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase));
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// A folder that vanished or cannot be read simply has nothing to list.
		}
	}

	private static bool IsCycleId(string id) =>
		id.Length == 4 && int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out _);
}
