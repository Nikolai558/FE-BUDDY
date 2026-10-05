using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Infrastructure.FileSystem;

namespace FeBuddy.Core.Application.Airac;

/// <summary>
/// Where an AIRAC Service run writes its files.
/// </summary>
/// <remarks>
/// <para>
/// Every run of a cycle writes into one folder, <c>&lt;output&gt;[\FE-Buddy_Output]\AIRAC_&lt;cycle&gt;</c>
/// (<see cref="CycleDirectory"/>). Inside it, every sub-service shares the same layout:
/// </para>
/// <code>
/// AIRAC_2610\
/// ├── Duplicate_Alias_Commands.txt      the alias commands the run's alias files share
/// ├── Aliases\                          every alias file (Airways.txt, Faa_Chart_Recall.txt, ...)
/// │   └── Combined_Alias.txt            all of them in one, for vNAS (see VnasAliasFileWriter)
/// ├── Geojson\                          every GeoJSON file
/// └── Publication_Docs\                 the Procedures sub-service's two documents
/// </code>
/// <para>
/// Every file is ready for vNAS. vNAS takes one alias file per facility, so the Concatenate Aliases
/// sub-service copies every alias file the run wrote, one after another, into
/// <c>Combined_Alias.txt</c>, followed by the user's own custom alias files. A folder is created only
/// when a file is written into it.
/// </para>
/// <para>
/// The file names here are FE-Buddy's. The user can give most files a name of their own (the File
/// Names tab, see <see cref="OutputFileNames"/>); a renamed file keeps its folder and its extension.
/// </para>
/// </remarks>
public static class AiracOutputPaths
{
	/// <summary>The folder GeoJSON files go in, inside the cycle folder.</summary>
	public const string GeojsonFolder = "Geojson";

	/// <summary>The folder every alias file goes in, inside the cycle folder.</summary>
	public const string AliasFolder = "Aliases";

	/// <summary>
	/// The one alias file to upload to vNAS, inside <see cref="AliasFolder"/>: every alias file the run
	/// wrote, then the user's custom alias files.
	/// </summary>
	public const string CombinedAliasFileName = "Combined_Alias.txt";

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

	/// <summary>The folder every GeoJSON file goes in, inside the folder a run writes into: <c>&lt;output&gt;\Geojson</c>.</summary>
	/// <param name="outputDirectory">The folder the run writes into - the cycle folder, for an AIRAC Service run.</param>
	/// <returns>The folder's full path.</returns>
	public static string GeojsonDirectory(string outputDirectory) => Path.Combine(outputDirectory, GeojsonFolder);

	/// <summary>The folder every alias file goes in, inside the folder a run writes into: <c>&lt;output&gt;\Aliases</c>.</summary>
	/// <param name="outputDirectory">The folder the run writes into - the cycle folder, for an AIRAC Service run.</param>
	/// <returns>The folder's full path.</returns>
	public static string AliasDirectory(string outputDirectory) => Path.Combine(outputDirectory, AliasFolder);

	/// <summary>The one alias file to upload to vNAS: <c>&lt;output&gt;\Aliases\Combined_Alias.txt</c>.</summary>
	/// <param name="outputDirectory">The folder the run writes into - the cycle folder, for an AIRAC Service run.</param>
	/// <param name="fileName">The file's name, when the user gave it one of their own (see <see cref="OutputFileNames"/>).</param>
	/// <returns>The file's full path.</returns>
	public static string CombinedAliasFilePath(string outputDirectory, string fileName = CombinedAliasFileName) =>
		Path.Combine(AliasDirectory(outputDirectory), fileName);

	/// <summary>
	/// The folder the Procedures sub-service's two documents go in:
	/// <c>&lt;output&gt;\Publication_Docs</c>.
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
