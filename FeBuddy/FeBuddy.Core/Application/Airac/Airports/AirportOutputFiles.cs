using FeBuddy.Core.Application.Airac.Models;

namespace FeBuddy.Core.Application.Airac.Airports;

/// <summary>
/// The files the Airports sub-service writes, by file key - the name the <c>UploadToVnas</c> and
/// <c>CrcDefaultsFor</c> settings use (see <see cref="VnasFileChoices"/>).
/// </summary>
/// <remarks>A GeoJSON file's key is its name without <c>.geojson</c>; the alias file's is its name.</remarks>
public static class AirportOutputFiles
{
	/// <summary><c>Runways_Lines.geojson</c>: each airport's runway centrelines.</summary>
	public const string RunwaysLines = "Runways_Lines";

	/// <summary><c>Airports_Symbols.geojson</c>: a symbol per airport.</summary>
	public const string AirportsSymbols = "Airports_Symbols";

	/// <summary><c>Airports_Text.geojson</c>: a label per airport.</summary>
	public const string AirportsText = "Airports_Text";

	/// <summary>The alias file.</summary>
	public const string Alias = "Airports.txt";

	/// <summary>Every GeoJSON file key, in the order Lines, Symbols, Text.</summary>
	public static IReadOnlyList<string> GeojsonKeys { get; } = [RunwaysLines, AirportsSymbols, AirportsText];

	/// <summary>Whether a key names one of the Airports GeoJSON files, ignoring case.</summary>
	/// <param name="key">The file key.</param>
	/// <returns><see langword="true"/> for a GeoJSON file key.</returns>
	public static bool IsGeojsonKey(string key) => GeojsonKeys.Contains(key, StringComparer.OrdinalIgnoreCase);
}
