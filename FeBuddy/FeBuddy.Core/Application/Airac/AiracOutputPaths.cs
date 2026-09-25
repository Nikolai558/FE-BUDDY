using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// Where an AIRAC Service run writes its files.
/// </summary>
/// <remarks>
/// <para>
/// Every run of a cycle writes into one folder, <c>&lt;output&gt;[\FE-Buddy_Output]\AIRAC_&lt;cycle&gt;</c>
/// (<see cref="CycleDirectory"/>). Inside it, every sub-service shares the same layout
/// (<see cref="FileDirectory"/>):
/// </para>
/// <code>
/// AIRAC_2610\
/// ├── Airways.txt, Airports.txt, ...    alias and other non-GeoJSON files
/// ├── Geojson\                          every GeoJSON file
/// └── Upload_to_vNAS\                   only the files marked for vNAS, same layout
///     └── Geojson\
/// </code>
/// <para>
/// A file marked for vNAS is written to <c>Upload_to_vNAS</c> instead of, not as well as, the
/// ordinary folder. A folder is created only when a file is written into it.
/// </para>
/// </remarks>
public static class AiracOutputPaths
{
	/// <summary>The folder the "Add a FE-Buddy_Output folder" preference puts the cycle folders in.</summary>
	public const string FeBuddyOutputFolder = "FE-Buddy_Output";

	/// <summary>The folder GeoJSON files go in, inside the cycle folder and inside <see cref="VnasFolder"/>.</summary>
	public const string GeojsonFolder = "Geojson";

	/// <summary>The folder for the files the user marked for upload to vNAS.</summary>
	public const string VnasFolder = "Upload_to_vNAS";

	/// <summary>The name of a cycle's folder, e.g. <c>AIRAC_2610</c>.</summary>
	/// <param name="cycleId">The four-digit cycle ID.</param>
	/// <returns>The folder name.</returns>
	public static string CycleFolderName(string cycleId) => $"AIRAC_{cycleId}";

	/// <summary>
	/// The folder a run of <paramref name="cycleId"/> writes into:
	/// <c>&lt;output&gt;\FE-Buddy_Output\AIRAC_&lt;cycle&gt;</c> when
	/// <paramref name="addFeBuddyOutputFolder"/> is set, otherwise <c>&lt;output&gt;\AIRAC_&lt;cycle&gt;</c>.
	/// </summary>
	/// <param name="outputDirectory">The folder the user pointed output at.</param>
	/// <param name="addFeBuddyOutputFolder">Whether to put the cycle folder inside a <c>FE-Buddy_Output</c> folder.</param>
	/// <param name="cycleId">The four-digit cycle ID.</param>
	/// <returns>The cycle folder's full path.</returns>
	public static string CycleDirectory(string outputDirectory, bool addFeBuddyOutputFolder, string cycleId)
	{
		string root = addFeBuddyOutputFolder ? Path.Combine(outputDirectory, FeBuddyOutputFolder) : outputDirectory;
		return Path.Combine(root, CycleFolderName(cycleId));
	}

	/// <summary>
	/// The folder one file goes in, inside the folder a run writes into.
	/// </summary>
	/// <param name="outputDirectory">The folder the run writes into - the cycle folder, for an AIRAC Service run.</param>
	/// <param name="isGeojson">Whether the file is GeoJSON (it goes in a <c>Geojson</c> folder).</param>
	/// <param name="uploadToVnas">Whether the user marked the file for vNAS (it goes under <c>Upload_to_vNAS</c>).</param>
	/// <returns>The folder's full path.</returns>
	public static string FileDirectory(string outputDirectory, bool isGeojson, bool uploadToVnas)
	{
		string root = uploadToVnas ? Path.Combine(outputDirectory, VnasFolder) : outputDirectory;
		return isGeojson ? Path.Combine(root, GeojsonFolder) : root;
	}

	/// <summary>
	/// The word a GeoJSON file of this kind ends in, e.g. <c>Lines</c> in
	/// <c>Airways_High_Lines.geojson</c>.
	/// </summary>
	/// <param name="kind">The kind of feature the file holds.</param>
	/// <returns><c>Lines</c>, <c>Symbols</c> or <c>Text</c>.</returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown for an unknown kind.</exception>
	public static string FileKindSuffix(CrcFeatureKind kind) => kind switch
	{
		CrcFeatureKind.Line => "Lines",
		CrcFeatureKind.Symbol => "Symbols",
		CrcFeatureKind.Text => "Text",
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown feature kind."),
	};
}
