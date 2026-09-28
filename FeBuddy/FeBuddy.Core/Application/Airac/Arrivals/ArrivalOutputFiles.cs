using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Arrivals.Models;
using FeBuddy.Core.Domain.Crc.Models;

namespace FeBuddy.Core.Application.Airac.Arrivals;

/// <summary>
/// The files the Arrivals sub-service writes: their file keys - the names the
/// <c>UploadToVnas</c> and <c>CrcDefaultsFor</c> settings use (see <see cref="VnasFileChoices"/>) -
/// and where each one goes.
/// </summary>
/// <remarks>
/// A run writes up to three GeoJSON files per airport + procedure, often thousands in all, so
/// they are chosen by kind: <see cref="Lines"/> covers every procedure's Lines file. Each
/// airport's files go in <c>&lt;ARTCC&gt;\&lt;ARPT&gt;</c> inside the GeoJSON folder (see
/// <see cref="AiracOutputPaths"/>), e.g. <c>Geojson\ZLA\LAS\LAS_BLAID_STAR_Lines.geojson</c> - the
/// same folder as that airport's Departures files.
/// </remarks>
public static class ArrivalOutputFiles
{
	/// <summary>Every procedure's <c>_Lines.geojson</c> file.</summary>
	public const string Lines = "Arrivals_Lines";

	/// <summary>Every procedure's <c>_Symbols.geojson</c> file.</summary>
	public const string Symbols = "Arrivals_Symbols";

	/// <summary>Every procedure's <c>_Text.geojson</c> file.</summary>
	public const string Text = "Arrivals_Text";

	/// <summary>The alias file.</summary>
	public const string Alias = "Arrivals.txt";

	/// <summary>Every GeoJSON file key, in the order Lines, Symbols, Text.</summary>
	public static IReadOnlyList<string> GeojsonKeys { get; } = [Lines, Symbols, Text];

	/// <summary>Whether a key names one of the Arrivals kinds of GeoJSON file, ignoring case.</summary>
	/// <param name="key">The file key.</param>
	/// <returns><see langword="true"/> for a GeoJSON file key.</returns>
	public static bool IsGeojsonKey(string key) => GeojsonKeys.Contains(key, StringComparer.OrdinalIgnoreCase);

	/// <summary>The key for every file of one kind, e.g. <see cref="Lines"/>.</summary>
	/// <param name="kind">The kind of feature the files hold.</param>
	/// <returns>The file key.</returns>
	public static string KeyFor(CrcFeatureKind kind) => $"Arrivals_{AiracOutputPaths.FileKindSuffix(kind)}";

	/// <summary>
	/// The folder one airport's files of one kind go in, e.g.
	/// <c>…\Geojson\ZLA\LAS</c> (or under <c>Upload_to_vNAS</c> when that kind is marked for vNAS).
	/// </summary>
	/// <param name="settings">The parsed settings.</param>
	/// <param name="airportProcedure">The airport + procedure being written.</param>
	/// <param name="kind">The kind of file.</param>
	/// <returns>The directory.</returns>
	/// <remarks>
	/// Unlike Departures, this reads <see cref="ArrivalAirportProcedure.Artcc"/> - the ARTCC of
	/// this airport's copy of the arrival - not a single procedure-level ARTCC, because a STAR can
	/// be shared by two centres (see <see cref="ArrivalProcedure.ArtccFor"/>).
	/// </remarks>
	internal static string GeojsonDirectory(ArrivalSettings settings, ArrivalAirportProcedure airportProcedure, CrcFeatureKind kind) =>
		Path.Combine(
			AiracOutputPaths.FileDirectory(settings.OutputDirectory, isGeojson: true, settings.Vnas.IsUploaded(KeyFor(kind))),
			airportProcedure.Artcc,
			airportProcedure.AirportId);

	/// <summary>A GeoJSON file name, e.g. <c>LAS_BLAID_STAR_Lines.geojson</c>.</summary>
	/// <param name="airportProcedure">The airport + procedure being written.</param>
	/// <param name="kind">The kind of file.</param>
	/// <returns>The file name.</returns>
	/// <remarks>
	/// <c>STAR</c> marks the file as an arrival. It shares its airport's folder with the Departures
	/// files, and a SID and a STAR can share an identifier at one airport (the FAA publishes ORF's
	/// NUTIY and SWOPE departures in the STAR data too), so without it one would overwrite the
	/// other.
	/// </remarks>
	internal static string GeojsonFileName(ArrivalAirportProcedure airportProcedure, CrcFeatureKind kind) =>
		$"{airportProcedure.AirportId}_{airportProcedure.Procedure.CodeId}_STAR_{AiracOutputPaths.FileKindSuffix(kind)}.geojson";
}
