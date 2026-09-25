using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Domain.Crc.Models;
using FeBuddy.Core.Domain.Departures.Models;

namespace FeBuddy.Core.Application.Airac.Departures;

/// <summary>
/// The files the Departures sub-service writes: their file keys - the names the
/// <c>UploadToVnas</c> and <c>CrcDefaultsFor</c> settings use (see <see cref="VnasFileChoices"/>) -
/// and where each one goes.
/// </summary>
/// <remarks>
/// A run writes up to three GeoJSON files per airport + procedure, often thousands in all, so
/// they are chosen by kind: <see cref="Lines"/> covers every procedure's Lines file. Each
/// airport's files go in <c>&lt;ARTCC&gt;\&lt;ARPT&gt;</c> inside the GeoJSON folder (see
/// <see cref="AiracOutputPaths"/>), e.g. <c>Geojson\ZLA\LAX\LAX_DOTSS_Lines.geojson</c>.
/// </remarks>
public static class DepartureOutputFiles
{
	/// <summary>Every procedure's <c>_Lines.geojson</c> file.</summary>
	public const string Lines = "Departures_Lines";

	/// <summary>Every procedure's <c>_Symbols.geojson</c> file.</summary>
	public const string Symbols = "Departures_Symbols";

	/// <summary>Every procedure's <c>_Text.geojson</c> file.</summary>
	public const string Text = "Departures_Text";

	/// <summary>The alias file.</summary>
	public const string Alias = "Departures.txt";

	/// <summary>Every GeoJSON file key, in the order Lines, Symbols, Text.</summary>
	public static IReadOnlyList<string> GeojsonKeys { get; } = [Lines, Symbols, Text];

	/// <summary>Whether a key names one of the Departures kinds of GeoJSON file, ignoring case.</summary>
	/// <param name="key">The file key.</param>
	/// <returns><see langword="true"/> for a GeoJSON file key.</returns>
	public static bool IsGeojsonKey(string key) => GeojsonKeys.Contains(key, StringComparer.OrdinalIgnoreCase);

	/// <summary>The key for every file of one kind, e.g. <see cref="Lines"/>.</summary>
	/// <param name="kind">The kind of feature the files hold.</param>
	/// <returns>The file key.</returns>
	public static string KeyFor(CrcFeatureKind kind) => $"Departures_{AiracOutputPaths.FileKindSuffix(kind)}";

	/// <summary>
	/// The folder one airport's files of one kind go in, e.g.
	/// <c>…\Geojson\ZLA\LAX</c> (or under <c>Upload_to_vNAS</c> when that kind is marked for vNAS).
	/// </summary>
	/// <param name="settings">The parsed settings.</param>
	/// <param name="airportProcedure">The airport + procedure being written.</param>
	/// <param name="kind">The kind of file.</param>
	/// <returns>The directory.</returns>
	internal static string GeojsonDirectory(DepartureSettings settings, DepartureAirportProcedure airportProcedure, CrcFeatureKind kind) =>
		Path.Combine(
			AiracOutputPaths.FileDirectory(settings.OutputDirectory, isGeojson: true, settings.Vnas.IsUploaded(KeyFor(kind))),
			airportProcedure.Procedure.Artcc,
			airportProcedure.AirportId);

	/// <summary>A GeoJSON file name, e.g. <c>LAX_DOTSS_Lines.geojson</c>.</summary>
	/// <param name="airportProcedure">The airport + procedure being written.</param>
	/// <param name="kind">The kind of file.</param>
	/// <returns>The file name.</returns>
	internal static string GeojsonFileName(DepartureAirportProcedure airportProcedure, CrcFeatureKind kind) =>
		$"{airportProcedure.AirportId}_{airportProcedure.Procedure.CodeId}_{AiracOutputPaths.FileKindSuffix(kind)}.geojson";
}
