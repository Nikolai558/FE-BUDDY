using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Infrastructure.FileSystem;

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
/// ├── Duplicate_Alias_Commands.txt      the alias commands the run's alias files share
/// ├── Aliases\                          every alias file (Airways.txt, FAA_CHART_RECALL.txt, ...)
/// ├── Geojson\                          every GeoJSON file
/// ├── Publication_Docs\                 the Procedures sub-service's two documents
/// └── Upload_to_vNAS\                   only the files marked for vNAS
///     ├── Airways.txt, ...              alias files, directly inside
///     └── Geojson\
/// </code>
/// <para>
/// A file marked for vNAS is written to <c>Upload_to_vNAS</c> instead of, not as well as, the
/// ordinary folder. A folder is created only when a file is written into it.
/// </para>
/// </remarks>
public static class AiracOutputPaths
{
	/// <summary>The folder GeoJSON files go in, inside the cycle folder and inside <see cref="VnasFolder"/>.</summary>
	public const string GeojsonFolder = "Geojson";

	/// <summary>The folder alias files not marked for vNAS go in, inside the cycle folder.</summary>
	public const string AliasFolder = "Aliases";

	/// <summary>The folder for the files the user marked for upload to vNAS.</summary>
	public const string VnasFolder = "Upload_to_vNAS";

	/// <summary>The folder the Procedures sub-service's documents go in, inside the cycle folder.</summary>
	public const string PublicationDocsFolder = "Publication_Docs";

	/// <summary>
	/// The report of the alias commands more than one line of the run's alias files uses, written
	/// directly inside the cycle folder (<c>DuplicateAliasReport</c>).
	/// </summary>
	public const string DuplicateAliasReportFileName = "Duplicate_Alias_Commands.txt";

	/// <summary>The name of a cycle's folder, e.g. <c>AIRAC_2610</c>.</summary>
	/// <param name="cycleId">The four-digit cycle ID.</param>
	/// <returns>The folder name.</returns>
	public static string CycleFolderName(string cycleId) => $"AIRAC_{cycleId}";

	/// <summary>
	/// The folder a run of <paramref name="cycleId"/> writes into:
	/// <c>&lt;output&gt;\FE-Buddy_Output\AIRAC_&lt;cycle&gt;</c> when
	/// <paramref name="addFeBuddyOutputFolder"/> is set, otherwise <c>&lt;output&gt;\AIRAC_&lt;cycle&gt;</c>.
	/// It sits next to the File Conversions' folders: both go through <see cref="ServiceOutputPaths"/>.
	/// </summary>
	/// <param name="outputDirectory">The folder the user pointed output at.</param>
	/// <param name="addFeBuddyOutputFolder">Whether to put the cycle folder inside a <c>FE-Buddy_Output</c> folder.</param>
	/// <param name="cycleId">The four-digit cycle ID.</param>
	/// <returns>The cycle folder's full path.</returns>
	public static string CycleDirectory(string outputDirectory, bool addFeBuddyOutputFolder, string cycleId) =>
		ServiceOutputPaths.Resolve(outputDirectory, addFeBuddyOutputFolder, CycleFolderName(cycleId));

	/// <summary>
	/// The folder one file goes in, inside the folder a run writes into. An alias file goes in
	/// <see cref="AliasDirectory"/> instead.
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
	/// The folder an alias file goes in, inside the folder a run writes into:
	/// <c>&lt;output&gt;\Aliases</c>, or <c>&lt;output&gt;\Upload_to_vNAS</c> itself when the user
	/// marked the file for vNAS.
	/// </summary>
	/// <param name="outputDirectory">The folder the run writes into - the cycle folder, for an AIRAC Service run.</param>
	/// <param name="uploadToVnas">Whether the user marked the file for vNAS.</param>
	/// <returns>The folder's full path.</returns>
	public static string AliasDirectory(string outputDirectory, bool uploadToVnas) =>
		uploadToVnas ? Path.Combine(outputDirectory, VnasFolder) : Path.Combine(outputDirectory, AliasFolder);

	/// <summary>
	/// The folder the Procedures sub-service's two documents go in:
	/// <c>&lt;output&gt;\Publication_Docs</c>. Never the vNAS folder - <see cref="FileDirectory"/> is
	/// not used for these files.
	/// </summary>
	/// <param name="outputDirectory">The folder the run writes into - the cycle folder, for an AIRAC Service run.</param>
	/// <returns>The folder's full path.</returns>
	public static string PublicationDocsDirectory(string outputDirectory) =>
		Path.Combine(outputDirectory, PublicationDocsFolder);

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
